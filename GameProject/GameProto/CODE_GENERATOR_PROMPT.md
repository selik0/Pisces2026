# GameProto 协议和配置表代码生成提示词

你正在修改 Unity 游戏框架工程 `G:\Pisces2026`。请实现 GameProto 的网络协议和 Excel 配置表代码生成系统。本提示词只负责 Schema、Excel、代码生成和配置导出；二进制 Reader、Writer、Size、消息基类和文件格式运行时实现请遵循同目录的 `CODE_GENERATION_PROMPT.md`。

> `GameProject/GameProto` 中已有一批文件被用户主动删除。忽略这些删除，不要恢复、还原或复用被删除的旧实现。不要覆盖工作区内与本任务无关的修改。

## 一、边界和目录

- 运行时代码和生成后的 C# 文件放在 `GameProject/GameProto/`。
- Excel 生成工具必须使用 Python 实现，统一放在 `Excel/tools/`；不得使用 C#、Unity Editor 或 EPPlus 作为 Excel 生成工具的实现语言。
- GameProto 运行时只包含运行时代码和生成后的 C# 代码，不包含 Python 运行时依赖。
- GameProto 不得引用 UnityEditor、EPPlus 或 GameEngineEditor。
- Python 工具负责读取 Excel、解析表头、校验数据、生成 C# 协议/配置代码，并导出二进制配置文件；生成结果写入 GameProto 或项目约定的资源目录。
- 允许依赖方向：`GameEngineEditor -> GameProto`，禁止反向依赖和循环依赖。
- 生成器不得使用运行时反射；生成代码必须直接按字段顺序读写。
- 建议 Schema 输入目录：`GameProject/GameProto/Schemas/`。
- 建议协议生成目录：`GameProject/GameProto/Runtime/Network/Generated/`。
- 建议配置生成目录：`GameProject/GameProto/Runtime/Config/Generated/`。
- 建议全局生成目录：`GameProject/GameProto/Runtime/Generated/`。
- 配置二进制输出目录先检查现有资源加载约定；没有约定时做成集中可配置项，不要散落硬编码路径。

修改前执行 `git status --short`；完成后执行：

```powershell
dotnet build "GameProject\GameProject.sln" -c Debug
git status --short
git diff --check
```

## 二、统一生成格式

所有数字使用小端序和固定宽度：

- bool：1 byte；只允许 0/1；
- byte/sbyte：1 byte；
- short/ushort：2 bytes；
- int/uint/float：4 bytes；
- long/ulong/double：8 bytes；
- float/double 按 IEEE 754 原始位模式编码。

所有可变长度数据统一使用小端序 `uint`，固定占 4 bytes。禁止使用 byte、short、ushort、int 或 varint 作为可变长度前缀。

- `string`：`uint Utf8ByteLength + UTF-8 bytes`；
- `bytes`：`uint ByteLength + raw bytes`；
- `T[]`：`uint ElementCount + elements`；
- null 字符串按空字符串编码，0 长度解码为 `string.Empty`；
- null bytes/数组按0长度编码；
- 长度表示 UTF-8 字节数或元素数量，不是 C# 字符数；
- 运行时仍必须校验 `uint`、`int.MaxValue` 和集中安全上限；
- 禁止截断、整数溢出和静默接受非法数据。

## 三、自定义 Schema

不要兼容 Google Protobuf，不实现 field number、wire type、未知字段跳过、varint 或字段级兼容。字段顺序就是二进制布局的一部分。

建议语法：

```text
namespace GameProto.Network;

message LoginRequest 1001
{
    string Account;
    string Token;
}

message LoginResponse 1002
{
    int ResultCode;
    long PlayerId;
    string Message;
}

message ItemListResponse 1003
{
    int[] ItemIds;
    bytes Signature;
}
```

MVP 支持：

- namespace；
- message 名称；
- ushort 范围内的 MessageId；
- 字段类型和字段名；
- 空行和 `//` 单行注释；
- bool、byte、sbyte、short、ushort、int、uint、long、ulong、float、double；
- string、bytes；
- 基础类型一维数组。

MVP 暂不支持：

- map、Dictionary；
- oneof；
- optional、nullable；
- object、多态、继承、循环引用；
- 多维数组和交错数组；
- field number、未知字段兼容和自动宽度推断。

解析时必须校验：

