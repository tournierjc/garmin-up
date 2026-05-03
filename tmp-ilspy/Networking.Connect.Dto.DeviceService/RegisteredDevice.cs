namespace Networking.Connect.Dto.DeviceService;

public record RegisteredDevice(string ProductDisplayName, long UnitId, bool Primary, bool PrimaryTrainingCapable, bool PrimaryActivityTrackerIndicator, bool IsPrimaryUser, bool LhaBackupCapable, bool Hybrid, bool Wellness);
