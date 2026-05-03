using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Networking.Omt.Dto.UniversalMaps;

public record Content
{
	public required string PartNumber { get; init; }

	public required string FileName { get; init; }

	public required string DataType { get; init; }

	public required DeliverableOption[] DeliverableOptions { get; init; }

	public required string Locale { get; init; }

	public required string ExternalFileName { get; init; }

	[CompilerGenerated]
	[SetsRequiredMembers]
	protected Content(Content original)
	{
		PartNumber = original.PartNumber;
		FileName = original.FileName;
		DataType = original.DataType;
		DeliverableOptions = original.DeliverableOptions;
		Locale = original.Locale;
		ExternalFileName = original.ExternalFileName;
	}

	public Content()
	{
	}
}
