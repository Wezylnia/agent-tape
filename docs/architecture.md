# AgentTape Mimari Tasarimi

Bu dokuman AgentTape'in moduler mimarisini tarif eder. Implementasyon yapacak model once bu dosyayi, sonra `docs/implementation-plan.md` dosyasini okumalidir.

## Urun Siniri

AgentTape bir AI agent degildir. Kod yazmaz, refactor onermez, sandbox uygulamaz, terminal videosu kaydetmez ve varsayilan olarak hicbir veriyi dis API'ye gondermez.

AgentTape'in isi:

- Komut oturumunu kaydetmek
- Git durumunu ve final diff'i yakalamak
- Stdout/stderr ciktilarini redaction ile guvenli hale getirmek
- Test sinyallerini deterministik olarak cikarmak
- Risk uyarilari uretmek
- HTML ve Markdown rapor olusturmak

## Katmanlar

### AgentTape.Cli

Sadece komut satiri giris noktasi ve orkestrasyon katmanidir.

Sorumluluklari:

- `agenttape init`
- `agenttape record -- <command>`
- `agenttape report`
- `agenttape export`
- `agenttape pack`
- CLI argumanlarini parse etmek
- Core portlarini somut adapter'lara baglamak
- Kullaniciya kisa ozet yazmak

Yapmamasi gerekenler:

- Redaction regex'i yazmak
- Git status parse etmek
- HTML template icermek
- Test framework parse etmek
- Risk kurali mantigi tasimak

### AgentTape.Core

Domain model ve port katmanidir. Diger moduller Core'a baglanabilir; Core baska AgentTape modulune baglanmamalidir.

Icerik:

- `TapeSession`
- `CommandRun`
- `GitSnapshot`
- `FileChange`
- `RiskWarning`
- `TestSummary`
- `CommandRequest`
- `CommandResult`
- `ICommandRunner`
- `IGitSnapshotProvider`
- `IRedactor`
- `IReportGenerator`
- `ITestResultDetector`
- `IRiskRule`
- `ISessionStore`

Kural: Core icinde IO minimumda tutulmali. Process calistirma gibi ortak ve kucuk altyapi kabul edilebilir, fakat rapor, git, parser ve redaction mantigi Core'a girmemeli.

### AgentTape.Git

Git CLI uzerinden snapshot ve diff alir.

MVP'de libgit2 veya baska native bagimlilik kullanilmayacak. Git zaten developer makinesinde beklenen bir arac oldugu icin ilk surumde `git` CLI cagrilari yeterlidir.

Sorumluluklari:

- Repo icinde miyiz kontrolu
- Branch adini alma
- HEAD SHA alma
- `git status --porcelain=v1` alma ve `FileChange` listesine cevirme
- `git diff --no-ext-diff` alma
- Git yoksa veya repo degilse graceful fallback

Hata davranisi:

- Repo degilse hata firlatma; `GitSnapshot { IsRepository = false }` don
- Git komutu beklenmedik sekilde patlarsa record akisini tamamen durdurma karari CLI seviyesinde verilmelidir

### AgentTape.Redaction

Cikti ve rapora girecek metinleri maskeleyen lokal-only moduldur.

Modlar:

- `Off`: hicbir sey maskelemez. Varsayilan olamaz.
- `Standard`: token, password, connection string, JWT, local user path maskeler. Varsayilan budur.
- `Strict`: Standard'a ek olarak email ve ileride IP/domain gibi kimlik bilgilerini de maskeler.

MVP'de regex tabanli olmalidir. Scanner/SAST aracina donusturulmemelidir.

### AgentTape.Reporting

Statik rapor uretir.

MVP formatlari:

- Markdown: PR/issue summary icin
- HTML: lokal paylasilabilir rapor icin

Kurallar:

- Rapor raw secret gostermemeli
- HTML tek dosya olarak acilabilmeli
- Server gerektirmemeli
- LLM summary icermemeli

### AgentTape.Testing

Test ciktilarindan deterministik sinyal cikarir.

MVP:

- Basit `dotnet test` text output parse

v0.2:

- TRX parser
- xUnit XML parser
- NUnit XML parser

