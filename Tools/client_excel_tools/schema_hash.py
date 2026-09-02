"""配置 Schema 的稳定 SHA-256 哈希。"""
import hashlib
import json

def schema_hash(config):
    model = {
        "class": config.class_name,
        "key": config.key_fields,
        "group": config.group_fields,
        "fields": [{"name": f.name, "type": f.type_name, "source": f.source, "export": f.export.upper()} for f in config.fields],
    }
    payload = json.dumps(model, ensure_ascii=False, separators=(",", ":"), sort_keys=True).encode("utf-8")
    digest = hashlib.sha256(payload).digest()
    return digest[:8], digest.hex()
