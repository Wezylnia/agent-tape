# AgentTape Detayli Implementasyon Plani

Bu plan, projeyi daha dusuk kapasiteli bir modelin sirayla implemente edebilmesi icin yazildi. Her adim bitmeden sonraki adima gecilmemelidir.

## Oncelik Sirasi

1. v0.1 Local Flight Recorder
2. v0.2 Test-Aware Reports
3. v0.3 Agent-Aware Workflows
4. v1.0 Shareable AI Coding Session Reports

Ilk hedef v0.1'dir. v0.2+ ozelliklerine dosya veya TODO eklenebilir, fakat v0.1 bitmeden implementasyonuna girilmemelidir.

## v0.1 Kapsami

v0.1 sadece su akislari guvenilir hale getirmelidir:

```bash
agenttape init
agenttape record -- dotnet test
agenttape record -- npm run build
agenttape report
agenttape export --format markdown
```

Zorunlu ciktlar:

- `.agenttape/sessions/<session-id>/session.json`
- `.agenttape/sessions/<session-id>/commands.jsonl`
- `.agenttape/sessions/<session-id>/stdout/001.txt`
- `.agenttape/sessions/<session-id>/stderr/001.txt`
- `.agenttape/sessions/<session-id>/git/final.diff`
- `.agenttape/reports/session.html`
- `.agenttape/reports/session.md`

## Faz 0: Mevcut Iskeleti Dogrula

Durum: Baslatildi.

Yapilacaklar:

- Solution .NET 10 hedeflemeli
- `dotnet build AgentTape.slnx` calismali
- `dotnet test AgentTape.slnx` calismali
- README build komutlari dogru olmali
- `docs/architecture.md` ve bu dokuman guncel olmali

Kabul kriteri:

- Tum projeler `net10.0`
- Testler yesil
- Kodda template `Class1.cs` veya `UnitTest1.cs` kalmamali

## Faz 1: Session Storage Standardizasyonu

Amac: CLI icindeki daginik dosya yazimini `ISessionStore` uygulamasina tasimak.

Yeni/duzenlenecek dosyalar:

- `src/AgentTape.Core/Storage/FileSystemSessionStore.cs`
- `src/AgentTape.Core/Storage/SessionIdFactory.cs`
- `tests/AgentTape.Core.Tests/Storage/FileSystemSessionStoreTests.cs`

Adimlar:

1. `SessionIdFactory` ekle.
2. Id formati: `yyyy-MM-dd-HHmmss-<safe-name>`.
3. Safe name sadece lowercase harf, rakam ve tire icermeli.
4. `FileSystemSessionStore.CreateSessionLayoutAsync` session klasorlerini olustursun.
5. `SaveSessionAsync` `session.json` yazsin.
6. `commands.jsonl` her command icin tek satir JSON yazsin.
7. Stdout/stderr path bilgisi `CommandRun` icinde dogru set edilsin.
8. CLI dosya yazimini store'a devretsin.

Kabul kriteri:

- `record` sonrasi hedef layout birebir olusur
- Session JSON pretty printed ve okunabilir olur
- Existing report path bozulmaz

Testler:

- Session id safe karakterlerden olusur
- Store tum klasorleri olusturur
- Session JSON deserialize edilebilir

## Faz 2: CLI Parser'i Netlestir

Amac: Basit ama deterministik CLI davranisi.

Komutlar:

- `agenttape init`
- `agenttape record [--name <name>] [--redact standard|strict|off] [--no-git] -- <command>`
- `agenttape report [--html] [--markdown] [--open]`
- `agenttape export --format markdown|json`

Adimlar:

1. `AgentTape.Cli/Commands` klasoru ac.
2. Her komut icin ayri handler sinifi ekle.
3. `Program.cs` sadece dispatch yapsin.
4. `record` icin `--` separator zorunlu olsun.
5. `--redact` default `standard` olsun.
6. Bilinmeyen option icin exit code `2` don.
7. Calistirilan komutun exit code'u aynen donsun.

