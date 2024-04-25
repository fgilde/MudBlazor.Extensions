namespace MudBlazor.Extensions.Core;


/// <summary>
/// Represents the result of a speech recognition operation.
/// </summary>
public class SpeechRecognitionResult
{
    /// <summary>
    /// The transcript of the recognized speech.
    /// </summary>
    public string Transcript { get; set; }

    /// <summary>
    /// Detailed changes between interim and final transcripts if interim results are provided.
    /// </summary>
    public string TranscriptChanges { get; set; }

    /// <summary>
    /// Indicates whether the result is the final transcript of the speech recognition.
    /// </summary>
    public bool IsFinalResult { get; set; }

    /// <summary>
    /// The options used for this particular instance of speech recognition.
    /// </summary>
    public SpeechRecognitionOptions Options { get; set; }

    /// <summary>
    /// The audio data used for speech recognition.
    /// </summary>
    public byte[] AudioData { get; set; }
}