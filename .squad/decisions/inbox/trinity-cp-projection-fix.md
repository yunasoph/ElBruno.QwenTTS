# Decision: Remove CP Projection from ONNX Graph (Issue #27)

**Date:** 2026-04-03
**Author:** Trinity (ML Engineer)
**Status:** Implemented & deployed

## Context

GitHub Issue #27: "1.7b works, but the text was trimmed" — only first ~2 words generated with 1.7B model.

The `code_predictor.onnx` for the 1.7B model had `small_to_mtp_projection` (Linear: 2048→1024) baked inside the ONNX graph. The model's `inputs_embeds` expected 2048-dim input. During CP inference, the prefill step receives talker hidden state (2048-dim), but decode steps feed 1024-dim codec embeddings — shape mismatch breaks generation after the first code group.

## Decision

1. **Remove the projection from the ONNX graph.** `CodePredictorWrapper.forward()` no longer applies `self.projection(inputs_embeds)`. The ONNX model now expects `cp_hidden`-dim (1024) input directly.

2. **Export projection weights as separate NPY files.** `export_embeddings.py` saves `cp_projection_weight.npy` (1024, 2048) and `cp_projection_bias.npy` (1024,) when the model has `small_to_mtp_projection` (1.7B only).

3. **C# applies projection externally.** The C# `LanguageModel` or `EmbeddingStore` must apply the projection only for the CP prefill step (where input is the 2048-dim talker hidden state). For decode steps (groups 2-15), codec embeddings are already 1024-dim and skip the projection.

## Impact

- **Python scripts modified:** `export_lm.py`, `reexport_lm_novmap.py`, `reexport_base_novmap.py`, `export_embeddings.py`
- **HuggingFace artifacts updated:** `code_predictor.onnx`, `code_predictor.onnx.data`, `embeddings/cp_projection_weight.npy`, `embeddings/cp_projection_bias.npy` on `elbruno/Qwen3-TTS-12Hz-1.7B-CustomVoice-ONNX`
- **C# changes required:** Morpheus/Neo need to load the projection NPY files and apply `y = x @ W^T + b` before the CP prefill step only. The `ModelDownloader` file manifest needs updating to include the two new NPY files for 1.7B.
- **0.6B unaffected:** No projection exists (talker_hidden == cp_hidden == 1024). `hasattr` guard safely skips export. The 0.6B ONNX model remains identical.

## Key Technical Detail

Storing an `nn.Module` submodule (even unused in `forward()`) as an attribute of a wrapper class causes `torch.onnx.export` to include its parameters in the ONNX graph as initializers. This was silently doubling the data file size. Fix: never store unused submodules on ONNX wrapper classes.
