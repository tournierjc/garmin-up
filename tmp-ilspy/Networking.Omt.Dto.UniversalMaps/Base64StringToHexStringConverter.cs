using System;
using Newtonsoft.Json;

namespace Networking.Omt.Dto.UniversalMaps;

internal sealed class Base64StringToHexStringConverter : JsonConverter<string>
{
	public override string ReadJson(JsonReader reader, Type objectType, string? existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		return BitConverter.ToString(Convert.FromBase64String((string)reader.Value)).Replace("-", string.Empty).ToLowerInvariant();
	}

	public override void WriteJson(JsonWriter writer, string? value, JsonSerializer serializer)
	{
		throw new NotSupportedException();
	}
}
