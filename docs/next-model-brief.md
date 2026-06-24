# Next Model Brief

Bu repo bir implementasyon iskeletidir. Amac rastgele yeni mimari kurmak degil, mevcut moduller uzerinden v0.1'i tamamlamaktir.

## Once Oku

1. `docs/architecture.md`
2. `docs/implementation-plan.md`
3. `README.md`
4. `private-docs/project-idea.md`

## Mevcut Teknik Kararlar

- Target framework: `net10.0`
- SDK pin: `global.json` ile `10.0.301`
- Solution file: `AgentTape.slnx`
- Core bagimsiz domain/port katmani
- Git adapter: git CLI
- Redaction: regex tabanli, local-only
- Reporting: static Markdown/HTML
- Test parser: once dotnet text output

## Ilk Yapilacak Is

`docs/implementation-plan.md` icindeki Faz 0'i yesil hale getir:

```bash
dotnet build AgentTape.slnx
dotnet test AgentTape.slnx
```

Sonra Faz 1'e gec:

- `FileSystemSessionStore`
- `SessionIdFactory`
- layout testleri
- CLI dosya yazimini store'a tasima

## Yasaklar

- Core'a HTML, git veya redaction detayi koyma
- Redaction'i default kapatma
- LLM entegrasyonu ekleme
- Replay execute ekleme
- Web dashboard baslatma
- Yeni frontend app kurma
