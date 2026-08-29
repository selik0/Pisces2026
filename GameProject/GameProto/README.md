# GameProto

协议程序集。当前提供一套**不依赖第三方库**的轻量级 protobuf 序列化框架（线格式与标准 protobuf 兼容），
用于游戏网络协议与**只读配置表**（Excel → protobuf 二进制 → struct 数组）的序列化/反序列化。
纯 C#、零 Unity 依赖，可同时用于客户端（IL2CPP/AOT 友好）与服务器。

## 快速开始

```csharp
using System.Collections.Generic;
using GameProto;

public enum JobType
{
    None = 0,
    Warrior = 1,
    Mage = 2,
}

public class SkillInfo : ProtoMessage
{
    [ProtoField(1)] public int SkillId;
    [ProtoField(2)] public int Level;
    [ProtoField(3)] public string Name = string.Empty;
}

public class PlayerInfo : ProtoMessage
{
    [ProtoField(1)] public int Id;
    [ProtoField(2)] public string Name = string.Empty;
    [ProtoField(3)] public JobType Job;
    [ProtoField(4)] public float Speed;
    [ProtoField(5)] public double Score;
    [ProtoField(6)] public long Exp;
    [ProtoField(7)] public bool Online;
    [ProtoField(8)] public byte[] Data;
    [ProtoField(9)] public SkillInfo MainSkill;
    [ProtoField(10)] public List<int> Items = new List<int>();
    [ProtoField(11, Packed = true)] public List<long> Scores = new List<long>();
    [ProtoField(12)] public List<string> Tags = new List<string>();
    [ProtoField(13)] public List<SkillInfo> Skills = new List<SkillInfo>();
    [ProtoField(14)] public Dictionary<string, int> Stats = new Dictionary<string, int>();
    [ProtoField(15, ZigZag = true)] public int Delta;
}
```

序列化与反序列化：

```csharp
PlayerInfo player = new PlayerInfo { Id = 1001, Name = "阿呆", Job = JobType.Mage };

byte[] data = player.ToByteArray();                    // 序列化
PlayerInfo copy = PlayerInfo.FromByteArray<PlayerInfo>(data); // 反序列化

byte[] bytes = ProtoCodec.Serialize(player);           // 泛型入口
PlayerInfo restored = ProtoCodec.Deserialize<PlayerInfo>(bytes);
```

## 功能特性

- **线格式兼容标准 protobuf**：varint、zigzag（sint32/sint64）、fixed32/fixed64、
  length-delimited、packed 重复字段、map 条目、嵌套消息。
- **class 与 struct 消息均支持**：class 继承 `ProtoMessage`，struct 直接实现 `IProtoMessage`
  （见下文"struct 消息"）；`ProtoCodec.Serialize/Deserialize<T>` 两种类型通用。
- **struct 反序列化直写**：struct 消息首次解析时用表达式树编译 (ref T, ProtoReader) 直写委托，
  字段直接赋值、无反射、顶层无装箱；表达式编译不可用的环境（如 IL2CPP/AOT）自动回退反射路径。
- **代码生成直写（ProtoCodeGen）**：由字段定义生成手写直写版 struct 源码
  （WriteTo/ComputeSize/MergeFrom 全部内联，无反射、无表达式树依赖，IL2CPP/AOT 完全安全），
  生成时默认按内存对齐重排字段声明顺序（大对齐在前，减少 padding）。
- **特性驱动自动编解码**：`[ProtoField]` 标注公开字段或属性即可，类型描述按类型缓存，
  首次使用后不再反射。
- **未知字段跳过**：解析时按 wire type 跳过未声明的字段（含 group），保证前后向兼容。
- **合并语义**：`MergeFrom`/`ParseFrom` 遵循 protobuf 语义——单数消息字段在已有值上合并、
  重复字段追加（struct 的嵌套消息字段同样支持）。
- **packed 兼容**：写入端与读取端是否声明 `Packed` 不要求一致，解析端自动兼容两种形式。
- **手工编解码**：消息可覆写 `ProtoMessage.WriteTo/ComputeSize/MergeFrom` 完全手工实现
  （高频消息优化路径），并可继续作为嵌套消息参与自动编解码。
- **proto3 默认值语义**：标量 0/false/0f/0d、空串、空 bytes、空列表、空 map 不编码；
  struct 嵌套消息字段所有成员均为默认值时也不编码；class 嵌套消息非 null 总是编码
  （保留"存在"语义）。

## 类型映射

