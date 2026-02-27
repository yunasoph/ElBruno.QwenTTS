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
