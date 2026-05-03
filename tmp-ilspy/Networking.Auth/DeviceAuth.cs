using LiteDB;
using Networking.Connect.Dto.UserProfileService;

namespace Networking.Auth;

internal sealed class DeviceAuth
{
	[BsonId]
	public long UnitId { get; set; }

	public ConnectAuth? ConnectAuth { get; set; }

	public ITAuth? ITAuth { get; set; }

	public ConnectAccount? ConnectAccount { get; set; }

	public DIToken? DIToken { get; set; }
}
