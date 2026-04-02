# Final Review: Issue #26 — 1.7B Model Support

**Reviewer:** Morpheus (Lead / Architect)
**Date:** 2026-04-02
**Verdict:** ❌ NEEDS_WORK

---

## Summary

The architecture and API design are excellent. The `QwenModelVariant` enum, config-driven dimensions, variant-aware download, instruct gating, and consumer app integration are all well-designed and backward-compatible. However, the implementation **cannot ship** in its current state: **LanguageModel.cs has 16 unresolved merge conflict markers** that break the build entirely (55 compiler errors). This is a hard blocker.

Beyond the build break, I identified one latent runtime bug in the 1.7B code path and several documentation gaps.

---

## 🔴 CRITICAL — Build Blocker

### 1. LanguageModel.cs: 16 Unresolved Merge Conflict Markers

**File:** `src/ElBruno.QwenTTS.Core/Models/LanguageModel.cs`
**Status:** `UU` (unmerged) — `dotnet build` fails with 55 CS8300 errors.

Two versions are interleaved:
- **HEAD** (main branch): Uses `ArrayPool<float>` for memory efficiency with **hardcoded `1024`** dimension constants
- **29e4183** (1.7B feature): Uses **config-driven `_hiddenSize`/`_cpHiddenSize`** with simple `new float[]` allocations

The correct resolution must combine both: **config-driven dimensions** (mandatory for 1.7B) **+ ArrayPool optimization** (desirable for performance). Key conflicts at lines: 152-159, 164-175, 229-249, 258-268, 276-462, 660-675, 736-748.

**Also:** `TtsPipeline.cs` shows `UU` git status (unmerged) but has no conflict markers in content — just needs `git add` to clear the status.

**Action:** Neo must resolve all merge conflicts, prioritizing config-driven dimensions over hardcoded 1024 constants. ArrayPool optimization can be preserved where it doesn't conflict with dynamic sizing.

---

## 🟠 HIGH — Runtime Bug (1.7B Code Path)

### 2. IndexOutOfRangeException in "Build next Talker input" Loop

**File:** `src/ElBruno.QwenTTS.Core/Models/LanguageModel.cs` (1.7B branch side, near line 416)

```csharp
// Bug: loop bound is _hiddenSize (2048 for 1.7B) but cpEmbedBuf is _cpHiddenSize (1024)
for (int g = 1; g < 16; g++)
{
    _embeddings.CpCodecEmbedding(g - 1, (int)codes[g], cpEmbedBuf);
    for (int i = 0; i < _hiddenSize; i++)  // ← iterates 2048 times
        nextInputBuf[i] += cpEmbedBuf[i];  // ← cpEmbedBuf only has 1024 elements!
}
```

For **0.6B**: `_hiddenSize` = `_cpHiddenSize` = 1024 → no issue.
For **1.7B**: `_hiddenSize` = 2048, `_cpHiddenSize` = 1024 → **crash on first inference step.**

**Fix:** Change loop bound to `_cpHiddenSize`:
```csharp
for (int i = 0; i < _cpHiddenSize; i++)
    nextInputBuf[i] += cpEmbedBuf[i];
```

This effectively adds CP codec embeddings to the first 1024 dimensions of the 2048-dim talker input, which is architecturally consistent with how the ONNX models were exported (Trinity confirmed "correctly extract 1024 elements from 2048-dim talker hidden state").

**Note:** The HEAD version (hardcoded `1024`) accidentally avoids this bug. The config-driven version introduces it by using `_hiddenSize` instead of `_cpHiddenSize`.

---

## 🟡 MEDIUM — Documentation Gaps

### 3. README Missing 1.7B HuggingFace Link

Lines 12-14 list pre-exported models but only link 0.6B repos. Add:
```
[**elbruno/Qwen3-TTS-12Hz-1.7B-CustomVoice-ONNX**](https://huggingface.co/elbruno/Qwen3-TTS-12Hz-1.7B-CustomVoice-ONNX) (1.7B preset voices + instruct)
```

Also missing from References section (line 201).

### 4. README Examples Use --instruct Without --variant 1.7b

Lines 108-109 show:
```
dotnet run ... --instruct "speak with excitement" --output excited.wav
```
Without `--variant 1.7b`, this hits 0.6B (default) and triggers a warning. Add `--variant 1.7b` to instruct examples.

