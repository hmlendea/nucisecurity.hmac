using System;

namespace NuciSecurity.HMAC
{
    /// <summary>
    /// Attribute to mark properties that should be ignored when generating HMAC tokens.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class HmacIgnoreAttribute : Attribute { }
}
