using System.Buffers.Binary;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ConfigTableGenerator;

internal static class GeneratedCodeSchema
{
    public static ConfigOutputModel Apply(string generatedCodePath, ConfigOutputModel model)
    {
        if (!File.Exists(generatedCodePath))
        {
            return Copy(model, model.Fields, model.Rows, 1);
        }

        string source = File.ReadAllText(generatedCodePath);
        IReadOnlyList<string> previousFieldNames = model.Target == ExportTarget.Client
            ? ReadCSharpDecodeOrder(source, model.ClassName, generatedCodePath)
            : ReadGoFieldNames(source, model.ClassName, generatedCodePath);
        ulong previousSchemaHash = ReadSchemaHash(source, model, generatedCodePath);
        bool hasFormatVersion = TryReadFormatVersion(source, model, out uint previousVersion);

        Dictionary<string, int> currentIndexes = model.Fields
            .Select((field, index) => new { field.Name, Index = index })
            .ToDictionary(item => item.Name, item => item.Index, StringComparer.Ordinal);
        List<int> orderedIndexes = new();
        foreach (string fieldName in previousFieldNames)
        {
            if (currentIndexes.Remove(fieldName, out int index))
            {
                orderedIndexes.Add(index);
            }
        }
        orderedIndexes.AddRange(currentIndexes.Values.OrderBy(index => index));

        List<LogicalField> fields = orderedIndexes.Select(index => model.Fields[index]).ToList();
        List<LogicalRow> rows = model.Rows.Select(row => new LogicalRow
        {
            ExcelRow = row.ExcelRow,
            Values = orderedIndexes.Select(index => row.Values[index]).ToList()
        }).ToList();
        ConfigOutputModel orderedModel = Copy(model, fields, rows, previousVersion);
        ulong currentSchemaHash = BinaryPrimitives.ReadUInt64LittleEndian(SchemaHash.Compute(orderedModel));
        uint formatVersion = hasFormatVersion && currentSchemaHash == previousSchemaHash
            ? previousVersion
            : checked(previousVersion + 1);
        return Copy(orderedModel, fields, rows, formatVersion);
    }

    private static IReadOnlyList<string> ReadCSharpDecodeOrder(string source, string className, string path)
    {
        Match classMatch = Regex.Match(source, $@"public\s+sealed\s+class\s+{Regex.Escape(className)}\s*:\s*ConfigRecord\s*\{{");
        if (!classMatch.Success)
        {
            throw new InvalidDataException($"无法从已生成代码读取类 {className}：{path}");
        }

        int constantsIndex = source.IndexOf("public const", classMatch.Index + classMatch.Length, StringComparison.Ordinal);
        if (constantsIndex < 0)
        {
            throw new InvalidDataException($"无法从已生成代码读取类 {className} 的常量：{path}");
        }

        Match decodeMatch = Regex.Match(source[constantsIndex..], @"(?:public\s+override\s+void|public\s+void)\s+Decode\s*\(ref\s+ProtoReader\s+reader\s*\)\s*\{");
        if (!decodeMatch.Success)
        {
            throw new InvalidDataException($"无法从已生成代码读取类 {className} 的 Decode：{path}");
        }

        string decodeSource = source[(constantsIndex + decodeMatch.Index + decodeMatch.Length)..];
        return Regex.Matches(decodeSource, @"(?m)^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(?:reader\.|new\s+|[A-Za-z_][A-Za-z0-9_]*Value\s*;)")
            .Select(match => match.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<string> ReadGoFieldNames(string source, string className, string path)
    {
        Match classMatch = Regex.Match(source, $@"type\s+{Regex.Escape(className)}\s+struct\s*\{{(?<body>[^}}]*)\}}", RegexOptions.Singleline);
        if (!classMatch.Success)
        {
            throw new InvalidDataException($"无法从已生成代码读取结构 {className}：{path}");
        }

        return Regex.Matches(classMatch.Groups["body"].Value, @"(?m)^\s*([A-Za-z_][A-Za-z0-9_]*)\s+\S+")
            .Select(match => match.Groups[1].Value)
            .ToArray();
    }

    private static ulong ReadSchemaHash(string source, ConfigOutputModel model, string path)
    {
        string name = model.Target == ExportTarget.Client ? "SchemaHash" : model.ClassName + "SchemaHash";
        Match match = Regex.Match(source, $@"\b{Regex.Escape(name)}\b[^\r\n]*?0x([0-9A-Fa-f]{{16}})");
        if (!match.Success)
        {
            throw new InvalidDataException($"无法从已生成代码读取 {name}：{path}");
        }

        return ulong.Parse(match.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    }

    private static bool TryReadFormatVersion(string source, ConfigOutputModel model, out uint formatVersion)
    {
        string name = model.Target == ExportTarget.Client ? "CurrentFormatVersion" : model.ClassName + "CurrentFormatVersion";
        Match match = Regex.Match(source, $@"\b{Regex.Escape(name)}\b[^\r\n=]*=\s*(\d+)");
        formatVersion = match.Success ? uint.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) : 1;
        return match.Success;
    }

    private static ConfigOutputModel Copy(ConfigOutputModel model, IReadOnlyList<LogicalField> fields, IReadOnlyList<LogicalRow> rows, uint formatVersion)
    {
        return new ConfigOutputModel
        {
            SourcePath = model.SourcePath,
            SheetName = model.SheetName,
            ClassName = model.ClassName,
            KeyFieldName = model.KeyFieldName,
            Target = model.Target,
            FileSuffix = model.FileSuffix,
            FormatVersion = formatVersion,
            Fields = fields,
            Rows = rows
        };
    }
}
