using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Networking.Omt.Dto.Common;
using Newtonsoft.Json;

namespace Networking.Omt.Dto.UniversalMaps;

public record DeliverableOption
{
	[JsonConverter(typeof(Base64StringToHexStringConverter))]
	public required string Md5 { get; init; }

	public required ulong SizeInBytes { get; init; }

	public required UrlType Type { get; init; }

	public required string Url { get; init; }

	[CompilerGenerated]
	[SetsRequiredMembers]
	protected DeliverableOption(DeliverableOption original)
	{
		Md5 = original.Md5;
		SizeInBytes = original.SizeInBytes;
		Type = original.Type;
		Url = original.Url;
	}

	public DeliverableOption()
	{
	}
}
