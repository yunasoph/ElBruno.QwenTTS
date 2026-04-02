using System.Text.Json;

namespace ElBruno.QwenTTS.Models;

/// <summary>
/// Loads and provides access to all embedding matrices (.npy files).
/// Includes text embeddings, projection layers, codec embeddings, and speaker IDs.
/// </summary>
internal sealed class EmbeddingStore : IDisposable
{
    private readonly float[,] _textEmbedding;           // (vocab_size, text_hidden_size)
    private readonly float[,] _fc1Weight;               // (fc1_out, text_hidden_size)
    private readonly float[] _fc1Bias;                  // (fc1_out,)
    private readonly float[,] _fc2Weight;               // (hidden_size, fc1_out)
    private readonly float[] _fc2Bias;                  // (hidden_size,)
    private readonly float[,] _talkerCodecEmbedding;    // (vocab, hidden_size)
    private readonly float[][,] _cpCodecEmbeddings;     // 15 × (cp_vocab, cp_hidden)
    private readonly Dictionary<string, int> _speakerIds;

    // Dimensions derived from loaded arrays — no hardcoding
    private readonly int _textHiddenSize;   // text embedding dim (2048 for both 0.6B and 1.7B)
    private readonly int _fc1OutSize;       // intermediate MLP size
    private readonly int _hiddenSize;       // talker hidden_size (1024 for 0.6B, 2048 for 1.7B)
    private readonly int _cpHiddenSize;     // CP embedding dim (1024 for both variants)
    
    public ModelConfig Config { get; }

    /// <summary>Talker hidden_size derived from loaded embedding dimensions.</summary>
    public int HiddenSize => _hiddenSize;

    /// <summary>Text embedding dimension derived from loaded data.</summary>
    public int TextHiddenSize => _textHiddenSize;

    /// <summary>Code Predictor embedding dimension derived from loaded data.</summary>
    public int CpHiddenSize => _cpHiddenSize;

    public EmbeddingStore(string embeddingsDir, string configPath)
    {
        // Load config
        var configJson = File.ReadAllText(configPath);
        Config = JsonSerializer.Deserialize<ModelConfig>(configJson)
            ?? throw new InvalidDataException("Failed to parse config.json");

        // Load text embedding and projection
        _textEmbedding = NpyReader.ReadFloat2D(Path.Combine(embeddingsDir, "text_embedding.npy"));
        _fc1Weight = NpyReader.ReadFloat2D(Path.Combine(embeddingsDir, "text_projection_fc1_weight.npy"));
        _fc1Bias = NpyReader.ReadFloat1D(Path.Combine(embeddingsDir, "text_projection_fc1_bias.npy"));
        _fc2Weight = NpyReader.ReadFloat2D(Path.Combine(embeddingsDir, "text_projection_fc2_weight.npy"));
        _fc2Bias = NpyReader.ReadFloat1D(Path.Combine(embeddingsDir, "text_projection_fc2_bias.npy"));

        // Load talker codec embedding
        _talkerCodecEmbedding = NpyReader.ReadFloat2D(Path.Combine(embeddingsDir, "talker_codec_embedding.npy"));

        // Load CP codec embeddings (15 groups)
        _cpCodecEmbeddings = new float[15][,];
        for (int i = 0; i < 15; i++)
        {
            var path = Path.Combine(embeddingsDir, $"cp_codec_embedding_{i}.npy");
            _cpCodecEmbeddings[i] = NpyReader.ReadFloat2D(path);
        }

        // Load speaker IDs
        var speakerIdsPath = Path.Combine(embeddingsDir, "speaker_ids.json");
        var speakerJson = File.ReadAllText(speakerIdsPath);
        _speakerIds = JsonSerializer.Deserialize<Dictionary<string, int>>(speakerJson)
            ?? throw new InvalidDataException("Failed to parse speaker_ids.json");

        // Derive dimensions from loaded arrays
        _textHiddenSize = _textEmbedding.GetLength(1);
        _fc1OutSize = _fc1Weight.GetLength(0);
        _hiddenSize = _fc2Weight.GetLength(0);
        _cpHiddenSize = _cpCodecEmbeddings[0].GetLength(1);
    }

    /// <summary>
    /// Looks up text embedding for a token ID and writes to output.
    /// </summary>
    public void TextEmbedding(int tokenId, Span<float> output)
    {
        if (output.Length != _textHiddenSize)
            throw new ArgumentException($"Output must be length {_textHiddenSize}");
        
        for (int i = 0; i < _textHiddenSize; i++)
            output[i] = _textEmbedding[tokenId, i];
    }

