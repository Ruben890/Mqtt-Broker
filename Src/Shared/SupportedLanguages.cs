using System.Globalization;
using System.Resources;

namespace Shared
{
    public static class SupportedLanguages
    {
        private static readonly ResourceManager _resourceManager =
                        new ResourceManager("Shared.Resources.Resources", typeof(SupportedLanguages).Assembly);

        public static string GetMessage(string key) =>
                        _resourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;

        public const string Es = "es";
        public const string En = "en";

        public static readonly string[] SupportedCultures = new[] { En, Es };
        public static readonly CultureInfo[] SupportedCultureInfos = Array.ConvertAll(SupportedCultures, c => new CultureInfo(c));
    }
}
