public static class CurrencyFormatter
{
    public static string ToToman(this decimal amount)
    {
        return string.Format("{0:N0} تومان", amount);
    }

    public static string ToRial(this decimal amount)
    {
        return string.Format("{0:N0} ریال", amount);
    }

    public static string ToToman(this int amount)
    {
        return string.Format("{0:N0} تومان", amount);
    }

    public static string ToPrice(this decimal amount, string suffix = "تومان")
    {
        return $"{amount:N0} {suffix}";
    }
}