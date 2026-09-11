namespace ConfigTableGenerator;

[Flags]
internal enum ExportTarget
{
    None = 0,
    Client = 1,
    Server = 2,
    All = Client | Server
}

internal enum FieldSourceKind
{
    Direct,
    CreateFile,
    Array,
    List,
    CustomMember
}

internal enum ConfigTypeKind
{
    Scalar,
    Array,
    List,
    Custom
}

internal sealed class ConfigType
{
    public required ConfigTypeKind Kind { get; init; }
    public required string Name { get; init; }
    public ConfigType? ElementType { get; init; }
    public IReadOnlyList<ConfigMember> Members { get; init; } = Array.Empty<ConfigMember>();

    public static ConfigType Scalar(string typeName)
    {
        string name = typeName.Trim().ToLowerInvariant();
        string[] supported = ["uint", "int", "bool", "string", "float", "long", "double"];
        if (!supported.Contains(name, StringComparer.Ordinal))
        {
            throw new InvalidDataException($"不支持的字段类型：{typeName}");
        }

        return new ConfigType { Kind = ConfigTypeKind.Scalar, Name = name };
    }
}

internal sealed class ConfigMember
{
    public required string Name { get; init; }
    public required ConfigType Type { get; init; }
}

internal sealed class FieldSource
{
    public required FieldSourceKind Kind { get; init; }
    public string? FileSuffix { get; init; }
    public string? MemberName { get; init; }
    public ConfigType? MemberType { get; init; }
}

internal sealed class PhysicalColumn
{
    public required int Column { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required ConfigType Type { get; init; }
    public required FieldSource Source { get; init; }
    public required ExportTarget Export { get; init; }
}

internal sealed class PhysicalRow
{
    public required int ExcelRow { get; init; }
    public required IReadOnlyDictionary<int, string> Values { get; init; }
}

internal sealed class ConfigSheetModel
{
    public required string SourcePath { get; init; }
    public required string SheetName { get; init; }
    public required string ClassName { get; init; }
    public required string KeyFieldName { get; init; }
    public required IReadOnlyList<PhysicalColumn> Columns { get; init; }
    public required IReadOnlyList<PhysicalRow> Rows { get; init; }
}

internal sealed class LogicalField
{
    public required string Name { get; init; }
    public required ConfigType Type { get; init; }
    public required IReadOnlyList<PhysicalColumn> Columns { get; init; }
}

internal sealed class LogicalRow
{
    public required int ExcelRow { get; init; }
    public required IReadOnlyList<object> Values { get; init; }
}

internal sealed class ConfigOutputModel
{
    public required string SourcePath { get; init; }
    public required string SheetName { get; init; }
    public required string ClassName { get; init; }
    public required string KeyFieldName { get; init; }
    public required ExportTarget Target { get; init; }
    public required string? FileSuffix { get; init; }
    public required uint FormatVersion { get; init; }
    public required IReadOnlyList<LogicalField> Fields { get; init; }
    public required IReadOnlyList<LogicalRow> Rows { get; init; }
}