- namespace、类型名和字段名是合法 C# 标识符；
- MessageId 在 uint 范围且全局唯一；
- 类型名不重复；
- 同一消息内字段名不重复；
- 字段类型受支持；
- 错误至少包含文件名和行号，条件允许时包含列号。

## 四、网络协议代码生成

每个 message 生成 `sealed class`，继承运行时 `ProtoMessage`：

```csharp
public abstract class ProtoMessage
{
    public abstract uint MessageId { get; }
    public abstract int GetEncodedSize();
    public abstract void Encode(ref ProtoWriter writer);
    public abstract void Decode(ref ProtoReader reader);
}
```

生成类必须：

- 具有唯一的 uint MessageId；
- 按 Schema 声明顺序生成字段；
- 直接生成 GetEncodedSize、Encode、Decode；
- string 调用 `ReadString`/`WriteString`；
- bytes 调用 `ReadBytes`/`WriteBytes`；
- 数组直接读取或写入 uint 数量并逐项处理；
- 固定宽度数组尺寸使用 checked 计算；
- 不使用反射、Attribute 扫描、FieldInfo、表达式树或 Activator；
- 生成文件包含自动生成声明，不放手写业务逻辑。

同时生成无反射消息注册表：

```csharp
public static bool TryCreate(uint messageId, out ProtoMessage message);
public static ProtoMessage Create(uint messageId);
```

使用生成的 switch。生成阶段检测重复 MessageId；未知 ID 明确失败。

至少生成并验证：

- LoginRequest，MessageId 1001，字段 string Account、string Token；
- LoginResponse，MessageId 1002，字段 int ResultCode、long PlayerId、string Message。

## 五、Schema Hash

生成全局 Protocol Schema Hash，并写入生成代码。Hash 必须：

- 使用 SHA-256 等跨平台稳定算法；
- 不使用 `string.GetHashCode()`；
- 基于规范化后的 Schema 模型；
- 文件、消息和字段按明确稳定规则排序；
- 字段顺序参与 Hash；
- 注释、无意义空白和换行风格不影响 Hash；
- 明确保存的 Hash 长度、截取规则和字节顺序。

配置表每张表也生成独立 Schema Hash。代码与数据 Hash 不匹配时，运行时拒绝解析。

生成输出必须稳定：相同输入产生完全相同的文件，不写入当前时间，固定排序、编码和换行，只有内容变化时才覆盖文件。

## 六、Excel 模板结构与表头规范

必须以 `Excel/模板.xlsx` 的实际布局为准，而不是自行重新定义成之前的四行表头格式。

### 6.1 模板总体结构

模板当前包含两个 Sheet：

- `Sheet1`：字段元数据说明和多语言/配置字段示例；
- `Sheet2`：配置类及数据表索引配置示例。

模板的特殊约定是：

- 第1行描述配置类、字段用途或示例语言等表级/列级信息；
- 第1列从第2行开始描述字段相关元数据；
- Sheet2 的第1行描述当前 Sheet 对应的配置类名、是否只在当前 Sheet 生效、数据表索引 key 和数据分类规则；
- Sheet2 的第2行开始按列描述字段信息；
- 第2行包含字段显示描述；
- 第3行包含字段名；
- 第4行包含字段数据类型；
- 第5行包含字段来源/父类型信息；
- 第6行包含导出范围：`All`、`Client` 或 `Server`。

Python 工具必须先读取并解析这些元数据，再生成代码和二进制数据，不能把第一行和第一列当作普通业务数据跳过。

### 6.2 Sheet2 配置索引元数据

Sheet2 第1行的非空列用于描述配置表级规则，当前模板示例含义为：

1. 第1列：当前 Sheet 生成的配置类名，例如 `TextConfig`；括号中的说明表示类名只在当前 Sheet 有效；不同 Sheet 使用相同类名时必须告警或失败，具体行为由工具配置确定；
2. 第2列：`TRUE` 表示类名只在当前 Sheet 有效；
3. 第3列：数据表字典索引 key，例如 `Level.Start`，支持单个字段或最多3个字段组合，字段名通过 `.` 分隔；
4. 第4列：数据分类索引规则，例如 `Level.Start`，单字段生成 `Dictionary<key, List<T>>`，组合字段生成嵌套 Dictionary，最多支持3级；
5. 后续非空列可以继续作为表级生成配置，但必须明确记录并参与校验。

Python 工具至少需要解析并校验：

