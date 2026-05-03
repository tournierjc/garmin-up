using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Networking.Omt.Dto.Common;

public record ContentsUnlock
{
	public required string FileName { get; init; }

	public required byte[] Gma { get; init; }

	public required ContentsUnlockCode[] Codes { get; init; }

	[CompilerGenerated]
	[SetsRequiredMembers]
	protected ContentsUnlock(ContentsUnlock original)
	{
		FileName = original.FileName;
		Gma = original.Gma;
		Codes = original.Codes;
	}

	public ContentsUnlock()
	{
	}
}
