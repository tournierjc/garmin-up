// Serialize Garmin Express map-update protobuf DTOs using the real assemblies
// (Garmin.Cartography.Services.Interface.ProtoBufService.Dto.dll + protobuf-net.dll).
//
// Usage:
//   dotnet build -p:GarminExpressDir=/path/to/Garmin/Express
//   dotnet run -- preload <serial>
//   dotnet run -- download <serial> <softwarePartNumber> <softwareVersion> <mapPartNumber>
//
// Flags: --hex (default) | --raw
//
// Requires .NET 8+ SDK.

using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

internal static class Program
{
    private static Assembly? _protobufNet;
    private static Assembly? _dto;

    private static int Main(string[] args)
    {
        var (flags, rest) = SplitFlags(args);
        var asHex = !flags.Contains("raw");

        if (rest.Length < 1)
        {
            PrintUsage();
            return 1;
        }

        var sub = rest[0].ToLowerInvariant();
        if (sub != "wiretest" && rest.Length < 2)
        {
            PrintUsage();
            return 1;
        }

        if (!LoadAssemblies())
            return 1;

        try
        {
            return sub switch
            {
                "wiretest" => EmitWireTestGolden(asHex),
                "preload" when rest.Length >= 2 => EmitPreloaded(rest[1], asHex),
                "download" when rest.Length >= 5 => EmitDownload(rest[1], rest[2], rest[3], rest[4], asHex),
                _ => UsageFail(),
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static int UsageFail()
    {
        PrintUsage();
        return 1;
    }

    private static bool LoadAssemblies()
    {
        var baseDir = AppContext.BaseDirectory;
        var dtoName = "Garmin.Cartography.Services.Interface.ProtoBufService.Dto.dll";
        var pbnName = "protobuf-net.dll";
        var dtoPath = Path.Combine(baseDir, dtoName);
        var pbnPath = Path.Combine(baseDir, pbnName);
        if (!File.Exists(dtoPath) || !File.Exists(pbnPath))
        {
            Console.Error.WriteLine(
                "Garmin DLLs not found next to the executable. Build with a valid GarminExpressDir.");
            Console.Error.WriteLine($"Expected: {dtoPath}");
            Console.Error.WriteLine($"Expected: {pbnPath}");
            return false;
        }

        try
        {
            _protobufNet = Assembly.LoadFrom(pbnPath);
            _dto = Assembly.LoadFrom(dtoPath);
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("LoadFrom failed: " + ex.Message);
            return false;
        }
    }

    private static int EmitPreloaded(string serial, bool asHex)
    {
        var dto = _dto ?? throw new InvalidOperationException("DTO assembly not loaded.");
        var reqType = FindType(dto, "PreloadedMapUpdatesRequest")
            ?? throw new InvalidOperationException("PreloadedMapUpdatesRequest not found.");
        var req = Activator.CreateInstance(reqType)
            ?? throw new InvalidOperationException("Failed to create request instance.");

        SetPascal(req, "ClientInfo", BuildGarminClient(dto));

        var basicType = GetPropertyType(reqType, "BasicUnitInfo")
            ?? throw new InvalidOperationException("BasicUnitInfo property missing.");
        var basic = Activator.CreateInstance(basicType)
            ?? throw new InvalidOperationException("Failed to create BasicUnitInfo.");
        SetPascal(basic, "UnitId", 0L);
        SetPascal(basic, "SerialNumber", serial);
        SetNullableLong(basic, "FirstFix", null);
        SetPascal(req, "BasicUnitInfo", basic);

        SetPascal(req, "IsUserInteractive", true);
        // Older Express DTO builds omit AutoCheckSettings on PreloadedMapUpdatesRequest entirely.

        return WriteSerialized(req, asHex);
    }

    /// <summary>Fixed payload kept in sync with garmin_up_lib::maps::omt::tests::preloaded_map_updates_request_wire_matches_express_layout.</summary>
    private static int EmitWireTestGolden(bool asHex)
    {
        var dto = _dto ?? throw new InvalidOperationException("DTO assembly not loaded.");
        var reqType = FindType(dto, "PreloadedMapUpdatesRequest")
            ?? throw new InvalidOperationException("PreloadedMapUpdatesRequest not found.");
        var req = Activator.CreateInstance(reqType)
            ?? throw new InvalidOperationException("Failed to create request instance.");

        var client = BuildGarminClientFixed(dto);
        SetPascal(req, "ClientInfo", client);

        var basicType = GetPropertyType(reqType, "BasicUnitInfo")
            ?? throw new InvalidOperationException("BasicUnitInfo property missing.");
        var basic = Activator.CreateInstance(basicType)
            ?? throw new InvalidOperationException("Failed to create BasicUnitInfo.");
        SetPascal(basic, "UnitId", 42L);
        SetPascal(basic, "SerialNumber", "SN1");
        SetNullableLong(basic, "FirstFix", null);
        SetPascal(req, "BasicUnitInfo", basic);
        SetPascal(req, "IsUserInteractive", true);

        return WriteSerialized(req, asHex);
    }

    private static object BuildGarminClientFixed(Assembly dto)
    {
        var clientType = dto.GetTypes().FirstOrDefault(t =>
            t is { Name: "Client", Namespace: not null } &&
            t.Namespace.Contains("ProtoBufService.Dto.Common", StringComparison.Ordinal))
            ?? throw new InvalidOperationException("Common.Client not found.");

        var client = Activator.CreateInstance(clientType)
            ?? throw new InvalidOperationException("Failed to create Client.");
        SetPascal(client, "ClientType", "GarminExpress");
        SetPascal(client, "LocaleCode", "en_US");
        SetPascal(client, "OperatingSystemType", "Linux");
        SetPascal(client, "OperatingSystemVersion", "Linux test");
        return client;
    }

    private static int EmitDownload(
        string serial,
        string softwarePartNumber,
        string softwareVersion,
        string mapPartNumber,
        bool asHex)
    {
        var dto = _dto ?? throw new InvalidOperationException("DTO assembly not loaded.");
        var reqType = FindType(dto, "DownloadDetailsRequest")
            ?? throw new InvalidOperationException("DownloadDetailsRequest not found.");
        var req = Activator.CreateInstance(reqType)
            ?? throw new InvalidOperationException("Failed to create DownloadDetailsRequest.");

        SetPascal(req, "ClientInfo", BuildGarminClient(dto));

        var fullType = GetPropertyType(reqType, "FullUnitInfo")
            ?? throw new InvalidOperationException("FullUnitInfo property missing.");
        var full = Activator.CreateInstance(fullType)
            ?? throw new InvalidOperationException("Failed to create FullUnitInfo.");
        SetPascal(full, "UnitId", 0L);
        SetPascal(full, "SerialNumber", serial);
        SetNullableLong(full, "FirstFix", null);
        SetPascal(full, "SoftwarePartNumber", softwarePartNumber);
        SetPascal(full, "SoftwareVersion", softwareVersion);
        SetStringList(full, "SupportedContentTypes");
        SetStringList(full, "CurrentPartNumbers");
        SetPascal(req, "FullUnitInfo", full);
        SetPascal(req, "PartNumber", mapPartNumber);
        // Some Express builds omit FilesToRemove on DownloadDetailsRequest.

        return WriteSerialized(req, asHex);
    }

    private static object BuildGarminClient(Assembly dto)
    {
        var clientType = dto.GetTypes().FirstOrDefault(t =>
            t is { Name: "Client", Namespace: not null } &&
            t.Namespace.Contains("ProtoBufService.Dto.Common", StringComparison.Ordinal))
            ?? throw new InvalidOperationException("Common.Client not found.");

        var client = Activator.CreateInstance(clientType)
            ?? throw new InvalidOperationException("Failed to create Client.");

        var locale = Environment.GetEnvironmentVariable("LANG")?.Split('.').FirstOrDefault() ?? "en_US";
        var osDesc = RuntimeInformation.OSDescription;
        var osKind = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "Windows"
            : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? "MacOSX"
                : "Linux";

        SetPascal(client, "ClientType", "GarminExpress");
        SetPascal(client, "LocaleCode", locale);
        SetPascal(client, "OperatingSystemType", osKind);
        SetPascal(client, "OperatingSystemVersion", osDesc);
        return client;
    }

    private static void SetNullableLong(object target, string name, long? value)
    {
        var p = target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
            ?? throw new InvalidOperationException($"{target.GetType().Name} missing '{name}'.");
        if (!p.CanWrite)
            throw new InvalidOperationException($"{name} not writable.");
        p.SetValue(target, value);
    }

    private static (HashSet<string> flags, string[] rest) SplitFlags(string[] args)
    {
        var flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var restList = new List<string>();
        foreach (var a in args)
        {
            if (a.Equals("--hex", StringComparison.OrdinalIgnoreCase))
                flags.Add("hex");
            else if (a.Equals("--raw", StringComparison.OrdinalIgnoreCase))
                flags.Add("raw");
            else
                restList.Add(a);
        }

        return (flags, restList.ToArray());
    }

    private static void PrintUsage()
    {
        var sb = new StringBuilder();
        sb.AppendLine("omt-probe — serialize map-update protobufs with Garmin Express DLLs.");
        sb.AppendLine();
        sb.AppendLine("Build: dotnet build -p:GarminExpressDir=/path/to/Garmin/Express");
        sb.AppendLine("Run:   dotnet run -- [--hex|--raw] wiretest");
        sb.AppendLine("       dotnet run -- [--hex|--raw] preload <serial>");
        sb.AppendLine("       dotnet run -- [--hex|--raw] download <serial> <swPart> <swVersion> <mapPart>");
        Console.Error.Write(sb.ToString());
    }

    private static Type? FindType(Assembly asm, string name)
    {
        try
        {
            return asm.GetTypes().FirstOrDefault(t => t.Name == name);
        }
        catch (ReflectionTypeLoadException ex)
        {
            var first = ex.LoaderExceptions?.FirstOrDefault();
            throw new InvalidOperationException("ReflectionTypeLoadException: " + first?.Message, ex);
        }
    }

    private static Type? GetPropertyType(Type owner, string propName)
    {
        var p = owner.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        return p?.PropertyType;
    }

    private static void SetPascal(object target, string name, object? value)
    {
        var t = target.GetType();
        var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (p == null || !p.CanWrite)
            throw new InvalidOperationException($"{t.Name} has no writable property '{name}'.");

        var pt = p.PropertyType;
        var converted = ConvertForProperty(pt, value);
        p.SetValue(target, converted);
    }

    private static object? ConvertForProperty(Type pt, object? value)
    {
        if (value == null)
            return null;
        if (pt.IsInstanceOfType(value))
            return value;
        if (pt.IsEnum && value is int iv)
            return Enum.ToObject(pt, iv);
        if (Nullable.GetUnderlyingType(pt) is { } ut)
            return Convert.ChangeType(value, ut);
        return Convert.ChangeType(value, pt);
    }

    private static void SetStringList(object target, string propName)
    {
        var p = target.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
            ?? throw new InvalidOperationException($"{target.GetType().Name} missing '{propName}'.");

        var pt = p.PropertyType;
        IList list = CreateListForProperty(pt, typeof(string));
        p.SetValue(target, list);
    }

    private static IList CreateListForProperty(Type propertyType, Type elementType)
    {
        if (propertyType.IsInterface || propertyType.IsAbstract)
        {
            var concrete = typeof(List<>).MakeGenericType(elementType);
            return (IList)(Activator.CreateInstance(concrete) ?? throw new InvalidOperationException("List alloc failed"));
        }

        return (IList)(Activator.CreateInstance(propertyType) ?? throw new InvalidOperationException($"Cannot create {propertyType.Name}"));
    }

    private static int WriteSerialized(object message, bool asHex)
    {
        var pbn = _protobufNet ?? throw new InvalidOperationException("protobuf-net not loaded.");
        var serializerType = pbn.GetType("ProtoBuf.Serializer")
            ?? throw new InvalidOperationException("ProtoBuf.Serializer type not found.");

        using var ms = new MemoryStream();
        var gm = serializerType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m is { Name: "Serialize", IsGenericMethodDefinition: true })
            .Select(m => (m, ps: m.GetParameters()))
            .Where(x => x.ps.Length == 2 && x.ps[0].ParameterType == typeof(Stream))
            .Select(x => x.m)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("Serialize(Stream, T) not found on ProtoBuf.Serializer.");

        var concrete = gm.MakeGenericMethod(message.GetType());
        concrete.Invoke(null, new object[] { ms, message });
        var bytes = ms.ToArray();

        if (asHex)
        {
            Console.WriteLine(Convert.ToHexString(bytes));
            Console.WriteLine(bytes.Length);
        }
        else
        {
            using var stdout = Console.OpenStandardOutput();
            stdout.Write(bytes, 0, bytes.Length);
        }

        return 0;
    }
}
