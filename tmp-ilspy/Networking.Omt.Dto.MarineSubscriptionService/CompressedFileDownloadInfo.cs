using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Networking.Omt.Dto.UniversalMaps;
using Newtonsoft.Json;

namespace Networking.Omt.Dto.MarineSubscriptionService;

public record CompressedFileDownloadInfo
{
	public required long CompressedFileSize { get; init; }

	[JsonConverter(typeof(Base64StringToHexStringConverter))]
	public required string Md5 { get; init; }

	public required Uri Url { get; init; }

	public required long TotalUncompressedFileSize { get; init; }

	[CompilerGenerated]
	[SetsRequiredMembers]
	protected CompressedFileDownloadInfo(CompressedFileDownloadInfo original)
	{
		CompressedFileSize = original.CompressedFileSize;
		Md5 = original.Md5;
		Url = original.Url;
		TotalUncompressedFileSize = original.TotalUncompressedFileSize;
	}

	public CompressedFileDownloadInfo()
	{
	}
}
