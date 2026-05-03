using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Networking.Omt.Dto.Common;

public record Release
{
	public required string ProductGroupName { get; init; }

	public required string ProductGroupCode { get; init; }

	public required int MajorVersion { get; init; }

	public required int MinorVersion { get; init; }

	public required string[] Tags { get; init; }

	[CompilerGenerated]
	[SetsRequiredMembers]
	protected Release(Release original)
	{
		ProductGroupName = original.ProductGroupName;
		ProductGroupCode = original.ProductGroupCode;
		MajorVersion = original.MajorVersion;
		MinorVersion = original.MinorVersion;
		Tags = original.Tags;
	}

	public Release()
	{
	}
}
