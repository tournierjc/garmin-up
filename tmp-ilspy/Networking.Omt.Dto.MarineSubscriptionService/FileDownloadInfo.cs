using System;

namespace Networking.Omt.Dto.MarineSubscriptionService;

public class FileDownloadInfo
{
	public required long CreationDay { get; init; }

	public required string DownloadFilePath { get; init; }

	public required bool Expires { get; init; }

	public required string FileFlags { get; init; }

	public required string FileName { get; init; }

	public required long FileSize { get; init; }

	public required long MapId { get; init; }

	public required string Md5 { get; init; }

	public required long ProductIdentifier { get; init; }

	public required bool RemoveFile { get; init; }

	public required Uri Url { get; init; }
}
