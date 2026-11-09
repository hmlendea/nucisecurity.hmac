namespace NuciSecurity.HMAC.UnitTests.Helpers
{
    public sealed class ObjectWithIgnoreAttributes
    {
        public string? UsedProperty { get; set; }

        [HmacIgnore]
        public string? IgnoredProperty { get; set; }
    }
}
