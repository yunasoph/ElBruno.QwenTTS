# Orchestration Log: Trinity 1.7B ONNX Export (2026-04-02T18:15Z)

**Agent:** Trinity (ML Engineer)
**Mode:** Background
**Duration:** ~50 minutes (2963 seconds)
**Outcome:** SUCCESS

## Spawn Request

Full 1.7B ONNX model export pipeline — download PyTorch model, export LM/embeddings/vocoder to ONNX on GPU (NVIDIA A10 24GB), extract tokenizer, upload to HuggingFace.

## Work Completed

1. ✅ Downloaded 1.7B PyTorch model from HuggingFace
2. ✅ Fixed vmap masking bug in export_lm.py (attention mask broadcast)
3. ✅ Fixed Code Predictor dimensions (CP hidden=1024, talker hidden=2048)
4. ✅ Fixed data consolidation in embeddings export (proper tensor movement to CPU)
5. ✅ Exported all ONNX models to python/onnx_1.7b/ (~12.5 GB total):
   - Vocoder, Code Predictor (31 variants), Talker LM with KV-cache
   - TextTokenizer embeddings (all codec tokens)
   - Config files and speaker_ids.json
6. ✅ Extracted tokenizer artifacts (vocab.json, merges.txt, validation cases)
7. ✅ Uploaded all models + artifacts to elbruno/Qwen3-TTS-12Hz-1.7B-CustomVoice-ONNX

## Bugs Fixed During Export

| Bug | Root Cause | Fix |
|-----|-----------|-----|
| vmap masking | Attention mask had wrong shape for broadcasting | Added squeeze(-1) before expand |
| CP dimensions | Code Predictor hidden size (1024) vs Talker (2048) mismatch | Export only copies 1024 elements from talker hidden state |
| Data consolidation | GPU tensors not moved to CPU before .numpy() | Added .cpu() before detach().numpy() in consolidate_embeddings() |

## Files Produced

- `python/onnx_1.7b/` — Full ONNX model set (~12.5 GB total)
- `python/export_lm.py` — Updated with bug fixes (vmap, consolidation)
- `python/reexport_lm_novmap.py` — Updated with bug fixes
- `python/reexport_base_novmap.py` — Updated with bug fixes

## Commit

**Commit Hash:** 602ba29
**Message:** "fix: 1.7B ONNX export pipeline — vmap masking, CP dims, data consolidation"

## HuggingFace Upload

**Model Repository:** elbruno/Qwen3-TTS-12Hz-1.7B-CustomVoice-ONNX
**Files Uploaded:**
- 2 vocoder models (ONNX + .data files)
- 31 Code Predictor models (per generation step)
- 1 Talker LM model with KV-cache sessions
- TextTokenizer embeddings (codec tokens)
- Config files, speaker_ids.json, tokenizer artifacts

## Requested By

Bruno Capuano

## Notes

- All models tested against Python reference implementation before upload
- GPU utilization: NVIDIA A10 24GB (peak ~22GB during export)
- Export scripts now config-driven (no hardcoded dimensions) — supports future variants
- 1.7B model ready for C# integration (Phase 2)
