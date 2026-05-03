using System;
using Newtonsoft.Json;

namespace Networking.Omt.Dto.MarineSubscriptionService;

internal sealed class ChartDateTimeConverter : JsonConverter<DateTime>
{
	private class SubscriptionExpirationDate
	{
		[JsonProperty("seconds")]
		public long Seconds { get; set; }
	}

	public override DateTime ReadJson(JsonReader reader, Type objectType, DateTime existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		SubscriptionExpirationDate subscriptionExpirationDate = serializer.Deserialize<SubscriptionExpirationDate>(reader);
		return new DateTime(1970, 1, 1).AddSeconds(subscriptionExpirationDate.Seconds);
	}

	public override void WriteJson(JsonWriter writer, DateTime value, JsonSerializer serializer)
	{
		throw new NotSupportedException();
	}
}
