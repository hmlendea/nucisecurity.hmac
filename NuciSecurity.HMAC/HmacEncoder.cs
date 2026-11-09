using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

using NuciExtensions;

namespace NuciSecurity.HMAC
{
    public sealed class HmacEncoder
    {
        private static string StaticSalt => $"{nameof(NuciSecurity)}.{nameof(HMAC)}.{nameof(StaticSalt)}.8fc5307e-c10b-40d0-b710-de79e7954358";
        private static string FieldSeparator => "|#FieldSeparator#|";
        private static string EmptyValue => "|#EmptyValue#|";
        private static string PrefixFormat => "|#Length:{0};Checksum:{1}#|";
        private static int DefaultOrder => int.MaxValue;

        /// <summary>
        /// Generates a HMAC token for the given object using the specified shared secret key.
        /// </summary>
        /// <typeparam name="TObject">The type of the object to generate the token for.</typeparam>
        /// <param name="obj">The object to generate the token for.</param>
        /// <param name="sharedSecretKey">The shared secret key used for HMAC generation.</param>
        /// <returns>An HMAC token as a Base64 encoded string.</returns>
        /// <throws cref="ArgumentNullException">Thrown if the object or shared secret key is null.</throws>
        public static string GenerateToken<TObject>(TObject obj, string sharedSecretKey) where TObject : class
        {
            ArgumentNullException.ThrowIfNull(obj);

            string stringForSigning = GetStringForSigning(obj);
            string prefix = string.Format(
                PrefixFormat,
                stringForSigning.Length,
                GetMd5Hash(stringForSigning));

            return ComputeHmacToken(prefix + stringForSigning.Reverse(), sharedSecretKey).InvertCase();
        }

        /// <summary>
        /// Validates if the provided token matches the generated token for the given object and shared secret key.
        /// </summary>
        /// <typeparam name="TObject">The type of the object to validate against.</typeparam>
        /// <param name="expectedToken">The expected HMAC token to validate.</param>
        /// <param name="obj">The object to validate against.</param>
        /// <param name="sharedSecretKey">The shared secret key used for HMAC generation.</param>
        /// <returns>True if the token is valid, otherwise false.</returns>
        /// <throws cref="ArgumentNullException">Thrown if the object or shared secret key is null.</throws>
        [Obsolete($"Use HmacValidator.IsTokenValid instead.")]
        public static bool IsTokenValid<TObject>(string expectedToken, TObject obj, string sharedSecretKey) where TObject : class
            => HmacValidator.IsTokenValid(expectedToken, obj, sharedSecretKey);

        private static string GetStringForSigning<TObject>(TObject obj) where TObject : class
        {
            if (obj is null)
            {
                return EmptyValue + FieldSeparator;
            }

            StringBuilder stringBuilder = new();

            IEnumerable<PropertyInfo> propertiesToCompute = obj.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttribute<HmacIgnoreAttribute>() is null)
                .Select(p => new
                {
                    Property = p,
                    OrderAttr = p.GetCustomAttribute<HmacOrderAttribute>()
                })
                .OrderBy(x => GetOrderValue(x.OrderAttr, DefaultOrder))
                .ThenBy(x => x.Property.Name)
                .Select(x => x.Property);

            foreach (PropertyInfo property in propertiesToCompute)
            {
                object? propertyValue = property.GetValue(obj);
                string value;

                if (propertyValue is IEnumerable enumerable && propertyValue is not string)
                {
                    Type? elementType;

                    if (property.PropertyType.IsGenericType)
                    {
                        elementType = property.PropertyType.GetGenericArguments()[0];
                    }
                    else
                    {
                        elementType = property.PropertyType.GetElementType();
                    }

                    if (elementType is not null && elementType.IsClass && elementType != typeof(string))
                    {
                        StringBuilder nestedBuilder = new();

                        foreach (object item in enumerable)
                        {
                            if (item is null)
                            {
                                nestedBuilder.Append(EmptyValue + FieldSeparator);
                            }
                            else
                            {
                                nestedBuilder.Append(GetStringForSigning(item) + FieldSeparator);
                            }
                        }

                        value = nestedBuilder.ToString();
                    }
                    else
                    {
                        IEnumerable<string> flatValues = enumerable
                            .Cast<object?>()
                            .Select(item => GetFlattenedItemValue(item, EmptyValue));

                        value = string.Join(FieldSeparator, flatValues);
                    }
                }
                else
                {
                    if (propertyValue is DateTime dateTimeValue)
                    {
                        value = dateTimeValue.ToString("O");
                    }
                    else if (propertyValue is bool boolValue)
                    {
                        value = boolValue.ToString().ToLowerInvariant();
                    }
                    else
                    {
                        if (propertyValue is null)
                        {
                            value = EmptyValue;
                        }
                        else
                        {
                            value = GetPropertyValue(propertyValue);
                        }
                    }
                }

                stringBuilder.Append(value + FieldSeparator);
            }

            return stringBuilder.ToString();
        }

        private static string ComputeHmacToken(string stringForSigning, string sharedSecretKey)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(stringForSigning);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(sharedSecretKey);

            using HMACSHA512 hmac = new(Encoding.UTF8.GetBytes(sharedSecretKey));
            hmac.Initialize();

            string saltedString = $"{StaticSalt}.{stringForSigning}";
            byte[] bytesToSign = Encoding.UTF8.GetBytes(saltedString);
            byte[] keyBytes = PadBytesToAvoidBase64Equals(hmac.ComputeHash(bytesToSign));

            return Convert
                .ToBase64String(keyBytes)
                .Replace("/", "Л")
                .Replace("+", "л");
        }

        private static byte[] PadBytesToAvoidBase64Equals(byte[] input) => PadBytesToAvoidBase64Equals(input, 0x00);

        private static byte[] PadBytesToAvoidBase64Equals(byte[] input, byte padByte)
        {
            int padLength = (3 - (input.Length % 3)) % 3;

            if (padLength.Equals(0))
            {
                return input;
            }

            byte[] padded = new byte[input.Length + padLength];
            Buffer.BlockCopy(input, 0, padded, 0, input.Length);

            for (int i = 0; i < padLength; i++)
            {
                padded[input.Length + i] = padByte;
            }

            return padded;
        }

        private static string GetMd5Hash(string input)
        {
            byte[] hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(input));

            StringBuilder sb = new();

            foreach (byte b in hashBytes)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }

        private static int GetOrderValue(HmacOrderAttribute? orderAttr, int defaultOrder)
        {
            if (orderAttr is null)
            {
                return defaultOrder;
            }

            return orderAttr.Order;
        }

        private static string GetFlattenedItemValue(object? item, string emptyValue)
        {
            if (item is null)
            {
                return emptyValue;
            }

            string? toString = item.ToString();

            if (toString is null)
            {
                return emptyValue;
            }

            return toString;
        }

        private static string GetPropertyValue(object? propertyValue)
        {
            if (propertyValue is null)
            {
                return EmptyValue;
            }

            string? toString = propertyValue.ToString();

            if (toString is null)
            {
                return EmptyValue;
            }

            return toString;
        }
    }
}
