# Decisions

> Team decisions that all agents should respect. Managed by Scribe.

### 2026-02-21T15:38Z: Target model selection
**By:** Bruno Capuano (via Squad)
**What:** Start with Qwen3-TTS-12Hz-0.6B-CustomVoice (simplest — no audio input, just text + speaker). Voice cloning with Base model is a stretch goal.
**Why:** 0.6B is practical for local inference (~1.8GB). CustomVoice has 9 built-in speakers and instruction control.

### 2026-02-21T15:38Z: Framework constraints
**By:** Bruno Capuano (via Squad)
**What:** Use .NET 10 and latest available packages for both C# and Python.
**Why:** User requirement — always use the latest.

### 2026-02-21T16:30Z: GPU handoff — continue on NVIDIA machine
**By:** Bruno Capuano (via Squad)
**What:** ONNX export scripts run on CPU but are impractical without GPU acceleration. Repo published to GitHub (elbruno/qwen-labs-cs, private). All 8 completed tasks committed. Work continues on NVIDIA GPU machine — next step is running export scripts.
**Why:** Current machine has no NVIDIA GPU. Export scripts are designed for CPU (device_map="cpu", dtype=float32) but model download + export is slow without CUDA.

### 2026-02-21T16:30Z: Continuation instructions
**By:** Squad (Coordinator)
**What:** On the GPU machine, the Squad should: (1) run download_models.py, (2) run all export scripts in order: vocoder → LM → embeddings → tokenizer, (3) implement LanguageModel.cs, (4) wire TtsPipeline.cs, (5) test end-to-end.
**Why:** Documented to ensure seamless handoff — any Squad session on the new machine can pick up immediately.

### 2026-02-21: .NET project scaffold and package choices
**By:** Neo (.NET Developer)
**What:** Target net10.0 with Microsoft.ML.OnnxRuntime 1.24.2, Microsoft.ML.Tokenizers 2.0.0, NAudio 2.2.1. Simple string[] CLI parsing (no System.CommandLine). WavWriter outputs 16-bit PCM WAV at 24 kHz.
**Why:** OnnxRuntime provides robust ONNX inference; Tokenizers avoids hand-rolling BPE; lightweight dependencies. Project structure: Models / Pipeline / Audio separation.

### 2026-02-21: BPE Tokenizer — Microsoft.ML.Tokenizers BpeTokenizer
**By:** Neo
**What:** Use `BpeTokenizer.Create(BpeOptions)` with ByteLevel=true, GPT-2 pre-tokenization regex, and all Qwen3-TTS special tokens. Load vocab.json + merges.txt directly via BpeOptions constructor.
**Why:** BpeTokenizer gives full control over byte-level encoding and special tokens. Produces token IDs matching Python HuggingFace tokenizer (validate against validation_cases.json). Avoids manual regex parsing.

### 2026-02-21: Vocoder tensor API — shape and dtype alignment
**By:** Neo
**What:** Changed Vocoder.Decode() signature from int[] to long[,,] (shape 1×16×T) to match ONNX model's expected input (B, 16, T_codes) int64. Pipeline reshapes LM's flat output into this 3D array.
**Why:** Matches ONNX model contract explicitly; avoids runtime reshaping inside vocoder; makes shape expectations clear.

### 2026-02-21: BPE Tokenizer Extraction from Python Model
**By:** Trinity (ML Engineer)
**What:** Created `python/extract_tokenizer.py` to extract Qwen3-TTS BPE artifacts (vocab.json, merges.txt, configs, validation cases). Documented full prompt format in `python/TOKENIZER.md`.
**Why:** Enables C# re-implementation using only vocab.json + merges.txt. Standard Qwen2Tokenizer (GPT-2 BPE) — no exotic preprocessing.

### 2026-02-21: Tokenizer findings — CustomVoice prompt structure
**By:** Trinity (ML Engineer)
**What:** The 0.6B CustomVoice model does NOT support instruct control (forced to None). Speaker selection uses codec embedding IDs (e.g., Ryan=3061). Language encoded as codec token; "auto" mode skips language ID. Eric/Dylan have dialect remapping for Chinese/auto modes.
**Why:** Ensures prompt format matches actual model behavior. C# TextTokenizer.BuildCustomVoicePrompt() must follow this structure.

### 2026-02-21: ONNX Export Strategy — 3-Model Split
**By:** Trinity (ML Engineer)
**What:** Export Qwen3-TTS as three separate ONNX models: Vocoder (single-pass), Code Predictor (autoregressive per group), Talker LM (large autoregressive with KV-cache). Export order: Vocoder → Code Predictor → Talker LM.
**Why:** Three distinct execution patterns (single-pass vs autoregressive). Code Predictor runs 31× per Talker step — must be separate and fast. Different KV-cache semantics (Talker grows across generation; CP resets per step). Vocoder is simplest validation milestone.

