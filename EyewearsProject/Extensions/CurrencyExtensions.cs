using System.Globalization;

namespace EyewearsProject.Extensions
{
    public static class CurrencyExtensions
    {
        private static readonly CultureInfo PkrCulture = new CultureInfo("ur-PK");

        public static string ToPkr(this decimal amount)
        {
            return "Rs " + amount.ToString("N0", PkrCulture);
        }
    }
}