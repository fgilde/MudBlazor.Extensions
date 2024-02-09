using System.ComponentModel;

namespace MudBlazor.Extensions.Components;

public enum VisualizationMode
{
    DescreteFrequencies = 0,
    AreaGraph,

    Waveform,
    
    FullOctaveBands,

    HalfOctaveBands,

    ThirdOctaveBands,

    
    LineGraph,

    Default
}

public enum MirrorMode
{
    None = 0,
    Left = -1,
    Right = 1,
}


public enum ChannelLayout
{
    [Description("single")]
    Single,

    [Description("dual-horizontal")]
    DualHorizontal,

    [Description("dual-vertical")]
    DualVertical,

    [Description("dual-combined")]
    DualCombined
}

public enum ColorMode
{
    [Description("gradient")]
    Gradient,

    [Description("bar-index")]
    BarIndex,

    [Description("bar-level")]
    BarLevel
}

public enum FrequencyScale
{
    [Description("log")]
    Log,

    [Description("bark")]
    Bark,

    [Description("mel")]
    Mel,

    [Description("linear")]
    Linear
}

public enum WeightingFilter
{
    [Description("")]
    None,

    [Description("A")]
    A,

    [Description("B")]
    B,

    [Description("C")]
    C,

    [Description("D")]
    D,

    [Description("468")]
    Filter468
}

public enum AudioMotionGradient
{
    [Description("classic")]
    Classic,

    [Description("prism")]
    Prism,

    [Description("rainbow")]
    Rainbow,

    [Description("orangered")]
    OrangeRed,

    [Description("steelblue")]
    SteelBlue

}