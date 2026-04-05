# Tank — Test Coverage Audit for 1.7B Model Support (Issue #26)

**Date:** 2026-04-02
**Auditor:** Tank (Tester/QA)
**Scope:** All test files related to 1.7B variant support
**Baseline:** 88 tests passing → **122 tests passing** (+34 new tests)

---

## Audit Summary

The existing 47 variant tests (ModelVariantTests, ModelVariantDownloaderTests, TtsPipelineVariantTests) covered config dimensions, download isolation, and backward compatibility well. However, several 1.7B-specific behaviors were untested. This audit identified **7 coverage gaps** and added **34 new tests** across 3 new test files.

## Gaps Found & Tests Added

### Gap 1: SupportsInstruct() Not Tested (CRITICAL)
**Risk:** The key 1.7B differentiator — instruction control — had zero test coverage.
**Added:** `SupportsInstructTests.cs` — 6 tests
- `SupportsInstruct_06B_ReturnsFalse` / `SupportsInstruct_17B_ReturnsTrue`
- `SupportsInstruct_DefaultVariant_ReturnsFalse`
- `SupportsInstruct_InvalidVariant_ReturnsFalse` (verifies graceful `false`, not exception)
- `SupportsInstruct_AllVariants_MatchExpected` (parameterized)
- Structural invariant: `SupportsInstruct` only true when `hidden_size >= 2048`

### Gap 2: GetDefaultModelDir Invalid Variant Not Tested
**Risk:** All other config methods had invalid-variant tests except `GetDefaultModelDir`.
**Added:** `ModelVariantEdgeCaseTests.GetDefaultModelDir_InvalidVariant_Throws`

### Gap 3: ModelDownloader.ResolveForVariant() Not Tested (HIGH)
**Risk:** Critical resolution logic used by `CreateAsync()` had zero coverage.
**Added:** 6 tests in `ModelVariantEdgeCaseTests.cs`:
- Null overrides resolve to variant defaults for both 0.6B and 1.7B
- Custom modelDir overrides but repo stays default
- Custom repoId overrides but dir stays default
- Both custom overrides work together
- 0.6B resolution matches legacy paths exactly

### Gap 4: QwenTtsOptions.InstructText Not Tested
**Risk:** New property for default instruction text had no coverage.
**Added:** 3 tests: default is null, can be set, can be empty string

### Gap 5: QwenTextToSpeechClient 1.7B Constructor Not Tested
**Risk:** No validation that the client accepts `QwenModelVariant.Qwen17B`.
**Added:** 4 tests in `VariantClientIntegrationTests.cs`:
- Default constructor uses 0.6B
- 1.7B variant constructs successfully
- 1.7B with modelDir constructs successfully
- Disposed 1.7B client throws ObjectDisposedException

### Gap 6: DI Registration with 1.7B Variant Not Tested
**Risk:** AddQwenTextToSpeechClient and AddQwenTts with variant config not validated.
**Added:** 2 tests: DI with `Qwen17B` + `InstructText` for both `ITextToSpeechClient` and `ITtsPipeline`

### Gap 7: Enum Uniqueness Not Tested
**Risk:** Duplicate enum values or config mappings could cause silent bugs.
**Added:** 4 tests: unique enum values, unique repo IDs, unique sub-dirs, unique default model dirs

### Gap 8: CreateAsync Variant Parameter Flow
**Added:** 3 tests: CreateAsync with null modelDir resolves 1.7B/0.6B defaults, detailed progress overload accepts 1.7B

## Remaining Gaps (Cannot Test Without Models)

These require actual ONNX model files and are marked for future integration testing:

1. **Instruct gating in TtsPipeline.SynthesizeAsync** — The warning/nullify logic (lines 49-55 of TtsPipeline.cs) when passing instruct to 0.6B. Requires a loaded pipeline.
2. **TtsPipeline.ModelVariant property** — Verifying the constructed pipeline returns its variant. Requires model files.
3. **Config.json parsing for 1.7B dimensions** — EmbeddingStore reading `hidden_size=2048` from config. Requires actual .npy and config files.
4. **Concurrent CreateAsync with different variants** — Thread-safety of variant-specific downloads.

## Regression Risk Assessment

| Area | Risk | Status |
|------|------|--------|
| Default variant backward compat | Low | ✅ Well tested (enum=0, legacy paths, API unchanged) |
| Existing API signatures | None | ✅ All optional params, no breaking changes |
| ModelDownloader.DefaultRepoId | None | ✅ Unchanged, tested |
| 0.6B behavior unaffected | Low | ✅ Default paths, `default(QwenModelVariant)` → 0.6B |

## Test Quality Assessment

**Strengths:**
- Consistent naming: `Method_Condition_ExpectedResult` pattern throughout
- Good use of Theory/InlineData for parameterized tests
- Proper IDisposable cleanup on all fixture classes
- Cancellation-based testing pattern avoids 5GB downloads

**Minor Observations:**
- Some test files have overlapping assertions (e.g., enum default tested in 3 places). Acceptable — defense in depth.
- TtsPipelineVariantTests duplicates 2 tests from TtsPipelineFactoryTests. Low impact.

## New Test Files

| File | Tests | Purpose |
|------|-------|---------|
| `SupportsInstructTests.cs` | 6 | Instruction control gating |
| `ModelVariantEdgeCaseTests.cs` | 18 | Edge cases, ResolveForVariant, QwenTtsOptions |
| `VariantClientIntegrationTests.cs` | 10 | Client/DI variant integration |
| **Total new** | **34** | |

## Final Count

| Suite | Before | After |
|-------|--------|-------|
| Core.Tests | 78 | 112 |
| VoiceCloning.Tests | 10 | 10 |
| **Total** | **88** | **122** |
