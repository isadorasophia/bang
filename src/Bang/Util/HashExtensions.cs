using System;

namespace Bang.Util
{
    internal static class HashExtensions
    {
        /// <summary>
        /// Algorithm taken from: https://stackoverflow.com/a/3405330.
        /// </summary>
        /// <param name="values">Values that will be applied the hash algorithm.</param>
        public static int GetHashCodeImpl(this List<int> values)
        {
            int hc = values.Count;
            foreach (int val in values)
            {
                hc = unchecked(hc * 314159 + val);
            }

            return hc;
        }

        /// <summary>
        /// Algorithm taken from: https://stackoverflow.com/a/3405330.
        /// </summary>
        /// <param name="values">Values that will be applied the hash algorithm.</param>
        public static int GetHashCodeImpl(this Span<int> values)
        {
            int hc = values.Length;
            foreach (int val in values)
            {
                hc = unchecked(hc * 314159 + val);
            }

            return hc;
        }

        public static int GetHashCode(int a, int b)
        {
            int hash = 23;

            hash = hash * 31 + a;
            hash = hash * 31 + b;

            return hash;
        }
    }
}