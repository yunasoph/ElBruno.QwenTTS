### Instruction Control API — Variant-Aware Design
**By:** Neo (.NET Developer)
**Date:** 2026-04-02
**Context:** Issue #26 Phase 2 — instruction control support for 1.7B model.

**Decision:** Instruction control is variant-gated at the pipeline level, not the tokenizer level. `TtsPipeline.SynthesizeAsync()` checks `QwenModelVariantConfig.SupportsInstruct(_variant)` and nullifies instruct text with a warning for unsupported variants (0.6B). The tokenizer's `BuildCustomVoicePrompt` continues to accept instruct unconditionally — the gating happens one layer above.

**Rationale:**
1. The tokenizer is a low-level component that shouldn't enforce model-variant policy.
2. The pipeline is the natural boundary where user intent meets model capability.
3. Warning (not exception) for unsupported instruct preserves backward compat and avoids breaking existing workflows that happen to pass instruct to 0.6B.
4. `QwenModelVariantConfig.SupportsInstruct()` is the single source of truth for instruction support — consumer apps (Web, CLI, FileReader) all use it for UI/UX decisions.

**Impact:** Non-breaking. All 88 tests pass. Default behavior unchanged.
