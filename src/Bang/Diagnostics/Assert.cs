namespace Bang.Diagnostics
{
    /// <summary>
    /// Helper class for asserting and throwing exceptions on unexpected scenarios.
    /// </summary>
    public static class Assert
    {
        /// <summary>
        /// Verify whether a condition is valid. If not, throw a <see cref="InvalidOperationException"/>.
        /// This throws regardless if it's on release on debug binaries.
        /// </summary>
        public static void Verify(bool condition, string text)
        {
            /*
             * Hi! 
             * (╯°□°)╯︵ ┻━┻
             * I am so sorry that you hit this spot.
             * You can check the call stack to see what of unexpected happened.
             * ┬─┬ノ( º _ ºノ)
             * */
            if (!condition)
            {
                throw new InvalidOperationException(text);
            }
        }
    }
}