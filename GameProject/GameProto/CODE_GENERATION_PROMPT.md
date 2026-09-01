# GameProto 自定义二进制运行时实现提示词

你正在修改 Unity 游戏框架工程 `G:\Pisces2026`，请基于当前工作区实现 GameProto 的自定义二进制运行时。协议和配置表的 Schema、Excel 读取、代码生成及导出工具已经单独定义在：

`GameProject/GameProto/CODE_GENERATOR_PROMPT.md`

本文件只负责运行时二进制格式和运行时基础代码，不要在本文件范围内实现代码生成器。

> `GameProject/GameProto` 中已有一批文件被用户主动删除。忽略这些删除，不要恢复、还原或复用被删除的旧实现；基于当前状态重新实现。不要修改工作区内与任务无关的用户改动。

## 一、项目边界

- Unity 2022.3.62f2 及以上。
- GameProject 项目目标框架为 `.NET Framework 4.7.2`。
- 运行时项目：`GameProject/GameProto/`。
- Excel、EPPlus 和 Unity Editor 工具属于 `GameProject/GameEngineEditor/`，不得被 GameProto 引用。
- `GameProto` 只放运行时代码和生成后的运行时代码。
- 不使用 Google Protobuf、varint、反射序列化或运行时程序集扫描。
- 不直接修改 `GameClient/Assets` 下 DLL，DLL 由 GameProject 构建生成。

修改前执行 `git status --short`。完成后执行：

```powershell
dotnet build "GameProject\GameProject.sln" -c Debug
git status --short
git diff --check
```

## 二、固定基础类型格式

统一使用小端序，固定宽度如下：

| 类型 | 字节数 |
|---|---:|
| bool | 1 |
| byte/sbyte | 1 |
| short/ushort | 2 |
| int/uint/float | 4 |
| long/ulong/double | 8 |

`float` 和 `double` 使用 IEEE 754 原始位模式。`bool` 只允许 0 和 1，其他值必须抛出格式异常。

## 三、可变长度格式

所有可变长度数据统一使用小端序 `uint`，固定占 4 bytes。不得使用 byte、short、ushort、int 或 varint 作为可变长度前缀。

### string

```text
uint Utf8ByteLength
byte[Utf8ByteLength] Utf8Data
```

统一 UTF-8、无 BOM。长度表示 UTF-8 字节数，不是 C# `string.Length`。null 按空字符串处理，0 长度解码为 `string.Empty`。读取时必须检查 `uint` 到 `int` 的转换、运行时安全上限和剩余字节，禁止截断。

### bytes

```text
uint ByteLength
byte[ByteLength] Data
```

null 和空数组均编码为 0 长度。读取时检查 `uint`、`int.MaxValue`、安全上限和剩余字节。

### 数组

```text
uint ElementCount
Element[ElementCount]
```

长度表示元素数量。读取后转换为 int 前必须检查范围和安全上限。MVP 只需要支持生成代码使用的一维数组；不实现任意 Dictionary、map、oneof、多维数组、循环引用或多态对象。

## 四、ProtoReader

在 `GameProject/GameProto/Runtime/Codec/ProtoReader.cs` 中实现基于 `byte[] + offset + end` 的边界安全 Reader：

- 支持完整 byte[] 和指定 offset/length 子区间；
- 构造参数必须校验；
- 不得越过子区间；
- 每次读取前检查剩余字节；
- 基础数值读取不创建临时 byte[]；
- 数值按小端序直接读取；
- 提供 `Position`、`Remaining`、`IsAtEnd`、`EnsureRemaining(int)`、`EnsureFullyConsumed()`；
- 提供所有固定宽度基础类型的读取方法；
- 提供统一的 `ReadString()` 和 `ReadBytes()`，其长度前缀都是 uint；
- uint 长度转 int 前使用范围检查；
- 错误包含位置、期望长度和剩余长度。

建议保持 net472 和 Unity 2022 兼容，不强制依赖 Span<T> 或 BinaryPrimitives。

## 五、ProtoWriter

在 `GameProject/GameProto/Runtime/Codec/ProtoWriter.cs` 中实现对应 Writer：

- 支持所有固定宽度基础类型；
- 提供统一的 `WriteString(string)` 和 `WriteBytes(byte[])`；
- 字符串先计算 UTF-8 字节数，再写入 uint；
- bytes 先校验长度，再写入 uint；
- 数组数量由生成代码校验并写入 uint；
- 提供 Position、Capacity、Remaining；
- 写入前检查容量；
- 禁止静默截断；
- 所有长度计算、加法和乘法使用 checked 或等价溢出检查；
- 可以支持调用方提供缓冲区和 ToArray()，避免不必要扩容。