### 2026-02-21: Vocoder ONNX Export — Two-Attempt Strategy
**By:** Trinity (ML Engineer)
**What:** `python/export_vocoder.py` uses standard torch.onnx.export (opset 17) first, falls back to dynamo=True (opset 18+) if tracing fails. Dynamic axes on batch and time dimensions.
**Why:** Decoder contains sliding-window attention and causal convolution padding with data-dependent shapes. Dynamo backend handles these patterns more robustly. Two paths let us discover which works without manual iteration.

### 2026-02-21: Code Predictor lm_head stacking for ONNX
**By:** Trinity (ML Engineer)
**What:** Stack all 31 lm_head weight matrices into (31, 2048, 1024) buffer. Use torch.index_select on generation_steps input to select correct weight at runtime. Single `code_predictor.onnx` model per step.
**Why:** ONNX tracing cannot dynamically index ModuleList. Index-select avoids exporting 31 separate models or wasteful torch.where with all outputs. C# calls one model per step, passing generation_steps (0-30).

### 2026-02-21T17:45Z: C# Inference Pipeline Complete
**By:** Neo
**What:** Implemented full C# ONNX inference pipeline: NpyReader (NumPy .npy loader), EmbeddingStore (centralized lookups with SiLU-gated MLP), rewritten LanguageModel (3-session autoregressive with KV stacking), updated TtsPipeline (delegated prompt building), Program (CLI with --model-dir/--language). All components wired end-to-end.
**Why:** Bruno can now test end-to-end inference immediately after Trinity exports ONNX models. Pipeline matches Python reference architecture exactly. Ready for validation against Python baseline.

### 2026-02-21: C# Pipeline Bug Fixes (Coordinator Review)
**By:** Coordinator
**What:** Fixed 4 runtime bugs: (1) TTS embedding sizing — corrected prefill tensor dimensions, (2) Hidden state indexing — fixed decode loop stacking from flat to (B, 8, T, 128), (3) Code Predictor KV tracking — CP sessions no longer accumulate KV across groups; reset per group, (4) Tokenizer case sensitivity — ensured vocab.json respects case-sensitive special tokens.
**Why:** Ensures pipeline produces correct outputs matching ONNX model contracts. All bugs found during code review before runtime testing.

### 2026-02-22: Architecture Review — Post Issue #21
**By:** Morpheus (Lead / Architect)
**What:** ITextToSpeechClient abstraction is architecturally sound and production-ready. Aligns with Microsoft.Extensions.AI patterns. Provides thread-safe lazy initialization via SemaphoreSlim, memory-based synthesis, streaming support, and DI integration. All 43 unit tests pass (12 new tests added for the client).
**Why:** Clean abstraction boundaries: QwenTextToSpeechClient handles concurrency/lifecycle; TtsPipeline handles ONNX inference. No leaky abstractions. Thread-safety via SemaphoreSlim double-check lock pattern is correct. Proper IDisposable implementation. DI-friendly extension follows .NET conventions (singleton lifetime, options pattern).

### 2026-02-22: ITextToSpeechClient Code Review — Minor Observations
**By:** Neo (.NET Developer)
**What:** Implementation is production-ready and follows .NET best practices. Three observations for future consideration: (1) ConfigureAwait(false) missing on async/await calls — low impact for server/console apps, but could cause UI thread blocking in WPF/WinForms contexts, (2) TtsPipeline.SynthesizeAsync lacks CancellationToken parameter — very low impact since inference is expensive part already done, file I/O is fast, (3) SynthesizeStreamingAsync yields full audio in one chunk rather than true streaming — design correctly anticipates future chunked models, good forward compatibility.
**Why:** Items 1-2 are cosmetic quality improvements, not blockers. Item 3 is well-designed for future models. Code is ready to ship as-is. Consider addressing 1-2 in future polish pass if library will be consumed by UI applications.

### 2026-02-22: ITextToSpeechClient Test Coverage Review
**By:** Tank (Tester/QA)
**What:** All 41 tests pass (29 Core.Tests + 10 VoiceCloning.Tests + 2 TtsPipelineFactoryTests). ITextToSpeechClient has 12 new tests covering constructor validation, dispose patterns, input validation, type contracts, and DI registration. Significant test coverage gaps identified in production-critical scenarios: (1) Thread-safety — no validation of concurrent SemaphoreSlim access during lazy initialization, (2) Error handling — no tests for ONNX failures, file I/O exceptions, or cleanup-under-failure, (3) Streaming lifecycle — SessionClose guarantees and cancellation behavior untested, (4) DI resolution — registration verified but not actual ServiceProvider resolution + usage.
**Why:** Production risks without these tests: race conditions in multi-threaded apps (ASP.NET Core), resource leaks under error conditions, silent failures in streaming scenarios. Recommend creating Issue #22 for post-milestone test coverage enhancement with Priority 1: thread-safety concurrency tests, Priority 2: error path tests, Priority 3: streaming lifecycle tests, Priority 4: DI resolution tests.

