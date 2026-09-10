using System.Text.Json;

namespace ConfigTableGenerator;

internal sealed class GeneratorSettings
{
    public string ExcelRoot { get; set; } = "Excel";
    public string[] IncludedFolders { get; set; } = Array.Empty<string>();
    public string[] ExcludedFolders { get; set; } = Array.Empty<string>();
    public string ClientCodeDirectory { get; set; } = "GameProject/GameProto/Runtime/Config/Generated";
    public string ClientTableDirectory { get; set; } = "GameProject/GameLogic/Runtime/Config/Generated";
    public string ServerCodeDirectory { get; set; } = "Server/Config/Generated";
    public string ClientDataDirectory { get; set; } = "GameClient/Assets/GameAssets/Configs";
    public string ServerDataDirectory { get; set; } = "Server/Config/Data";
    public string ClientNamespace { get; set; } = "GameProto.Config.Generated";
    public string ClientTableNamespace { get; set; } = "GameLogic.Config.Generated";
    public string ServerPackage { get; set; } = "config";
    public int DataStartRow { get; set; } = 7;

    public static GeneratorSettings Load(string path)
    {
        string json = File.ReadAllText(path);
        GeneratorSettings? settings = JsonSerializer.Deserialize<GeneratorSettings>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        return settings ?? throw new InvalidDataException($"配置文件内容无效：{path}");
    }
}
