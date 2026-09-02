# Excel 配置生成工具

本目录提供基于 Python 的 Excel 配置代码和二进制生成工具。

运行环境：Python 3.9+ 和 `openpyxl`（`python -m pip install openpyxl`）。Excel 文件只放在 `Excel/` 及其分类子文件夹中，工具位于 `Tools/client_excel_tools/`。Windows 用户可以直接双击 `Tools/client_excel_tools/generate_excel.bat`，或从命令行执行：

```powershell
py -3 Tools/client_excel_tools/generate.py --config Tools/client_excel_tools/config.json
```

每个大区使用独立的 `config.json`，在其中配置该大区的 `excel_root`、`common_folders`、排除目录和三个输出目录。复制一份配置文件即可创建新的大区。批处理脚本只需要修改顶部的 `CONFIG` 路径，不需要命令行传入配置。配置表记录类生成到 `GameProto`，配置数据表容器类生成到 `GameLogic`，二进制数据按 Excel 相对目录生成到 Unity 客户端资源目录。

```powershell
Tools/client_excel_tools/generate_excel.bat
Tools/client_excel_tools/generate_folder.bat
```

通用配置位于各大区自己的 `config.json`：`common_folders` 配置该大区批量转换的 Excel 子文件夹，`excluded_folders` 配置排除目录，`code_dir`、`table_dir`、`data_dir` 配置三类输出目录。批量转换和单独转换均通过 BAT 顶部的 `CONFIG` 选择大区配置文件。工具严格读取 Sheet2 的第 1 行表级元数据，以及第 2~6 行字段元数据；空行忽略，错误包含文件、Sheet、Excel 行/列和字段信息。生成文件内容未变化时不会覆盖已有文件。

当前第一版支持固定宽度标量、string、bytes；数组/List 会被识别并在数据导出时明确拒绝，避免猜测单元格分隔规则。

输入模板：

```text
Excel/模板.xlsx
```

工具必须读取模板中的表级元数据和字段元数据：

- 第一行描述配置类、数据表索引、分类规则、语言或字段用途等信息；
- 第一列从第二行开始描述字段名、类型、父类型/来源和导出范围；
- Sheet2 第1行描述配置类名、类名作用域、索引字段和分类字段；
- Sheet2 第2行开始描述各个字段列。

所有 string、bytes、数组和 List 的可变长度/数量前缀统一使用小端序 uint，占4字节。生成器需要生成 C# class 代码和二进制配置文件，不应只生成原始 Schema 或 CSV 文件。
