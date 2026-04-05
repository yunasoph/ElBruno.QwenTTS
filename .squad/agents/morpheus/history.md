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

### 2026-04-02: Phase 1 Code Review — APPROVED

📌 Review (Morpheus): Full Phase 1 implementation reviewed and approved. Architecture is clean — QwenModelVariant enum handles download/storage, config.json handles runtime dimensions, .npy shapes provide ground truth. Zero hardcoded constants in inference code. Backward compatibility perfect (Qwen06B=0, legacy paths preserved). Build clean (0 errors), 88 tests pass. One latent risk identified: CP input dimension for 1.7B — `_cpHiddenSize` (1024) is used to truncate `_hiddenSize` (2048) hidden states at LanguageModel.cs:187-192. Whether truncation vs projection is correct depends on `small_to_mtp_projection.in_features` in the 1.7B model weights, which can only be verified during Phase 2 ONNX export. Not a current bug — 0.6B is unaffected. Phase 2 action: Trinity must validate CP input dimension when exporting 1.7B.
### 2026-02-28: Issue #22 Triage — Security/Performance/CI Audit
**By:** Morpheus
**What:** Assessed Issue #22 (lessons from elbruno.localembeddings v1.1.0) and created 3-phase roadmap: Phase 1 (NOW: Security — 4 items), Phase 2 (NEXT: Performance — 4 items), Phase 3 (LATER: CI — 2 items). Triaged work breakdown in `.squad/decisions/inbox/morpheus-issue-22-triage.md`. Key findings: (1) Path traversal risk is low (hardcoded repIds); (2) Input validation is **blocking** for voice cloning release (SEC-1); (3) Top-K sampling optimization can yield 5–10% latency gain (PERF-1); (4) CI validation for git tags is low-priority (manual tag creation).
**Why:** Security > performance > CI in priority. SEC-1 (text input validation) unblocks VoiceCloning Web feature. Neo owns all Phase 1 security fixes with Morpheus code review. Phase 2/3 deferred to prevent scope creep.

### 2026-02-28: Neo's Queue
📌 **Team update:** Issue #22 triage completed. Created 3-phase roadmap for security/performance/CI audit. Phase 1 (security) is blocking for voice cloning release; Phase 2/3 deferred. SEC-1 (input validation) now in Neo's queue. — Morpheus

### 2026-02-28: Phase 2 & 3 Merge Review — All Branches Complete
**By:** Morpheus (Lead)
**What:** Reviewed and merged all Phase 2 (Performance) and Phase 3 (CI/Linux) branches to main:
  - **PERF-1 (squad/perf-1-topk-heap):** Top-K heap speaker search — O(n log k) optimization with SIMD acceleration. Clean implementation with 21 tests. Min-heap pattern is textbook-correct.
  - **PERF-2 (squad/perf-2-arraypool):** ArrayPool adoption in LanguageModel inference — reduces GC pressure during autoregressive generation. Proper rent/return lifecycle, no leaks.
  - **PERF-3 (squad/perf-3-benchmarks):** BenchmarkDotNet baseline infrastructure — enables continuous performance validation. Good test coverage for benchmark execution.
  - **Phase 3 (squad/phase-3-ci-linux):** CI/Linux workflow hardening — version detection from GitHub releases, manual dispatch, csproj fallback. Linux validation added to build matrix.
**Merge Strategy:** Squash merge with conventional commits (fix(perf), fix(build), fix(ci)) referencing #22. Each commit includes Co-authored-by trailer per team convention.
**Validation:** All 4 branches validated independently (60 tests, 0 warnings, 0 errors). Final merge validation on main confirms no integration issues.
**PR Review Patterns:** Validated build output, test metrics, implementation quality, and architecture decisions for each branch. Code quality high across all work items.
**Why:** Systematic review ensures production readiness. Squash commits maintain clean git history. All work traceable to Issue #22.
**Closure Decision:** Issue #22 closed after all work merged. Phase 1 (SEC-1/2/3/4) + Phase 2 (PERF-1/2/3) + Phase 3 (CI) complete. Total 7 work items resolved.
**Learnings:** Squash merge strategy works well for multi-agent branches. Review gates (build + test validation) catch integration issues early. Decision memo consolidation provides audit trail for all architectural choices.