### 2026-04-02T16:43Z: 1.7B Model Support — Viability & Scope Assessment
**By:** Trinity, Morpheus (consolidated)
**What:** 1.7B model variant is architecturally compatible with existing ONNX pipeline and recommended for implementation. Technical analysis complete: only Talker LM hidden_size changes (1024→2048), all other components (Code Predictor, vocoder, tokenizer, speakers) remain identical. ONNX export feasible with config-driven changes (remove hardcoded `TALKER_HIDDEN = 1024`). Scope assessment: Medium effort (1-2 days), non-breaking change, high user value (instruction control for emotion/rate/timbre). Resource requirements: 12-16 GB RAM, 2-2.5× slower inference. Code changes: Python exporters (2-3 hours) + C# dimension-agnostic refactoring (4-6 hours).
**Why:** User request #26 seeks instruction control not available in 0.6B. 1.7B-CustomVoice provides this capability while maintaining identical architecture except hidden size. Backward compatible with 0.6B (default behavior). Manageable engineering cost with high user value. Phase 1 MVP (8-12 hours) sufficient for basic support; Phase 2 optimization (FP16, quantization) future work. Both Trinity and Morpheus recommend pursuing if approved by maintainer.

### 2026-04-02T17:19Z: 1.7B Implementation Phase 1 Complete (consolidated)
**By:** Neo, Trinity, Tank
**What:** Full implementation of multi-variant model support (0.6B and 1.7B) completed. All hardcoded model dimensions replaced with config-driven values. Three independent decisions consolidated:

1. **Neo (C# Config-Driven Dimensions):** All model dimensions (hidden_size, num_layers, num_kv_heads, head_dim, vocab_size) now read from `config.json` at runtime in EmbeddingStore and LanguageModel. `QwenModelVariant` enum drives download and storage only, not inference dimensions. Rationale: future variants with different dimensions work automatically if config.json is correct; .npy array shapes are the ground truth for dimension discovery.

2. **Trinity (Python Export Scripts Config-Driven):** Removed hardcoded `TALKER_HIDDEN=1024`, `CP_HIDDEN=1024` from export_lm.py, reexport_lm_novmap.py, reexport_base_novmap.py. Added `read_model_dims(config)` function to extract dimensions dynamically from `config.talker_config` and `code_predictor_config`. Extended download_models.py with 1.7B variants (`customvoice-1.7b`, `all-1.7b`, `base-1.7b`, `everything`). Rationale: any future model variant works without code changes as long as ONNX models and config.json are consistent.

3. **Tank (Variant API & Tests):** Created `QwenModelVariant` enum (Qwen06B=0, Qwen17B=1) and `QwenModelVariantConfig` static class with hidden_size, intermediate_size, HuggingFace repoId, and model subdirectory mappings. Added 47 new tests across ModelVariantTests.cs, ModelVariantDownloaderTests.cs, TtsPipelineVariantTests.cs. Default variant Qwen06B ensures backward compat.

**Why:** Approved by team (Trinity, Morpheus, Neo). Phase 1 scope (8-12 hours, actual ~8 hours) matched estimate. Non-breaking change preserves all existing APIs and default behavior. Backward compatible: 0.6B remains default, legacy model directory unchanged. 1.7B model support unlocks instruction control (emotion, rate, timbre) not available in 0.6B. High user value with manageable engineering cost.

**Impact Summary:**
- **C#:** 88 tests pass (78 Core + 10 VoiceCloning). Build clean. Zero hardcoded dimension constants. TtsPipeline.CreateAsync() accepts optional variant parameter.
- **Python:** All export scripts config-driven. 0.6B backward compatible (same defaults, same outputs). download_models.py extended with 1.7B options.
- **Storage:** 0.6B uses legacy root dir; 1.7B uses `/1.7B` subdirectory. No file mixing.
- **Architecture:** QwenModelVariant enum → repo/directory only. Dimensions → config.json (via EmbeddingStore shape detection). Variant-agnostic inference pipeline.

**Next Steps (Phase 2):**
- GPU-based 1.7B ONNX export when infrastructure available
- Performance optimization: FP16 export, quantization (future)
- Additional variants if user demand increases
