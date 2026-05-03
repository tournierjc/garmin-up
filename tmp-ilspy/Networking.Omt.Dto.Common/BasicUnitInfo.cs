using DeviceXmlUtil;

namespace Networking.Omt.Dto.Common;

public record BasicUnitInfo
{
	public long UnitId { get; }

	public long? FirstFix { get; }

	public string? SerialNumber { get; }

	public BasicUnitInfo(UnitId unitId)
	{
		UnitId = unitId.Id;
	}

	public BasicUnitInfo(XmlDevice xmlDevice)
	{
		UnitId = xmlDevice.Id;
		FirstFix = xmlDevice.Extensions.DeviceExtension?.Ifix;
	}
}