Kabul kriteri:

- Hatalar kullaniciya kisa ve anlasilir yazilir
- `agenttape record dotnet test` yerine `agenttape record -- dotnet test` beklenir
- `agenttape record --redact off -- dotnet test` acikca desteklenir

Testler:

- Parser command ve args'i dogru ayirir
- Missing separator hata verir
- Unknown command hata verir

## Faz 3: Process Runner'i Guclendir

Amac: stdout/stderr capture guvenilir olsun.

Dosyalar:

- `src/AgentTape.Core/ProcessCommandRunner.cs`
- `tests/AgentTape.Core.Tests/ProcessCommandRunnerTests.cs`

Adimlar:

1. Command id runner icinde hardcoded `001` kalmasin.
2. Runner id'yi disaridan alsin veya session orchestrator set etsin.
3. Buyuk output icin memory riski not edilmeli; MVP'de limit ekle.
4. Preview max 4000 karakter kalsin.
5. Full redacted output store tarafindan dosyaya yazilsin.
6. Cancellation durumunda process kill edilsin.

Kabul kriteri:

- stdout ve stderr ayrik yakalanir
- exit code dogru gelir
- working directory dogru uygulanir
- command preview full output yerine limitli tutulur

## Faz 4: Git Snapshot ve Diff

Amac: Repo durumu dogru ve toleransli yakalansin.

Dosyalar:

- `src/AgentTape.Git/Snapshots/GitCliSnapshotProvider.cs`
- `tests/AgentTape.Git.Tests/...` yeni test projesi eklenebilir

Adimlar:

1. Git yoksa veya repo degilse crash etme.
2. `before-status.txt` ve `after-status.txt` yaz.
3. `final.diff` yaz.
4. Porcelain rename formatini parse et.
5. Binary dosya sinyali icin diff summary veya numstat ekle.
6. Added/deleted line sayisi icin `git diff --numstat` parse et.

Kabul kriteri:

- Git repo olmayan klasorde record calisir
- Git repo icinde branch, head, changes dolu gelir
- Diff redaction sonrasi rapora eklenir

## Faz 5: Redaction Engine

Amac: Rapor ve pack icinde secret sizmasini varsayilan olarak engellemek.

Dosyalar:

- `src/AgentTape.Redaction/Rules/RegexRedactor.cs`
- `src/AgentTape.Redaction/Rules/RedactionRule.cs`
- `tests/AgentTape.Redaction.Tests/Rules/RegexRedactorTests.cs`

Adimlar:

1. Regexleri ayri rule listesine cikar.
2. Her rule icin code ve replacement belirle.
3. Redaction log modeli ekle.
4. Log secret degeri yazmamali; sadece rule code ve match count yazmali.
5. Standard mode:
   - GitHub token
   - OpenAI key
   - AWS access key
   - JWT
   - `Password=...`
   - `token=...`
   - Windows user path
6. Strict mode:
   - Email
   - Opsiyonel IP/domain masking

Kabul kriteri:

- Redaction default standard
- Testlerde raw token kalmadigi kanitlanir
- Replacement secret'in son parcasini gostermemeli

## Faz 6: Risk Rules

Amac: Oturum risk sinyallerini deterministik uretmek.

Dosyalar:

- `src/AgentTape.Rules/Risk/DefaultRiskRules.cs`
- `tests/AgentTape.Rules.Tests/...`

Adimlar:

1. Her risk icin ayri class tercih edilebilir, fakat MVP'de tek class kabul.
2. Sensitive path listesi case-insensitive olmali.
3. Config dosyalari:
   - `appsettings*.json`
   - `.config`
   - `.env*`
   - `*.yml`, `*.yaml` icinde config isimleri
4. Suspicious command:
   - `rm -rf`
   - `del /s`
   - `format`
   - `shutdown`
   - `curl ... | bash`
   - `Invoke-Expression`
