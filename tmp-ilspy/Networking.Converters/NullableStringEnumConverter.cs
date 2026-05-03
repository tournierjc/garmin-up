using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Networking.Converters;

internal sealed class NullableStringEnumConverter : StringEnumConverter
{
	private static readonly Dictionary<Type, Dictionary<string, object>> s_map = new Dictionary<Type, Dictionary<string, object>>();

	private static readonly object s_lock = new object();

	public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
	{
		if (reader.TokenType == JsonToken.Null)
		{
			return null;
		}
		if (reader.TokenType != JsonToken.String)
		{
			throw new JsonException($"Received unexpected TokenType {reader.TokenType}");
		}
		Type type = Nullable.GetUnderlyingType(objectType) ?? objectType;
		lock (s_lock)
		{
			if (!s_map.ContainsKey(type))
			{
				s_map[type] = new Dictionary<string, object>();
				FieldInfo[] fields = type.GetFields(BindingFlags.Static | BindingFlags.Public);
				foreach (FieldInfo fieldInfo in fields)
				{
					string key = fieldInfo.GetCustomAttribute<EnumMemberAttribute>()?.Value ?? fieldInfo.Name;
					s_map[type][key] = Enum.Parse(type, fieldInfo.Name);
				}
			}
			string key2 = (string)reader.Value;
			if (s_map[type].TryGetValue(key2, out object value))
			{
				return value;
			}
			return null;
		}
	}
}
