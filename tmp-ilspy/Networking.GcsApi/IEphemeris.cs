using System.Threading.Tasks;

namespace Networking.GcsApi;

public interface IEphemeris
{
	public delegate IEphemeris Factory(UnitId unitId);

	Task<byte[]> GetMtkEphemerisAsync(string softwarePartNumber);

	Task<byte[]> GetSonyEphemerisAsync(string softwarePartNumber, string coverage);
}
