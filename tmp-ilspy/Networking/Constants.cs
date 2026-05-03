using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Networking;

internal static class Constants
{
	public static string AppName => Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName);

	public static OSPlatform OSPlatform
	{
		get
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				return OSPlatform.Windows;
			}
			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				return OSPlatform.OSX;
			}
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				return OSPlatform.Linux;
			}
			throw new NotSupportedException();
		}
	}
}
