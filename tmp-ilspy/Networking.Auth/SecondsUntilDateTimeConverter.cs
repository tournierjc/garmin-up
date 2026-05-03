using System;
using Newtonsoft.Json;

namespace Networking.Auth;

internal sealed class SecondsUntilDateTimeConverter : JsonConverter<DateTime>
{
	public override DateTime ReadJson(JsonReader reader, Type objectType, DateTime existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		return DateTime.Now.AddSeconds(Convert.ToDouble(reader.Value));
	}

	public override void WriteJson(JsonWriter writer, DateTime value, JsonSerializer serializer)
	{
		throw new NotSupportedException();
	}
}
