using Microsoft.AspNetCore.Mvc;
using ElBruno.QwenTTS.Pipeline;

namespace ElBruno.QwenTTS.Web.Controllers;

[ApiController]
[Route("api")]
public class TtsController : ControllerBase
{
    private static TtsPipeline? _pipeline;
    private static readonly object _lock = new();

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] TtsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Text is required.");

        // Initialize the pipeline once (downloads 5.5GB on first run, cached after)
        if (_pipeline == null)
        {
            lock (_lock)
            {
                if (_pipeline == null)
                {
                    string modelDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QwenTTS");
                    _pipeline = TtsPipeline.CreateAsync(modelDir).GetAwaiter().GetResult();
                }
            }
        }

        string speaker = string.IsNullOrWhiteSpace(request.Speaker) ? "ryan" : request.Speaker.ToLower();
        string language = string.IsNullOrWhiteSpace(request.Language) ? "english" : request.Language.ToLower();

        // Generate audio in memory
        var wavBytes = await _pipeline.SynthesizeWavAsync(request.Text, speaker, language);

        return File(wavBytes.ToArray(), "audio/wav");
    }
}

public class TtsRequest
{
    public string Text { get; set; } = string.Empty;
    public string? Speaker { get; set; }
    public string? Language { get; set; }
}
