using System.Text;

namespace WisperKemal.Core;

/// <summary>One allowlist for recognition and final delivery. Never transliterates unknown words.</summary>
public static class LatinYazi
{
    // UTF-8 lead bytes: Turkish letters use C3/C4/C5; lira, quotes and ellipsis use E2.
    public const string SuppressRegex = @"[\s\S]*[\xCE\xCF\xD0-\xDF\xE0\xE1\xE3-\xED\xEE\xEF\xF0-\xF4][\s\S]*";

    private static bool Allowed(int c) => c is >= 0x20 and <= 0x7e or 0x09 or 0x0a or 0x0d
        or >= 0xa0 and <= 0x24f or >= 0x2010 and <= 0x2027 or >= 0x2030 and <= 0x205e
        or >= 0x2070 and <= 0x209f or >= 0x20a0 and <= 0x20cf or >= 0x2100 and <= 0x22ff;
    private static bool LatinLetter(Rune r) => r.Value <= 0x24f && Allowed(r.Value) && Rune.IsLetter(r);

    public static bool HasForbiddenLetter(string text) => text.EnumerateRunes().Any(r => Rune.IsLetter(r) && !LatinLetter(r));

    public static bool TamLatinDisi(string text)
    {
        bool hasLetter = false;
        foreach (Rune rune in text.EnumerateRunes())
        {
            if (!Rune.IsLetter(rune)) continue;
            if (LatinLetter(rune)) return false;
            hasLetter = true;
        }
        return hasLetter;
    }

    public sealed record Result(string Text, int DroppedWords, int RemovedSigns);

    public static Result Clean(string text, bool dropForbiddenWords = true)
    {
        var output = new StringBuilder(text.Length);
        int dropped = 0, signs = 0;
        bool changed = false;
        // A word is a whitespace-delimited token: mixed-script words are removed in their entirety.
        for (int at = 0; at < text.Length;)
        {
            if (char.IsWhiteSpace(text[at]))
            {
                if (Allowed(text[at])) output.Append(text[at]);
                else { signs++; changed = true; }
                at++;
                continue;
            }
            int start = at;
            while (at < text.Length && !char.IsWhiteSpace(text[at])) at++;
            string token = text[start..at];
            if (dropForbiddenWords && HasForbiddenLetter(token)) { dropped++; changed = true; continue; }
            bool latin = false;
            foreach (Rune rune in token.EnumerateRunes())
            {
                int c = rune.Value;
                bool accent = c is >= 0x300 and <= 0x36f;
                if ((Allowed(c) && (!Rune.IsLetter(rune) || LatinLetter(rune))) || (accent && latin))
                {
                    output.Append(rune.ToString());
                    if (!accent) latin = LatinLetter(rune);
                }
                else { signs++; changed = true; latin = false; }
            }
        }
        if (!changed) return new(text, 0, 0);
        // Only repair spacing when something was removed; clean dictation is byte-for-byte unchanged.
        string result = output.ToString();
        while (result.Contains("  ", StringComparison.Ordinal)) result = result.Replace("  ", " ", StringComparison.Ordinal);
        return new(result.Trim(), dropped, signs);
    }
}

/// <summary>Only counts cross the worker boundary; no recognized characters or text.</summary>
public sealed record AlfabeEvent(string asama, bool yeniden_cozum, int atilan_kelime, int silinen_isaret);

/// <summary>Shared, fakeable decode policy used by all Whisper paths.</summary>
public static class LatinDecode
{
    public static async Task<string> Run(Func<bool, CancellationToken, Task<string>> decode,
        Action<AlfabeEvent>? log = null, bool force = false,
        Action<double, double, bool>? measure = null, CancellationToken ct = default, bool singlePiece = false)
    {
        long start = force ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
        string normal = await decode(false, ct).ConfigureAwait(false);
        double normalMs = force ? System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds : 0;
        bool retry = LatinYazi.HasForbiddenLetter(normal);
        bool tamLatinDisi = singlePiece && LatinYazi.TamLatinDisi(normal);
        string selected = normal;
        if (retry || force)
        {
            ct.ThrowIfCancellationRequested();
            start = force ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
            string suppressed = await decode(true, ct).ConfigureAwait(false);
            double suppressedMs = force ? System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds : 0;
            if (force) measure?.Invoke(normalMs, suppressedMs, string.Equals(normal, suppressed, StringComparison.Ordinal));
            if (retry) selected = suppressed;
        }
        LatinYazi.Result clean = LatinYazi.Clean(selected);
        if (tamLatinDisi)
            log?.Invoke(new("tam_latin_disi", true, clean.DroppedWords, clean.RemovedSigns));
        if (retry || clean.DroppedWords > 0 || clean.RemovedSigns > 0)
            log?.Invoke(new("motor", retry, clean.DroppedWords, clean.RemovedSigns));
        return clean.Text;
    }
}
