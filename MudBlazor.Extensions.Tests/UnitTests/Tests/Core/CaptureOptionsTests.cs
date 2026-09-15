using System.Text.Json;
using MudBlazor.Extensions.Core.Capture;
using MudBlazor.Extensions.Core.W3C;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Core;

/// <summary>
/// Issue #139: a camera stream opened with SelectCaptureSourceAsync can be handed to the capture
/// instead of opening the camera a second time. Covers what the JS side relies on.
/// </summary>
public class CaptureOptionsTests
{
    private static readonly JsonSerializerOptions JsInterop = new(JsonSerializerDefaults.Web);

    private static MediaStreamTrack Track(string id = "track-1") => new() { Id = id };

    [Fact]
    public void A_preselected_camera_source_alone_is_something_to_capture()
    {
        var options = new CaptureOptions { VideoSource = Track() };

        Assert.True(options.Valid());
    }

    [Fact]
    public void Empty_options_capture_nothing()
    {
        Assert.False(new CaptureOptions().Valid());
    }

    [Fact]
    public void Simple_options_report_a_preselected_camera_source_as_camera_recording()
    {
        var simple = SimpleCaptureOptions.From(new CaptureOptions { VideoSource = Track() });

        Assert.True(simple.RecordCamera);
    }

    [Fact]
    public void Source_and_keep_alive_flag_reach_the_js_side()
    {
        var json = JsonSerializer.Serialize(
            new CaptureOptions { VideoSource = Track("abc"), KeepSourceStreamsAlive = true }, JsInterop);

        using var parsed = JsonDocument.Parse(json);
        Assert.Equal("abc", parsed.RootElement.GetProperty("videoSource").GetProperty("id").GetString());
        Assert.True(parsed.RootElement.GetProperty("keepSourceStreamsAlive").GetBoolean());
    }

    [Fact]
    public void Keeping_sources_alive_is_off_by_default()
    {
        Assert.False(new CaptureOptions().KeepSourceStreamsAlive);
    }
}
