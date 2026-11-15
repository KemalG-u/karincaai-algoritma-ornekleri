using WisperKemal.Core;

static class Tests
{
    static int count, failures;
    static void Check(bool ok, string name) { if (!ok) { failures++; Console.WriteLine("FAIL " + name); } else { count++; Console.WriteLine("PASS " + name); } }
    static SpeechSegment S(int a, int b) => new(TimeSpan.FromMilliseconds(a), TimeSpan.FromMilliseconds(b));
    static async Task Main()
    {
        Check(SpeechRegionPlanner.Regions([], 16000, 0).Count == 0, "empty regions");
        var r = SpeechRegionPlanner.Regions([S(500,800), S(100,300), S(250,600)], 16000, 0);
        Check(r.Count == 1 && r[0] == new SampleRegion(1600,12800), "unordered overlap merge");
        r = SpeechRegionPlanner.Regions([S(0,100),S(900,1200)],16000,100);
        Check(r.SequenceEqual([new SampleRegion(0,3200),new SampleRegion(12800,16000)]), "margin clamp");
        var chunks = SpeechRegionPlanner.Chunks([new(0,8000),new(9000,17000),new(18000,34001)],1000);
        Check(chunks.Count == 2 && chunks[0].Count == 2 && chunks[1][0].Length == 16001, "chunk exact boundary and oversize");
        Check(SpeechRegionPlanner.Extract([1f,2f,3f,4f], [new(1,3)]).SequenceEqual([2f,3f]), "extract");
        try { SpeechRegionPlanner.Extract([1f], [new(0,2)]); throw new Exception("FAIL bounds"); } catch (ArgumentOutOfRangeException) { Check(true,"extract bounds"); }
        Check(SpeechRegionPlanner.TrimLongEdges([],16000,200,100) == new SampleRegion(0,16000),"empty trim");
        Check(SpeechRegionPlanner.TrimLongEdges([S(300,600)],16000,200,100) == new SampleRegion(3200,11200),"trim edges");
        Check(SpeechRegionPlanner.TailEnd([S(900,950),S(100,400)],16000,200,100,100)==8000,"tail blip");
        float[] fade = [1,1,1,1]; SpeechRegionPlanner.FadeOut(fade,1);
        Check(fade.SequenceEqual([.75f,.5f,.25f,0f]),"short fade");
        Check(LatinYazi.Clean("Türkçe, güzel! ₺").Text == "Türkçe, güzel! ₺", "Turkish unchanged");
        var clean=LatinYazi.Clean("merhaba teст dünya 😀");
        Check(clean.Text=="merhaba dünya" && clean.DroppedWords==1 && clean.RemovedSigns==1,"mixed script and emoji");
        Check(LatinYazi.TamLatinDisi("Привет") && !LatinYazi.TamLatinDisi("Привет hello") && !LatinYazi.TamLatinDisi("123"),"script detection");
        Check(LatinYazi.Clean("e\u0301").Text == "e\u0301", "combining accent");
        int calls=0; string result=await LatinDecode.Run((suppressed,ct)=>{calls++; return Task.FromResult(suppressed ? "merhaba" : "привет");});
        Check(result=="merhaba" && calls==2,"decode retry fake");
        var risk=MeaningPreservationGate.EvaluateRuntime("5 gram ver", "6 gram ver");
        Check(!risk.IsSafe && risk.RiskCodes.Contains("NUMBER_CHANGED"),"meaning number");
        Check(MeaningPreservationGate.EvaluateRuntime("merhaba", "merhaba").IsSafe,"meaning unchanged");
        Check(LatinYazi.HasForbiddenLetter("Ω") && LatinYazi.TamLatinDisi("Ω") && LatinYazi.Clean("abcΩ").DroppedWords == 1, "omega forbidden letter");
        Check(!LatinYazi.HasForbiddenLetter("Türkçe") && LatinYazi.Clean("Iİıi şŞ öÖ üÜ çÇ ğĞ").DroppedWords == 0, "Turkish letters");
        Check(LatinYazi.Clean("s\u0327").Text == "s\u0327", "combining Turkish");
        foreach (var pair in new[] { ("5 mg ver", "5 g ver"), ("5mg ver", "5g ver"), ("2 kg ver", "2 g ver") })
            Check(!MeaningPreservationGate.EvaluateRuntime(pair.Item1, pair.Item2).IsSafe, "quantity " + pair.Item1 + " -> " + pair.Item2);
        Check(!MeaningPreservationGate.EvaluateRuntime("bunu silme", "bunu sil").IsSafe, "negation changed");
        Check(!MeaningPreservationGate.EvaluateRuntime("KDV uygula", "vergi uygula").IsSafe, "protected builtin");
        Check(!MeaningPreservationGate.EvaluateRuntime("alfa sakla", "beta sakla", ["alfa"]).IsSafe, "protected custom");
        float[] huge = [1,1,1,1]; SpeechRegionPlanner.FadeOut(huge, int.MaxValue);
        Check(huge.SequenceEqual([.75f,.5f,.25f,0f]), "huge fade");
        bool rejected = false; try { SpeechRegionPlanner.FadeOut([1f], -1); } catch (ArgumentOutOfRangeException) { rejected = true; }
        Check(rejected, "negative fade rejected");
        Check(LatinYazi.HasForbiddenLetter("Ω") && LatinYazi.Clean("aΩb").Text == "", "Greek mixed token");
        Check(LatinYazi.Clean("aΩb", false).Text == "ab", "omega removed without token dropping");
        Check(LatinYazi.Clean("\u0301a").Text == "a", "orphan combining mark removed");
        foreach (var pair in new[] { ("5000 mg ver", "5 g ver"), ("1.5 kg ver", "1.5 g ver"), ("8 gram ver", "8 kilogram ver"), ("5 ms bekle", "5 s bekle") })
            Check(!MeaningPreservationGate.EvaluateRuntime(pair.Item1, pair.Item2).IsSafe, "unit risk " + pair.Item1 + " -> " + pair.Item2);
        Check(MeaningPreservationGate.EvaluateRuntime("5 mg ver", "5 mg ver").IsSafe, "same quantity safe");
        Check(!MeaningPreservationGate.EvaluateRuntime("5 ver", "6 ver").IsSafe, "number changed again");
        Check(!MeaningPreservationGate.EvaluateRuntime("yok", "var").IsSafe, "negation yok");
        Check(MeaningPreservationGate.EvaluateRuntime("alfa", "alfa", ["alfa"]).IsSafe, "protected same");
        float[] zero = [1,1]; SpeechRegionPlanner.FadeOut(zero, 0);
        Check(zero.SequenceEqual([1f,1f]), "zero fade");
        SpeechRegionPlanner.FadeOut(Span<float>.Empty, int.MaxValue); Check(true, "empty huge fade");
        float[] boundary = [1,1]; SpeechRegionPlanner.FadeOut(boundary, 134218);
        Check(boundary.SequenceEqual([.5f,0f]), "overflow boundary fade");
        rejected = false; try { SpeechRegionPlanner.FadeOut(Span<float>.Empty, int.MinValue); } catch (ArgumentOutOfRangeException) { rejected = true; }
        Check(rejected, "minimum fade rejected on empty");
        Console.WriteLine($"TOTAL {count} PASS");
        if (failures != 0) { Console.WriteLine($"TOTAL {failures} FAIL"); Environment.ExitCode = 1; }
    }
}
