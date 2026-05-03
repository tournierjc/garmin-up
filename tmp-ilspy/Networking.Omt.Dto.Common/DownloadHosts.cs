using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Networking.Omt.Dto.Common;

public record DownloadHosts
{
	public required string ForegroundPrimaryHost { get; init; }

	public required string BackgroundPrimaryHost { get; init; }

	public required string[] FailoverHosts { get; init; }

	[CompilerGenerated]
	[SetsRequiredMembers]
	protected DownloadHosts(DownloadHosts original)
	{
		ForegroundPrimaryHost = original.ForegroundPrimaryHost;
		BackgroundPrimaryHost = original.BackgroundPrimaryHost;
		FailoverHosts = original.FailoverHosts;
	}

	public DownloadHosts()
	{
	}
}
