using System.Security.Cryptography;

namespace Networking;

internal static class Hash
{
	public static byte[] GetSha1(string data)
	{
		using IncrementalHash incrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
		incrementalHash.AppendData(data);
		return incrementalHash.GetHashAndReset();
	}
}
