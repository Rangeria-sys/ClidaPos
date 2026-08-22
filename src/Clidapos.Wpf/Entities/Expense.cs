namespace Clidapos.Wpf.Entities
{
    /// <summary>Maps to the Expense table - a master list of named expense items, each tagged with a type.</summary>
    public class Expense
    {
        public string ExpenseName { get; set; } = "";
        public string ExpenseType { get; set; } = "";
    }
}