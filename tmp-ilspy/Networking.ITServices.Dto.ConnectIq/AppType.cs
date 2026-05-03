using System.Runtime.Serialization;

namespace Networking.ITServices.Dto.ConnectIq;

public enum AppType
{
	Unknown,
	WatchFace,
	[EnumMember(Value = "watch-app")]
	WatchApp,
	Widget,
	DataField,
	[EnumMember(Value = "audio-content-provider-app")]
	MusicApp,
	Activity
}
