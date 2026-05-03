using System;

namespace Networking.Omt.Dto.SdCardUpdateService;

public record SdCardUpdateResponse(Guid UniqueCardId, Map[] Maps);
