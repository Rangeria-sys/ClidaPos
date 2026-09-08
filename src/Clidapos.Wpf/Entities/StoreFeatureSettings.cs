namespace Clidapos.Wpf.Entities
{
    /// <summary>Per-shop feature toggles, editable live from Back Office - no
    /// restart needed, unlike appsettings.json values. A singleton, one row.</summary>
    public class StoreFeatureSettings
    {
        public int Id { get; set; }
        public string EnableMpesaSTKPush { get; set; } = "Y";
        public string EnableLoyaltyProgram { get; set; } = "Y";
        public string EnableBankPayment { get; set; } = "Y";
        public string EnableCreditPayment { get; set; } = "Y";
    }
}
