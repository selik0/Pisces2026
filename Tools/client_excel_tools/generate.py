#!/usr/bin/env python3
"""GameProto Excel 配置生成入口。"""
import argparse
import json
import sys
from pathlib import Path

if __package__ in (None, ""):
    sys.path.insert(0, str(Path(__file__).resolve().parent.parent.parent))
    from Tools.client_excel_tools.excel_reader import load_workbook
    from Tools.client_excel_tools.code_generator import generate, generate_table
    from Tools.client_excel_tools.binary_writer import export_config
else:
    from .excel_reader import load_workbook
    from .code_generator import generate, generate_table
    from .binary_writer import export_config


def read_settings(path):
    with Path(path).open("r", encoding="utf-8-sig") as stream:
        return json.load(stream)


def find_excels(root, folders, excluded):
    selected = []
    for folder in folders:
        directory = root / folder
        if not directory.is_dir():
            raise ValueError(f"Excel 配置文件夹不存在：{directory}")
        selected.extend(directory.rglob("*.xlsx"))
    excluded_paths = {root / folder for folder in excluded}
    return sorted(path for path in selected if not any(excluded_path in path.parents for excluded_path in excluded_paths))


def main(argv=None):
    parser = argparse.ArgumentParser(description="从 Excel 配置文件生成 C# 配置类、数据表类和 GCFG 数据")
    parser.add_argument("--config", default=str(Path(__file__).with_name("config.json")))
    parser.add_argument("--folder", action="append", dest="folders", help="只生成指定 Excel 子文件夹，可重复传入")
    args = parser.parse_args(argv)
    config_path = Path(args.config).resolve()
    settings = read_settings(config_path)
    project_root = config_path.parent.parent.parent
    root = Path(settings["excel_root"])
    if not root.is_absolute():
        root = project_root / root
    folders = args.folders or settings.get("common_folders", [])
    if not folders:
        raise ValueError("未配置 common_folders；请在 config.json 中配置通用文件夹，或使用 --folder")
    files = find_excels(root, folders, settings.get("excluded_folders", []))
    if not files:
        raise ValueError(f"配置文件夹中没有找到 xlsx：{folders}")
    for excel in files:
        configs = load_workbook(excel, None, settings.get("data_start_row", 7))
        relative = excel.relative_to(root).parent
        for config in configs:
            code_dir = Path(settings["code_dir"])
            table_dir = Path(settings["table_dir"])
            data_dir = Path(settings["data_dir"])
            if not code_dir.is_absolute():
                code_dir = project_root / code_dir
            if not table_dir.is_absolute():
                table_dir = project_root / table_dir
            if not data_dir.is_absolute():
                data_dir = project_root / data_dir
            generate(config, code_dir / relative / f"{config.class_name}.g.cs")
            generate_table(config, table_dir / relative / f"{config.class_name}Table.g.cs")
            export_config(config, data_dir / relative / f"{config.class_name}.bytes")
            print(f"Generated {excel}: {config.class_name}")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except Exception as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        sys.exit(1)
