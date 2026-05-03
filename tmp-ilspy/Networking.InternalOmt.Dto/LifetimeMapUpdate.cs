using System.Collections.Generic;

namespace Networking.InternalOmt.Dto;

public record LifetimeMapUpdate(bool IsBundled, string Name, List<string> ProductGroups);
