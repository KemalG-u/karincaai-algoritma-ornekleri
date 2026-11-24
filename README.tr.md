> 🚧 **Durum: aktif geliştirme.** Yazarın kullanımında çalışır; arayüzler değişebilir.

[English](README.md)

# karincaai-algoritma-ornekleri

Bu, dikte uygulaması veya bitmiş ürün değil; bağımsız C# kaynak örneğidir. Sentetik örneklerde ses bölgesi genişletme/birleştirme, parçalara ayırma, çıkartma, kenar ve kuyruk kırpma, fade; Latin karakter temizliği ve sınırlı anlam değişikliği uyarıları içerir. Bölge belirleme bir VAD değildir; sınırlar dışarıdan verilir. Latin filtresi karma alfabeli kelimeleri atabilir; anlam kapısı kural tabanlıdır, anlamın korunduğunu garanti etmez. Hassas, tıbbi, hukuki veya otomatik işlem kararlarında tek başına kullanılmamalıdır. `Chunks` uzun tek bölgeyi bölmez; ayrı ve limitten uzun parça bırakır. Fade özgün uygulamada ilk örneği de azaltır.

## Hızlı başlangıç

[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) ve Git gerekir. Depoyu klonlayıp klasöründe çalıştırın.

```sh
git clone https://github.com/KemalG-u/karincaai-algoritma-ornekleri.git
cd karincaai-algoritma-ornekleri
dotnet build AlgorithmSamples.csproj --configfile NuGet.Config
dotnet run --project AlgorithmSamples.csproj --no-build --no-restore
```

16 Kasım 2026'da bu iki komutun eşdeğerleri, bir Windows bilgisayarda yeni yalıtılmış klasörde .NET SDK **10.0.402** ile denendi: derleme 0 uyarı ve 0 hatayla, çalıştırma `TOTAL 43 PASS` ile sonuçlandı. Başka SDK sürümü denenmedi; sonuç her ortam için garanti değildir. Linux için `.github/workflows/ci.yml` eklendi; ilk koşusu bu yazı yazılırken henüz görülmemişti.

Alternatif: `python run-tests.py` geçici bir klasöre derler ve aynı kontrolleri çalıştırır; `dotnet` için `PATH` ya da `DOTNET_EXE` ortam değişkenini kullanır.

43 sentetik kontrol: boş, sırasız, örtüşen ve sınıra taşan bölgeler; tam sınır/uzun parça; çıkarma ve sınır hatası; kenar/kuyruk ve fade; Türkçe, karışık alfabe, emoji, birleştirici aksan, sahte çözümleyiciyle tekrar denemesi; sayı değişimi ve değişmeyen metin. Gerçek ses, kullanıcı kaydı, mikrofon, ASR, VAD, model, CUDA ve gerçek ürün entegrasyonu **test edilmedi**. Bu testler ürün doğruluğu veya güvenlik garantisi değildir. Model, DLL, wordfreq, Zemberek, bağımlılık paketi, özel veri, derleme çıktısı ve SDK bu seçkide yoktur.

## Sınırlar

`tests/ExcludedFixture.cs` kasıtlı `#error` içerir; proje açık kaynak listesine eklenirse derleme durur.

Latin kontrolü tam Unicode Script sınıflandırıcısı değil, sınırlı aralık sezgisidir. `Ω` (U+2126) ve `Ω` gibi Latin dışı harfler karma kelimeyi attırır; kelime atmama modunda harf silinir. Türkçe ve Latin harfini izleyen U+0300–U+036F işaretler sınandı; diğer Latin aralıkları veya karmaşık işaret dizileri için garanti yoktur. `SuppressRegex` değiştirilmedi; gerçek tokenizer/model davranışı sınanmadı.

Anlam kapısı seçilmiş sayı, birim, olumsuzluk ve terim kurallarıdır, güvence değildir; bilinmeyen birim, yazıyla sayı, çekim ve bağlamı kaçırabilir veya yanlış uyarabilir. Birim dönüşümü hesaplanmaz; eşdeğer `5000 mg` → `5 g` da ihtiyatla işaretlenir. Fade negatif süreyi reddeder, büyük süreyi küçük dizide güvenli sınırlar. Asıl ürün kodu değiştirilmedi.

## Köken ve değişiklikler

Yazarın özel dikte projesinin çekirdek kitaplığından türetilmiştir. Kaynağa göre: deney içeren XML yorumlar kaldırıldı, `FadeOut` taşması ve negatif süre düzeltildi, Latin filtresi izinli sembol aralığındaki Latin dışı harfleri de yakalıyor, anlam kapısına mg/g/kg ve "miligram" eklendi, `SpeechSegment` kaydı dedektörden adaptörü ve bağımlılığı olmadan ayrıldı. Yerel inceleme seçilen kodda belirgin bir dış kaynak kopyalama atfı bulmadı; bu bağımsız hukuki güvence değildir.

## Lisans

MIT, bkz. `LICENSE` ve `NOTICE.md`. Telif (c) 2026 Kemal (KarıncaAI). .NET ayrı lisanslı bir önkoşuldur, yeniden lisanslanmaz. Güvenlik: `SECURITY.md`.
