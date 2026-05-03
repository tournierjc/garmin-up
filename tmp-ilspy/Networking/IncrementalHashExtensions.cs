using System;
using System.Security.Cryptography;
using System.Text;

namespace Networking;

public static class IncrementalHashExtensions
{
	public static void AppendData(this IncrementalHash hash, string data)
	{
		hash.AppendData(Encoding.UTF8.GetBytes(data));
	}

	public static void AppendData(this IncrementalHash hash, long data)
	{
		hash.AppendData(BitConverter.GetBytes(data));
	}

	public static void AppendData(this IncrementalHash hash, string[] data)
	{
		foreach (string data2 in data)
		{
			hash.AppendData(data2);
		}
	}

	public static void AppendData(this IncrementalHash hash, Guid data)
	{
		hash.AppendData(data.ToByteArray());
	}

	public static void AppendData(this IncrementalHash hash, Enum value)
	{
		hash.AppendData($"{value.GetType().FullName}.{value}");
	}
}
