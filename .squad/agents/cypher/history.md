# Cypher — History

## Release Log

| Tag | Date | PR/Commit | NuGet Version | Notes |
|-----|------|-----------|---------------|-------|
| `v0.0.1-preview` | 2026-02-21 | — | 0.0.1-preview | Initial preview release |
| `v0.0.2-preview` | 2026-02-21 | PR #3 | 0.0.2-preview | Voice cloning library + base model support |
| `v1.0.0` | 2026-02-22 | Release | 1.0.0 | Stable: full 0.6B inference pipeline, ITextToSpeechClient, CLI/Web/FileReader apps |
| `v1.1.0` | 2026-04-02 | Release | 1.1.0 | Stable: 1.7B model support, instruct control API, multi-variant architecture |
| `v1.2.1-preview` | 2026-04-05 | Commit 4f2afcd | 1.2.1-preview | Preview: CP projection bias dimension fix for 1.7B models (Issue #28) |

## Learnings

### v1.0.0 Release (2026-02-22)
- First stable release after preview builds. Stabilization focused on ITextToSpeechClient abstraction and DI integration.
- NuGet publish workflow (publish.yml) triggered automatically on release tag.
- 41 unit tests across Core + VoiceCloning projects.

### v1.1.0 Release (2026-04-02)
- **Scope:** Stable release adding 1.7B model variant with instruct control (emotion, rate, timbre instructions).
- **Architecture:** Config-driven dimensions eliminate hardcoded constants. QwenModelVariant enum drives download/storage; config.json drives inference dimensions.
- **Backward compatibility:** Zero breaking changes. 0.6B remains default. All existing APIs unchanged.
- **Release process:** Tag + CHANGELOG + version update → commit → git tag → gh release create (includes release notes markdown).
- **Test coverage:** 88 unit tests pass (78 Core + 10 VoiceCloning), includes 47 new variant-specific tests.
- **Build status:** Clean build (0 errors), all tests passing, working tree clean before release.
- **Key decision:** Instruction control gated at pipeline level (TtsPipeline.SynthesizeAsync) via QwenModelVariantConfig.SupportsInstruct() — single source of truth.
- **ONNX export:** 1.7B models (~12.5 GB) exported to python/onnx_1.7b/ and uploaded to HuggingFace (elbruno/Qwen3-TTS-12Hz-1.7B-CustomVoice-ONNX). Export scripts now config-driven (no hardcoded dims) for future variants.

### v1.2.1-preview Release (2026-04-05)
- **Scope:** Patch release fixing CP projection bias dimension mismatch for 1.7B model variant (Issue #28).
- **CHANGELOG format:** Fixed header format from `[2026-04-05]` to `[1.2.1-preview] - 2026-04-05` to follow semantic versioning convention.
- **Version sequence:** Correctly identified v1.2.1-preview as next patch after v1.2.0 (which addressed Issue #27 text truncation).
- **Release readiness:** All 225 tests pass (215 Core + 10 VoiceCloning), clean build, working tree clean.
- **Release process:** Commit with Co-authored-by trailer → git tag → git push origin main --tags → gh release create with --prerelease flag.
- **NuGet package:** Version field in csproj must match tag without 'v' prefix but include '-preview' suffix for pre-release packages.
