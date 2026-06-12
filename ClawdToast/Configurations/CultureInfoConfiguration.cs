using System.Globalization;

namespace ClawdToast.Configurations;

internal static class CultureInfoConfiguration
{
    public static void Initialize()
    {
        var customCulture = (CultureInfo)CultureInfo.CurrentCulture.Clone();

        customCulture.DateTimeFormat.ShortDatePattern = "yyyy/MM/dd";
        customCulture.DateTimeFormat.LongTimePattern = "HH:mm:ss.fff";

        CultureInfo.CurrentCulture = customCulture;
        CultureInfo.CurrentUICulture = customCulture;

        CultureInfo.DefaultThreadCurrentCulture = customCulture;
        CultureInfo.DefaultThreadCurrentUICulture = customCulture;
    }
}
