# KarıncaAI — bağımsız algoritma seçkisi

Bu, dikte uygulaması veya bitmiş ürün değil; bağımsız C# kaynak örneğidir. Sentetik örneklerde ses bölgesi genişletme/birleştirme, parçalara ayırma, çıkartma, kenar ve kuyruk kırpma, fade; Latin karakter temizliği ve sınırlı anlam değişikliği uyarıları içerir. Bölge belirleme bir VAD değildir; sınırlar dışarıdan verilir. Latin filtresi karma alfabeli kelimeleri atabilir; anlam kapısı kural tabanlıdır, anlamın korunduğunu garanti etmez. Hassas, tıbbi, hukuki veya otomatik işlem kararlarında tek başına kullanılmamalıdır. `Chunks` uzun tek bölgeyi bölmez; ayrı ve limitten uzun parça bırakır. Fade özgün uygulamada ilk örneği de azaltır.

## Çalıştırma ve sınırlar

Bu makinede Windows, Python ve mevcut portable .NET SDK **10.0.402** gerekir. Portföy ağacının mevcut `sdk` ve `test-kapisi` kardeş dizinlerine ve `D:/antigravity/_tmp/r133-kilit/agir.sh` kapısına bağlı test koşucusu:

```text
python -B -X utf8 E:/AI/projeler/github-portfoy/karincaai-dikte/algoritma-adayi/run-tests.py
```

Kaynaklar `net10.0` projesidir; NuGet.Config tüm paket kaynaklarını kapatır, harici paket gerekmez. Koşucu ortak Windows dosya kilidini alır, ardından ağır iş kapısında `AGIR_KAPATMA=0`, `AGIR_BAYAT_TEMIZLE=0` ile derler ve sentetik testleri çalıştırır; yalnız kendi geçici çıktılarını temizler. Kapının kendi anahtar kontrolü küresel atomik kilit değildir; portföy dışı çağıranlar için eşzamanlılık garantisi yoktur. Bu dosya makineye özgüdür; başka ortamda aynı SDK ile projeyi kendi güvenli çevrimdışı ortamında derlemek gerekir.

43 sentetik kontrol: boş, sırasız, örtüşen ve sınıra taşan bölgeler; tam sınır/uzun parça; çıkarma ve sınır hatası; kenar/kuyruk ve fade; Türkçe, karışık alfabe, emoji, birleştirici aksan, sahte çözümleyiciyle tekrar denemesi; sayı değişimi ve değişmeyen metin. Gerçek ses, kullanıcı kaydı, mikrofon, ASR, VAD, model, CUDA ve gerçek ürün entegrasyonu **test edilmedi**. Bu testler ürün doğruluğu veya güvenlik garantisi değildir. Model, DLL, wordfreq, Zemberek, bağımlılık paketi, özel veri, derleme çıktısı ve SDK bu seçkide yoktur. NuGet kaynakları kapalıdır; tam işletim sistemi ağ yalıtımı kanıtlanmış değildir.

## Köken ve değişiklikler

Kaynak: `uygulama-adayi/src/WisperKemal.Core`, yerel seçki; kaynak projenin kendisi değiştirilmedi. Kökendeki özgün dosyaların SHA-256 değerleri:

- `SpeechRegionPlanner.cs`: `c634be09b9a7e6dfc0c107486b681168de0f4bf9e15296817514f74069f74f07` — deney/kullanım örnekleri içeren XML yorumlar kaldırıldı; adayda FadeOut taşması ve negatif süre düzeltildi.
- `LatinYazi.cs`: `e014ca4d099986af8117ac4030b0abea71b5cfcd40d7417050509d85a639ef3d` — artık byte olarak aynı değil; adayda izinli sembol aralığındaki Latin dışı harfler de yakalanır.
- `MeaningPreservationGate.cs`: `80fb78297c1e0fb6fd820155d9186d4fdf984df0db1798de1e216ee70ab35057` — artık byte olarak aynı değil; adayda seçili birimlere mg/g/kg, miligram ve bitişik yazım eklendi.
- `WhisperNetSpeechSegmentDetector.cs`: `ce32523b18304373df3f07ad5b71ea40158ed4b275fcc9406134f89527dd6faf` — yalnız bağımsız `SpeechSegment` kayıt bildirimi ayrıldı; adaptör ve bağımlılığı alınmadı.

