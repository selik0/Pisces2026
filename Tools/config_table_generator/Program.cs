using ConfigTableGenerator;
using System.Text;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

if (args.Length < 2 || args[0] != "--config")
{
    Console.Error.WriteLine("Usage: dotnet run --project Tools/config_table_generator -- --config <config.json> [--folder <relative-folder>]");
    return 1;
}

try
{
    string configPath = Path.GetFullPath(args[1]);
    string? selectedFolder = null;
    for (int i = 2; i < args.Length; i++)
    {
        if (args[i] == "--folder" && i + 1 < args.Length)
        {
            selectedFolder = args[++i];
        }
        else
        {
            throw new ArgumentException($"未知参数：{args[i]}");
        }
    }

    GeneratorSettings settings = GeneratorSettings.Load(configPath);
    string projectRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(configPath)!, "..", ".."));
    string excelRoot = Resolve(projectRoot, settings.ExcelRoot);
    string[] folders = selectedFolder == null ? settings.IncludedFolders : [selectedFolder];
    if (folders.Length == 0)
    {
        throw new InvalidDataException("IncludedFolders 不能为空");
    }

    StringComparison pathComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    HashSet<string> excluded = settings.ExcludedFolders.Select(folder => Path.GetFullPath(Path.Combine(excelRoot, folder)))
        .ToHashSet(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    List<string> excelFiles = folders.SelectMany(folder => Directory.EnumerateFiles(Path.Combine(excelRoot, folder), "*.xlsx", SearchOption.AllDirectories))
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
        string relativeDirectory = Path.GetDirectoryName(Path.GetRelativePath(excelRoot, excelPath)) ?? string.Empty;
        foreach (ConfigSheetModel sheet in ExcelConfigReader.Read(excelPath, settings.DataStartRow))
        {
            IReadOnlyList<ConfigOutputModel> outputs = ExcelConfigReader.CreateOutputs(sheet);
            foreach (IGrouping<ExportTarget, ConfigOutputModel> targetOutputs in outputs.GroupBy(output => output.Target))
            {
                ConfigOutputModel canonical = targetOutputs.First();
                byte[] schemaHash = SchemaHash.Compute(canonical);
                if (targetOutputs.Any(output => !SchemaHash.Compute(output).SequenceEqual(schemaHash)))
                {
                    throw new InvalidDataException($"{excelPath}，Sheet={sheet.SheetName}：同一导出目标的 CreateFile 变体字段结构不一致");
                }

                if (canonical.Target == ExportTarget.Client)
                {
                    WriteIfChanged(Path.Combine(Resolve(projectRoot, settings.ClientCodeDirectory), relativeDirectory, canonical.ClassName + ".g.cs"), SourceGenerator.GenerateClientRecord(canonical, settings.ClientNamespace));
                    WriteIfChanged(Path.Combine(Resolve(projectRoot, settings.ClientTableDirectory), relativeDirectory, canonical.ClassName + "Table.g.cs"), SourceGenerator.GenerateClientTable(canonical, settings.ClientTableNamespace, settings.ClientNamespace));
                }
                else
                {
                    WriteIfChanged(Path.Combine(Resolve(projectRoot, settings.ServerCodeDirectory), relativeDirectory, Snake(canonical.ClassName) + ".gen.go"), SourceGenerator.GenerateGo(canonical, settings.ServerPackage));
                }

                string dataRoot = canonical.Target == ExportTarget.Client ? settings.ClientDataDirectory : settings.ServerDataDirectory;
                foreach (ConfigOutputModel output in targetOutputs)
                {
                    string suffix = output.FileSuffix == null ? string.Empty : "." + output.FileSuffix;
                    WriteBytesIfChanged(Path.Combine(Resolve(projectRoot, dataRoot), relativeDirectory, output.ClassName + suffix + ".bytes"), BinaryConfigWriter.Write(output));
                    Console.WriteLine($"Generated {output.Target} {output.ClassName}{suffix} from {excelPath} [{output.SheetName}], rows={output.Rows.Count}");
                }
            }
        }
    }

    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"ERROR: {exception.Message}");
    return 1;
}

static string Resolve(string root, string path) => Path.IsPathRooted(path) ? Path.GetFullPath(path) : Path.GetFullPath(Path.Combine(root, path));
static bool IsUnder(string path, string directory, StringComparison comparison) => Path.GetFullPath(path).StartsWith(directory + Path.DirectorySeparatorChar, comparison);
static string Snake(string value) => string.Concat(value.Select((character, index) => index > 0 && char.IsUpper(character) ? "_" + char.ToLowerInvariant(character) : char.ToLowerInvariant(character).ToString()));

static void WriteIfChanged(string path, string content)
{
    byte[] data = new System.Text.UTF8Encoding(false).GetBytes(content.Replace("\r\n", "\n"));
    WriteBytesIfChanged(path, data);
}

static void WriteBytesIfChanged(string path, byte[] data)
{
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    if (!File.Exists(path) || !File.ReadAllBytes(path).SequenceEqual(data))
    {
        File.WriteAllBytes(path, data);
    }
}