    /// <summary>
    /// Applies text projection MLP: output = fc2(silu(fc1(input)))
    /// Maps from text_hidden_size → talker hidden_size.
    /// </summary>
    public void TextProjection(ReadOnlySpan<float> input, Span<float> output)
    {
        if (input.Length != _textHiddenSize)
            throw new ArgumentException($"Input must be length {_textHiddenSize}");
        if (output.Length != _hiddenSize)
            throw new ArgumentException($"Output must be length {_hiddenSize}");

        // fc1: (fc1_out, text_hidden_size) @ input + bias → hidden
        var hidden = new float[_fc1OutSize];
        MatMul(_fc1Weight, input, hidden);
        for (int i = 0; i < _fc1OutSize; i++)
            hidden[i] = SiLU(hidden[i] + _fc1Bias[i]);

        // fc2: (hidden_size, fc1_out) @ hidden + bias → output
        MatMul(_fc2Weight, hidden, output);
        for (int i = 0; i < _hiddenSize; i++)
            output[i] += _fc2Bias[i];
    }

    /// <summary>
    /// Looks up talker codec embedding for a token ID.
    /// </summary>
    public void TalkerCodecEmbedding(int tokenId, Span<float> output)
    {
        if (output.Length != _hiddenSize)
            throw new ArgumentException($"Output must be length {_hiddenSize}");
        
        for (int i = 0; i < _hiddenSize; i++)
            output[i] = _talkerCodecEmbedding[tokenId, i];
    }

    /// <summary>
    /// Looks up CP codec embedding for a group and token ID.
    /// groupIndex is 0-14 (maps to cp_codec_embedding_0..14).
    /// </summary>
    public void CpCodecEmbedding(int groupIndex, int tokenId, Span<float> output)
    {
        if (groupIndex < 0 || groupIndex >= 15)
            throw new ArgumentException($"groupIndex must be 0-14, got {groupIndex}");
        if (output.Length != _cpHiddenSize)
            throw new ArgumentException($"Output must be length {_cpHiddenSize}");
        
        var table = _cpCodecEmbeddings[groupIndex];
        for (int i = 0; i < _cpHiddenSize; i++)
            output[i] = table[tokenId, i];
    }

    /// <summary>
    /// Gets the speaker token ID for a speaker name.
    /// </summary>
    public int GetSpeakerId(string speaker)
    {
        if (!_speakerIds.TryGetValue(speaker, out var id))
            throw new ArgumentException($"Unknown speaker: {speaker}");
        return id;
    }

    /// <summary>
    /// Gets the list of available speaker names.
    /// </summary>
    public IReadOnlyCollection<string> GetAvailableSpeakers() => _speakerIds.Keys;

    public void Dispose()
    {
        // No unmanaged resources
    }

    private static float SiLU(float x) => x / (1.0f + MathF.Exp(-x));

    /// <summary>
    /// Matrix-vector multiply: output = weight @ input
    /// weight is (M, N), input is (N,), output is (M,)
    /// </summary>
    private static void MatMul(float[,] weight, ReadOnlySpan<float> input, Span<float> output)
    {
        int M = weight.GetLength(0);
        int N = weight.GetLength(1);
        
        for (int i = 0; i < M; i++)
        {
            float sum = 0;
            for (int j = 0; j < N; j++)
                sum += weight[i, j] * input[j];
            output[i] = sum;
        }
    }
}

/// <summary>
/// Model configuration loaded from config.json
/// </summary>
internal sealed class ModelConfig
{
    public TalkerConfig talker { get; set; } = new();
    public CodePredictorConfig code_predictor { get; set; } = new();
    public TtsConfig tts { get; set; } = new();
    public Dictionary<string, int> language_ids { get; set; } = new();
    public Dictionary<string, object> speaker_dialect { get; set; } = new();
}

internal sealed class TalkerConfig
{
    public int codec_eos_token_id { get; set; }
    public int codec_pad_id { get; set; }
    public int codec_bos_id { get; set; }
    public int codec_think_id { get; set; }
    public int codec_nothink_id { get; set; }
    public int codec_think_bos_id { get; set; }
    public int codec_think_eos_id { get; set; }
    public int num_code_groups { get; set; }
    public int hidden_size { get; set; }
    public int text_hidden_size { get; set; }
    public int num_hidden_layers { get; set; }
    public int num_key_value_heads { get; set; }
    public int head_dim { get; set; }
    public int vocab_size { get; set; }
}

internal sealed class CodePredictorConfig
{
    public int num_hidden_layers { get; set; }
    public int num_key_value_heads { get; set; }
    public int head_dim { get; set; }
    public int vocab_size { get; set; }
}

internal sealed class TtsConfig
{
    public int tts_bos_token_id { get; set; }
    public int tts_eos_token_id { get; set; }
    public int tts_pad_token_id { get; set; }
}
