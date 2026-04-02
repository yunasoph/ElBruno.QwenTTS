# Decision: Config-Driven ONNX Export (1.7B Support)

**By:** Trinity (ML Engineer)
**Date:** 2026-04-02
**Issue:** #26

## What

All Python ONNX export scripts (`export_lm.py`, `reexport_lm_novmap.py`, `reexport_base_novmap.py`)
now read model dimensions from `config.talker_config` and `config.talker_config.code_predictor_config`
at load time, replacing the hardcoded 0.6B constants (`TALKER_HIDDEN=1024`, `CP_HIDDEN=1024`, etc.).

The `read_model_dims(config)` function extracts all export-relevant dimensions into a `dims` dict,
which is threaded through wrapper constructors and export functions.

## Why

- The 1.7B model has `hidden_size=2048` and `intermediate_size=6144` (vs 1024/3072 for 0.6B).
  Hardcoded constants would produce wrong ONNX graphs for 1.7B.
- Config-driven approach means the scripts work for any future model variant without code changes.
- The 0.6B default behavior is preserved — same `--model-dir` default, same output files.

## Impact

- **Python export scripts**: `export_lm.py`, `reexport_lm_novmap.py`, `reexport_base_novmap.py` — refactored
- **download_models.py**: Added `--model customvoice-1.7b`, `all-1.7b`, `everything` options
- **extract_tokenizer.py**: Added `--model` CLI arg (tokenizer is shared, but configurable)
- **export_embeddings.py**: Already config-driven, docstring updated
- **Vocoder/Speaker encoder**: Unchanged (architecture-independent of Talker hidden size)
- **C# side**: Not touched by this change — Neo handles that

## Who Needs to Know

- **Neo**: The exported `config.json` (from `export_embeddings.py`) already includes `talker.hidden_size`
  dynamically. C# code should read from config rather than hardcoding 1024.
- **Morpheus**: Upload scripts (`upload_to_hf.py`) will need a 1.7B variant when the ONNX models
  are ready to publish.
