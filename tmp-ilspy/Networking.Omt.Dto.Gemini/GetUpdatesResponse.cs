using System.Collections.Immutable;
using Networking.Omt.Dto.Common;

namespace Networking.Omt.Dto.Gemini;

public class GetUpdatesResponse
{
	public required ImmutableArray<GeminiMapSection> MapImages { get; init; }

	public required DownloadHosts DownloadHosts { get; init; }

	public required ImmutableArray<GeminiFileToRemove> FilesToRemove { get; init; }

	public required ImmutableArray<PurchasableUpdate> PurchasableUpdates { get; init; }
}