5. Command exit code'a gore:
   - Test command fail olduysa `TEST_FAILED`
   - Build command fail olduysa `BUILD_FAILED`

Kabul kriteri:

- Warning'ler raporda gorunur
- False positive kabul edilebilir ama acik mesaj olmali
- Warning secret degeri icermez

## Faz 7: Markdown Export

Amac: PR/issue aciklamasina koyulabilir ozet uretmek.

Dosyalar:

- `src/AgentTape.Reporting/Markdown/MarkdownReportGenerator.cs`
- `src/AgentTape.Cli/Commands/ExportCommand.cs`

Adimlar:

1. `agenttape export --format markdown` latest session'i bulsun.
2. Output stdout'a yazilabilsin.
3. `--output <path>` opsiyonu sonra eklenebilir.
4. Summary alanlari:
   - Session
   - Branch
   - Duration
   - Commands
   - Files changed
   - Tests before/after, varsa
   - Risk warnings
   - Reproduction command

Kabul kriteri:

- Markdown GitHub'da temiz render olur
- Code block'lar kapanir
- Raw stdout komple markdown'a basilmaz

## Faz 8: HTML Report

Amac: Projenin vitrin ozelligi olan statik timeline raporu.

Dosyalar:

- `src/AgentTape.Reporting/Html/HtmlReportGenerator.cs`

Adimlar:

1. Tek dosya HTML uret.
2. Baslikta session name, branch, duration.
3. Metric satiri:
   - Commands
   - Files changed
   - Warnings
   - Exit code
4. Timeline bolumu command sirasini gostersin.
5. File changes bolumu path ve kind gostersin.
6. Risk warnings bolumu severity'ye gore ayrilsin.
7. Test summary varsa goster.
8. Final diff ilk surumde collapsed `<details>` icinde olabilir.

Kabul kriteri:

- Browser'da file olarak acilir
- HTML encode her yerde kullanilir
- Secret raw olarak gorunmez
- CSS inline olabilir

## Faz 9: Test Detection

Amac: `.NET test` ciktilarindan basic sinyal.

Dosyalar:

- `src/AgentTape.Testing/DotNet/DotNetTestOutputDetector.cs`

Adimlar:

1. Ingilizce dotnet summary parse et.
2. Turkce/localized output'a guvenme; TRX v0.2'de gelecek.
3. Failed test isimleri icin basit pattern ekle.
4. No signal durumunda bos `TestSummary` don.

Kabul kriteri:

- Parser hata firlatmaz
- No-match bos summary doner
- Basic summary testleri var

## Faz 10: Packaging

Amac: .NET global tool kurulumu.

Dosya:

- `src/AgentTape.Cli/AgentTape.Cli.csproj`

Eklenecek property'ler:

```xml
<PackAsTool>true</PackAsTool>
<ToolCommandName>agenttape</ToolCommandName>
<PackageId>AgentTape</PackageId>
<Version>0.1.0</Version>
```

Kabul kriteri:

```bash
dotnet pack src/AgentTape.Cli/AgentTape.Cli.csproj
dotnet tool install --global --add-source ./src/AgentTape.Cli/bin/Release AgentTape
agenttape --help
```

## Yapilmayacaklar

v0.1 icinde bunlar yapilmamalidir:

- LLM summary
- Full replay execute
- VS Code extension
- Web dashboard
- Cloud sync
- Account sistemi
- GitHub Action
- Gercek sandbox
- Terminal emulator

## Definition of Done: v0.1

v0.1 tamam sayilmasi icin:

- `agenttape init` config olusturur
- `agenttape record -- <command>` komutu calistirir
- stdout/stderr redacted dosyaya yazilir
- git before/after ve final diff yakalanir
- `session.json` yazilir
- HTML report uretilir
- Markdown export uretilir
- Risk warnings uretilir
- Secret redaction testleri var
- Windows ve Linux CI calisir
- Global tool pack calisir
