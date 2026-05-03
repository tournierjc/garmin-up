using System;
using Utilities.Progress;

namespace Networking.DownloadManager;

public sealed record Downloadable(Uri Uri, long? Size, string Md5, string Destination, ProgressReporter Progress)
{
	public string Id { get; } = Guid.NewGuid().ToString().Substring(0, 8);
}
