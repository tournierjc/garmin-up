using System;

namespace Networking.Omt.Dto.DeviceSoftware;

public class DeviceSoftwareUpdateDeliverable
{
	public string? DownloadUrl { get; init; }

	public string? Md5 { get; init; }

	public long SizeInBytes { get; init; }

	public required Uri SecureDownloadUrl { get; set; }
}
