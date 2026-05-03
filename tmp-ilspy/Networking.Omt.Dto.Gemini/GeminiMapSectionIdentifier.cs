using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Networking.Omt.Dto.Gemini;

public record GeminiMapSectionIdentifier
{
	public required string RegionPartNumber { get; init; }

	public required string MapImagePartNumber { get; init; }

	[CompilerGenerated]
	[SetsRequiredMembers]
	protected GeminiMapSectionIdentifier(GeminiMapSectionIdentifier original)
	{
		RegionPartNumber = original.RegionPartNumber;
		MapImagePartNumber = original.MapImagePartNumber;
	}

	public GeminiMapSectionIdentifier()
	{
	}
}
