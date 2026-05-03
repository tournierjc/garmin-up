using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Networking.Omt.Dto.Common;

public record FileToRemove
{
	public required string FileName { get; init; }

	public required string PartNumber { get; init; }

	public required ulong SizeInBytes { get; init; }

	[CompilerGenerated]
	[SetsRequiredMembers]
	protected FileToRemove(FileToRemove original)
	{
		FileName = original.FileName;
		PartNumber = original.PartNumber;
		SizeInBytes = original.SizeInBytes;
	}

	public FileToRemove()
	{
	}
}
