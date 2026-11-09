using System;

namespace NuciSecurity.HMAC
{
    /// <summary>
    /// Attribute to specify the order of properties when generating HMAC tokens.
    /// </summary>
    /// <param name="order">The order in which the property should be processed.</param>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class HmacOrderAttribute(int order) : Attribute
    {
        public int Order { get; } = order;
    }
}