| C# 类型 | protobuf 类型 | wire type |
| --- | --- | --- |
| `int` | int32 | Varint（负数占 10 字节） |
| `long` | int64 | Varint |
| `uint` / `ulong` | uint32 / uint64 | Varint |
| `bool` | bool | Varint |
| `enum` | enum | Varint |
| `float` | float | Fixed32 |
| `double` | double | Fixed64 |
| `string` | string | LengthDelimited（UTF-8） |
| `byte[]` | bytes | LengthDelimited |
| `IProtoMessage` 派生类 | 嵌套消息 | LengthDelimited |
| `List<T>` | repeated T | 逐个 tag；标量可用 `Packed = true` |
| `Dictionary<K, V>` | map<K, V> | LengthDelimited 条目 |

- `int`/`long` 加 `ZigZag = true` 对应 sint32/sint64。
- map 键支持 `int/long/uint/ulong/bool/string`；map 值支持上述任意标量、string、bytes、嵌套消息。
- 重复字段元素支持标量、string、bytes、嵌套消息；`Packed = true` 仅对数值标量有效。

## 手工编解码示例

```csharp
public class ManualMessage : ProtoMessage
{
    public int A;
    public string B = string.Empty;

    public override void WriteTo(ProtoWriter writer)
    {
        writer.WriteTag(1, ProtoWireType.Varint);
        writer.WriteInt32(A);
        if (!string.IsNullOrEmpty(B))
        {
            writer.WriteTag(2, ProtoWireType.LengthDelimited);
            writer.WriteString(B);
        }
    }

    public override int ComputeSize()
    {
        int size = ProtoWireFormat.GetVarintSize(ProtoWireFormat.MakeTag(1, ProtoWireType.Varint)) +
                   ProtoWireFormat.GetVarintSize((ulong)(long)A);
        if (!string.IsNullOrEmpty(B))
        {
            int byteCount = System.Text.Encoding.UTF8.GetByteCount(B);
            size += ProtoWireFormat.GetVarintSize(ProtoWireFormat.MakeTag(2, ProtoWireType.LengthDelimited)) +
                    ProtoWireFormat.GetVarintSize((ulong)byteCount) + byteCount;
        }
        return size;
    }

    public override void MergeFrom(ProtoReader reader)
    {
        while (true)
        {
            uint tag = reader.ReadTag();
            if (tag == 0) return;
            int fieldNumber = ProtoWireFormat.GetFieldNumber(tag);
            ProtoWireType wireType = ProtoWireFormat.GetWireType(tag);
            if (fieldNumber == 1 && wireType == ProtoWireType.Varint)
            {
                A = reader.ReadInt32();
            }
            else if (fieldNumber == 2 && wireType == ProtoWireType.LengthDelimited)
            {
                B = reader.ReadString();
            }
            else
            {
                reader.SkipField(tag);
            }
        }
    }
}
```

注意：`WriteTo`、`ComputeSize`、`MergeFrom` 三个方法必须一起覆写（或都不覆写），
保证长度计算与实际写入一致。

## struct 消息（只读配置表场景）

struct 不能继承 `ProtoMessage`（基类是 class），需直接实现 `IProtoMessage`，
三个方法分别委托给 `ProtoCodec` 的 struct 入口即可，行为与 class 完全一致
（含嵌套 struct 字段、List/map、默认值语义、合并语义）：

```csharp
public struct SkillCfg : IProtoMessage
{
    [ProtoField(1)] public int SkillId;
    [ProtoField(2)] public string Name;

    public void WriteTo(ProtoWriter writer)
    {
        ProtoCodec.WriteStructFields(this, writer);
    }

    public int ComputeSize()
    {
        return ProtoCodec.ComputeStructSize(this);
    }

    public void MergeFrom(ProtoReader reader)
    {
        // ref this：解析结果写回实例本身，不会因装箱丢失
        ProtoCodec.MergeStructFields(ref this, reader);
    }
}

// 使用：与 class 相同的泛型入口
SkillCfg skill = new SkillCfg { SkillId = 1, Name = "火球" };
byte[] data = ProtoCodec.Serialize(skill);
SkillCfg back = ProtoCodec.Deserialize<SkillCfg>(data);
```

结构嵌套（struct 字段 / struct 列表 / struct map 值）与 class 组合使用均受支持。
struct 反序列化已由框架直写化（表达式树编译），无需手工生成解析代码；
如需 IL2CPP 下的极致性能，可用 `ProtoCodeGen` 生成手写直写版源码
（无反射、无表达式树依赖，见下）。

## 代码生成直写（ProtoCodeGen）

