# OtoCAD — Optical CAD

[简体中文](README.md) | **English**

> **🌐 Website & Downloads: [cad.optic.chat](https://cad.optic.chat)** — portable, unzip-and-run (Windows / Linux / macOS)

> A 2D CAD drawing tool built for optical engineers. Cross-platform desktop app (Win/Mac/Linux),
> optionally paired with **OtoCAD Cloud** for AI assistant / collaboration / sync.

[![CI Build](https://github.com/otocad/Otocad-Community/actions/workflows/ci.yml/badge.svg)](https://github.com/otocad/Otocad-Community/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

![cover](./docs/images/otocad.png)

---

## Features

### Drawing & Editing
- **Draw**: line · circle · arc · rectangle · polyline · polygon · ellipse · spline · point · ray · xline · text · mtext · leader
- **Edit**: undo · redo · move · copy · rotate · scale · mirror · offset · delete
- **Select**: pick · window/crossing selection · Shift add · Ctrl+A select-all
- **Interaction**: drag selected entities directly (AutoCAD-standard UX)
- **Snap**: endpoint / midpoint / center (toggleable visual indicators)
- **View**: wheel zoom · middle-button pan · ZoomExtents/In/Out · adaptive grid · origin axes

### Optical-specific
- **24 annotation marks**: coating (13 coating types) · blackening · polishing · sandblasting · diamond turning · grinding · surface roughness · surface form · centration · surface defects · surface texture · clear aperture · focus · optical axis · assembly · inspection · machining · material defects, etc.
- **ISO 10110 series**: -2 birefringence · -3 bubbles · -4 inhomogeneity · -12 aspheric · -14 wavefront · laser damage threshold
- **6 dimension types**: linear · aligned · radius · diameter · 3-point angular · ordinate
- **GB/T 13323-2009 optical drawing compliance**: auto dimensioning (R1/R2/d/Ø) · dual tables (material / part requirements) · glass library (Schott/CDGM/Ohara/Hoya, 40+ grades) · prism entities
- **Manufacturing drawing output (ISO 10110)**: one-click standard sheet from a selected singlet/cemented lens — frame · title block · material/part dual tables · dimensions (R/d/Ø, symmetric `±` or stacked upper/lower deviations) · glass section symbol (short-long-short) · clear / mechanical aperture (mechanical ≥ clear) · surface-roughness marks

### Documents & workflow
- **Multi-document tabs (MDI)**: layout overview + per-element manufacturing sheets; tabs are closable, with an unsaved-changes prompt (Save / Don't save / Cancel) before closing
- **Autosave & recovery**: 30 s incremental backups to local (`%APPDATA%/OtoCAD/autosave`, never overwrites your file); on startup, detects backups and offers crash recovery

### File formats
- Native: `.otocad` (JSON, full lcdb serialization)
- Import/Export: DXF (via [netDxf](https://github.com/haplokuon/netDxf), MIT) · PDF · print
- Round-trip self-check (View > Round-trip)

---

## OtoCAD Community vs OtoCAD Cloud

This repository is **OtoCAD Community** (MIT, cross-platform desktop), fully usable standalone,
with complete drawing / annotation / optical-drafting capability. No online service required.

It ships scaffolding UI (e.g. ChatPanel) for cloud features that connect to **OtoCAD Cloud**
(commercial SaaS). Without Cloud, those shells stay greyed-out and never block core features.

| Capability | Community | Cloud |
|------|------|------|
| Drawing / editing / selection / view / snap | ✅ | ✅ |
| 24 annotation marks / ISO 10110 / GB/T 13323-2009 | ✅ | ✅ |
| DXF / PDF / print / auto-dimensioning | ✅ | ✅ |
| Manufacturing drawing output (ISO 10110, per element) | ✅ | ✅ |
| Multi-document tabs / autosave & crash recovery | ✅ | ✅ |
| Design import (Zemax) → full-system auto drawing set | — | ✅ |
| AI assistant (optical-drafting Q&A) | 🪧 UI only | ✅ LLM-backed |
| Cross-device sync / cloud storage | — | ✅ |
| Template marketplace | — | ✅ |
| Team collaboration | — | ✅ |
| Factory edition (encryption / process integration) | — | ✅ Enterprise |

OtoCAD Cloud is closed-source SaaS by RayPragma.

---

## Quick start

### Requirements
- **.NET 10 SDK** (10.0.102+)
- Any OS (Windows / macOS / Linux)

### Build & run
```bash
git clone https://github.com/otocad/Otocad-Community.git
cd Otocad-Community
dotnet build src/OtoCAD.Avalonia/OtoCAD.Avalonia.csproj
dotnet run --project src/OtoCAD.Avalonia/OtoCAD.Avalonia.csproj
```

> The main project is named `OtoCAD.Avalonia` for historical reasons (UI built on Avalonia).

### Package (Windows)
```powershell
# Self-contained single-file exe + zip (unzip and run, no .NET install needed)
.\scripts\publish-avalonia.ps1
```
Output lands in `src/BuildOutput/Publish/`.

---

## Architecture

`IGraphicsDraw` keeps the **lcdb** business layer fully decoupled from the rendering backend —
swapping UI frameworks or render engines only requires a new interface implementation, with zero
changes to business code.

```
OtoCAD Community (cross-platform desktop)
        │
Business Core (shared): lcinterface (IGraphicsDraw) · lcdb (Entity/Database/Annotation) · LitMath
        │ HTTPS (optional)
OtoCAD Cloud (optional, closed-source SaaS): AI · sync · templates · collaboration
```

---

## Contributing

Contributions welcome — priorities: cross-platform bugs (Mac/Linux), command completion,
new annotation mark types, GB/T 13323-2009 compliance details.

> ⚠️ **This repo is a sanitized snapshot mirror of the upstream repo.** Each release is
> force-pushed here and git history is reset to a single `Snapshot` commit. PRs merged
> directly here would be overwritten on the next snapshot — we apply accepted changes upstream
> and they return via the next snapshot (contributors credited in [NOTICE.md](NOTICE.md)).

See [CONTRIBUTING.md](CONTRIBUTING.md) for environment / commit conventions / code style / CI.

---

## Privacy

**This open-source build contains no telemetry.** The AI assistant, if opened, connects to the
official `cadask.optic.chat` for Q&A; the request carries only your question text and a random
anonymous device GUID — **no drawings, files, or personal data**. All other CAD features work
fully offline. Point `OTOCAD_BACKEND_URL` at your own backend, or simply don't open the AI panel.

## License

MIT License — see [LICENSE](LICENSE) and [NOTICE.md](NOTICE.md).

---

## Links
- 🌐 **Website**: https://cad.optic.chat
- 📺 **Bilibili**: https://space.bilibili.com/88091818 (tutorials / demos)
- 💬 **Discussions**: https://github.com/otocad/Otocad-Community/discussions
- 🐛 **Issues**: https://github.com/otocad/Otocad-Community/issues
