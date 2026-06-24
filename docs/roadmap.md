# AgentTape Roadmap

## v0.1 - Local Flight Recorder

Hedef: Lokal komut oturumunu kaydet, redaction uygula, git diff yakala, HTML/Markdown rapor uret.

Ozellikler:

- `agenttape init`
- `agenttape record -- <command>`
- stdout/stderr capture
- exit code ve duration
- git status before/after
- final diff
- standard secret redaction
- basic risk warnings
- static HTML report
- Markdown PR summary
- .NET global tool packaging

Yayin kriteri:

- Windows ve Linux build/test yesil
- Global tool kuruluyor
- README quick start calisiyor
- Sample repo var

## v0.2 - Test-Aware Reports

Hedef: Test sonucunu oturum raporunun ana parcasi yapmak.

Ozellikler:

- TRX parser
- xUnit XML parser
- NUnit XML parser
- before/after test comparison
- failed/fixed tests listesi
- command classification iyilestirme
- GitHub Actions summary markdown

## v0.3 - Agent-Aware Workflows

Hedef: AI coding agent oturumlarini daha iyi anlamak.

Ozellikler:

- Codex profile
- Claude profile
- Aider profile
- session goal/name iyilestirme
- prompt capture sadece opt-in
- optional LLM explain, default kapali

## v1.0 - Shareable AI Coding Session Reports

Hedef: Stabil CLI ve paylasilabilir session standardi.

Ozellikler:

- Stable command surface
- HTML timeline polished
- Markdown export polished
- Pack format stable
- Dry-run replay
- Strong redaction
- Config file stable
- Example repos
- Demo GIF
- Documentation complete

## Urun Konumlandirmasi

AgentTape su cumleyle anlatilmali:

> AgentTape records, explains, and packages AI coding agent sessions into safe, reproducible developer timelines.

Turkce:

> AgentTape, AI coding agent oturumlarini guvenli, izlenebilir ve paylasilabilir gelistirici zaman cizelgelerine donusturur.
