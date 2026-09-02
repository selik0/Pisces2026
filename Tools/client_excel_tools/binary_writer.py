"""按 GameProto 固定小端序格式导出配置数据。"""
import struct
from pathlib import Path
from .schema_hash import schema_hash

LIMIT = 2_000_000
FORMATS = {"byte": "B", "sbyte": "b", "short": "h", "ushort": "H", "int": "i", "uint": "I", "long": "q", "ulong": "Q", "float": "f", "double": "d"}

def _value(text, type_name, location):
    t = type_name
    if t == "string":
        data = text.encode("utf-8")
        if len(data) > 0xFFFFFFFF or len(data) > LIMIT: raise ValueError(f"{location}，字符串 UTF-8 长度超限")
        return struct.pack("<I", len(data)) + data
    if t == "bytes":
        try:
            data = bytes.fromhex(text.replace(" ", "")) if text else b""
        except ValueError as exc:
            raise ValueError(f"{location}，bytes 必须是十六进制字符串") from exc
        if len(data) > LIMIT: raise ValueError(f"{location}，bytes 长度超限")
        return struct.pack("<I", len(data)) + data
    if t == "bool":
        value = text.upper()
        if value not in ("TRUE", "FALSE", "0", "1"): raise ValueError(f"{location}，非法 bool：{text}")
        return bytes([1 if value in ("TRUE", "1") else 0])
    if t in FORMATS:
        try: return struct.pack("<" + FORMATS[t], float(text) if t in ("float", "double") else int(text))
        except (TypeError, ValueError, struct.error) as exc: raise ValueError(f"{location}，{t} 数值非法或超范围：{text}") from exc
    if t.endswith("[]") or (t.startswith("List<") and t.endswith(">")):
        raise ValueError(f"{location}，数组/List 暂不支持隐式分隔，请使用标量字段")
    raise ValueError(f"{location}，不支持类型：{t}")

def export_config(config, output):
    hash_bytes, _ = schema_hash(config)
    chunks = [b"GCFG", struct.pack("<I", 1), hash_bytes]
    records = []
    field_indexes = {f.name: i for i, f in enumerate(config.fields)}
    keys = set()
    for row_number, values in config.rows:
        location = f"Sheet={config.name}，Excel行={row_number}"
        key = tuple(values[field_indexes[name]] for name in config.key_fields)
        if key in keys: raise ValueError(f"{location}，重复主键：{key}")
        keys.add(key)
        records.append(b"".join(_value(value, field.type_name, f"{location}，字段={field.name}") for field, value in zip(config.fields, values)))
    if len(records) > 0xFFFFFFFF: raise ValueError(f"{config.name}，记录数超出 uint")
    chunks.append(struct.pack("<I", len(records)))
    chunks.extend(records)
    data = b"".join(chunks)
    destination = Path(output)
    destination.parent.mkdir(parents=True, exist_ok=True)
    if not destination.exists() or destination.read_bytes() != data:
        destination.write_bytes(data)
    return data
