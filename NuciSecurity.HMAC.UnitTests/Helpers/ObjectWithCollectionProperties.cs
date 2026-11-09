using System.Collections.Generic;

namespace NuciSecurity.HMAC.UnitTests.Helpers
{
    public sealed class ObjectWithCollectionProperties
    {
        public List<ObjectWithIgnoreAttributes>? Collection { get; set; }

        public string? Text { get; set; }
    }
}
