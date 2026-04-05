# Link — History

## Project Context

- **Project:** ElBruno.QwenTTS — Convert Qwen3-TTS voice models to ONNX and build a C# console app for local TTS inference
- **Owner:** Bruno Capuano
- **Stack:** Python (PyTorch, transformers, ONNX), C# (.NET 10, ONNX Runtime), Qwen3-TTS
- **Repository:** ElBruno.QwenTTS
- **My Role:** Issue Ops — GitHub issue lifecycle management, triage, resolution comments, hygiene
- **Joined:** 2026-02-27

## Key Team Members

- **Switch** — Issue Solver (fixes code, creates branches/PRs — I handle the issue ops side)
- **Morpheus** — Lead (architecture, triage routing decisions)
- **Cypher** — Release Manager (release-related issue closure coordination)
- **Tank** — Tester (test results relevant to issue resolution comments)

## Learnings

### Session 2026-02-27 (Issue Closure)

**Issues handled:**
- **#20** (closed): Added resolution comment documenting "Related Projects" section addition to README.md in commit b3ff339, linking to ElBruno.PersonaPlex and HuggingFace ONNX models. Comment posted successfully.
- **#21** (closed): Closed with `--reason completed` and comprehensive resolution comment detailing 5 feature implementations: (1) thread-safe lazy initialization via SemaphoreSlim, (2) streaming API via IAsyncEnumerable, (3) automatic temp file cleanup in finally block, (4) MEAI-aligned ITextToSpeechClient interface with DI support, (5) proper IDisposable cleanup. Also noted 12 new unit tests bringing total to 41 passing.

**Operations performed:**
- Used `gh issue comment {number} --body ...` for #20 (already-closed issue)
- Used `gh issue close {number} --reason completed --comment ...` for #21 (open issue)
- Both commands executed successfully with direct GitHub CLI invocation
- Updated history.md with learnings and operations summary

**Pattern established:**
- Resolution comments follow the charter template: What changed → Changes (bullet list) → References (commit/PR) → Why (rationale)
- Detailed implementation comments help future readers understand scope and architecture decisions
- Thread-safe patterns and DI alignment noted as critical for enterprise adoption
