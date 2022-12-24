namespace Bang.Diagnostics
{
    public static class Assert
    {
        public static void Verify(bool condition, string text)
        {
            if (!condition)
            {
                throw new InvalidOperationException(text);
            }
        }
    }
}
