namespace Clidapos.Wpf.Entities
{
    /// <summary>Maps to the ExpenseType table - a single-column lookup, same shape as RMCategory/UnitMaster.</summary>
    public class ExpenseType
    {
        public string Type { get; set; } = "";
    }
}