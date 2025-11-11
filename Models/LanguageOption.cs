using System;

namespace CN_GreenLumaGUI.Models
{
    public class LanguageOption
    {
        public LanguageOption(string code, string displayName)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        }

        public string Code { get; }

        public string DisplayName { get; }

        public override string ToString() => DisplayName;
    }
}
