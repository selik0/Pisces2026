namespace ConfigTableGenerator;

/// <summary>
/// 提供配置代码生成和二进制数据导出的独立调用接口。
/// </summary>
public static class ConfigTableTool
{
    /// <summary>
    /// 仅根据 Excel 表头生成客户端 C# 和服务器 Go 配置代码。
    /// </summary>
    public static void GenerateCode(string configPath, string? folder = null)
    {
        Execute(configPath, folder, true, false);
    }

    /// <summary>
    /// 仅根据 Excel 表头和数据行导出客户端与服务器二进制配置。
    /// </summary>
    public static void ExportData(string configPath, string? folder = null)
    {
        Execute(configPath, folder, false, true);
    }

    private static void Execute(string configPath, string? folder, bool generateCode, bool exportData)
    {
        string fullConfigPath = Path.GetFullPath(configPath);
        GeneratorSettings settings = GeneratorSettings.Load(fullConfigPath);
        string projectRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(fullConfigPath)!, "..", ".."));
        string excelRoot = Resolve(projectRoot, settings.ExcelRoot);
        string[] folders = folder == null ? settings.IncludedFolders : [folder];
        if (folders.Length == 0)
        {
            throw new InvalidDataException("IncludedFolders 不能为空");
        }

        StringComparison pathComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        HashSet<string> excluded = settings.ExcludedFolders.Select(value => Path.GetFullPath(Path.Combine(excelRoot, value)))
            .ToHashSet(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        List<string> excelFiles = folders.SelectMany(value => Directory.EnumerateFiles(Path.Combine(excelRoot, value), "*.xlsx", SearchOption.AllDirectories))
            .Where(path => !Path.GetFileName(path).StartsWith("~$", StringComparison.Ordinal))
            .Where(path => !excluded.Any(directory => IsUnder(path, directory, pathComparison)))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();
        if (excelFiles.Count == 0)
        {
            throw new InvalidDataException("没有找到可转换的 .xlsx 文件");
        }

        foreach (string excelPath in excelFiles)
        {
            ProcessExcel(projectRoot, excelRoot, excelPath, settings, generateCode, exportData);
        }
    }

    private static void ProcessExcel(string projectRoot, string excelRoot, string excelPath, GeneratorSettings settings, bool generateCode, bool exportData)
    {
        string relativeDirectory = Path.GetDirectoryName(Path.GetRelativePath(excelRoot, excelPath)) ?? string.Empty;
        foreach (ConfigSheetModel sheet in ExcelConfigReader.Read(excelPath, settings.DataStartRow))
        {
            IReadOnlyList<ConfigOutputModel> outputs = ExcelConfigReader.CreateOutputs(sheet, exportData);
            foreach (IGrouping<ExportTarget, ConfigOutputModel> targetOutputs in outputs.GroupBy(output => output.Target))
            {
                ConfigOutputModel canonical = targetOutputs.First();
                ValidateVariantSchemas(excelPath, sheet, canonical, targetOutputs);
                if (generateCode)
                {
                    GenerateTargetCode(projectRoot, relativeDirectory, settings, canonical);
                }
                if (exportData)
                {
                    ExportTargetData(projectRoot, relativeDirectory, settings, targetOutputs);
                }
            }
        }
    }

    private static void ValidateVariantSchemas(string excelPath, ConfigSheetModel sheet, ConfigOutputModel canonical, IEnumerable<ConfigOutputModel> outputs)
    {
        byte[] schemaHash = SchemaHash.Compute(canonical);
        if (outputs.Any(output => !SchemaHash.Compute(output).SequenceEqual(schemaHash)))
        {
            throw new InvalidDataException($"{excelPath}，Sheet={sheet.SheetName}：同一导出目标的 CreateFile 变体字段结构不一致");
        }
    }

    private static void GenerateTargetCode(string projectRoot, string relativeDirectory, GeneratorSettings settings, ConfigOutputModel model)
    {
        if (model.Target == ExportTarget.Client)
        {
            WriteTextIfChanged(Path.Combine(Resolve(projectRoot, settings.ClientCodeDirectory), relativeDirectory, model.ClassName + ".g.cs"), SourceGenerator.GenerateClientRecord(model, settings.ClientNamespace));
            WriteTextIfChanged(Path.Combine(Resolve(projectRoot, settings.ClientTableDirectory), relativeDirectory, model.ClassName + "Table.g.cs"), SourceGenerator.GenerateClientTable(model, settings.ClientTableNamespace, settings.ClientNamespace));
        }
        else
        {
            WriteTextIfChanged(Path.Combine(Resolve(projectRoot, settings.ServerCodeDirectory), relativeDirectory, Snake(model.ClassName) + ".gen.go"), SourceGenerator.GenerateGo(model, settings.ServerPackage));
        }

        Console.WriteLine($"Generated code: {model.Target} {model.ClassName} [{model.SheetName}]");
    }

    private static void ExportTargetData(string projectRoot, string relativeDirectory, GeneratorSettings settings, IEnumerable<ConfigOutputModel> outputs)
    {
        foreach (ConfigOutputModel output in outputs)
        {
            string dataRoot = output.Target == ExportTarget.Client ? settings.ClientDataDirectory : settings.ServerDataDirectory;
            string suffix = output.FileSuffix == null ? string.Empty : "." + output.FileSuffix;
            WriteBytesIfChanged(Path.Combine(Resolve(projectRoot, dataRoot), relativeDirectory, output.ClassName + suffix + ".bytes"), BinaryConfigWriter.Write(output));
            Console.WriteLine($"Exported data: {output.Target} {output.ClassName}{suffix} [{output.SheetName}], rows={output.Rows.Count}");
        }
    }

    private static string Resolve(string root, string path) => Path.IsPathRooted(path) ? Path.GetFullPath(path) : Path.GetFullPath(Path.Combine(root, path));
    private static bool IsUnder(string path, string directory, StringComparison comparison) => Path.GetFullPath(path).StartsWith(directory + Path.DirectorySeparatorChar, comparison);
    private static string Snake(string value) => string.Concat(value.Select((character, index) => index > 0 && char.IsUpper(character) ? "_" + char.ToLowerInvariant(character) : char.ToLowerInvariant(character).ToString()));

    private static void WriteTextIfChanged(string path, string content)
    {
        WriteBytesIfChanged(path, new System.Text.UTF8Encoding(false).GetBytes(content.Replace("\r\n", "\n")));
    }

    private static void WriteBytesIfChanged(string path, byte[] data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (!File.Exists(path) || !File.ReadAllBytes(path).SequenceEqual(data))
        {
            File.WriteAllBytes(path, data);
        }
    }
}
