# GameProto 协议和配置表代码生成提示词

你正在修改 Unity 游戏框架工程 `G:\Pisces2026`。请实现 GameProto 的网络协议和 Excel 配置表代码生成系统。本提示词只负责 Schema、Excel、代码生成和配置导出；二进制 Reader、Writer、Size、消息基类和文件格式运行时实现请遵循同目录的 `CODE_GENERATION_PROMPT.md`。

> `GameProject/GameProto` 中已有一批文件被用户主动删除。忽略这些删除，不要恢复、还原或复用被删除的旧实现。不要覆盖工作区内与本任务无关的修改。

## 一、边界和目录

- 运行时代码和生成后的 C# 文件放在 `GameProject/GameProto/`。
- Excel、EPPlus 和 Unity Editor 工具放在 `GameProject/GameEngineEditor/Editor/`。
- GameProto 不得引用 UnityEditor、EPPlus 或 GameEngineEditor。
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

## 六、Excel 表头规范

Excel 继续使用 EPPlus，相关代码放在 `GameEngineEditor`，可以复用 `GameProject/GameEngineEditor/Editor/Excel/ExcelHelper.cs`，但不要破坏其现有通用功能。

推荐采用多行表头：

| 行 | 内容 |
|---|---|
| 第1行 | 字段名 |
| 第2行 | 字段类型 |
| 第3行 | 字段说明 |
| 第4行 | 导出标记，例如 `key,client` 或 `client` |
| 第5行起 | 数据 |

示例：

| Id | ParameterCount | Content |
|---|---|---|
| int | byte | string |
| 文本ID | 参数数量 | 文本内容 |
| key,client | client | client |
| 1001 | 2 | 获得{0}个{1} |

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
