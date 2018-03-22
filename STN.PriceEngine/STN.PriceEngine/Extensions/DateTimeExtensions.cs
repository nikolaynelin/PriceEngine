using System;

namespace STN.PriceEngine.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime GetDate(this DateTime dt)
        {
            if (dt == null)
                throw new ArgumentNullException(nameof(dt));

            return new DateTime(dt.Year, dt.Month, dt.Day);
        }
    }
}
