namespace MudBlazor.Extensions.Core.W3C;

/// <summary>
/// Represents a hardware device.
/// </summary>
public class HardwareDeviceInfo : IEquatable<HardwareDeviceInfo>
{
    #region Equality

    public bool Equals(HardwareDeviceInfo other) => DeviceId == other.DeviceId;

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((HardwareDeviceInfo)obj);
    }

    public override int GetHashCode() => DeviceId != null ? DeviceId.GetHashCode() : 0;

    #endregion

    /// <summary>
    /// Device id.
    /// </summary>
    public string DeviceId { get; set; }

    /// <summary>
    /// Group id.
    /// </summary>
    public string GroupId { get; set; }

    /// <summary>
    /// Label of the device.
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// Kind of the device.
    /// </summary>
    public DeviceKind Kind { get; set; }


    /// <inheritdoc />
    public override string ToString() => Label;
}