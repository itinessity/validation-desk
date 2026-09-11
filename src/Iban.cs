using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Concurrent;

static class Iban
{
    static readonly Dictionary<string, int> Lengths = new()
    {
        ["AT"] = 20,
        ["BE"] = 16,
        ["BG"] = 22,
        ["CH"] = 21,
        ["CY"] = 28,
        ["CZ"] = 24,
        ["DE"] = 22,
        ["DK"] = 18,
        ["EE"] = 20,
        ["ES"] = 24,
        ["FI"] = 18,
        ["FR"] = 27,
        ["GB"] = 22,
        ["GR"] = 27,
        ["HR"] = 21,
        ["HU"] = 28,
        ["IE"] = 22,
        ["IS"] = 26,
        ["IT"] = 27,
        ["LI"] = 21,
        ["LT"] = 20,
        ["LU"] = 20,
        ["LV"] = 21,
        ["MT"] = 31,
        ["NL"] = 18,
        ["NO"] = 15,
        ["PL"] = 28,
        ["PT"] = 25,
        ["RO"] = 24,
        ["SE"] = 24,
        ["SI"] = 19,
        ["SK"] = 24
    };
    public static bool Valid(string text)
    {
        if (!Regex.IsMatch(text, "^[A-Z]{2}[0-9]{2}[A-Z0-9]+$") || !Lengths.TryGetValue(text[..2], out var len) || text.Length != len)
            return false;
        int remainder = 0;
        foreach (var c in text[4..] + text[..4])
        {
            if (char.IsAsciiDigit(c))
                remainder = (remainder * 10 + c - '0') % 97;
            else
                remainder = (remainder * 100 + c - 'A' + 10) % 97;
        }

        return remainder == 1;
    }
}
