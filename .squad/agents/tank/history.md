# Project Context

- **Owner:** Bruno Capuano
- **Project:** Qwen3-TTS → ONNX → C# .NET 10 console app for local voice generation
- **Stack:** Python (PyTorch, transformers, ONNX), C# (.NET 10, ONNX Runtime), Qwen3-TTS
- **Created:** 2026-02-21T15:38Z

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-02-22: Test Coverage Review — ITextToSpeechClient & Core Library

**Status:** All 41 tests pass (29 Core.Tests + 10 VoiceCloning.Tests + 2 TtsPipelineFactoryTests)

**QwenTextToSpeechClient Coverage (12 new tests):**
- ✅ Constructor validation (defaults, custom values, null handling)
- ✅ Dispose patterns (multiple calls, ObjectDisposedException after disposal)
- ✅ Input validation (null, empty, whitespace rejection)
- ✅ Type contracts (TextToSpeechOptions, TextToSpeechResponse, TextToSpeechStreamingUpdate defaults)
- ✅ DI registration (singleton lifetime, configuration callbacks)

**Test Health:**
- Naming: Consistent AAA pattern (Arrange-Act-Assert), descriptive method names
- Assertions: Strong use of xUnit patterns (Assert.ThrowsAsync, Assert.Single, ObjectDisposedException.ThrowIf)
- Resource cleanup: Proper IDisposable implementation on all test fixtures
- Mocking: Minimal — tests favor real objects and cancellation-based short-circuits (good for integration readiness)

**Coverage Gaps Identified:**

1. **Thread-Safety (CRITICAL):** No tests verify concurrent access to SemaphoreSlim in EnsureInitializedAsync. Need:
   - Test multiple concurrent calls to SynthesizeToMemoryAsync before pipeline initialization
   - Verify only one TtsPipeline.CreateAsync is invoked (not N concurrent attempts)
   - Test concurrent calls after initialization completes (should not block)

2. **Error Handling:** No tests for TtsPipeline.SynthesizeAsync failures. Need:
   - Test behavior when ONNX inference fails (corrupt models, OOM, unsupported input)
   - Verify temp file cleanup happens even when synthesis throws
   - Test File.Delete failures in finally block (permissions, locked files)

3. **Streaming Edge Cases:** SynthesizeStreamingAsync has minimal coverage. Need:
   - Test cancellation mid-stream (between SessionOpen and AudioChunk)
   - Verify SessionClose always emitted even on exception
   - Test with zero-length audio output (edge case: empty text after tokenization)
   - Test with very long input (>1000 chars) — does it stream chunks or buffer fully?

4. **DI Integration:** Current DI tests verify registration but not resolution. Need:
   - Test resolving ITextToSpeechClient from ServiceProvider and calling methods
   - Verify singleton behavior (same instance across multiple resolves)
   - Test with QwenTtsOptions that trigger different ExecutionProvider paths (CPU, CUDA, DirectML)

5. **Temp File Lifecycle:** No validation that temp files are actually deleted. Need:
   - Test that verifies temp file count before/after synthesis
   - Test behavior when temp directory is full or read-only
   - Verify unique file names prevent collision across concurrent calls

6. **ModelDownloader Integration:** TtsPipelineFactoryTests uses cancellation to abort before download. Need:
   - Test with a mock/stub HTTP client to verify download flow without 5GB transfer
   - Test partial download recovery (resume vs restart)
   - Test progress reporting accuracy (byte-level progress validation)

**Recommendations:**
- **Priority 1:** Add thread-safety tests (use Task.WhenAll with 10+ concurrent calls)
- **Priority 2:** Add error path tests (mock ONNX failures, file I/O failures)
- **Priority 3:** Enhance streaming tests (cancellation, lifecycle guarantees)
- **Priority 4:** Add full DI resolution tests (end-to-end with ServiceProvider)

**Overall Assessment:** Test suite is well-structured with strong fundamentals (naming, cleanup, basic contracts). The 12 new tests cover happy-path scenarios and basic validation. However, production-critical paths — concurrency, error handling, resource cleanup under failure — are under-tested. No performance benchmarks or integration tests exist yet.

