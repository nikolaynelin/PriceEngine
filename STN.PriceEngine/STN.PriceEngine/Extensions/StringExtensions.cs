using System;

namespace STN.PriceEngine.Extensions
{
    public static class StringExtensions
    {
        public static string RemoveEndZerosAfterDot(this string text)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentException(nameof(text));

            if (!text.Contains(".") || !text.EndsWith("0"))
                return text;

            while (text.EndsWith("0"))
            {
                text = text.Remove(text.Length - 1);
            }

            return text;
        }
    }
}
