using System.Text.RegularExpressions;

namespace WisperKemal.Core;

internal sealed record MeaningPreservationOutcome(
    bool IsSafe,
    IReadOnlyList<string> RiskCodes);

internal static partial class MeaningPreservationGate
{
    // Runtime policy can grow without making a previously valid schema-v1 personal
    // dictionary unreadable. Evaluate remains the persisted dictionary's validation
    // contract; every pipeline transformation must use EvaluateRuntime instead.
    internal static MeaningPreservationOutcome EvaluateRuntime(string source, string candidate)
        => EvaluateRuntime(source, candidate, null);

    internal static MeaningPreservationOutcome EvaluateRuntime(
        string source, string candidate, IReadOnlyCollection<string>? protectedTerms)
    {
        MeaningPreservationOutcome existing = Evaluate(source, candidate);
        var risks = existing.RiskCodes.ToList();
        if (!risks.Contains("NUMBER_CHANGED", StringComparer.Ordinal))
        {
            AddRiskIfChanged(risks, "NUMBER_CHANGED",
                Extract(AttachedQuantityToken(), source),
                Extract(AttachedQuantityToken(), candidate));
        }
        string normalizedSource = source.Normalize().ToLower(TurkishCulture.Value);
        string normalizedCandidate = candidate.Normalize().ToLower(TurkishCulture.Value);
        AddRuntimeRiskIfChanged(risks, "NEGATION_CHANGED",
            Extract(RuntimeNegationToken(), normalizedSource), Extract(RuntimeNegationToken(), normalizedCandidate));
        AddRiskIfChanged(risks, "QUESTION_PARTICLE_CHANGED",
            Extract(QuestionParticleToken(), normalizedSource),
            Extract(QuestionParticleToken(), normalizedCandidate));
        AddRuntimeRiskIfChanged(risks, "QUESTION_MARK_CHANGED",
            ExtractRuntimeQuestionAnchors(normalizedSource), ExtractRuntimeQuestionAnchors(normalizedCandidate));
        AddRuntimeRiskIfChanged(risks, "UNIT_CHANGED", ExtractRuntimeUnits(source), ExtractRuntimeUnits(candidate));
        AddRuntimeRiskIfChanged(risks, "ACTION_DIRECTION_CHANGED",
            ExtractRuntimeActions(normalizedSource), ExtractRuntimeActions(normalizedCandidate));
        if (protectedTerms is not null)
        {
            foreach (string term in protectedTerms) ArgumentNullException.ThrowIfNull(term);
            AddRuntimeRiskIfChanged(risks, "PROTECTED_TERM_CHANGED",
                ExtractRuntimeProtectedTerms(source, protectedTerms),
                ExtractRuntimeProtectedTerms(candidate, protectedTerms));
        }
        return new MeaningPreservationOutcome(risks.Count == 0, risks);
    }

    private static void AddRuntimeRiskIfChanged(
        ICollection<string> risks, string code, IReadOnlyList<string> source, IReadOnlyList<string> candidate)
    {
        if (!risks.Contains(code, StringComparer.Ordinal)) AddRiskIfChanged(risks, code, source, candidate);
    }

    private static string[] ExtractRuntimeUnits(string text) => RuntimeUnitToken().Matches(text.Normalize())
        .Select(static match => match.Groups["value"].Value + ":" +
            match.Groups["unit"].Value.ToLowerInvariant()).ToArray();

    private static string[] ExtractRuntimeActions(string text) => RuntimeActionToken().Matches(text)
        .Select(static match => match.Groups["direction"].Value + ":" + match.Groups["action"].Value).ToArray();

