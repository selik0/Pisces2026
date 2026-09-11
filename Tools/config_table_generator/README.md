# C# Excel 配置生成工具

该工具使用 C#、.NET 8 和 EPPlus 4.5.3.3 读取 `.xlsx`，可在 Windows 和 Linux 上运行。工具不依赖 Unity Editor，不修改 Excel 文件，NuGet 会还原 EPPlus 及其跨平台依赖。

## 运行

安装 .NET 8 SDK 后，在仓库根目录分别执行代码生成和数据导出。

仅根据 Excel 表头生成客户端 C# 和服务器 Go 配置代码，不解析和导出数据行：

```shell
dotnet run --project Tools/config_table_generator/ConfigTableGenerator.csproj -- code --config Tools/config_table_generator/config.json
```

仅解析数据行并导出客户端和服务器二进制，不生成配置类：

```shell
dotnet run --project Tools/config_table_generator/ConfigTableGenerator.csproj -- data --config Tools/config_table_generator/config.json
```

只处理某个 Excel 子目录时，两种命令都可以追加：

```shell
--folder 通用
```

## 发布单文件

在工具目录执行以下命令，生成不依赖目标机器 .NET Runtime 的自包含单文件：

```shell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true
```

输出位置。Windows 和 Linux 文件直接放在同一个目录，不再按平台创建子目录：

```text
Excel/tools/ConfigTableGenerator.exe
Excel/tools/ConfigTableGenerator
```

Windows：

```powershell
ConfigTableGenerator.exe code --config config.json
ConfigTableGenerator.exe data --config config.json
```

Linux：

```shell
chmod +x ConfigTableGenerator
./ConfigTableGenerator code --config config.json
./ConfigTableGenerator data --config config.json
```

## C# 调用接口

工具程序集提供两个相互独立的公开接口：

```csharp
ConfigTableTool.GenerateCode(configPath, folder);
ConfigTableTool.ExportData(configPath, folder);
```

`folder` 可以为 `null`，此时使用 `config.json` 中的 `includedFolders`。

路径使用 `/`，配置文件中的相对路径均相对于仓库根目录。工具自动忽略 Excel 锁文件 `~$*.xlsx`，相同内容不会重复覆盖。

## Excel 格式

每个可导出 Sheet 沿用项目现有表头：

| 行 | 内容 |
|---:|---|
| 1 | A 列为 C#/Go 类型名，C 列为主键字段名 |
| 2 | 字段描述 |
| 3 | 字段名 |
| 4 | 字段类型 |
| 5 | 字段来源，当前保留 |
| 6 | 导出范围：`All`、`Client` 或 `Server` |
| 7+ | 配置数据 |

字段通常从 B 列开始；如果 A3 是合法字段名且 A4 是支持类型，则从 A 列开始。C1 必须声明一个主键字段名，该名称必须与第3行的字段名完全一致，且第4行对应类型必须是 `uint`。第4行支持：

第3行字段名为空时，代码生成和二进制导出都会忽略整列；该列的类型、来源、导出范围和数据均不参与处理。

```text
uint int bool string float long double
```

第5行定义同名字段的归并方式：

- `CreateFile.En`：生成 `ClassName.En.bytes` 分文件。必须包含明确后缀，裸 `CreateFile` 会报错。
- `Array`：连续同名列归并为 C# 数组和 Go slice，每个非空单元格为一个元素。
- `List`：连续同名列归并为 C# List 和 Go slice，每个非空单元格为一个元素。
- `ItemId.int`：连续同名列归并为自定义类，其成员名为 `ItemId`、成员类型为 `int`。

重复列集合只允许尾部空值，不允许中间出现空值后再次出现元素。自定义类成员名不得重复。

第6行导出范围决定输出：

- `All`：同时进入 C# 客户端和 Go 服务器配置及各自二进制。
- `Client`：只进入客户端 C# 和客户端二进制。
- `Server`：只进入服务器 Go 和服务器二进制。

客户端和服务器根据各自字段投影独立计算 Schema Hash，分别输出二进制文件。

## 输出

- 客户端配置记录：C# `ConfigRecord` 派生类及 `Decode`
- 客户端配置表：C# `ConfigTable<uint, T>` 派生类及 `Load`
- 服务器配置记录：Go struct 及 `DecodeXxx`
- 客户端配置数据：与 GameProto Reader 对应的 `GCFG` 小端序二进制
- 服务器配置数据：与 Go Reader 对应的相同线格式二进制

生成代码直接写入配置项指定的 `Generated` 目录，不会根据 Excel 所在目录创建子目录；代码 namespace 固定使用 `GameProto` 和 `GameLogic`。配置记录属性使用 `{ get; private set; }`，并通过重写 `ConfigRecord.Decode` 填充。

生成的 C# 配置加载代码遇到 null 数据、Schema Hash 不匹配或记录数超出 int 范围时，通过 `ConfigLog.Error` 记录错误并返回 null，不抛出异常。

Go 代码假定服务器包中提供与 GameProto 线格式一致的 `Reader`，包括 `ReadUInt32`、`ReadString`、`ReadBytes` 和其他生成代码调用的方法。

二进制格式为：

```text
GCFG Magic       4 bytes
FormatVersion    uint32, little-endian
SchemaHash       SHA-256 规范模型的前 8 bytes
RecordCount      uint32, little-endian
Records          每条记录为 int32 byteLength + 按生成类 Decode 顺序编码的数据
```

字符串和 bytes 使用 `uint32 byteLength + data`，数组使用 `uint32 count + elements`。所有文本均使用严格 UTF-8，无 BOM。

工具会先读取已有生成代码的 `Decode` 字段赋值顺序，以该顺序重排当前模型中仍存在的字段，并将新增字段追加到末尾。字段结构发生变化时 `CurrentFormatVersion` 自动加 1；首次生成时版本为 1。读取单条记录时会使用记录长度创建有界读取器，字段解码不会越过该记录。

C# 生成类使用 `StructLayout(LayoutKind.Sequential)`，属性声明按引用类型、8 字节、4 字节、1 字节的对齐优先级排序。属性声明顺序只用于改善对象字段布局，不参与 Schema Hash 或二进制字段顺序计算。

`ConfigFileHeader` 只负责头部字段的编解码，不判断字段是否合法。每个生成的配置加载类使用自身的 `Magic`、`CurrentFormatVersion` 和 `SchemaHash` 常量决定是否接受该配置文件。
