using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Networking.Omt.Dto.Common;

public record ContentsUnlockCode
{
	public required string? Code { get; init; }

	public required string[]? PartNumbers { get; init; }

	[CompilerGenerated]
	[SetsRequiredMembers]
	protected ContentsUnlockCode(ContentsUnlockCode original)
	{
		Code = original.Code;
		PartNumbers = original.PartNumbers;
	}

	public ContentsUnlockCode()
	{
	}
}
