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

### 2026-02-27: Issue #26 — 1.7B Model Support Architecture Assessment
**Key Finding:** Supporting the 1.7B model variant requires **ZERO breaking changes** to the C# API surface. The architecture is well-designed with config-driven dimensions.

**Model Comparison (0.6B vs 1.7B CustomVoice):**
- **Critical change:** Talker hidden_size: 1024 → **2048** (doubles model size)
- **Critical change:** Talker intermediate_size: 3072 → **6144** (FFN width doubles)
- **Same:** text_hidden_size = 2048 (both models), Code Predictor = 1024 hidden (both)
- **Same:** All layer counts, KV heads, vocab sizes, speaker IDs, tokenizer unchanged
- **Key benefit:** 1.7B supports instruction control (instruct parameter), 0.6B forces instruct=None

**C# Code Impact:**
1. **EmbeddingStore.cs**: Hardcoded dimensions (1024, 2048, 3072) in comments/sizes — reads config.json at runtime via `ModelConfig.talker.hidden_size/text_hidden_size`. **NO CHANGES NEEDED.**
2. **LanguageModel.cs**: Uses hardcoded `1024` buffer sizes in 6 locations (lines 101, 114, 136-146, 164-179). These assume `hidden_size=1024` for 0.6B. For 1.7B, needs `cfg.talker.hidden_size` (2048) instead. **Needs dynamic allocation refactor.**
3. **TtsPipeline.cs**: Generic orchestration — no hardcoded dimensions. **NO CHANGES NEEDED.**
4. **ModelDownloader.cs**: Hardcoded `DefaultRepoId` for 0.6B model. Needs model selection parameter. **Needs enum/config for multi-model support.**

**Memory Impact:** 1.7B ONNX export estimated at ~10-12 GB (2× 0.6B's 5.5 GB). Users need more disk space and RAM.

**Scope:** **MEDIUM effort** (1-2 days). Non-breaking — add new factory overload or options pattern to select model variant. Python export scripts need 1.7B target path.

### 2026-04-02T16:43Z: 1.7B Model Support Recommendation — Ready for Phase 1

📌 Team update (2026-04-02T16:43Z): Complete scope assessment delivered. 1.7B support is MEDIUM effort (1-2 days), non-breaking, high user value (instruction control). Phase 1 MVP targets: (1) LanguageModel.cs dimension-agnostic refactor, (2) ModelDownloader.cs enum-based variant selection, (3) 1.7B ONNX export + HuggingFace upload, (4) unit tests, (5) docs update. Awaiting maintainer approval. — Morpheus
