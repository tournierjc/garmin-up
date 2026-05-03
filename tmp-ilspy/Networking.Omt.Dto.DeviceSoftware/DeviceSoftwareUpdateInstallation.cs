namespace Networking.Omt.Dto.DeviceSoftware;

public class DeviceSoftwareUpdateInstallation
{
	public bool IsProductInstalled { get; init; }

	public bool IsReinstall { get; init; }

	public string? Path { get; init; }

	public string? FileName { get; init; }

	public int Order { get; init; }

	public string[]? Instructions { get; init; }

	public bool IsRestartRequired { get; init; }

	public bool IsDelta { get; init; }
}
