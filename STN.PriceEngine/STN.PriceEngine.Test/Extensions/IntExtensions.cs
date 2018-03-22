using System;

namespace STN.PriceEngine.Test.Extensions
{
    public static class IntExtensions
    {
        public static void Times(this int times, Action<int> action)
        {
            for (var i = 0; i < times; i++)
            {
                action(i);
            }
        }

        public static void Times(this int times, Action action)
        {
            for (var i = 0; i < times; i++)
            {
                action();
            }
        }
    }
}
