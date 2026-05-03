using System.Collections.Generic;
using System.Linq;
using DeviceXmlUtil;

namespace Networking.Omt.Dto.Common;

public record FullUnitInfo : BasicUnitInfo
{
	public string SoftwarePartNumber { get; }

	public string SoftwareVersion { get; }

	public UnitUpdateFile[] UpdateFiles { get; }

	public UnitDataType[] DataTypes { get; }

	public FullUnitInfo(XmlDevice xmlDevice)
		: base(xmlDevice)
	{
		SoftwarePartNumber = xmlDevice.Model.PartNumber;
		SoftwareVersion = xmlDevice.Model.SoftwareVersion;
		UpdateFiles = xmlDevice.MassStorageMode.UpdateFiles.Select((UpdateFile x) => new UnitUpdateFile
		{
			FileName = x.FileName,
			MajorVersion = x.Version.Major,
			MinorVersion = x.Version.Minor,
			PartNumber = x.PartNumber,
			Path = x.Path
		}).ToArray();
		DataTypes = xmlDevice.MassStorageMode.DataTypes.Select<KeyValuePair<string, DataType>, UnitDataType>((KeyValuePair<string, DataType> x) => new UnitDataType
		{
			Name = x.Key,
			Locations = x.Value.Files.Select((DataType.File y) => new UnitDataTypeLocation
			{
				BaseName = (y.Location.BaseName ?? string.Empty),
				Extension = y.Location.FileExtension,
				Path = y.Location.Path
			}).ToArray()
		}).ToArray();
	}
}
