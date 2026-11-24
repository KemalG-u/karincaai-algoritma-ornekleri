> 🚧 **Status: active development.** It works for the author; interfaces may change.

# karincaai-algoritma-ornekleri

[Türkçe](README.tr.md)

Small, dependency-free C# (.NET 10) building blocks taken from a speech-to-text (dictation) project, published as source examples: they are **not** a dictation app or a finished product.

- `SpeechRegionPlanner` — works on sample indexes: merges and expands speech regions, splits them into chunks, extracts audio, trims edges and tails, applies fade-in/fade-out.
- `LatinYazi` — Latin-script text clean-up and a retry policy for text that came back with foreign-script characters.
- `MeaningPreservationGate` — a limited, rule-based check that warns when a rewrite changes numbers, units, negations or selected terms.
- `SpeechSegment` — a small record type used by the planner.

They exist as a separate, readable collection so the logic can be reused and tested without audio, models or the original app.

## Quick start

Requirements: Git and the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```sh
git clone https://github.com/KemalG-u/karincaai-algoritma-ornekleri.git
cd karincaai-algoritma-ornekleri
dotnet build AlgorithmSamples.csproj --configfile NuGet.Config
dotnet run --project AlgorithmSamples.csproj --no-build --no-restore
```

The run prints one `PASS` line per check and ends with `TOTAL 43 PASS` (exit code 1 on any failure). `NuGet.Config` clears all package sources: no external package is needed, and the build does not download anything.

Alternative: `python run-tests.py` builds into a temporary directory and runs the same checks. It uses `dotnet` from `PATH`, or the executable named in the `DOTNET_EXE` environment variable.

## What is tested

43 synthetic checks: empty, unsorted, overlapping and out-of-range regions; exact-limit and over-long chunks; extraction and boundary errors; edge/tail trimming and fade; Turkish, mixed-alphabet, emoji and combining-accent text with a fake retrying parser; number changes and unchanged text. A GitHub Actions workflow (`.github/workflows/ci.yml`) builds and runs the same checks on Linux; its first run had not been observed when this was written.

## Limits

- Region detection is **not** a VAD: the boundaries must be supplied from outside.
- `Chunks` does not split a single region that is longer than the limit; it leaves a separate over-long chunk.
- Fade also attenuates the first sample, as in the original application.
- The Latin filter is a limited range heuristic, not a full Unicode script classifier; it may drop words that mix alphabets (for example with the Ohm sign U+2126 or a Greek Omega). Turkish letters and combining marks U+0300–U+036F were tested; other Latin ranges or complex mark sequences are not guaranteed.
- The meaning gate is a set of rules for selected numbers, units, negations and terms. It does not guarantee that meaning is preserved, can miss unknown units, numbers written as words, inflection and context, and can warn wrongly. It does not convert units: `5000 mg` to `5 g` is flagged as a precaution. Do not use it alone for medical, legal, sensitive or automatic-action decisions.
- Real audio, recordings, microphones, ASR, VAD, models, CUDA and product integration were **not** tested. The tests are not a guarantee of product accuracy or security.
- Tested locally with .NET SDK 10.0.402 on Windows (43 PASS); other SDK versions and operating systems are untested apart from the CI workflow.
- `tests/ExcludedFixture.cs` intentionally contains `#error`; it is excluded from the project and would stop the build if added.

## Provenance and changes

Derived from the core library of the author's private dictation project. Relative to that source: documentation comments with experiments were removed, a `FadeOut` overflow and negative-duration handling were fixed, the Latin filter now also catches non-Latin letters in the allowed symbol ranges, units mg/g/kg and "miligram" were added to the meaning gate, and the `SpeechSegment` record was extracted from the detector without its adapter and dependency. No model, binary, third-party package, word list, private data or build output is included. A local review found no explicit third-party copy attribution in the selected code; this is not independent legal assurance.

## Turkey-specific notes

The text rules (Latin filter, meaning gate) are written with Turkish text in mind (Turkish letters and combining marks). The Turkish README is `README.tr.md`.

## Licence

MIT, see `LICENSE` and `NOTICE.md`. Copyright (c) 2026 Kemal (KarıncaAI). .NET is a separately licensed prerequisite and is not relicensed. Report security issues privately, see `SECURITY.md`.
