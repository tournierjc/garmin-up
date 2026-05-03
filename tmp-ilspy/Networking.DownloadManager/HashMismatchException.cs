using System;

namespace Networking.DownloadManager;

public class HashMismatchException : Exception
{
	public string Expected { get; }

	public string Actual { get; }

	internal HashMismatchException(string expected, string actual)
	{
		Expected = expected;
		Actual = actual;
	}
}
