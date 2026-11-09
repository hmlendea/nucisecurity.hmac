namespace NuciSecurity.HMAC.UnitTests.Helpers
{
    public sealed class ObjectWithOrderAttributes
    {
        [HmacOrder(1)]
        public string? Property1 { get; set; }

        [HmacOrder(2)]
        public string? Property2 { get; set; }
    }
}
