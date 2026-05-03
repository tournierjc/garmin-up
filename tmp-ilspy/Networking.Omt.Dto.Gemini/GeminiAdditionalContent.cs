using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Networking.Omt.Dto.Gemini;

public record GeminiAdditionalContent : GeminiContent
{
	public required string PartNumber { get; init; }

	public required string? Locale { get; init; }

	[CompilerGenerated]
	[SetsRequiredMembers]
	protected GeminiAdditionalContent(GeminiAdditionalContent original)
		: base(original)
	{
		PartNumber = original.PartNumber;
		Locale = original.Locale;
	}

	public GeminiAdditionalContent()
	{
	}
}
