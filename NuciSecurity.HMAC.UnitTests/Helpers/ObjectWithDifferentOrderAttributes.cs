namespace NuciSecurity.HMAC.UnitTests.Helpers
{
    public sealed class ObjectWithDifferentOrderAttributes
    {
        [HmacOrder(2)]
        public string? Property1 { get; set; }

        [HmacOrder(1)]
        public string? Property2 { get; set; }
    }
}
