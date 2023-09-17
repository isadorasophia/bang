namespace Bang.Util
{
    internal static class HashExtensions
    {
        /// <summary>
        /// Algorithm taken from: https://stackoverflow.com/a/3405330.
        /// This is a reasonable algorithm for array of values between -1000 and 1000. We can replace this afterwards if there is a need.
        /// </summary>
        /// <param name="values">Values that will be applied the hash algorithm.</param>
        public static int GetHashCodeImpl(this IEnumerable<int> values)
        {
            int result = 0;
            int shift = 0;

            foreach (int v in values)
            {
                shift = (shift + 11) % 21;
                result ^= (v + 1024) << shift;
            }

            return result;
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