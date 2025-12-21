using CN_GreenLumaGUI.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace CN_GreenLumaGUI.tools
{
    public static class LocalizationService
    {
        public const string DefaultLanguageCode = "en-US";

        private static readonly Dictionary<string, Uri> LanguageDictionaryMap = new()
        {
            { "en-US", new Uri("Languages/Strings.en-US.xaml", UriKind.Relative) },
            { "zh-CN", new Uri("Languages/Strings.zh-CN.xaml", UriKind.Relative) },
            { "zh-TW", new Uri("Languages/Strings.zh-TW.xaml", UriKind.Relative) }
        };

        private static readonly HashSet<string> LanguageResourcePaths = new()
        {
            "Languages/Strings.en-US.xaml",
            "Languages/Strings.zh-CN.xaml",
            "Languages/Strings.zh-TW.xaml"
        };

        private static readonly List<LanguageOption> SupportedLanguageOptions = new()
        {
            new LanguageOption("zh-CN", "简体中文"),
            new LanguageOption("zh-TW", "繁體中文"),
            new LanguageOption("en-US", "English"),
        };

        public static IReadOnlyList<LanguageOption> SupportedLanguages => SupportedLanguageOptions;

        public static string CurrentLanguageCode { get; private set; } = DefaultLanguageCode;

        public static void ApplyLanguage(string? languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode) || !LanguageDictionaryMap.ContainsKey(languageCode))
            {
                languageCode = DefaultLanguageCode;
            }

            if (Application.Current is null)
            {
                CurrentLanguageCode = languageCode;
                return;
            }

            if (CurrentLanguageCode == languageCode && HasLanguageDictionary())
            {
                return;
            }

            CurrentLanguageCode = languageCode;

            try
            {
                var mergedDictionaries = Application.Current.Resources.MergedDictionaries;

                // 使用精確匹配移除已知的語言資源字典
                for (int i = mergedDictionaries.Count - 1; i >= 0; i--)
                {
                    var source = mergedDictionaries[i].Source;
                    if (source is not null && LanguageResourcePaths.Any(path =>
                        source.OriginalString.Equals(path, StringComparison.OrdinalIgnoreCase)))
                    {
                        mergedDictionaries.RemoveAt(i);
                    }
                }

                var dictionary = new ResourceDictionary { Source = LanguageDictionaryMap[languageCode] };
                mergedDictionaries.Add(dictionary);
            }
            catch (Exception ex)
            {
                OutAPI.PrintLog($"Failed to apply language '{languageCode}': {ex.Message}");

                // 如果不是預設語言且載入失敗，嘗試回退到預設語言
                if (languageCode != DefaultLanguageCode)
                {
                    CurrentLanguageCode = DefaultLanguageCode;
                    try
                    {
                        var dictionary = new ResourceDictionary { Source = LanguageDictionaryMap[DefaultLanguageCode] };
                        Application.Current.Resources.MergedDictionaries.Add(dictionary);
                    }
                    catch (Exception fallbackEx)
                    {
                        OutAPI.PrintLog($"Failed to fallback to default language: {fallbackEx.Message}");
                    }
                }
            }
        }

        public static string GetSteamAcceptLanguageCode(string languageCode)
        {
            return languageCode switch
            {
                "en-US" => "en-us",
                "zh-TW" => "zh-tw",
                "zh-CN" => "zh-cn",
                _ => "en-us"
            };
        }

        private static bool HasLanguageDictionary()
        {
            if (Application.Current is null)
            {
                return false;
            }

            foreach (var dictionary in Application.Current.Resources.MergedDictionaries)
            {
                var source = dictionary.Source;
                if (source is not null && LanguageResourcePaths.Any(path =>
                    source.OriginalString.Equals(path, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            return false;
        }

        public static string GetString(string key)
        {
            if (Application.Current?.TryFindResource(key) is string value)
            {
                return value;
            }

            return key;
        }

        public static LanguageOption GetLanguageOption(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                code = DefaultLanguageCode;
            }

            return SupportedLanguageOptions.FirstOrDefault(option => option.Code == code)
                   ?? SupportedLanguageOptions.First();
        }

        /// <summary>
        /// 根據 Windows 系統地區自動檢測應使用的語言代碼
        /// </summary>
        /// <returns>語言代碼：zh-CN (中國)、zh-TW (台灣/香港) 或 en-US (其他)</returns>
        public static string GetSystemLanguageCode()
        {
            try
            {
                // 獲取系統地區資訊
                var regionInfo = RegionInfo.CurrentRegion;
                var twoLetterRegion = regionInfo.TwoLetterISORegionName.ToUpperInvariant();

                // 根據地區代碼返回對應語言
                switch (twoLetterRegion)
                {
                    case "CN": // 中國
                        return "zh-CN";

                    case "TW": // 台灣
                    case "HK": // 香港
                    case "MO": // 澳門
                        return "zh-TW";

                    default: // 其他地區
                        // 如果 UI 語言是中文，但地區不是中國/台灣/香港，則判斷是簡體還是繁體
                        var cultureInfo = CultureInfo.CurrentUICulture;
                        if (cultureInfo.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
                        {
                            // zh-Hans 或包含 CN 的使用簡體
                            if (cultureInfo.Name.Contains("Hans", StringComparison.OrdinalIgnoreCase) ||
                                cultureInfo.Name.Contains("CN", StringComparison.OrdinalIgnoreCase))
                            {
                                return "zh-CN";
                            }
                            // zh-Hant 或包含 TW/HK 的使用繁體
                            if (cultureInfo.Name.Contains("Hant", StringComparison.OrdinalIgnoreCase) ||
                                cultureInfo.Name.Contains("TW", StringComparison.OrdinalIgnoreCase) ||
                                cultureInfo.Name.Contains("HK", StringComparison.OrdinalIgnoreCase))
                            {
                                return "zh-TW";
                            }
                        }

                        return "en-US";
                }
            }
            catch (Exception ex)
            {
                // 記錄錯誤並返回預設語言
                OutAPI.PrintLog($"Failed to detect system language: {ex.Message}");
                return DefaultLanguageCode;
            }
        }
    }
}
