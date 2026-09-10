using ConfigTableGenerator;
using System.Text;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

if (args.Length < 3 || args[1] != "--config" || args[0] is not ("code" or "data"))
{
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  ConfigTableGenerator code --config <config.json> [--folder <relative-folder>]");
    Console.Error.WriteLine("  ConfigTableGenerator data --config <config.json> [--folder <relative-folder>]");
    return 1;
}

try
{
    string configPath = args[2];
    string? folder = null;
    for (int i = 3; i < args.Length; i++)
    {
        if (args[i] == "--folder" && i + 1 < args.Length)
        {
            folder = args[++i];
        }
        else
        {
            throw new ArgumentException($"未知参数：{args[i]}");
        }
    }

    if (args[0] == "code")
    {
        ConfigTableTool.GenerateCode(configPath, folder);
    }
    else
    {
        ConfigTableTool.ExportData(configPath, folder);
    }
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"ERROR: {exception.Message}");
    return 1;
}
