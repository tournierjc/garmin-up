using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Networking.Omt.Dto.UniversalMaps;
using Newtonsoft.Json;

namespace Networking.Omt.Dto.MarineSubscriptionService;

public record ManifestFileDownloadInfo
{
	public required long FileSize { get; init; }

	[JsonConverter(typeof(Base64StringToHexStringConverter))]
	public required string Md5 { get; init; }

	public required Uri Url { get; init; }

	[CompilerGenerated]
	[SetsRequiredMembers]
	protected ManifestFileDownloadInfo(ManifestFileDownloadInfo original)
	{
		FileSize = original.FileSize;
		Md5 = original.Md5;
		Url = original.Url;
	}

	public ManifestFileDownloadInfo()
	{
	}
}
