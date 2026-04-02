# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.1.0] - 2026-04-02

### Added

- **Qwen3-TTS 1.7B model support** — run either 0.6B or 1.7B model variant ([#26](https://github.com/elbruno/ElBruno.QwenTTS/issues/26))
  - `QwenModelVariant` enum (`Qwen06B`, `Qwen17B`) drives model selection
  - `QwenModelVariantConfig` maps each variant to HuggingFace repo, model directory, and dimension defaults
  - Config-driven dimensions: hidden_size, num_layers, head_dim read from model's `config.json` at runtime
  - Zero hardcoded dimension constants — `LanguageModel` and `EmbeddingStore` fully parameterized
  - `TtsPipeline.CreateAsync()` accepts optional `variant` parameter (defaults to 0.6B for backward compatibility)
  - Variant-specific model download directories under `DefaultModelDir`
- **Instruct control API** — natural-language style control for 1.7B models
  - `SynthesizeAsync` accepts optional `instruct` parameter (e.g., "speak with excitement")
  - Instruct gated by variant: 1.7B flows through to prompt, 0.6B warns and ignores
  - `QwenModelVariantConfig.SupportsInstruct()` as single source of truth
- **CLI `--variant` and `--instruct` flags** — select model variant and instruct text from command line
- **FileReader `--variant` flag** — batch process with either model variant
- **Blazor Web app variant support** — variant selector dropdown, dynamic instruct field (disabled for 0.6B)
- **47 new unit tests** — `ModelVariantTests`, `ModelVariantDownloaderTests`, `TtsPipelineVariantTests`
- **Python export updates** — `read_model_dims(config)` replaces hardcoded constants; 1.7B download support

- **`ITextToSpeechClient`** — MEAI-aligned service contract for text-to-speech with `SynthesizeToMemoryAsync` and `SynthesizeStreamingAsync`
- **`QwenTextToSpeechClient`** — production-ready implementation with:
  - Thread-safe lazy initialization via `SemaphoreSlim` (model loads on first use, shared across concurrent callers)
  - `SynthesizeToMemoryAsync` — returns audio as `byte[]` with automatic temp file cleanup
  - `SynthesizeStreamingAsync` — `IAsyncEnumerable<TextToSpeechStreamingUpdate>` with session lifecycle events (Open → AudioChunk → Close)
  - Proper `IDisposable` with cleanup of ONNX sessions, semaphore, and temp files
  - GPU support via `sessionOptionsFactory` / `vocoderSessionOptionsFactory` parameters
- **`TextToSpeechResponse`** — response type with `AudioData`, `MediaType`, `SampleRate`, `ModelId`
- **`TextToSpeechStreamingUpdate`** — streaming update with `Kind` enum (`SessionOpen`, `AudioChunk`, `SessionClose`)
- **`TextToSpeechOptions`** — request options with `VoiceId`, `Language`, `Instruct`, `ModelId`
- **`AddQwenTextToSpeechClient()`** — DI extension method for registering `ITextToSpeechClient` as singleton
- **Related Projects** section in README with link to [ElBruno.PersonaPlex](https://github.com/elbruno/ElBruno.PersonaPlex)
- **GPU Acceleration** — configurable `SessionOptions` injection for CUDA and DirectML support
  - `TtsPipeline` and `VoiceClonePipeline` accept optional `Func<SessionOptions>?` parameter
  - `OrtSessionHelper` static class with `CreateCpuOptions()`, `CreateCudaOptions()`, `CreateDirectMlOptions()`
  - All ONNX sessions (LanguageModel, Vocoder, SpeakerEncoder) respect the configured execution provider
  - Default behavior unchanged (CPU with full graph optimization)
  - [GPU Acceleration docs](docs/gpu-acceleration.md)

## [1.0.0] - 2026-02-22

### Added

- **ElBruno.QwenTTS** NuGet package — complete Qwen3-TTS inference pipeline for .NET
- `TtsPipeline` — full TTS orchestrator: text → tokenize → LM → vocoder → WAV
- `TtsPipeline.CreateAsync()` — factory method with automatic model download from HuggingFace
- `ModelDownloader` — auto-download 33 model files (~5.5 GB) with byte-level progress reporting
- `ModelDownloader.DefaultModelDir` — shared model directory (`%LOCALAPPDATA%/ElBruno.QwenTTS/models`)
- `ModelDownloader.IsModelDownloaded` — check if models are ready without downloading
- Multi-speaker support: ryan, serena, vivian, aiden, eric, dylan, uncle_fu, ono_anna, sohee
- Multi-language support: english, spanish, chinese, japanese, korean
- Instruction-based voice control (e.g., "speak with excitement")
- 24 kHz mono WAV output
- CLI app (`ElBruno.QwenTTS`)
- File reader app (`ElBruno.QwenTTS.FileReader`) for batch text/SRT → audio
- Web app (`ElBruno.QwenTTS.Web`) — Blazor UI for speech generation
- Unit tests (`ElBruno.QwenTTS.Core.Tests`)
