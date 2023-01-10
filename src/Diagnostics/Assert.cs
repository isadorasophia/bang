namespace Bang.Diagnostics
{
    public static class Assert
    {
        /*
         * Hi! 
         * (╯°□°)╯︵ ┻━┻
         * I am so sorry that you hit this spot.
         * You can check the call stack to see what of unexpected happened.
         * ┬─┬ノ( º _ ºノ)
         * */
        public static void Verify(bool condition, string text)
        {
            if (!condition)
            {
                throw new InvalidOperationException(text);
            }
        }
    }
}
