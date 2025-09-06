public static class CurrencyFormatter
{
    public static string ToRial(this decimal amount)
    {
        return string.Format("{0:N0} ریال", amount);
    }
    public static string ToPrice(this decimal amount, string suffix = "ریال")
    {
        return $"{amount:N0} {suffix}";
    }
}