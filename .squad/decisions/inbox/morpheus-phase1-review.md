# Code Review: Phase 1 — 1.7B Model Support (Issue #26)

**Reviewer:** Morpheus (Lead / Architect)
**Date:** 2026-04-02
**Scope:** All C# pipeline changes (uncommitted), test files, Python export scripts

---

## Verdict: ✅ APPROVE

Phase 1 is architecturally sound, backward-compatible, and ready to merge.

---

## Summary

Three agents delivered a clean, config-driven multi-variant architecture:
- **Neo** eliminated all hardcoded dimensions from C# inference code
- **Trinity** made Python export scripts model-agnostic via `read_model_dims()`
- **Tank** added 47 tests covering variant config, download isolation, and backward compat

Build: **0 errors**, 8 pre-existing warnings (unrelated).
Tests: **88 pass** (78 Core + 10 VoiceCloning), 0 failures.

---

## Detailed Findings

### 1. Architecture — CLEAN ✅

The variant abstraction is well-layered:
- `QwenModelVariant` enum → download/storage routing only
- `config.json` → runtime dimensions (no enum-to-dimension lookup at inference time)
- `.npy` array shapes → ground truth for embedding dimensions

This means future variants (e.g., 3B, quantized) work automatically if config.json and embeddings are consistent. No code changes needed.

`QwenModelVariantConfig` static methods provide download-time values (repo ID, subdirectory). Clean separation from runtime config.

### 2. Backward Compatibility — PERFECT ✅

- `QwenModelVariant.Qwen06B = 0` → `default(QwenModelVariant)` maps to 0.6B
- `ModelDownloader.DefaultRepoId` unchanged → 0.6B repo
- 0.6B uses legacy root directory (no migration needed)
- All existing API signatures preserved with optional `variant` parameter
- `IsModelReady()` alias retained for backward compat

Existing users upgrading to this version will see zero behavioral changes.

### 3. Correctness — GOOD ✅ (with one latent risk)

All hardcoded `1024` dimension constants properly replaced:
- **EmbeddingStore**: Dimensions from `.npy` array shapes (`GetLength(1)`)
- **LanguageModel**: All 9 dimension fields from `config.json` via `EmbeddingStore.Config`
- **TtsPipeline/ModelDownloader**: Variant-aware resolution via `ResolveForVariant()`

Remaining `1024` references are only in comments and `QwenModelVariantConfig` static data — correct.

**⚠️ Latent Risk — CP Input Dimension (LanguageModel.cs:187-192):**

For the Code Predictor input, the C# code copies `_cpHiddenSize` (1024) elements from `hiddenStates` (dim `_hiddenSize`). For 0.6B where both are 1024, this is correct. For 1.7B where `_hiddenSize=2048` and `_cpHiddenSize=1024`, the code truncates the hidden state to 1024 elements.

The same pattern exists in Python export scripts: `dims["cp_hidden"]` (= `code_predictor_config.hidden_size` = 1024) is used as the CP ONNX input dimension, while the actual talker hidden state is 2048-dim for 1.7B.

Whether this is correct depends on `small_to_mtp_projection.in_features` in the 1.7B model. If the projection expects 2048-dim input, both Python export and C# inference will need adjustment.

**Impact:** NOT a current bug (0.6B works correctly). Will surface during Phase 2 when 1.7B ONNX models are actually exported. The export will either succeed (confirming the approach) or fail with a dimension mismatch (revealing the fix needed).

**Recommendation:** Track as a Phase 2 validation item. When Trinity exports 1.7B, verify `small_to_mtp_projection.in_features` matches `cp_hidden`.

### 4. Test Coverage — THOROUGH ✅

Tank's 47 new tests cover:
- ✅ Enum values and default behavior (Qwen06B=0)
- ✅ Hidden/intermediate size mappings for all variants
- ✅ HuggingFace repo ID correctness
- ✅ Directory isolation between variants
- ✅ Invalid variant throws `ArgumentOutOfRangeException`
- ✅ Backward compat: existing APIs without variant param
- ✅ `GetAllVariants()` completeness
- ✅ Download with cancellation (tests API shape, not network)

**Minor gap:** No cross-validation test that config.json values match `QwenModelVariantConfig` static values. Acceptable — would require real model files.

### 5. Security/Safety — NO ISSUES ✅

- Download paths properly scoped under `DefaultModelDir`
- 1.7B subdirectory is nested under shared root — no path traversal risk
- HuggingFace URLs use public repos, no credentials
- File markers in tests properly cleaned up via `IDisposable`

### 6. Python Changes — CLEAN ✅

- `read_model_dims()` extracts all dimensions from config
- All export functions accept `dims` dict — no hardcoded constants
- `download_models.py` extended with 1.7B variants and `everything` option
- CLI `--model-dir` arg enables variant selection without code changes

---

## Action Items

| Priority | Item | Owner | Status |
|----------|------|-------|--------|
| Phase 2 | Validate CP input dimension for 1.7B (`small_to_mtp_projection.in_features`) | Trinity | Pending — during 1.7B ONNX export |
| Low | Add `hidden_size` to `CodePredictorConfig` C# class for completeness | Neo | Optional |

---

## Decision

**APPROVE for merge.** Architecture is clean, backward compatible, and the one latent risk is a known Phase 2 validation item that cannot be resolved without 1.7B model weights.