## 六、ProtoSize 与异常

实现 `ProtoSize`，提供基础类型、`String(string)` 和 `Bytes(byte[])` 的尺寸计算。统一规则：

- string 尺寸为 `4 + UTF8字节数`；
- bytes 尺寸为 `4 + 数据字节数`；
- 数组尺寸为 `4 + 所有元素尺寸`；
- 超限和整数溢出必须抛出异常；
- 尺寸结果必须与 Writer 实际写入长度一致。

定义明确的 `ProtoSerializationException` 和配置读取所需的 `ConfigSerializationException`。不得静默吞异常。

集中定义运行时安全上限，例如最大字符串字节数、最大 bytes 字节数、最大集合数量、最大 Payload 长度和最大配置记录数。uint 格式上限不意味着允许无限分配。

## 七、网络消息运行时

实现 `ProtoMessage`：

```csharp
public abstract class ProtoMessage
{
    public abstract uint MessageId { get; }
    public abstract int GetEncodedSize();
    public abstract void Encode(ref ProtoWriter writer);
    public abstract void Decode(ref ProtoReader reader);
}
```

生成的协议类必须是 sealed class，按字段顺序直接生成 GetEncodedSize、Encode、Decode，不使用反射、Attribute 扫描、FieldInfo、表达式树或 Activator。

## 八、网络包头

因为 PayloadLength 也属于可变长度字段，使用 uint：

```text
PayloadLength      uint      4 bytes
MessageId          uint      4 bytes
ProtocolVersion    uint      4 bytes
Sequence           uint      4 bytes
```

包头固定 16 bytes，PayloadLength 不包含包头。实际运行时 Payload 受 uint、int.MaxValue 和集中安全上限共同约束。GameProto 不耦合 Socket；文档和 API 注释必须说明 TCP 一次 Receive 不等于一条消息，接收方必须先累计完整 16 bytes 包头，再按 PayloadLength 累计 Payload。

生成无反射消息注册表，至少提供：

```csharp
bool TryCreate(uint messageId, out ProtoMessage message);
ProtoMessage Create(uint messageId);
```

使用生成的 switch。未知 ID 和重复 ID 必须明确失败。

## 九、配置文件运行时格式

配置记录运行时使用 `sealed class`，而不是 struct。这样可以避免配置对象在传参、集合操作和返回值过程中的值拷贝，以及大型或嵌套值类型带来的栈空间压力。配置对象只暴露只读属性，不提供修改接口。

配置文件头固定 20 bytes：

```text
Magic          4 bytes
FormatVersion  uint      4 bytes
SchemaHash     8 bytes
RecordCount    uint      4 bytes
Records        N bytes
```

Magic 可使用固定 ASCII `GCFG`。RecordCount 转 int 前必须检查 int.MaxValue 和安全上限。整个文件不受 uint 长度前缀限制，但仍受内存和安全上限限制。每条可变字段都独立使用 uint 长度。

加载时必须：

1. 验证 Magic；
2. 验证 FormatVersion；
3. 验证 SchemaHash；
4. 读取 RecordCount；
5. 预分配查询容器；
6. 调用生成的 Decode；
7. 检测重复 key；
8. 调用 EnsureFullyConsumed，拒绝尾随数据。

## 十、编码规则与验证

不得使用 `BinaryWriter.Write(string)`，必须显式写入 uint 长度和 UTF-8 数据。

TextConfig 示例布局：

```text
Id                  int       4 bytes
ParameterCount      byte      1 byte
ContentLength       uint      4 bytes
Content             UTF-8     ContentLength bytes
```

必须验证：

- 所有基础类型 round-trip；
- 小端序字节布局；
- UTF-8 中文和空字符串；
- uint、int.MaxValue 和安全上限边界；
- Reader 越界、子区间保护、Writer 容量不足；
- 截断数据、非法 bool、错误 Magic、版本和 SchemaHash；
- 多余尾随字节；
- GetEncodedSize 与实际编码长度一致；
- 网络 Payload 和配置记录数量的安全限制。

## 十一、代码规范和报告

- 4 个空格缩进，Allman 大括号；
- 所有控制流使用大括号；
- 公共 API 添加简洁中文 XML 注释；
- 日志使用 GameEngine.Log，不使用 Console.WriteLine 或 UnityEngine.Debug；
- 不修改 bin、obj、PDB 作为源码；
- 不恢复 GameProto 已删除文件。

如果只完成 .NET 编译，只能报告编译成功，不能称为测试通过；如果未启动 Unity，必须说明未进行 Unity Editor 和 Play Mode 验证。最终报告列出主要源码文件、二进制布局、构建结果、round-trip 结果、git diff --check、Unity 验证状态和构建更新的 DLL。