`ProtoCodeGen` 把字段定义生成手写直写版 struct 源码（`WriteTo`/`ComputeSize`/`MergeFrom`
全部内联为按字段号 switch 的直接赋值，IL2CPP/AOT 完全安全），并默认按**内存对齐重排
字段声明顺序**（对齐 8 → 4 → 2 → 1 降序，减少 padding，仅影响内存布局、不影响字段编号
与序列化格式）。两种输入方式：

```csharp
// 方式一：从已有类型反射（把三行委托版 struct 升级为直写版）
string code = ProtoCodeGen.GenerateStruct(typeof(SkillCfg), "SkillCfgDirect");

// 方式二：从字段定义生成（导出器从 Excel 表头构造）
var fields = new List<ProtoFieldDef>
{
    new ProtoFieldDef(1, "Id", typeof(int)),
    new ProtoFieldDef(2, "Name", typeof(string)),
    new ProtoFieldDef(3, "Price", typeof(int)),
    new ProtoFieldDef(4, "Tags", typeof(List<string>)),
};
string code2 = ProtoCodeGen.GenerateStruct("ItemCfg", fields);   // 默认 sortByAlignment: true
```

生成结果示例（字段按对齐排序：引用/8 对齐在前、bool 在后）：

```csharp
// 由 ProtoCodeGen 自动生成，请勿手动修改
public struct ItemCfg : IProtoMessage
{
    [ProtoField(2)] public string Name;
    [ProtoField(4)] public List<string> Tags;
    [ProtoField(1)] public int Id;
    [ProtoField(3)] public int Price;

    public void WriteTo(ProtoWriter writer) { /* 内联直写：默认值判断 + tag + 值 */ }
    public int ComputeSize() { /* 内联大小计算 */ }
    public void MergeFrom(ProtoReader reader) { /* while + switch(__field) 直接赋值 */ }
}
```

生成的代码与反射版/表达式树直写版**字节级一致**（已用 16 字段全形态 struct 验证往返）。

注意：

- struct 的 `List`/`Dictionary` 成员默认为 null，解析时框架会自动初始化；
- **对具体 struct 实例直接调用 `MergeFrom` 会装箱导致修改丢失**，需要修改时请走
  `ProtoCodec.MergeStructFields(ref instance, reader)` 或泛型约束调用（`Deserialize<T>` 内部即如此）；
- struct 解析已直写化（表达式树编译，按类型缓存，字段直接赋值、无反射、顶层无装箱）；
  `WriteStructFields`/`ComputeStructSize` 仍走反射路径，配置表只读场景不受影响；
- `ProtoReader.ReadMessage<T>()` 同样支持 struct 嵌套消息（装箱合并后解箱回写）；
- struct 全零与"未设置"无法区分——与 proto3 默认值语义一致，配置表只读场景无碍。

## 配置表落地：Excel → 二进制 → 加载

配置表（只读）推荐链路：策划维护 Excel → 编辑器导出为 protobuf 二进制 → 运行时加载为
struct 数组 + 主键索引。整套流程复用同一个 `GameProto` 框架，无 protoc 依赖。

```
Excel（策划）→ 导出器（GameEngineEditor，EPPlus 读表，复用 ExcelHelper）
    ├─ 生成 struct 配置类源码（ItemCfg + ItemTable）→ GameLogic/Config
    └─ 用 ProtoCodec.Serialize 写出 item.bytes → Resources/Addressables
→ 运行时 ConfigManager 加载 item.bytes → 解析为 struct 数组 + Dictionary 主键索引
```

以道具表 `item.xlsx`（Id / Name / Type / Price / MaxStack / Tag）为例：

```csharp
// ① 生成的配置类（导出器生成，请勿手改）
public struct ItemCfg : IProtoMessage
{
    [ProtoField(1)] public int Id;
    [ProtoField(2)] public string Name;
    [ProtoField(3)] public int Type;
    [ProtoField(4)] public int Price;
    [ProtoField(5)] public int MaxStack;
    [ProtoField(6)] public List<string> Tags;   // "武器;物理" 拆成列表

    public void WriteTo(ProtoWriter writer) { ProtoCodec.WriteStructFields(this, writer); }
    public int ComputeSize() { return ProtoCodec.ComputeStructSize(this); }
    public void MergeFrom(ProtoReader reader) { ProtoCodec.MergeStructFields(ref this, reader); }
}

// 整张表是一个消息：repeated 行 = 一个字段（一次解析 + 可扩展表头字段）
public struct ItemTable : IProtoMessage
{
    [ProtoField(1)] public List<ItemCfg> Items;

    public void WriteTo(ProtoWriter writer) { ProtoCodec.WriteStructFields(this, writer); }
    public int ComputeSize() { return ProtoCodec.ComputeStructSize(this); }
    public void MergeFrom(ProtoReader reader) { ProtoCodec.MergeStructFields(ref this, reader); }
}
```

