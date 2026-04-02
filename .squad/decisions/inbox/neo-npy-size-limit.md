# Decision: SEC-3 File Size Limits for 1.7B Model Support

**Author:** Neo  
**Date:** 2026-02-28  
**Issue:** #25  

## Context

The SEC-3 security checks in NpyReader.cs and Vocoder.cs had file size limits that were too low for 1.7B Qwen3-TTS models. The 1.7B model's `text_embedding.npy` is ~1.2 GB (vocab_size=151936 × hidden_size=2048 × 4 bytes), exceeding the 500 MB NPY limit.

## Decision

- **NpyReader.cs** `maxNpySize`: 500 MB → **2 GB** (supports 1.7B text_embedding.npy at ~1.2 GB with headroom)
- **Vocoder.cs** `maxOnnxSize`: 2 GB → **8 GB** (consistent with LanguageModel.cs)

## Rationale

All SEC-3 ONNX size limits should be 8 GB for consistency across LanguageModel, Vocoder, and any future model loaders. NPY limit at 2 GB is sufficient — the largest known NPY file is ~1.2 GB for the 1.7B model.

## SEC-3 Limit Summary (current state)

| File | Type | Limit |
|------|------|-------|
| LanguageModel.cs | ONNX | 8 GB |
| Vocoder.cs | ONNX | 8 GB |
| NpyReader.cs | NPY | 2 GB |
