namespace Networking;

public sealed record UnitId(uint Id)
{
	public override string ToString()
	{
		return Id.ToString();
	}
}