- 配置类名；
- 类名作用域标志；
- 主键/字典索引字段；
- 数据分类字段；
- 索引字段是否真实存在；
- 索引字段数量不超过3个；
- 组合索引字段使用 `.` 分割；
- 索引字段类型是否适合生成 Dictionary key；
- 不同 Sheet 的类名冲突。

### 6.3 Sheet2 字段元数据

Sheet2 第1列从第2行开始描述字段元数据，模板当前约定如下：

- 第2行第1列：字段描述；
- 第3行第1列：字段名；
- 第4行第1列：字段数据类型；支持 `uint`、`int`、`bool`、`string`、`float`、`long`、`double`，并支持带具体元素类型的数组和 List，例如 `int[]`、`List<string>`、`Consume[]`、`List<RewardData>`；
- 第5行第1列：字段来源/父类型；
- 第6行第1列：导出范围，可为 `All`、`Client`、`Server`。

实际字段列从第2列开始。字段名称可以在特定情况下重复，但必须满足模板约束：

- 不同语言/分文件字段可以使用相同字段名；
- Array 或 List 的重复字段需要结合具体集合配置识别；
- 第6行的导出范围和字段来源必须参与重复字段判定；
- 无法安全区分的重复字段必须报错，不能静默覆盖。

字段来源/父类型的约定：

- `CreateFile.后缀`：该字段按创建文件的后缀区分，例如 `CreateFile.En`；
- `class`、`struct`、`ClassStructList`、`ClassStructArray` 等：表示对应父类型或容器类型；
- 自定义类型使用 `自定义类型.字段名.字段类型`，例如 `Struct.ItemId.int`、`Class.ItemId.int`；
- Python 工具必须解析这些来源信息，并在生成嵌套类型或字段名冲突诊断中使用。

### 6.4 字段列解析

Python 工具不能简单使用第一行作为普通字段名。必须结合行号和第一列元数据，解析第2列及之后的字段列：

- 字段显示说明来自模板字段描述行；
- 字段名来自字段名行；
- C# 数据类型来自字段类型行；
- 父类型/来源来自父类型行；
- 导出范围来自导出范围行；
- 字段对应的数据值从模板约定的数据起始行读取；
- 空列忽略；
- 任何一个导出字段缺少字段名或类型都必须报错；
- C# 标识符必须合法；
- 类型必须在生成器支持范围内；
- `uint`、`int`、`bool`、`float`、`long`、`double` 使用固定宽度编码；
- `string`、bytes 和数组数量/长度统一使用 `uint` 前缀。

模板中的说明文字是规则的一部分，生成器实现应在代码注释或文档中保留，不得凭空将表头改造成与模板不一致的四行表头。


Excel 生成流程必须使用 Python 实现，工具统一放在 `Excel/tools/`。不要在本功能中使用 EPPlus、C# Excel 解析器或 Unity Editor 作为生成器实现。Python 工具可以使用 `openpyxl` 读取 `.xlsx`，并使用 Python 标准库完成代码生成、二进制编码、Hash 计算和文件写入。现有 `GameProject/GameEngineEditor/Editor/Excel/ExcelHelper.cs` 不作为本生成流程的实现基础，也不要为了本功能破坏它的现有通用功能。

## 6.5 数据解析和 Python 工具要求

Python 工具必须使用 `Excel/模板.xlsx` 的实际布局，不能把模板简化成普通的“第一行字段名、第二行类型”格式。工具负责读取表级元数据、字段元数据、数据行，校验后生成 C# 代码和二进制文件。

推荐工具文件：

- `Excel/tools/generate.py`：命令行入口；
- `Excel/tools/excel_reader.py`：模板和工作表解析；
- `Excel/tools/code_generator.py`：C# 代码生成；
- `Excel/tools/binary_writer.py`：二进制配置导出；
- `Excel/tools/schema_hash.py`：规范化模型和 Schema Hash；
- `Excel/tools/README.md`：Python 环境、参数和生成说明。

可以使用 `openpyxl` 读取 `.xlsx`，二进制编码和 Hash 使用 Python 标准库。Python 工具不能依赖 Unity、EPPlus 或 GameProto DLL。

模板解析规则：