    private static string[] ExtractRuntimeQuestionAnchors(string normalizedText)
    {
        // Bind each question mark to its sentence and lexical ordinal within
        // that sentence, plus at most three preceding tokens. A safe spelling
        // correction in an earlier sentence may change its token count; it
        // cannot move a question mark in this sentence. The local ordinal still
        // separates repeated command tails within one sentence.
        var anchors = new List<string>();
        var precedingTokens = new Queue<string>(3);
        int sentenceOrdinal = 0;
        int lexicalOrdinal = 0;
        foreach (Match match in RuntimeQuestionAnchorToken().Matches(normalizedText))
        {
            string token = match.Value;
            if (token is "." or "!" or "?")
            {
                if (token == "?")
                    anchors.Add(sentenceOrdinal.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                        ":" + lexicalOrdinal.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                        ":" + string.Join(" ", precedingTokens));
                precedingTokens.Clear();
                sentenceOrdinal++;
                lexicalOrdinal = 0;
            }
            else
            {
                lexicalOrdinal++;
                if (precedingTokens.Count == 3) precedingTokens.Dequeue();
                precedingTokens.Enqueue(token);
            }
        }
        return anchors.ToArray();
    }

    private static string[] ExtractRuntimeProtectedTerms(string text, IReadOnlyCollection<string> terms)
    {
        var values = new List<(int Index, string Term)>();
        foreach (string term in terms.Where(static term => !string.IsNullOrWhiteSpace(term))
                     .Select(static term => term.Normalize()).Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(static term => term, StringComparer.Ordinal))
        {
            string pattern = $"(?<![\\p{{L}}\\p{{M}}\\p{{N}}_]){Regex.Escape(term)}(?![\\p{{L}}\\p{{M}}\\p{{N}}_])";
            foreach (Match match in Regex.Matches(text.Normalize(), pattern,
                         RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                values.Add((match.Index, term));
        }
        return values.OrderBy(static value => value.Index).ThenBy(static value => value.Term, StringComparer.Ordinal)
            .Select(static value => value.Term).ToArray();
    }

    private static readonly string[] ProtectedTerms =
    [
        "KDV",
        "Claude Code",
        "Cursor",
        "Antigravity",
        "VS Code",
        ".NET",
        "CUDA12"
    ];

    internal static MeaningPreservationOutcome Evaluate(string source, string candidate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(candidate);
        var risks = new List<string>(6);

        AddRiskIfChanged(risks, "NUMBER_CHANGED", Extract(NumberToken(), source), Extract(NumberToken(), candidate));
        AddRiskIfChanged(
            risks,
            "NEGATION_CHANGED",
            ExtractNegations(source),
            ExtractNegations(candidate));
        AddRiskIfChanged(risks, "URL_CHANGED", Extract(UrlToken(), source, normalizeUnicode: true),
            Extract(UrlToken(), candidate, normalizeUnicode: true));
        AddRiskIfChanged(risks, "PATH_CHANGED", Extract(WindowsPathToken(), source, normalizeUnicode: true),
            Extract(WindowsPathToken(), candidate, normalizeUnicode: true));
        AddRiskIfChanged(risks, "CODE_TOKEN_CHANGED", Extract(CodeToken(), source), Extract(CodeToken(), candidate));

        string[] sourceTerms = ExtractProtectedTerms(source);
        string[] candidateTerms = ExtractProtectedTerms(candidate);
        AddRiskIfChanged(risks, "PROTECTED_TERM_CHANGED", sourceTerms, candidateTerms);

        return new MeaningPreservationOutcome(risks.Count == 0, risks);
    }

    internal static bool ContainsProtectedTerm(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return ExtractProtectedTerms(text).Length > 0;
    }

    private static void AddRiskIfChanged(
        ICollection<string> risks,
        string code,
        IReadOnlyList<string> source,
        IReadOnlyList<string> candidate)
    {
        if (!source.SequenceEqual(candidate, StringComparer.Ordinal)) risks.Add(code);
    }

    private static string[] Extract(
        Regex regex,
        string text,
        bool lowerTurkish = false,
        bool normalizeUnicode = false)
    {
        var values = new List<string>();
        foreach (Match match in regex.Matches(text))
        {
            string value = match.Value;
            if (normalizeUnicode) value = value.Normalize();
            if (lowerTurkish) value = value.ToLower(TurkishCulture.Value);
            values.Add(value);
        }
        return values.ToArray();
    }

    private static string[] ExtractNegations(string text)
    {
        string normalized = text.Normalize().ToLower(TurkishCulture.Value);
        return Extract(NegationToken(), normalized);
    }

    private static string[] ExtractProtectedTerms(string text)
    {
        var values = new List<(int Index, string Term)>();
        foreach (string term in ProtectedTerms)
        {
            string pattern = $"(?<![\\p{{L}}\\p{{N}}_]){Regex.Escape(term)}(?![\\p{{L}}\\p{{N}}_])";
            foreach (Match match in Regex.Matches(
                         text,
                         pattern,
                         RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                values.Add((match.Index, term));
            }
        }
        return values
            .OrderBy(static value => value.Index)
            .ThenBy(static value => value.Term, StringComparer.Ordinal)
            .Select(static value => value.Term)
            .ToArray();
    }

    private static class TurkishCulture
    {
        internal static readonly System.Globalization.CultureInfo Value =
            System.Globalization.CultureInfo.GetCultureInfo("tr-TR");
    }

    [GeneratedRegex(@"(?<![\p{L}\p{N}_])[+-]?%?\d+(?:[.,]\d+)*%?(?![\p{L}\p{N}_])", RegexOptions.CultureInvariant)]
    private static partial Regex NumberToken();

    // Bounded technical quantities, including the unit: 5ms -> 5s is a meaning
    // change too. Runtime-only so existing schema-v1 terms remain loadable.
    // Identifier substrings and other units are deliberately outside this slice.
    [GeneratedRegex(@"(?<![\p{L}\p{M}\p{N}_])[+-]?\d+(?:[.,]\d+)*(?:ms|s|MB|GB)(?![\p{L}\p{M}\p{N}_])", RegexOptions.CultureInvariant)]
    private static partial Regex AttachedQuantityToken();

    // Runtime-only finite vocabulary and suffix sets. This is a conservative
    // guard for common commands, not a Turkish morphological parser. Word
    // boundaries prevent matching roots inside unrelated identifiers/words.
    private const string RuntimeVerbRoots = "sil|gönder|ver|al|yap|et|onayla|reddet|kopyala|taşı|değiştir|kur|kaldır|bağlan|ayrıl|öde|yayınla|yayımla|yükle|indir|aç|kapat|uygula|koru|beklet|tut|sakla";
    private const string RuntimePersonEndings = "(?:m|n|k|nız|niz|nuz|nüz|lar|ler)?";
    private const string RuntimeNegativeEndings = "(?:ma|me)(?:" +
        "(?:dı|di|du|dü)" + RuntimePersonEndings +
        "|(?:mış|miş|muş|müş)(?:ım|im|um|üm|sın|sin|sun|sün|ız|iz|uz|üz|lar|ler)?" +
        "|(?:yacak|yecek)" + RuntimePersonEndings +
        "|(?:yalım|yelim|yın|yin|yınız|yiniz)|(?:malı|meli)(?:yım|yim|sın|sin|yız|yiz)?|z)?";

    [GeneratedRegex(@"(?<![\p{L}\p{M}\p{N}_])(?:faydasız(?:dı|dır|lar|lık)?|(?:" + RuntimeVerbRoots + @")(?:" + RuntimeNegativeEndings + @"|(?:mıyor|miyor|muyor|müyor)(?:um|üm|sun|sün|uz|üz|lar|ler)?|(?:ın|in|un|ün)?(?:ama|eme)(?:yacak|yecek)" + RuntimePersonEndings + @"))(?![\p{L}\p{M}\p{N}_])", RegexOptions.CultureInvariant)]
    private static partial Regex RuntimeNegationToken();

    // Attached ms/s/MB/GB quantities keep their existing NUMBER_CHANGED-only
    // diagnostic contract; this additional channel also protects selected attached units.
    [GeneratedRegex(@"(?<![\p{L}\p{M}\p{N}_])(?<value>[+-]?\d+(?:[.,]\d+)*)\s*(?<unit>miligram|kilogram|gram|kilo|milisaniye|saniye|dakika|saat|mg|kg|g|ms|s|MB|GB)(?![\p{L}\p{M}\p{N}_])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RuntimeUnitToken();

    [GeneratedRegex(@"[\p{L}\p{M}\p{N}_]+|[.?!]", RegexOptions.CultureInvariant)]
    private static partial Regex RuntimeQuestionAnchorToken();

    [GeneratedRegex(@"(?<![\p{L}\p{M}\p{N}_])(?:(?<direction>geri|onay|izin)\s+)?(?<action>" + RuntimeVerbRoots + @")(?:" +
        RuntimeNegativeEndings + @"|(?:mıyor|miyor|muyor|müyor|ıyor|iyor|uyor|üyor)(?:um|üm|sun|sün|uz|üz|lar|ler)?" +
        @"|(?:dı|di|du|dü|tı|ti|tu|tü|acak|ecek)" + RuntimePersonEndings +
        @"|(?:ın|in|un|ün)?(?:ama|eme)?(?:yacak|yecek|acak|ecek)" + RuntimePersonEndings +
        @"|(?:alım|elim|yalım|yelim|ın|in|un|ün|yın|yin|yınız|yiniz|ınız|iniz|unuz|ünüz|mak|mek|malı|meli))?(?![\p{L}\p{M}\p{N}_])", RegexOptions.CultureInvariant)]
    private static partial Regex RuntimeActionToken();

    [GeneratedRegex(@"(?<![\p{L}\p{N}_])(değil[\p{L}]*|yok[\p{L}]*|olmaz|olmayacak|olmamalı|olmadı|olmuyor|hariç|yalnız|ama)(?![\p{L}\p{N}_])", RegexOptions.CultureInvariant)]
    private static partial Regex NegationToken();

    // Separate tokens only, with a bounded set of common personal/tense forms.
    // This protects tokens even in non-question uses ("güzel mi güzel"); it does
    // not infer intent, restore a missing ASR particle, or parse all Turkish morphology.
    // Copula forms are excluded: "müdür" can be an ordinary noun, requiring context.
    [GeneratedRegex(@"(?<![\p{L}\p{M}\p{N}_])(?:mı(?:sın(?:ız)?|yım|yız|ydı(?:m|n|k|nız|lar)?|ymış(?:ım|sın|ız|sınız|lar)?|ysa(?:m|n|k|nız|lar)?)?|mi(?:sin(?:iz)?|yim|yiz|ydi(?:m|n|k|niz|ler)?|ymiş(?:im|sin|iz|siniz|ler)?|yse(?:m|n|k|niz|ler)?)?|mu(?:sun(?:uz)?|yum|yuz|ydu(?:m|n|k|nuz|lar)?|ymuş(?:um|sun|uz|sunuz|lar)?|ysa(?:m|n|k|nız|lar)?)?|mü(?:sün(?:üz)?|yüm|yüz|ydü(?:m|n|k|nüz|ler)?|ymüş(?:üm|sün|üz|sünüz|ler)?|yse(?:m|n|k|niz|ler)?)?)(?![\p{L}\p{M}\p{N}_])", RegexOptions.CultureInvariant)]
    private static partial Regex QuestionParticleToken();

    [GeneratedRegex(@"https?://[^\s]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UrlToken();

    [GeneratedRegex(@"(?<![\p{L}\p{N}_])[A-Za-z]:\\[^\s]+", RegexOptions.CultureInvariant)]
    private static partial Regex WindowsPathToken();

    [GeneratedRegex(@"(?<![\p{L}\p{N}_])--[A-Za-z0-9][A-Za-z0-9-]*(?![\p{L}\p{N}_])", RegexOptions.CultureInvariant)]
    private static partial Regex CodeToken();
}
