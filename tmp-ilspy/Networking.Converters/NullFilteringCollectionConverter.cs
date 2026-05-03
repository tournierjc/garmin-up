using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Networking.Converters;

internal sealed class NullFilteringCollectionConverter : JsonConverter
{
	public override bool CanWrite => false;

	public override bool CanConvert(Type objectType)
	{
		throw new NotImplementedException();
	}

	public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
	{
		Type type = ((!objectType.IsGenericType) ? objectType.GetElementType() : objectType.GetGenericArguments()[0]);
		Type objectType2 = ((!type.IsValueType || !(Nullable.GetUnderlyingType(type) == null)) ? type : typeof(Nullable<>).MakeGenericType(type));
		JArray jArray = JArray.Load(reader);
		foreach (JToken item in jArray.ToList())
		{
			if (item.ToObject(objectType2, serializer) == null)
			{
				jArray.Remove(item);
			}
		}
		return jArray.ToObject(objectType);
	}

	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		throw new NotImplementedException();
	}
}