- 模板第一行描述配置类、数据表规则、字段用途或语言等表级/列级信息；
- 模板第一列从第二行开始描述字段相关元数据；
- `Sheet1` 保存字段元数据说明和字段示例；
- `Sheet2` 保存配置类名、类名作用域、索引字段、分类字段和字段列示例；
- Sheet2 第1行第1列为配置类名，例如 `TextConfig`；
- Sheet2 第1行第2列为类名作用域标志，例如 `TRUE`；
- Sheet2 第1行第3列为数据表字典索引 key，例如 `Level.Start`，最多支持3个 `.` 分隔字段；
- Sheet2 第1行第4列为数据分类规则，例如 `Level.Start`，用于生成 `Dictionary<key, List<T>>` 或最多3层嵌套 Dictionary；
- Sheet2 第2行从第1列开始为字段显示描述；
- Sheet2 第3行为字段名；
- Sheet2 第4行为字段类型；
- Sheet2 第5行为字段父类型/来源；
- Sheet2 第6行为导出范围：`All`、`Client` 或 `Server`；
- Sheet2 第2列开始的每个非空列代表一个字段；
- 从模板约定的数据起始行开始读取该字段的实际数据；
- 第一行和第一列的说明文字必须被解析为规则，不能直接跳过。

字段父类型/来源规则：

- `CreateFile.后缀` 表示按创建文件后缀区分，例如 `CreateFile.En`；
- `class`、`struct`、`ClassStructList`、`ClassStructArray` 表示字段的父类型或容器类型；
- 自定义类型使用 `自定义类型.字段名.字段类型`，例如 `Struct.ItemId.int`、`Class.ItemId.int`；
- 字段名允许在语言分文件或数组/List 场景下重复，但必须结合父类型、来源、导出范围和容器规则判断；无法安全区分时必须报错，不能覆盖。

字段类型第一版至少支持：

- `uint`、`int`、`bool`、`float`、`long`、`double`、`string`；
- 后续可扩展具体元素类型的 `T[]` 和 `List<T>`，例如 `int[]`、`List<string>`、`Consume[]`、`List<RewardData>`；
- 所有 string、bytes、数组或 List 的可变长度/数量前缀统一使用小端序 `uint`；
- 固定数字按照运行时提示词中的固定字节宽度写入。

Python 工具必须校验：

- 配置类名和字段名是合法 C# 标识符；
- 配置类名在不同 Sheet 的作用域和冲突；
- 字段名、类型、父类型和导出范围不能为空或互相矛盾；
- 索引字段存在且最多3个；
- 类型受支持；
- 数值范围、bool 格式、UTF-8 字节数和 uint/int 安全上限；
- 重复主键、重复组合索引和无法区分的重复字段；
- 每个错误包含文件、Sheet、Excel 行、列和字段名。

至少支持并校验：

- 一个主键字段和 key 标记；
- client 导出标记；
- 完全空行忽略；
- 字段名重复、非法字段名；
- 不支持类型；
- 数值超出目标类型范围；
- 非法 bool；
- UTF-8 字节长度超出 uint/int 或安全上限；
- 重复主键；
- 错误包含 Excel 文件、Sheet、行、列和字段名。

第一版如果无法可靠定义数组单元格的分隔和转义规则，可以暂不支持 Excel 数组；禁止实现含义不明确的字符串拆分。

## 七、配置表代码生成

每张表生成 `public sealed class`，只生成只读属性、完整构造函数和直接 Decode 方法，不生成修改接口。使用 class 而不是 struct，避免配置记录在传参、集合操作和返回值过程中的值拷贝，以及大型或嵌套值类型带来的栈空间压力。

TextConfig 示例：

```csharp
public sealed class TextConfig
{
    public int Id { get; }
    public byte ParameterCount { get; }
    public string Content { get; }

    public TextConfig(int id, byte parameterCount, string content)
    {
        Id = id;
        ParameterCount = parameterCount;
        Content = content;
    }

    public static TextConfig Decode(ref ProtoReader reader)
    {
        int id = reader.ReadInt32();
        byte parameterCount = reader.ReadByte();
        string content = reader.ReadString();
        return new TextConfig(id, parameterCount, content);
    }
}
```

生成要求：

- 使用 sealed class；
- 使用只读属性；
- Decode 按 Excel 字段顺序直接生成；
- 不使用反射；
- 代码中不保存字段名和类型元数据；
- Excel 导出端按照完全相同的字段顺序调用明确写入代码。

同时生成配置表容器，例如：

