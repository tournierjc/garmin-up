using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Networking.Omt.Dto.Common;

namespace Networking.Omt.Dto.Gemini;

public record GeminiMapSection : GeminiContent
{
	public required GeminiMapSectionIdentifier Identifier { get; init; }

	public required ImmutableArray<GeminiAdditionalContent> AdditionalContents { get; init; }

	public required Release Release { get; init; }

	public required string DisplayName { get; init; }

	public required ImmutableArray<string> EulaUrls { get; init; }

	[CompilerGenerated]
	[SetsRequiredMembers]
	protected GeminiMapSection(GeminiMapSection original)
		: base(original)
	{
		Identifier = original.Identifier;
		AdditionalContents = original.AdditionalContents;
		Release = original.Release;
		DisplayName = original.DisplayName;
		EulaUrls = original.EulaUrls;
	}

	public GeminiMapSection()
	{
	}
}
