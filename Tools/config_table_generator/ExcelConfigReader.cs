using System.Globalization;
using System.Text.RegularExpressions;
using OfficeOpenXml;

namespace ConfigTableGenerator;

internal static class ExcelConfigReader
{
    private static readonly Regex Identifier = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant);

    public static IReadOnlyList<ConfigSheetModel> Read(string path, int dataStartRow)
    {
        using ExcelPackage package = new(new FileInfo(path));
        List<ConfigSheetModel> models = new();
        foreach (ExcelWorksheet sheet in package.Workbook.Worksheets)
        {
            ConfigSheetModel? model = ReadSheet(path, sheet, dataStartRow);
            if (model != null)
            {
                models.Add(model);
            }
        }

        return models;
    }

    public static IReadOnlyList<ConfigOutputModel> CreateOutputs(ConfigSheetModel sheet, bool includeData)
    {
        List<ConfigOutputModel> outputs = new();
        foreach (ExportTarget target in new[] { ExportTarget.Client, ExportTarget.Server })
        {
            List<PhysicalColumn> targetColumns = sheet.Columns.Where(column => (column.Export & target) != 0).ToList();
            if (targetColumns.Count == 0)
            {
                continue;
            }

            string[] suffixes = targetColumns
                .Where(column => column.Source.Kind == FieldSourceKind.CreateFile)
                .Select(column => column.Source.FileSuffix!)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            if (suffixes.Length == 0)
            {
                outputs.Add(CreateOutput(sheet, target, null, targetColumns, includeData));
                continue;
            }

            foreach (string suffix in suffixes)
            {
                List<PhysicalColumn> variantColumns = targetColumns
                    .Where(column => column.Source.Kind != FieldSourceKind.CreateFile || column.Source.FileSuffix == suffix)
                    .ToList();
                outputs.Add(CreateOutput(sheet, target, suffix, variantColumns, includeData));
            }
        }

        return outputs;
    }

    private static ConfigSheetModel? ReadSheet(string path, ExcelWorksheet sheet, int dataStartRow)
    {
        if (sheet.Dimension == null)
        {
            return null;
        }

        string className = Text(sheet.Cells[1, 1].Value);
        int noteIndex = className.IndexOf('（');
        if (noteIndex >= 0)
        {
            className = className[..noteIndex].Trim();
        }

        if (!Identifier.IsMatch(className))
        {
            return null;
        }

        string keyDeclaration = Text(sheet.Cells[1, 3].Value);

        int firstColumn = IsFieldDeclaration(sheet, 1) ? 1 : 2;
        List<PhysicalColumn> columns = new();
        for (int column = firstColumn; column <= sheet.Dimension.End.Column; column++)
        {
            string name = Text(sheet.Cells[3, column].Value);
            string typeText = Text(sheet.Cells[4, column].Value);
            string sourceText = Text(sheet.Cells[5, column].Value);
            string exportText = Text(sheet.Cells[6, column].Value);
            if (name.Length == 0)
            {
                continue;
            }

            if (!Identifier.IsMatch(name))
            {
                throw Error(path, sheet.Name, 3, column, $"字段名不是合法标识符：{name}");
            }

            ConfigType type;
            FieldSource source;
            ExportTarget export;
            try
            {
                type = ConfigType.Scalar(typeText);
                source = ParseSource(sourceText);
                export = ParseExport(exportText);
            }
            catch (Exception exception)
            {
                throw Error(path, sheet.Name, 3, column, exception.Message, exception);
            }

            if (source.Kind == FieldSourceKind.CustomMember && source.MemberType!.Name != type.Name)
            {
                throw Error(path, sheet.Name, 5, column, $"自定义成员类型 {source.MemberType.Name} 与第4行类型 {type.Name} 不一致");
            }

            columns.Add(new PhysicalColumn
            {
                Column = column,
                Name = name,
                Description = Text(sheet.Cells[2, column].Value),
                Type = type,
                Source = source,
                Export = export
            });
        }

        if (columns.Count == 0)
        {
            throw Error(path, sheet.Name, 3, firstColumn, "没有找到可导出的字段");
        }

        string keyFieldName = ResolveKeyFieldName(path, sheet.Name, keyDeclaration, columns);
        ValidateDuplicateColumns(path, sheet.Name, columns);
        List<PhysicalRow> rows = new();
        for (int row = dataStartRow; row <= sheet.Dimension.End.Row; row++)
        {
            Dictionary<int, string> values = columns.ToDictionary(column => column.Column, column => Text(sheet.Cells[row, column.Column].Value));
            if (values.Values.All(string.IsNullOrEmpty))
            {
                continue;
            }

            rows.Add(new PhysicalRow { ExcelRow = row, Values = values });
        }

        return new ConfigSheetModel
        {
            SourcePath = path,
            SheetName = sheet.Name,
            ClassName = className,
            KeyFieldName = keyFieldName,
            Columns = columns,
            Rows = rows
        };
    }

    private static ConfigOutputModel CreateOutput(ConfigSheetModel sheet, ExportTarget target, string? suffix, List<PhysicalColumn> columns, bool includeData)
    {
        List<LogicalField> fields = new();
        for (int index = 0; index < columns.Count;)
        {
            PhysicalColumn first = columns[index];
            int end = index + 1;
            while (end < columns.Count && columns[end].Name == first.Name)
            {
                end++;
            }

            List<PhysicalColumn> group = columns.GetRange(index, end - index);
            fields.Add(CreateLogicalField(sheet, group));
            index = end;
        }

        if (fields.Select(field => field.Name).Distinct(StringComparer.Ordinal).Count() != fields.Count)
        {
            throw new InvalidDataException($"{sheet.SourcePath}，Sheet={sheet.SheetName}：同名字段必须位于连续列中");
        }

        LogicalField? keyField = fields.Find(field => field.Name == sheet.KeyFieldName);
        if (keyField == null || keyField.Type.Kind != ConfigTypeKind.Scalar || keyField.Type.Name != "uint")
        {
            throw new InvalidDataException($"{sheet.SourcePath}，Sheet={sheet.SheetName}：{target} 输出缺少 uint 主键 {sheet.KeyFieldName}");
        }

        List<LogicalRow> rows = new();
        HashSet<uint> keys = new();
        if (!includeData)
        {
            return CreateOutputModel(sheet, target, suffix, fields, rows);
        }

        foreach (PhysicalRow sourceRow in sheet.Rows)
        {
            List<object> values = fields.Select(field => ParseLogicalValue(sheet, sourceRow, field)).ToList();
            uint key = (uint)values[fields.IndexOf(keyField)];
            if (!keys.Add(key))
            {
                throw new InvalidDataException($"{sheet.SourcePath}，Sheet={sheet.SheetName}，行={sourceRow.ExcelRow}：主键重复：{key}");
            }
            rows.Add(new LogicalRow { ExcelRow = sourceRow.ExcelRow, Values = values });
        }

        return CreateOutputModel(sheet, target, suffix, fields, rows);
    }

    private static ConfigOutputModel CreateOutputModel(ConfigSheetModel sheet, ExportTarget target, string? suffix, List<LogicalField> fields, List<LogicalRow> rows)
    {
        return new ConfigOutputModel
        {
            SourcePath = sheet.SourcePath,
            SheetName = sheet.SheetName,
            ClassName = sheet.ClassName,
            KeyFieldName = sheet.KeyFieldName,
            Target = target,
            FileSuffix = suffix,
            Fields = fields,
            Rows = rows
        };
    }

    private static LogicalField CreateLogicalField(ConfigSheetModel sheet, List<PhysicalColumn> group)
    {
        PhysicalColumn first = group[0];
        ConfigType type;
        switch (first.Source.Kind)
        {
            case FieldSourceKind.Direct:
            case FieldSourceKind.CreateFile:
                if (group.Count != 1)
                {
                    throw new InvalidDataException($"{sheet.SourcePath}，Sheet={sheet.SheetName}：字段 {first.Name} 重复但没有可归并的第5行规则");
                }
                type = first.Type;
                break;
            case FieldSourceKind.Array:
            case FieldSourceKind.List:
                EnsureSameSource(group, first.Source.Kind, first.Type.Name, sheet);
                type = new ConfigType
                {
                    Kind = first.Source.Kind == FieldSourceKind.Array ? ConfigTypeKind.Array : ConfigTypeKind.List,
                    Name = first.Name,
                    ElementType = first.Type
                };
                break;
            case FieldSourceKind.CustomMember:
                EnsureSameSource(group, FieldSourceKind.CustomMember, null, sheet);
                HashSet<string> members = new(StringComparer.Ordinal);
                List<ConfigMember> memberList = new();
                foreach (PhysicalColumn column in group)
                {
                    if (!members.Add(column.Source.MemberName!))
                    {
                        throw new InvalidDataException($"{sheet.SourcePath}，Sheet={sheet.SheetName}：自定义字段 {first.Name} 的成员重复：{column.Source.MemberName}");
                    }
                    memberList.Add(new ConfigMember { Name = column.Source.MemberName!, Type = column.Source.MemberType! });
                }
                type = new ConfigType { Kind = ConfigTypeKind.Custom, Name = first.Name + "Data", Members = memberList };
                break;
            default:
                throw new InvalidDataException($"无法归并字段：{first.Name}");
        }

        return new LogicalField { Name = first.Name, Type = type, Columns = group };
    }

    private static object ParseLogicalValue(ConfigSheetModel sheet, PhysicalRow row, LogicalField field)
    {
        try
        {
            return field.Type.Kind switch
            {
                ConfigTypeKind.Scalar => ParseScalar(row.Values[field.Columns[0].Column], field.Type),
                ConfigTypeKind.Array or ConfigTypeKind.List => ParseRepeatedValues(row, field),
                ConfigTypeKind.Custom => field.Columns.Select(column => ParseScalar(row.Values[column.Column], column.Source.MemberType!)).ToArray(),
                _ => throw new InvalidDataException($"不支持的字段类型：{field.Type.Kind}")
            };
        }
        catch (Exception exception)
        {
            throw new InvalidDataException($"{sheet.SourcePath}，Sheet={sheet.SheetName}，行={row.ExcelRow}，字段={field.Name}：{exception.Message}", exception);
        }
    }

    private static object[] ParseRepeatedValues(PhysicalRow row, LogicalField field)
    {
        List<object> values = new();
        bool foundEmpty = false;
        foreach (PhysicalColumn column in field.Columns)
        {
            string text = row.Values[column.Column];
            if (text.Length == 0)
            {
                foundEmpty = true;
                continue;
            }
            if (foundEmpty)
            {
                throw new FormatException("Array/List 重复列中间不能有空值");
            }
            values.Add(ParseScalar(text, column.Type));
        }
        return values.ToArray();
    }

    private static object ParseScalar(string text, ConfigType type)
    {
        return type.Name switch
        {
            "uint" => uint.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture),
            "int" => int.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture),
            "bool" => ParseBoolean(text),
            "string" => text,
            "float" => float.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture),
            "long" => long.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture),
            "double" => double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture),
            _ => throw new InvalidDataException($"不支持的字段类型：{type.Name}")
        };
    }

    private static bool ParseBoolean(string text)
    {
        if (text.Equals("true", StringComparison.OrdinalIgnoreCase) || text == "1") return true;
        if (text.Equals("false", StringComparison.OrdinalIgnoreCase) || text == "0") return false;
        throw new FormatException($"非法 bool：{text}");
    }

    private static FieldSource ParseSource(string text)
    {
        if (text.Length == 0) return new FieldSource { Kind = FieldSourceKind.Direct };
        if (text.Equals("Array", StringComparison.OrdinalIgnoreCase)) return new FieldSource { Kind = FieldSourceKind.Array };
        if (text.Equals("List", StringComparison.OrdinalIgnoreCase)) return new FieldSource { Kind = FieldSourceKind.List };
        if (text.StartsWith("CreateFile.", StringComparison.OrdinalIgnoreCase))
        {
            string suffix = text[11..];
            if (!Identifier.IsMatch(suffix)) throw new InvalidDataException($"CreateFile 后缀非法：{suffix}");
            return new FieldSource { Kind = FieldSourceKind.CreateFile, FileSuffix = suffix };
        }
        if (text.Equals("CreateFile", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("CreateFile 必须声明后缀，例如 CreateFile.En");
        }

        string[] parts = text.Split('.', StringSplitOptions.TrimEntries);
        if (parts.Length == 2 && Identifier.IsMatch(parts[0]))
        {
            return new FieldSource { Kind = FieldSourceKind.CustomMember, MemberName = parts[0], MemberType = ConfigType.Scalar(parts[1]) };
        }
        throw new InvalidDataException($"无法识别第5行字段作用：{text}");
    }

    private static ExportTarget ParseExport(string text)
    {
        if (text.Equals("All", StringComparison.OrdinalIgnoreCase)) return ExportTarget.All;
        if (text.Equals("Client", StringComparison.OrdinalIgnoreCase)) return ExportTarget.Client;
        if (text.Equals("Server", StringComparison.OrdinalIgnoreCase)) return ExportTarget.Server;
        throw new InvalidDataException($"导出范围必须是 All、Client 或 Server：{text}");
    }

    private static string ResolveKeyFieldName(string path, string sheet, string declaration, List<PhysicalColumn> columns)
    {
        string[] keyNames = declaration.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (keyNames.Length != 1 || !Identifier.IsMatch(keyNames[0]))
        {
            string actual = declaration.Length == 0 ? "<空>" : declaration;
            throw new InvalidDataException($"{path}，Sheet={sheet}，行=1，列=3：必须声明一个合法的主键字段名（实际={actual}）");
        }

        PhysicalColumn[] keyFields = columns.Where(column => column.Name == keyNames[0]).ToArray();
        if (keyFields.Length == 0)
        {
            throw new InvalidDataException($"{path}，Sheet={sheet}，行=1，列=3：主键字段 {keyNames[0]} 不存在于第3行字段定义中");
        }

        if (keyFields.Length > 1)
        {
            throw new InvalidDataException($"{path}，Sheet={sheet}，行=1，列=3：主键字段 {keyNames[0]} 在第3行存在多个定义");
        }

        PhysicalColumn keyField = keyFields[0];
        if (keyField.Type.Name != "uint")
        {
            throw new InvalidDataException($"{path}，Sheet={sheet}，行=4，列={keyField.Column}：主键字段 {keyField.Name} 必须是 uint，实际是 {keyField.Type.Name}");
        }

        return keyField.Name;
    }

    private static void ValidateDuplicateColumns(string path, string sheet, List<PhysicalColumn> columns)
    {
        foreach (IGrouping<string, PhysicalColumn> duplicate in columns.GroupBy(column => column.Name, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            FieldSourceKind[] kinds = duplicate.Select(column => column.Source.Kind).Distinct().ToArray();
            if (kinds.Length != 1 || kinds[0] == FieldSourceKind.Direct)
            {
                throw new InvalidDataException($"{path}，Sheet={sheet}：重复字段 {duplicate.Key} 必须统一声明 CreateFile.后缀、Array、List 或成员名.成员类型");
            }
            int[] positions = duplicate.Select(column => columns.IndexOf(column)).ToArray();
            if (kinds[0] != FieldSourceKind.CreateFile && positions[^1] - positions[0] + 1 != positions.Length)
            {
                throw new InvalidDataException($"{path}，Sheet={sheet}：重复字段 {duplicate.Key} 必须位于连续列中");
            }
        }
    }

    private static void EnsureSameSource(List<PhysicalColumn> columns, FieldSourceKind kind, string? typeName, ConfigSheetModel sheet)
    {
        if (columns.Any(column => column.Source.Kind != kind || typeName != null && column.Type.Name != typeName))
        {
            throw new InvalidDataException($"{sheet.SourcePath}，Sheet={sheet.SheetName}：字段 {columns[0].Name} 的重复列规则或类型不一致");
        }
    }

    private static bool IsFieldDeclaration(ExcelWorksheet sheet, int column)
    {
        string name = Text(sheet.Cells[3, column].Value);
        string type = Text(sheet.Cells[4, column].Value);
        try { ConfigType.Scalar(type); return Identifier.IsMatch(name); } catch { return false; }
    }
    private static string Text(object? value) => Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
    private static InvalidDataException Error(string path, string sheet, int row, int column, string message, Exception? inner = null) => new($"{path}，Sheet={sheet}，行={row}，列={column}：{message}", inner);
}
