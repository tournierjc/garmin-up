using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Networking.Converters;

internal class ByteArrayHexStringConverter : JsonConverter<string>
{
	public override string? ReadJson(JsonReader reader, Type objectType, string? existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		return BitConverter.ToString(JArray.Load(reader).ToObject<byte[]>()).Replace("-", "");
	}

	public override void WriteJson(JsonWriter writer, string? value, JsonSerializer serializer)
	{
		throw new NotImplementedException();
	}
}
