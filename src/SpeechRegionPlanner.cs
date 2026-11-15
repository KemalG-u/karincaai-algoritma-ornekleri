namespace WisperKemal.Core;

public readonly record struct SampleRegion(int Start, int End)
{
    public int Length => End - Start;
}

public static class SpeechRegionPlanner
{
    public const int SampleRate = 16_000;

    public static IReadOnlyList<SampleRegion> Regions(
        IReadOnlyList<SpeechSegment> segments,
        int sampleCount,
        int marginMs)
    {
        ArgumentNullException.ThrowIfNull(segments);
        ArgumentOutOfRangeException.ThrowIfNegative(sampleCount);
        ArgumentOutOfRangeException.ThrowIfNegative(marginMs);
        var merged = new List<SampleRegion>(segments.Count);
        foreach (SpeechSegment segment in segments.OrderBy(s => s.Start))
        {
            int start = ToSample(segment.Start.TotalMilliseconds - marginMs, sampleCount);
            int end = ToSample(segment.End.TotalMilliseconds + marginMs, sampleCount);
            if (end <= start) continue;
            if (merged.Count > 0 && start <= merged[^1].End)
                merged[^1] = merged[^1] with { End = Math.Max(merged[^1].End, end) };
            else
                merged.Add(new SampleRegion(start, end));
        }
        return merged;
    }

    public static IReadOnlyList<IReadOnlyList<SampleRegion>> Chunks(
        IReadOnlyList<SampleRegion> regions,
        int chunkMaxMs)
    {
        ArgumentNullException.ThrowIfNull(regions);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(chunkMaxMs);
        long max = (long)chunkMaxMs * SampleRate / 1000;
        var chunks = new List<IReadOnlyList<SampleRegion>>();
        List<SampleRegion>? current = null;
        long length = 0;
        foreach (SampleRegion region in regions)
        {
            if (current is null || length + region.Length > max)
            {
                current = new List<SampleRegion>();
                chunks.Add(current);
                length = 0;
            }
            current.Add(region);
            length += region.Length;
        }
        return chunks;
    }

    public static SampleRegion TrimLongEdges(
        IReadOnlyList<SpeechSegment> segments,
        int sampleCount,
        int minEdgeMs,
        int marginMs)
    {
        ArgumentNullException.ThrowIfNull(segments);
        var whole = new SampleRegion(0, sampleCount);
        if (segments.Count == 0) return whole;
        double first = segments.Min(s => s.Start.TotalMilliseconds);
        double last = segments.Max(s => s.End.TotalMilliseconds);
        double total = sampleCount * 1000d / SampleRate;
        int start = first >= minEdgeMs ? ToSample(first - marginMs, sampleCount) : 0;
        int end = total - last >= minEdgeMs ? ToSample(last + marginMs, sampleCount) : sampleCount;
        return end > start ? new SampleRegion(start, end) : whole;
    }

    public static int TailEnd(IReadOnlyList<SpeechSegment> segments, int sampleCount, int minTailMs, int marginMs,
        int blipMaxMs = 0)
    {
        ArgumentNullException.ThrowIfNull(segments);
        if (segments.Count == 0) return sampleCount;
        var ordered = segments.OrderBy(s => s.End).ToList();
        while (ordered.Count > 1)
        {
            SpeechSegment tail = ordered[^1];
            bool brief = (tail.End - tail.Start).TotalMilliseconds < blipMaxMs;
            bool apart = (tail.Start - ordered[^2].End).TotalMilliseconds >= minTailMs;
            if (!brief || !apart) break;
            ordered.RemoveAt(ordered.Count - 1);
        }
        double last = ordered[^1].End.TotalMilliseconds;
        double total = sampleCount * 1000d / SampleRate;
        return total - last >= minTailMs ? ToSample(last + marginMs, sampleCount) : sampleCount;
    }

    public static void FadeOut(Span<float> samples, int fadeMs)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(fadeMs);
        int n = (int)Math.Min(samples.Length, (long)fadeMs * SampleRate / 1000);
        int start = samples.Length - n;
        for (int i = 0; i < n; i++)
            samples[start + i] *= (float)(n - 1 - i) / n;
    }

    public static float[] Extract(ReadOnlySpan<float> samples, IReadOnlyList<SampleRegion> chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        int total = 0;
        foreach (SampleRegion region in chunk)
        {
            if (region.Start < 0 || region.End > samples.Length || region.End < region.Start)
                throw new ArgumentOutOfRangeException(nameof(chunk));
            total += region.Length;
        }
        var buffer = new float[total];
        int offset = 0;
        foreach (SampleRegion region in chunk)
        {
            samples[region.Start..region.End].CopyTo(buffer.AsSpan(offset));
            offset += region.Length;
        }
        return buffer;
    }

    private static int ToSample(double milliseconds, int sampleCount) =>
        (int)Math.Clamp(Math.Round(milliseconds * SampleRate / 1000), 0, sampleCount);
}