```csharp
// ② 导出器（GameEngineEditor；Editor 工程需引用 GameProto）
byte[] bytes = ProtoCodec.Serialize(table);          // table: ItemTable（含所有行）
File.WriteAllBytes("Assets/Config/item.bytes", bytes);
```

第 1 行的二进制（protobuf 线格式，默认值 0/空串自动跳过）：

```
08 01                          ← 字段1 Id = 1
12 03 E5 89 91                 ← 字段2 Name = "剑"（UTF-8）
18 01                          ← 字段3 Type = 1
20 64                          ← 字段4 Price = 100
28 63                          ← 字段5 MaxStack = 99
32 03 E6 AD A6  32 03 E5 99 A8 ← 字段6 Tags = ["武器", "物理"]
```

```csharp
// ③ 运行时加载与访问（ConfigManager 单例）
public sealed class ConfigManager
{
    public static ConfigManager Instance { get; } = new ConfigManager();

    private readonly Dictionary<int, ItemCfg> _items = new Dictionary<int, ItemCfg>();

    public void LoadItemTable(byte[] bytes)
    {
        ItemTable table = ProtoCodec.Deserialize<ItemTable>(bytes);   // 一次解析整表
        _items.Clear();
        foreach (ItemCfg item in table.Items)
        {
            _items[item.Id] = item;
        }
    }

    public ItemCfg GetItem(int id)
    {
        ItemCfg item;
        return _items.TryGetValue(id, out item) ? item : default(ItemCfg);
    }
}

// 启动时：ConfigManager.Instance.LoadItemTable(Resources.Load<TextAsset>("Config/item").bytes);
// 读取：ItemCfg sword = ConfigManager.Instance.GetItem(1); → sword.Price / sword.MaxStack
```

关键点：

- `GetItem` 返回 struct **副本**（值语义），改动只影响副本，天然"只读"，不会污染表数据；
- 索引用 `Dictionary<int, ItemCfg>`，按 Id 取行只做一次浅拷贝（string/List 仅拷引用）；
- 加载是一次性的：直写解析下 10 万行 × 16 字段约 200ms（与手写 switch 同数量级）；
- **版本容错**：策划加列（新增字段号），新旧二进制/新旧代码互相兼容（未知字段自动跳过）；
- 若不想用外层 `ItemTable`，可改为"每行带长度前缀顺序拼接"，运行时
  `while (!reader.IsAtEnd) rows.Add(reader.ReadMessage<ItemCfg>());` 逐行读。

## 限制与约定

- 不跟踪字段"存在性"（无 presence API），默认值解析后即为默认值（proto3 语义）。
- 重复字段使用 `List<T>`，map 使用 `Dictionary<K, V>`；不支持数组形式。
- 属性必须同时具有公开 getter 与 setter；字段必须是非静态公开字段。
- class 嵌套消息需要公开无参构造（struct 天然满足）。
- 字段编号同一消息内不可重复，范围 1 到 2^29-1。
- 不支持 group 类型消息（仅能跳过），不支持 `fixed32/fixed64/sfixed32/sfixed64` 与 nullable 标量。
- class 与 struct 的序列化/大小计算走反射路径；struct 反序列化已直写（表达式树编译，
  详见上文）。高频消息仍可覆写三个方法完全手工编解码。

## 目录结构

```
GameProject/GameProto/Runtime/
├── ProtoWireType.cs           wire type 枚举
├── ProtoWireFormat.cs         tag 构造/解析、zigzag、varint 长度工具
├── ProtoProtocolException.cs  协议错误异常
├── ProtoWriter.cs             二进制写入器
├── ProtoReader.cs             二进制读取器（长度受限区域、未知字段跳过）
├── IProtoMessage.cs           消息接口
├── ProtoMessage.cs            消息基类（特性驱动默认实现 + 便捷方法）
├── ProtoFieldAttribute.cs     [ProtoField] 字段标注特性
├── ProtoCodec.cs              特性驱动自动编解码器（类型描述缓存）
├── ProtoCodec.StructCodegen.cs struct 反序列化直写（表达式树编译，IL2CPP 回退反射）
└── ProtoCodeGen.cs            代码生成器（生成直写版 struct 源码 + 内存对齐排序）
```