### 2026-02-27: Warm-up Review — Tank's Coverage Assessment
📌 Team update (2026-02-27T16:59:44Z): Test coverage review complete. All 41 tests pass. Coverage gaps identified in concurrency, error paths, streaming lifecycle. Recommend Issue #22 for post-milestone enhancement. — Tank

### 2026-04-02: 1.7B Multi-Model Variant Tests (Issue #26)

**Status:** All 88 tests pass (78 Core + 10 VoiceCloning). 47 new tests added (up from 31 Core tests).

**New Files Created:**
- `src/ElBruno.QwenTTS.Core/Pipeline/QwenModelVariant.cs` — `QwenModelVariant` enum (Qwen06B=0, Qwen17B=1) + `QwenModelVariantConfig` static class (hidden_size, intermediate_size, repoId, modelSubDir mappings)
- `src/ElBruno.QwenTTS.Core.Tests/ModelVariantTests.cs` — 29 tests for variant config (dimensions, repo IDs, default variant, enum values, invalid variant handling)
- `src/ElBruno.QwenTTS.Core.Tests/ModelVariantDownloaderTests.cs` — 10 tests for download/directory isolation per variant + backward compatibility
- `src/ElBruno.QwenTTS.Core.Tests/TtsPipelineVariantTests.cs` — 8 tests for CreateAsync backward compatibility with variants

**Key Design Decisions:**
- `QwenModelVariant.Qwen06B = 0` ensures `default(QwenModelVariant)` maps to 0.6B (backward compat)
- `QwenModelVariantConfig.Default` is a const, not computed, for clarity
- Variant directories are subdirs under shared cache root (e.g., `DefaultModelDir/0.6B/`, `DefaultModelDir/1.7B/`)
- All config lookups use switch expressions with `ArgumentOutOfRangeException` for unknown variants
- intermediate_size is always 3× hidden_size (structural invariant tested)

**TDD Approach:** Created the enum + config types in the Core library as the API contract, then wrote tests against it. Neo can integrate these types into ModelDownloader, TtsPipeline, and EmbeddingStore/LanguageModel during the refactor.

📌 Team update (2026-04-02T1719): Phase 1 complete — multi-variant support (0.6B and 1.7B) implemented across C#, Python, and tests. Orchestration logs and decisions merged. Non-breaking change, 88 tests pass. — Scribe

### 2026-04-02: Test Coverage Audit — 1.7B Model Support (Issue #26)

**Status:** All 122 tests pass (112 Core + 10 VoiceCloning). 34 new tests added (up from 88).

**Coverage Gaps Found & Fixed:**
1. **SupportsInstruct() completely untested** — the key 1.7B feature had zero coverage. Added 6 tests covering true/false per variant, default variant, invalid variant graceful handling, and structural invariant (instruct requires hidden_size ≥ 2048).
2. **ModelDownloader.ResolveForVariant() untested** — critical path resolution used by CreateAsync had no tests. Added 6 tests for null overrides, custom overrides, and legacy path matching.
3. **GetDefaultModelDir invalid variant** — was the only config method without an invalid-variant test. Fixed.
4. **QwenTtsOptions.InstructText** — new property with no coverage. Added 3 tests.
5. **QwenTextToSpeechClient with 1.7B variant** — constructor never tested with non-default variant. Added 4 tests.
6. **DI registration with variant** — AddQwenTextToSpeechClient/AddQwenTts with 1.7B config untested. Added 2 tests.
7. **Enum/config uniqueness** — no tests verified all variants have unique values across all mappings. Added 4 tests.

**Remaining Gaps (require model files):**
- Instruct gating in SynthesizeAsync (warning + nullify on 0.6B) — needs loaded pipeline
- TtsPipeline.ModelVariant property validation — needs model files
- Config.json parsing for 1.7B dimensions (hidden_size=2048) — needs actual .npy + config
- Concurrent CreateAsync with different variants — thread-safety

**Key Insight:** SupportsInstruct uses `_ => false` (not throw) for invalid variants, which is intentionally different from all other config methods that throw ArgumentOutOfRangeException. This is correct — graceful degradation for feature gating vs fail-fast for required config. Tested both behaviors explicitly.

📌 Team update (2026-04-02): Coverage audit complete for #26. Added 34 tests, 7 gaps filled. 122 tests passing. Remaining gaps require model files for integration testing. — Tank
