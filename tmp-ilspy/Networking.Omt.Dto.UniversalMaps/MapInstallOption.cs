using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Networking.Omt.Dto.UniversalMaps;

public record MapInstallOption
{
	public required MapInstallIdentifier Identifier { get; init; }

	public required string DisplayName { get; init; }

	public required string PreviewUrl { get; init; }

	public required bool IsPreferred { get; init; }

	public required Content[] Files { get; init; }

	[CompilerGenerated]
	[SetsRequiredMembers]
	protected MapInstallOption(MapInstallOption original)
	{
		Identifier = original.Identifier;
		DisplayName = original.DisplayName;
		PreviewUrl = original.PreviewUrl;
		IsPreferred = original.IsPreferred;
		Files = original.Files;
	}

	public MapInstallOption()
	{
	}
}
