using System.Collections.Immutable;

namespace Networking.Omt.Dto.Gemini;

public class ActivateUpdatesResponse
{
	public required ImmutableArray<ContentsUnlock> ContentsUnlocks { get; init; }
}
