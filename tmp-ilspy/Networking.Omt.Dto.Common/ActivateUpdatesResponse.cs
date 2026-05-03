using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Networking.Omt.Dto.Common;

public record ActivateUpdatesResponse
{
	public required byte[] SignedSdCardBytes { get; init; }

	public required ContentsUnlock[] Unlocks { get; init; }

	public required EmbeddedUnlock[] EmbeddedUnlocks { get; init; }

	[CompilerGenerated]
	[SetsRequiredMembers]
	protected ActivateUpdatesResponse(ActivateUpdatesResponse original)
	{
		SignedSdCardBytes = original.SignedSdCardBytes;
		Unlocks = original.Unlocks;
		EmbeddedUnlocks = original.EmbeddedUnlocks;
	}

	public ActivateUpdatesResponse()
	{
	}
}
