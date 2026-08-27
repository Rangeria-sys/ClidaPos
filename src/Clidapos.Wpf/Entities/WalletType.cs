namespace Clidapos.Wpf.Entities
{
    /// <summary>A simple named wallet type (e.g. "Cash", "M-Pesa", "Bank") - this is
    /// purely a category list, matching UnitMaster/RMCategory/WarehouseType's pattern.
    /// There is no balance tracking here - the real table has only this one column.</summary>
    public class WalletType
    {
        public string Type { get; set; } = "";
    }
}