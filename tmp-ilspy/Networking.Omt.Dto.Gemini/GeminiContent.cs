using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Networking.Omt.Dto.Gemini;

public abstract record GeminiContent
{
	public required string ContentType { get; init; }

	public required ImmutableArray<GeminiContentUrl> Deliverables { get; init; }

	public required ImmutableArray<GeminiContentToReplace> ContentsToReplace { get; init; }

	public required bool IsReinstall { get; init; }

	[CompilerGenerated]
	[SetsRequiredMembers]
	protected GeminiContent(GeminiContent original)
	{
		ContentType = original.ContentType;
		Deliverables = original.Deliverables;
		ContentsToReplace = original.ContentsToReplace;
		IsReinstall = original.IsReinstall;
	}

	protected GeminiContent()
	{
	}
}
