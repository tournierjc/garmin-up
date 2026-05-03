namespace Networking.Auth;

public sealed class ConnectAuth
{
	public string Token { get; set; }

	public string TokenSecret { get; set; }

	public long UserId { get; set; }
}
