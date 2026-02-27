namespace ElBruno.QwenTTS.Pipeline;

/// <summary>
/// Kind of streaming update during text-to-speech synthesis.
/// </summary>
public enum TextToSpeechUpdateKind
{
    /// <summary>Synthesis session has opened.</summary>
    SessionOpen,

    /// <summary>An audio chunk is available.</summary>
    AudioChunk,

    /// <summary>Synthesis session has closed.</summary>
    SessionClose
}

/// <summary>
/// A streaming update from a text-to-speech synthesis operation.
/// </summary>
public sealed class TextToSpeechStreamingUpdate
{
    /// <summary>The kind of update.</summary>
    public required TextToSpeechUpdateKind Kind { get; init; }

    /// <summary>Audio data for <see cref="TextToSpeechUpdateKind.AudioChunk"/> updates. Null for other kinds.</summary>
    public byte[]? AudioData { get; init; }

    /// <summary>Sample rate in Hz. Present on <see cref="TextToSpeechUpdateKind.AudioChunk"/> updates.</summary>
    public int? SampleRate { get; init; }
}
