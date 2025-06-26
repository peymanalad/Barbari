namespace BarcopoloWebApi.Extensions
{
    public static class StringExtensions
    {
        public static string NormalizePersian(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            return input.Trim()
                .Replace("ي", "ی")
                .Replace("ك", "ک")
                .Replace("ة", "ه")
                .Replace("ؤ", "و")
                .Replace("إ", "ا")
                .Replace("أ", "ا")
                .Replace("‌", " "); // حذف نیم‌فاصله
        }
    }
}