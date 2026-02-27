# Project Context

- **Owner:** Bruno Capuano
- **Project:** Qwen3-TTS → ONNX → C# .NET 10 console app for local voice generation
- **Stack:** Python (PyTorch, transformers, ONNX), C# (.NET 10, ONNX Runtime), Qwen3-TTS
- **Created:** 2026-02-21T15:38Z

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-02-21: GPU Handoff
Project published to https://github.com/elbruno/qwen-labs-cs (private). 8/12 tasks complete. All scaffolding done — both Python scripts and C# code. Remaining work is execution (run exports) and implementation (LM inference in C#). Key risk: export_lm.py hasn't been validated against real model weights yet.

### 2026-02-22: Post-Issue-21 Architecture Review
**ITextToSpeechClient Integration:** New MEAI-aligned abstraction is architecturally sound — `QwenTextToSpeechClient` provides thread-safe lazy init (SemaphoreSlim), clean memory-based API, and streaming support. 12 new unit tests (43 total passing). Clean separation: ITextToSpeechClient handles safety/concurrency, TtsPipeline handles ONNX inference.

**Solution Structure:** 7 projects in clean layered architecture — Core (NuGet library), 3 CLI apps (console/FileReader/Web), VoiceCloning package with separate tests. All cross-cutting concerns (model download, GPU options, DI) centralized in Core.Pipeline.

**Documentation Gap:** `ITextToSpeechClient` not documented in `docs/core-library.md`. README mentions voice cloning + web app but API reference needs update for production client pattern. CHANGELOG correctly documents additions under "Unreleased".

**Technical Debt Observed:** (1) `SynthesizeToMemoryAsync` uses temp file I/O — no in-memory WAV generation API in TtsPipeline. (2) `SynthesizeStreamingAsync` is single-chunk (full audio at once) — not true streaming inference. Both are deferred optimization opportunities, not blockers.

**Next Priorities:** (1) Document ITextToSpeechClient in core-library.md, (2) Add usage examples to README Quick Start section, (3) Consider tagging v1.1.0 release after docs update.

### 2026-02-27: Code Review Results — ITextToSpeechClient (Neo's Assessment)
📌 Team update (2026-02-27T16:59:44Z): Production-ready implementation with minor cosmetic observations (ConfigureAwait(false), CancellationToken) noted for future polish pass. Code ships as-is. — Neo