```csharp
public sealed class TextConfigTable
{
    private readonly Dictionary<int, TextConfig> _items;
    public int Count { get; }
    public bool TryGet(int id, out TextConfig config);
    public TextConfig Get(int id);
    public static TextConfigTable Load(byte[] data);
}
```

MVP 默认只支持 int 主键；如果扩展其他主键，必须在 Schema 和文档中明确。加载时：

1. 校验 Magic；
2. 校验 FormatVersion；
3. 校验 Config Schema Hash；
4. 读取 uint RecordCount；
5. 转 int 前校验 int.MaxValue 和安全上限；
6. 预分配 Dictionary；
7. 逐条调用生成 Decode；
8. 检测重复 key；
9. 调用 EnsureFullyConsumed，拒绝尾随数据。

## 八、配置二进制导出

每张配置表文件头：

```text
Magic          4 bytes   固定 ASCII，例如 GCFG
FormatVersion  uint      4 bytes
SchemaHash     8 bytes
RecordCount    uint      4 bytes
Records        N bytes
```

文件头固定 20 bytes。整个文件不受单个 uint 长度前缀限制，但仍受内存和安全上限限制。RecordCount 使用 uint。

TextConfig 每条记录固定布局：

```text
Id                  int       4 bytes
ParameterCount      byte      1 byte
ContentLength       uint      4 bytes
Content             UTF-8     ContentLength bytes
```

不得使用 `BinaryWriter.Write(string)`。必须显式计算 UTF-8 字节数，写入 uint，再写入 UTF-8 数据。超过 uint、int.MaxValue 或安全上限必须失败，禁止截断。

所有可变字段均使用 uint 长度；数组使用 uint 元素数量；单个网络 PayloadLength 使用 uint。配置导出前完成全部解析和校验，避免只生成部分文件。只有内容变化时才写入文件。

## 九、Editor 入口

提供清晰的 Unity Editor 菜单或公共入口：

```text
Tools/Game/Generate Protocol Code
Tools/Game/Export Config Tables
Tools/Game/Generate All
```

菜单只负责调用核心服务。核心服务拆分为：输入发现、Schema 解析、Excel 解析、中间模型、校验、C# 生成、二进制导出、文件写入和 AssetDatabase 刷新。

失败时不写半成品；日志使用 `GameEngine.Log`，不使用 Console.WriteLine 或 UnityEngine.Debug；记录异常时保留异常对象和堆栈。

## 十、验证要求

至少提供或执行以下验证：

- Schema 解析和非法输入校验；
- 重复 MessageId 和重复主键检测；
- LoginRequest/LoginResponse 编码解码 round-trip；
- GetEncodedSize 与实际写入长度一致；
- TextConfig Excel 到二进制再到 class 的端到端验证；
- ASCII、中文和空字符串；
- UTF-8 字节长度而非字符数；
- uint、int.MaxValue 和运行时安全上限边界；
- 损坏 Magic、错误 FormatVersion、错误 Schema Hash；
- 截断数据、长度大于剩余数据、尾随多余字节；
- 数组数量和可变字段长度溢出；
- 生成结果可编译；
- 生成代码不含反射序列化。

如果只完成 .NET 编译，只能称为“.NET 编译成功”，不能称为测试通过。未启动 Unity 时，必须明确说明未进行 Unity Editor 和 Play Mode 验证。

## 十一、代码规范

- 4 个空格缩进；
- Allman 大括号；
- 所有控制流都使用大括号；
- 类型、方法、属性使用 PascalCase；私有字段使用 `_camelCase`；
- 公共 API 和关键格式约束添加简洁中文 XML 注释；
- 一个文件通常只定义一个主要类型；
- 生成文件包含 `// <auto-generated>` 声明；
- 不在生成文件中写业务逻辑；
- 不修改 bin、obj、PDB 作为源码；
- 不恢复 GameProto 已删除的旧文件。

## 十二、最终汇报

完成后报告：

1. Schema 解析、网络代码生成和消息注册表；
2. Excel 表头解析、配置 class 生成和二进制导出；
3. 主要新增和修改文件；
4. 生成的二进制布局；
5. round-trip 和边界验证结果；
6. dotnet build 错误数和警告数；
7. git diff --check 结果；
8. 是否进行了 Unity Editor/Play Mode 验证；
9. 构建更新的 DLL；
10. 开始任务前已有且未被覆盖的工作区改动；
11. 尚未支持的 map、oneof、optional、字段级兼容、多维数组等功能。