### 5. README Requirements: Disk Space for 1.7B

Line 175 says "~5.5 GB disk space" — should note "~10 GB for the 1.7B model."

### 6. Blazor UI Hardcodes "~5.5 GB" Download Size

- `Home.razor` line 37: "The CustomVoice model (~5.5 GB)" — not variant-aware
- `Home.razor` line 56: "Preparing to download model from HuggingFace (~5.5 GB)..."
- `Settings.razor` line 62: "⬇️ Download & Load (~5.5 GB)"

These should reflect the actual model size based on `Tts.ModelVariant`.

### 7. CHANGELOG Typo

Line 31: "contractfor" → "contract for"

---

## 🟢 LOW — Code Quality Observations

### 8. ParseVariant Duplication

`ParseVariant(string?)` is copy-pasted in 3 files:
- `src/ElBruno.QwenTTS/Program.cs:62`
- `src/ElBruno.QwenTTS.FileReader/Program.cs:163`
- `src/ElBruno.QwenTTS.Web/Services/TtsPipelineService.cs:65`

Consider adding a `QwenModelVariantConfig.Parse(string?)` method to centralize this. Not a blocker.

### 9. HEAD Version: Hardcoded Codec Suppression Range

In `SampleToken()` on the HEAD side, line 684: `for (int i = vocabSize - 1024; ...)` assumes a fixed codec range. The 1.7B branch's approach (using `cpVocabSize` from config) is correct. Ensure the merged version uses `cfg.code_predictor.vocab_size`.

---

## ✅ What's Good

1. **QwenModelVariant enum** — Clean, minimal, backward-compatible (Qwen06B=0 is default).
2. **Config-driven dimensions** — EmbeddingStore and LanguageModel derive all sizes from loaded .npy shapes and config.json. Zero hardcoded constants in the 1.7B branch.
3. **HuggingFace repo IDs** — Correct: `elbruno/Qwen3-TTS-12Hz-0.6B-CustomVoice-ONNX` and `elbruno/Qwen3-TTS-12Hz-1.7B-CustomVoice-ONNX`.
4. **Backward compatibility** — DefaultRepoId unchanged, Qwen06B=0 is default everywhere, 0.6B uses legacy root directory.
5. **Instruct gating** — Warning (not exception) for unsupported variants, `SupportsInstruct()` as single source of truth.
6. **ModelDownloader.ResolveForVariant()** — Clean API for variant-specific directory and repo resolution.
7. **Test coverage** — 47 new tests, all well-structured, testing backward compat, variant separation, and edge cases.
8. **Consumer apps** — CLI, FileReader, and Blazor all properly accept variant parameter.
9. **Python exports** — Config-driven `read_model_dims()` replaces hardcoded constants.

---

## Action Items (Priority Order)

| # | Priority | Owner | Action |
|---|----------|-------|--------|
| 1 | 🔴 BLOCKER | Neo | Resolve all 16 merge conflicts in LanguageModel.cs — config-driven dimensions required, ArrayPool optional |
| 2 | 🔴 BLOCKER | Neo | Fix `_hiddenSize` → `_cpHiddenSize` loop bound bug in "Build next Talker input" |
| 3 | 🔴 BLOCKER | Neo | Run `git add` on TtsPipeline.cs to clear UU status |
| 4 | 🟡 MEDIUM | Neo | Add 1.7B HuggingFace link to README (intro + References) |
| 5 | 🟡 MEDIUM | Neo | Add `--variant 1.7b` to README instruct examples |
| 6 | 🟡 MEDIUM | Neo | Update README requirements: ~10 GB for 1.7B |
| 7 | 🟢 LOW | Neo | Make Blazor download size variant-aware |
| 8 | 🟢 LOW | Cypher | Fix "contractfor" typo in CHANGELOG |
| 9 | 🟢 FUTURE | Neo | Centralize ParseVariant into QwenModelVariantConfig.Parse() |

**After items 1-3 are resolved:** `dotnet build && dotnet test` must pass (88+ tests, 0 errors). Then items 4-6 before closing #26.

---

**Decision: NEEDS_WORK — Cannot close #26 until merge conflicts are resolved and build is green.**
