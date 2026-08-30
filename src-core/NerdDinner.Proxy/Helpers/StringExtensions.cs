namespace NerdDinner.Proxy.Helpers
{
    public static class StringExtensions
    {
        public static string Truncate(this string s, int maxLength)
        {
            if (string.IsNullOrEmpty(s) || maxLength <= 0)
                return string.Empty;
            else if (s.Length > maxLength)
                return s.Substring(0, maxLength) + "...";
            else
                return s;
        }
    }
}
