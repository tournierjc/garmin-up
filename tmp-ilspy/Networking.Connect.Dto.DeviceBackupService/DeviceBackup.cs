using System;

namespace Networking.Connect.Dto.DeviceBackupService;

public record DeviceBackup(DateTime Date, uint DeviceId, long DeviceBackupId, string DeviceName, Uri DeviceImageUrl);