Kaynak kodu ve yorumlar yerel olarak incelendi; belirgin dış kaynak kopyalama atfı bulunmadı. Bu, bütün köken veya haklar hakkında bağımsız hukuki güvence değildir. Hak sahibinin kendi katkılarını lisanslama iznine dayanır. `LICENSE`, `NOTICE.md` ve `SHA256SUMS` dosyalarına bakın. Yayına çıkmadan önce ayrıca bağımsız yayın/gizlilik ve hak kontrolü gerekir.

## English

A source-code sample, **not a dictation app or finished product**. It includes sample-index region planning and extraction, edge/tail trimming and fade, Latin-script filtering/retry policy, and a limited rule-based meaning-change gate. Region detection is not implemented. Long individual regions remain over the chunk limit. Filtering may discard mixed-script words; the gate does not guarantee preservation of meaning. Do not rely on it alone for sensitive decisions.

The local Windows-only command above uses Python, the existing portable .NET SDK 10.0.402, the shared portfolio file lock and heavy-job gate. It builds offline with cleared NuGet sources, runs synthetic tests, and removes its own temporary output; paths are specific to this machine. The gate's key check is not a global atomic lock for unrelated callers. .NET itself is a separately licensed prerequisite.

Synthetic tests cover empty/overlapping/unsorted regions, clamping, splitting boundaries, extraction, trimming, fade, Turkish and non-Latin text, fake retry and selected meaning checks. Real audio, recordings, microphone, ASR, VAD, models, CUDA and product integration were **not tested**. No SDK, binaries, models, third-party packages, wordfreq data, Zemberek, private data or build outputs are included. NuGet sources are cleared; full OS network isolation was not established.

Source lineage and exact source hashes are listed above. Planner XML comments were removed and FadeOut fixed; LatinYazi and MeaningPreservationGate were modified in this candidate; the SpeechSegment record was extracted from the detector without its adapter. Local review found no explicit third-party copy attribution in the selected code, but this is not independent legal assurance. The owner's own contributions are licensed under MIT; third-party software is not relicensed. See LICENSE and NOTICE. Independent pre-publication privacy and rights review remains necessary.

## Bağımsız derleme ve ek sınırlar

Bu makinede bütün derleme/test işlemleri yukarıdaki ortak kapılı koşucuyla yapılmalıdır. Başka güvenli bir ortamda .NET 10 SDK kurulu ve çalışma dizini bu klasör iken:

```text
dotnet build AlgorithmSamples.csproj --configfile NuGet.Config
dotnet run --project AlgorithmSamples.csproj --no-build --no-restore
```

Bu iki doğrudan komut ortak kilidi almaz ve başka ortamda denenmedi. `tests/ExcludedFixture.cs` kasıtlı `#error` içerir; proje açık kaynak listesine eklenirse derleme durur.

Latin kontrolü tam Unicode Script sınıflandırıcısı değil, sınırlı aralık sezgisidir. `Ω` (U+2126) ve `Ω` gibi Latin dışı harfler karma kelimeyi attırır; kelime atmama modunda harf silinir. Türkçe ve Latin harfini izleyen U+0300–U+036F işaretler sınandı; diğer Latin aralıkları veya karmaşık işaret dizileri için garanti yoktur. `SuppressRegex` değiştirilmedi; gerçek tokenizer/model davranışı sınanmadı.

Anlam kapısı seçilmiş sayı, birim, olumsuzluk ve terim kurallarıdır, güvence değildir; bilinmeyen birim, yazıyla sayı, çekim ve bağlamı kaçırabilir veya yanlış uyarabilir. Birim dönüşümü hesaplanmaz; eşdeğer `5000 mg` → `5 g` da ihtiyatla işaretlenir. Fade negatif süreyi reddeder, büyük süreyi küçük dizide güvenli sınırlar. Asıl ürün kodu değiştirilmedi.

Seçkide harici veri, secret, model, DLL veya harici paket yoktur; metinler/diziler sentetiktir. Özgün dosya hashleri önceki kayıtlardan alınmıştır, bu görevde ürün dosyaları tekrar okunmamıştır.
