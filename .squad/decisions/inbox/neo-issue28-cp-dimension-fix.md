# Decision: Fix CP Projection Bias Dimension Mismatch (Issue #28)

**Author:** Neo  
**Date:** 2026-07-22  
**Status:** Implemented  
**Fixes:** Issue #28

## Context
For 1.7B models, `_cpHiddenSize` (derived from CP codec embedding array shapes) can be 2048, while `_cpProjectionBias` is only 1024 elements. This caused `IndexOutOfRangeException` in `CpProjection()`. A secondary bug: without projection files, `cpInputDim` was set to `_hiddenSize = 2048` but the code_predictor ONNX model expects 1024.

## Decision
Read `code_predictor.hidden_size` from config.json as the authoritative CP model dimension. Use the projection weight's output dimension (row count) for the bias loop instead of `_cpHiddenSize`. Both changes are backward-compatible.

## Changes
- **CodePredictorConfig**: Added `hidden_size` property
- **EmbeddingStore**: New `_cpModelHiddenSize` field (config-driven with fallback), `CpModelHiddenSize` property, fixed bias loop, added validation
- **LanguageModel**: `cpInputDim` uses `_embeddings.CpModelHiddenSize` instead of `_cpHiddenSize`

## Principle
Array shapes are ground truth for *most* dimensions, but CP projection/input dim must come from config.json because the codec embedding shape can differ from the code_predictor ONNX model's actual hidden_size in 1.7B exports.
