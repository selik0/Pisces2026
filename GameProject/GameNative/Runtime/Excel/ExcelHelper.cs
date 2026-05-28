using System;
using System.Collections.Generic;
using System.IO;
using OfficeOpenXml;

namespace GameNative.Excel
{
    /// <summary>
    /// Excel 读写工具类（基于 EPPlus，支持 .xlsx 格式）
    /// </summary>
    public static class ExcelHelper
    {
        static ExcelHelper()
        {
            // EPPlus 5.x 需要设置 LicenseContext（非商业用途选 NonCommercial）
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        // ────────────────────────────────────────────────
        //  读取
        // ────────────────────────────────────────────────

        /// <summary>
        /// 读取指定 Sheet 的所有数据，返回二维字符串列表。
        /// 第一维是行，第二维是列，所有值均转为 string。
        /// </summary>
        /// <param name="filePath">Excel 文件路径（.xlsx）</param>
        /// <param name="sheetName">Sheet 名称，为 null 时取第一个 Sheet</param>
        /// <param name="skipRows">跳过开头的行数（例如跳过表头行）</param>
        public static List<List<string>> ReadSheet(string filePath, string sheetName = null, int skipRows = 0)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Excel 文件不存在：{filePath}");

            var result = new List<List<string>>();

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var sheet = sheetName != null
                    ? package.Workbook.Worksheets[sheetName]
                    : package.Workbook.Worksheets[0];

                if (sheet == null)
                    throw new ArgumentException($"找不到 Sheet：{sheetName ?? "(第一个)"}");

                int rowCount = sheet.Dimension?.Rows ?? 0;
                int colCount = sheet.Dimension?.Columns ?? 0;

                for (int row = 1 + skipRows; row <= rowCount; row++)
                {
                    var rowData = new List<string>();
                    for (int col = 1; col <= colCount; col++)
                    {
                        var cell = sheet.Cells[row, col];
                        rowData.Add(cell.Text ?? string.Empty);
                    }
                    result.Add(rowData);
                }
            }

            return result;
        }

        /// <summary>
        /// 读取指定 Sheet，将第一行作为列名，返回字典列表（每行为一个字典）。
        /// </summary>
        /// <param name="filePath">Excel 文件路径（.xlsx）</param>
        /// <param name="sheetName">Sheet 名称，为 null 时取第一个 Sheet</param>
        /// <param name="headerRow">表头所在行号（1 起始），默认第 1 行</param>
        public static List<Dictionary<string, string>> ReadSheetAsDictionary(
            string filePath,
            string sheetName = null,
            int headerRow = 1)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Excel 文件不存在：{filePath}");

            var result = new List<Dictionary<string, string>>();

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var sheet = sheetName != null
                    ? package.Workbook.Worksheets[sheetName]
                    : package.Workbook.Worksheets[0];

                if (sheet == null)
                    throw new ArgumentException($"找不到 Sheet：{sheetName ?? "(第一个)"}");

                int rowCount = sheet.Dimension?.Rows ?? 0;
                int colCount = sheet.Dimension?.Columns ?? 0;

                // 读取表头
                var headers = new List<string>();
                for (int col = 1; col <= colCount; col++)
                    headers.Add(sheet.Cells[headerRow, col].Text ?? $"Col{col}");

                // 读取数据行
                for (int row = headerRow + 1; row <= rowCount; row++)
                {
                    var dict = new Dictionary<string, string>();
                    for (int col = 1; col <= colCount; col++)
                    {
                        string key = headers[col - 1];
                        dict[key] = sheet.Cells[row, col].Text ?? string.Empty;
                    }
                    result.Add(dict);
                }
            }

            return result;
        }

        /// <summary>
        /// 获取 Excel 文件中所有 Sheet 的名称列表。
        /// </summary>
        public static List<string> GetSheetNames(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Excel 文件不存在：{filePath}");

            var names = new List<string>();
            using (var package = new ExcelPackage(new FileInfo(filePath)))
                foreach (var ws in package.Workbook.Worksheets)
                    names.Add(ws.Name);

            return names;
        }

        // ────────────────────────────────────────────────
        //  写入
        // ────────────────────────────────────────────────

        /// <summary>
        /// 将二维数据写入 Excel 文件。
        /// 若文件已存在且 <paramref name="overwrite"/> 为 false，则追加 Sheet；否则覆盖该 Sheet。
        /// </summary>
        /// <param name="filePath">目标 Excel 文件路径（.xlsx）</param>
        /// <param name="sheetName">Sheet 名称</param>
        /// <param name="data">二维数据（行×列）</param>
        /// <param name="overwrite">是否覆盖同名 Sheet，默认 true</param>
        public static void WriteSheet(
            string filePath,
            string sheetName,
            IList<IList<object>> data,
            bool overwrite = true)
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            ExcelPackage package;
            if (File.Exists(filePath))
                package = new ExcelPackage(new FileInfo(filePath));
            else
                package = new ExcelPackage();

            using (package)
            {
                ExcelWorksheet sheet;
                var existing = package.Workbook.Worksheets[sheetName];
                if (existing != null)
                {
                    if (overwrite)
                        package.Workbook.Worksheets.Delete(existing);
                    else
                        sheetName = GetUniqueSheetName(package, sheetName);
                }
                sheet = package.Workbook.Worksheets.Add(sheetName);

                for (int row = 0; row < data.Count; row++)
                {
                    var rowData = data[row];
                    for (int col = 0; col < rowData.Count; col++)
                        sheet.Cells[row + 1, col + 1].Value = rowData[col];
                }

                package.SaveAs(new FileInfo(filePath));
            }
        }

        /// <summary>
        /// 将字典列表写入 Excel 文件，自动用字典的 Key 作为表头。
        /// </summary>
        /// <param name="filePath">目标 Excel 文件路径（.xlsx）</param>
        /// <param name="sheetName">Sheet 名称</param>
        /// <param name="data">字典列表</param>
        /// <param name="overwrite">是否覆盖同名 Sheet，默认 true</param>
        public static void WriteSheetFromDictionary(
            string filePath,
            string sheetName,
            IList<Dictionary<string, object>> data,
            bool overwrite = true)
        {
            if (data == null || data.Count == 0)
                return;

            // 收集所有列名（保持首次出现顺序）
            var headers = new List<string>();
            var headerSet = new HashSet<string>();
            foreach (var row in data)
                foreach (var key in row.Keys)
                    if (headerSet.Add(key))
                        headers.Add(key);

            // 构建二维数组
            var rows = new List<IList<object>>();
            var headerRow = new List<object>(headers.Count);
            foreach (var h in headers) headerRow.Add(h);
            rows.Add(headerRow);

            foreach (var row in data)
            {
                var rowData = new List<object>(headers.Count);
                foreach (var h in headers)
                    rowData.Add(row.TryGetValue(h, out var v) ? v : null);
                rows.Add(rowData);
            }

            WriteSheet(filePath, sheetName, rows, overwrite);
        }

        // ────────────────────────────────────────────────
        //  辅助
        // ────────────────────────────────────────────────

        private static string GetUniqueSheetName(ExcelPackage package, string baseName)
        {
            string name = baseName;
            int idx = 1;
            while (package.Workbook.Worksheets[name] != null)
                name = $"{baseName}_{idx++}";
            return name;
        }
    }
}
