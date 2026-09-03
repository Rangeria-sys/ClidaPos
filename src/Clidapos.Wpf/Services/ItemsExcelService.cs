using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    public class ProductImportResult
    {
        public bool Cancelled { get; set; }
        public int Added { get; set; }
        public int Updated { get; set; }
        public List<string> Errors { get; set; } = new();
        public bool HasErrors => Errors.Count > 0;
    }

    /// <summary>
    /// Round-trip Excel for the Items list. Export writes every product into a
    /// spreadsheet whose header row IS the import layout - the same file, edited
    /// (or added to) and saved, is exactly what Import reads back in. A row with
    /// an ID matching an existing item updates that item; a row with no ID (or
    /// one that doesn't match anything) creates a new item.
    /// </summary>
    public class ItemsExcelService
    {
        private static readonly string[] Headers =
        {
            "ID (leave blank for new item)", "Product Code", "Product Name*", "Category*",
            "Unit*", "Selling Price*", "Buying Price", "Quantity", "Reorder Point", "Supplier"
        };

        private readonly UnitService _unitService = new();
        private readonly CategoryService _categoryService = new();
        private readonly PurchaseService _purchaseService = new();
        private readonly LogService _logService = new();
        private readonly WarehouseService _warehouseService = new();

        /// <summary>Writes every product (with current stock qty and last buying price) to a new .xlsx and opens it. Returns the saved path, or null if cancelled.</summary>
        public string? Export(List<Product> products, Dictionary<int, decimal> quantities, Dictionary<int, decimal> buyingPrices)
        {
            var dlg = new SaveFileDialog
            {
                FileName = $"Clidapos_Items_{DateTime.Now:yyyyMMdd_HHmm}",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                AddExtension = true
            };
            if (dlg.ShowDialog() != true) return null;

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Items");

            for (var c = 0; c < Headers.Length; c++)
            {
                var cell = ws.Cell(1, c + 1);
                cell.Value = Headers[c];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E1E24");
                cell.Style.Font.FontColor = XLColor.FromHtml("#F3A123");
            }

            var r = 2;
            foreach (var p in products.OrderBy(x => x.ProductName.Trim()))
            {
                ws.Cell(r, 1).Value = p.PID;
                ws.Cell(r, 2).Value = p.ProductCode.Trim();
                ws.Cell(r, 3).Value = p.ProductName.Trim();
                ws.Cell(r, 4).Value = p.Category?.Trim() ?? "";
                ws.Cell(r, 5).Value = p.Unit?.Trim() ?? "";
                ws.Cell(r, 6).Value = p.Price;
                ws.Cell(r, 7).Value = buyingPrices.TryGetValue(p.PID, out var bp) ? bp : 0;
                ws.Cell(r, 8).Value = quantities.TryGetValue(p.PID, out var qty) ? qty : 0;
                ws.Cell(r, 9).Value = p.ReorderPoint;
                ws.Cell(r, 10).Value = p.P_Supplier?.Trim() ?? "";
                r++;
            }

            ws.Range(1, 1, 1, Headers.Length).SetAutoFilter();
            ws.SheetView.FreezeRows(1);
            ws.Columns().AdjustToContents();
            ws.Column(1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left; // keep ID readable, not squeezed by the long header

            try
            {
                wb.SaveAs(dlg.FileName);
            }
            catch (IOException)
            {
                System.Windows.MessageBox.Show(
                    "Could not save - a file with that name looks like it's still open in Excel. " +
                    "Close it, then try Export Excel again.",
                    "Clidapos");
                return null;
            }

            TryOpen(dlg.FileName);
            return dlg.FileName;
        }

        /// <summary>Prompts for an .xlsx file in the Export layout and upserts every valid row against the Products table. Invalid rows are skipped and reported rather than stopping the whole import.</summary>
        public async Task<ProductImportResult> ImportAsync(string userId)
        {
            var dlg = new OpenFileDialog { Filter = "Excel Workbook (*.xlsx)|*.xlsx" };
            if (dlg.ShowDialog() != true) return new ProductImportResult { Cancelled = true };

            var result = new ProductImportResult();

            XLWorkbook wb;
            try
            {
                wb = new XLWorkbook(dlg.FileName);
            }
            catch (IOException)
            {
                result.Errors.Add(
                    "Could not open the file - it looks like it's still open in Excel. " +
                    "Close it in Excel, then try Import Excel again.");
                return result;
            }
            using var _ = wb;

            var ws = wb.Worksheets.First();
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
            if (lastRow < 2) return result; // header only - nothing to import

            await _warehouseService.EnsureDefaultWarehouseAsync();

            using var db = new ClidaposDbContext();

            var existingProducts = await db.Products.ToListAsync();
            var byId = existingProducts.ToDictionary(p => p.PID);
            var codeOwners = existingProducts
                .Where(p => p.ProductCode.Trim().Length > 0)
                .ToDictionary(p => p.ProductCode.Trim().ToUpperInvariant(), p => p.PID);
            var nextId = (existingProducts.Count > 0 ? existingProducts.Max(p => p.PID) : 0) + 1;

            // Make sure every category/unit named in the sheet exists before touching
            // any product row, so EnsureExistsAsync isn't called once per row.
            var distinctCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var distinctUnits = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var r = 2; r <= lastRow; r++)
            {
                var row = ws.Row(r);
                if (row.IsEmpty()) continue;

                var category = row.Cell(4).GetString().Trim();
                var unit = row.Cell(5).GetString().Trim();
                if (category.Length > 0) distinctCategories.Add(category);
                if (unit.Length > 0) distinctUnits.Add(unit);
            }
            foreach (var category in distinctCategories) await _categoryService.EnsureExistsAsync(category);
            foreach (var unit in distinctUnits) await _unitService.EnsureExistsAsync(unit);

            for (var r = 2; r <= lastRow; r++)
            {
                var row = ws.Row(r);
                if (row.IsEmpty()) continue;

                try
                {
                    var idText = row.Cell(1).GetString().Trim();
                    var code = row.Cell(2).GetString().Trim();
                    var name = row.Cell(3).GetString().Trim();
                    var category = row.Cell(4).GetString().Trim();
                    var unit = row.Cell(5).GetString().Trim();
                    var priceText = row.Cell(6).GetString().Trim();
                    var buyingPriceText = row.Cell(7).GetString().Trim();
                    var qtyText = row.Cell(8).GetString().Trim();
                    var reorderText = row.Cell(9).GetString().Trim();
                    var supplier = row.Cell(10).GetString().Trim();

                    if (name.Length == 0)
                    {
                        result.Errors.Add($"Row {r}: Product Name is required - skipped.");
                        continue;
                    }
                    if (category.Length == 0)
                    {
                        result.Errors.Add($"Row {r}: Category is required - skipped.");
                        continue;
                    }
                    if (unit.Length == 0)
                    {
                        result.Errors.Add($"Row {r}: Unit is required - skipped.");
                        continue;
                    }
                    if (!decimal.TryParse(priceText, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) || price <= 0)
                    {
                        result.Errors.Add($"Row {r}: Selling Price must be a valid number above zero - skipped.");
                        continue;
                    }

                    decimal? buyingPrice = null;
                    if (buyingPriceText.Length > 0)
                    {
                        if (!decimal.TryParse(buyingPriceText, NumberStyles.Any, CultureInfo.InvariantCulture, out var bp))
                        {
                            result.Errors.Add($"Row {r}: Buying Price must be a valid number - skipped.");
                            continue;
                        }
                        buyingPrice = bp;
                    }
                    if (buyingPrice is > 0 && price <= buyingPrice.Value)
                    {
                        result.Errors.Add($"Row {r}: Selling Price must be higher than Buying Price - skipped.");
                        continue;
                    }

                    if (!decimal.TryParse(qtyText, NumberStyles.Any, CultureInfo.InvariantCulture, out var qty))
                        qty = 0;
                    if (!int.TryParse(reorderText, out var reorderPoint))
                        reorderPoint = 0;

                    int? existingId = null;
                    if (idText.Length > 0 && int.TryParse(idText, out var idVal) && byId.ContainsKey(idVal))
                        existingId = idVal;

                    var codeKey = code.ToUpperInvariant();
                    if (code.Length > 0 && codeOwners.TryGetValue(codeKey, out var ownerId) && ownerId != existingId)
                    {
                        result.Errors.Add($"Row {r}: Product Code '{code}' is already used by another item - skipped.");
                        continue;
                    }

                    Product product;
                    var isNew = existingId == null;

                    if (isNew)
                    {
                        var pid = nextId++;
                        product = new Product
                        {
                            PID = pid,
                            ProductCode = code.Length > 0 ? code : $"ITM-{pid}",
                            ProductName = name,
                            Category = category,
                            Unit = unit,
                            Price = price,
                            ReorderPoint = reorderPoint,
                            P_Supplier = supplier
                        };
                        db.Products.Add(product);
                        byId[pid] = product;
                        if (code.Length > 0) codeOwners[codeKey] = pid;
                        result.Added++;
                    }
                    else
                    {
                        product = byId[existingId!.Value];
                        product.ProductCode = code.Length > 0 ? code : product.ProductCode;
                        product.ProductName = name;
                        product.Category = category;
                        product.Unit = unit;
                        product.Price = price;
                        product.ReorderPoint = reorderPoint;
                        product.P_Supplier = supplier;
                        if (code.Length > 0) codeOwners[codeKey] = product.PID;
                        result.Updated++;
                    }

                    var stockRow = await db.ProductOpeningStocks
                        .FirstOrDefaultAsync(s => s.ProductID == product.PID && s.Warehouse == WarehouseService.DefaultWarehouseName);
                    if (stockRow != null)
                        stockRow.Qty = qty;
                    else
                        db.ProductOpeningStocks.Add(new ProductOpeningStock
                        {
                            ProductID = product.PID,
                            Warehouse = WarehouseService.DefaultWarehouseName,
                            Qty = qty,
                            HasExpiryDate = "N"
                        });

                    await db.SaveChangesAsync();

                    if (buyingPrice is > 0)
                        await _purchaseService.RecordBuyingPriceAsync(product.PID, qty, buyingPrice.Value);
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Row {r}: {ex.InnerException?.Message ?? ex.Message}");
                }
            }

            if (result.Added > 0 || result.Updated > 0)
            {
                await _logService.LogAsync(userId,
                    $"Imported items from Excel: {result.Added} added, {result.Updated} updated" +
                    (result.Errors.Count > 0 ? $", {result.Errors.Count} skipped" : ""));
            }

            return result;
        }

        private static void TryOpen(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch
            {
                // Non-fatal - the file is saved either way, just couldn't auto-open it.
            }
        }
    }
}
