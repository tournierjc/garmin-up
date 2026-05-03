using Networking.Converters;
using Networking.Omt.Dto.Common;
using Newtonsoft.Json;

namespace Networking.Omt.Dto.Gemini;

public class GeminiContentUrl
{
	public required UrlType Type { get; init; }

	public required string Url { get; init; }

	[JsonConverter(typeof(ByteArrayHexStringConverter))]
	public required string Md5 { get; init; }

	public required long SizeInBytes { get; init; }
}
