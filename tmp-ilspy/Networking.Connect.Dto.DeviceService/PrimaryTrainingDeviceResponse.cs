using System.Collections.Generic;

namespace Networking.Connect.Dto.DeviceService;

public record PrimaryTrainingDeviceResponse(PrimaryTrainingDevice PrimaryTrainingDevice, PrimaryTrainingDevicesInfo PrimaryTrainingDevices, List<RegisteredDevice> RegisteredDevices, WearableDevice WearableDevices);