Bu modul test runner calistirmamalidir. Sadece mevcut command output veya import edilen dosyayi parse etmelidir.

### AgentTape.Rules

Risk uyarilarini uretir.

Ilk kurallar:

- `CONFIG_CHANGED`
- `SECRET_FILE_TOUCHED`
- `LARGE_DELETE`
- `BINARY_CHANGED`
- `LOCKFILE_CHANGED`
- `MIGRATION_CHANGED`
- `BUILD_FAILED`
- `TEST_FAILED`
- `SUSPICIOUS_COMMAND`
- `NETWORK_SCRIPT_EXEC`

Kural motoru basit kalmalidir. Ilk versiyonda reflection, plugin engine veya DSL gerekmiyor.

## Bagimlilik Yonu

Dogru bagimlilik yonu:

```text
AgentTape.Cli
  -> AgentTape.Core
  -> AgentTape.Git
  -> AgentTape.Redaction
  -> AgentTape.Reporting
  -> AgentTape.Testing
  -> AgentTape.Rules

AgentTape.Git       -> AgentTape.Core
AgentTape.Reporting -> AgentTape.Core
AgentTape.Testing   -> AgentTape.Core
AgentTape.Rules     -> AgentTape.Core
AgentTape.Redaction -> AgentTape.Core
AgentTape.Core      -> no AgentTape module dependency
```

Yanlis bagimlilik ornekleri:

- `Core -> Reporting`
- `Core -> Git`
- `Redaction -> Reporting`
- `Rules -> Git`
- `Testing -> Cli`

## Session Dosya Yapisi

Hedef layout:

```text
.agenttape/
  sessions/
    2026-06-24-142001-fix-tests/
      session.json
      commands.jsonl
      stdout/
        001.txt
      stderr/
        001.txt
      git/
        before-status.txt
        after-status.txt
        final.diff
      tests/
      redaction-log.json
  reports/
    session.html
    session.md
```

MVP'de basit dosya yazimi kabul edilebilir, fakat v0.1 kapanmadan `ISessionStore` uzerinden bu layout standart hale getirilmelidir.

## Ana Akis: record

`agenttape record -- dotnet test` akisi:

1. CLI argumanlari parse eder.
2. Config okunur veya default options kullanilir.
3. Session id ve session klasoru olusturulur.
4. Git before snapshot alinir.
5. Komut calistirilir.
6. Stdout/stderr yakalanir.
7. Redaction uygulanir.
8. Redacted output dosyalara yazilir.
9. Git after snapshot alinir.
10. Git diff yakalanir.
11. Test detector output uzerinde calisir.
12. Risk rules session uzerinde calisir.
13. `session.json` ve `commands.jsonl` yazilir.
14. Markdown ve HTML rapor uretilir.
15. Konsola kisa ozet yazilir.
16. CLI exit code olarak calistirilan komutun exit code'u donulur.

## Cikis Kodu Kurali

`record` komutu, kaydedilen komutun exit code'unu dondurmelidir. Bu davranis CI ve script uyumlulugu icin onemlidir.

Ornek:

- `agenttape record -- dotnet test` icindeki `dotnet test` exit code `1` ise AgentTape de `1` donmeli
- AgentTape kendi ic hatasi yuzunden basarisiz olduysa farkli hata kodu kullanilmali

## Guvenlik Varsayimlari

- Raw output varsayilan rapora girmemeli
- `redaction.mode = standard` varsayilan olmali
- `redaction.mode = off` kullanici tarafindan acikca secilmeli
- Pack/export varsayilan olarak redacted veri kullanmali
- Replay varsayilan olarak sadece dry-run olmali

## MVP Icin Kabul Edilebilir Kisaltmalar

Bu kisaltmalar v0.1 iskeletinde kabul edilebilir:

- Dependency injection framework kullanmadan elle wiring
- System.CommandLine yerine basit parser
- HTML icin basit string template
- Git icin sadece porcelain v1 parser
- Test parse icin sadece dotnet text summary

Bu kisaltmalar kabul edilmez:

- Redaction'i atlamak
- Core'a rapor/gir/git parser mantigi koymak
- Raw secret'i rapora yazmak
- Full replay execute yapmak
- Cloud veya account sistemi eklemek
