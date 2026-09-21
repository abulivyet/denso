
using iTextSharp.text;
using OfficeOpenXml;
using Spire.Xls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DinkToPdf;

using System.Diagnostics;
using NPOI.HSSF.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;

namespace DENSOKB.Helper
{

    public class FileHelper
    {


        public  List<string> ConvertExcelToPdfs(string excelPath, string outputDir)
        {
            var pdfFiles = new List<string>();            
            List<string> ordercode = new List<string>();//订单号
            string pathEnd=Path.GetExtension(excelPath).ToLower();
            string filename = Path.GetFileName(excelPath);
            // 考虑点：所有文件解压后顺序最好按名称排好，下载时直接按照名称排好
            // 货剁标签根据sheet拆分成多个PDF
            if (excelPath.IndexOf("货垛标签")>=0)
            {
                SpireHelper.ActivateMemoryPatching();
                Workbook workbook = new Workbook();
                workbook.LoadFromFile(excelPath);
                // 遍历每个Sheet
                foreach (Worksheet sheet in workbook.Worksheets)
                {
                  // 测试 没有右边框和下边框，需优化往左上移动一点
                    string pdfPath = Path.Combine(outputDir, $"货垛标签{sheet.Name}.pdf");// 增加sheet标签
                    ordercode.Add(sheet.Name);
                    sheet.SaveToPdf(pdfPath);
                    pdfFiles.Add(pdfPath);
                }
            }
            else if(excelPath.IndexOf("看板") >= 0) 
            {
                /* 思路1：小看板Excel看板拆分 每16行成为一个卡片拆分成一个PDF ;A4拆分 每三个小看板生成一个A4PDF；将数据复制到PDF中
                 * 问1题：测试需要考虑文本大小，字体合并单元格极其复杂麻烦
                 * 问题2：每16行的卡片生一个PDF最总结果会有很多PDF，
                 * 延伸思路2：经过"货垛标签"测试，excel转PDF代码简单，不要考虑格式样式情况；
                 * 所以将每16行拆分成一个Excel，Excel再转PDF不用考虑Excel样式情况
                 */
                //  直接生成文件有问题，尝试转为图片再存入到PDF 或者截取
                // 打开文件流 思路错误 
                //using (var fileStream = new FileStream(excelPath, FileMode.Open, FileAccess.Read))
                //{
                //    // 根据文件扩展名选择工作簿类型
                //    IWorkbook workbook;
                //    if (excelPath.EndsWith(".xls"))
                //    {
                //        workbook = new HSSFWorkbook(fileStream);
                //    }
                //    else if (excelPath.EndsWith(".xlsx"))
                //    {
                //        workbook = new XSSFWorkbook(fileStream);
                //    }
                //    else
                //    {
                //        throw new Exception("不支持的文件格式！");
                //    }

                //    // 获取第一个工作表
                //    ISheet sheet = workbook.GetSheetAt(0); // 或使用 GetSheet("SheetName") 指定名称

                //    //根据思路 重写ProcessSheet1 存成多个Excel直接转PDF代码更简洁



                //    ProcessSheet1(workbook, outputDir);
                //}
                Workbook sourceWorkbook = new Workbook();
                sourceWorkbook.LoadFromFile(excelPath);
                ProcessSheet1(sourceWorkbook, outputDir);
               
            }

            return pdfFiles;
        }
        private static readonly Rectangle ThirdA4 = new Rectangle(210, 99); 
        private static readonly float MmToPoint = 72 / 25.4f; // 毫米转PDF点单位 deepseek查询三分之一A4大小及写法
        // 网上查的方法，参考使用
        private void ProcessSheet1(Workbook sourceWorkbook, string outputDir)
        {

            Worksheet sourceSheet = sourceWorkbook.Worksheets[0]; // 关
            // 循环行数
            int row = sourceSheet.LastRow;
            int targetRow = 1; // 新Excel行 
            int quotient;      // 商
            int remainder;     // 余数
            remainder=Math.DivRem(row, 16, out quotient);
            row = remainder == 0 ? quotient : quotient + 1;
            //  0*16+1 ~ 16*i
            for (int i=0;i< row;i++)// 创建Excel
            {
                string filename = sourceSheet.Range[(i - 1) * 10, 2].Text;
                if (string.IsNullOrEmpty(filename)) continue;
                Workbook targetWorkbook = new Workbook();
                targetWorkbook.Worksheets.Clear();
                // 添加一个工作表
                Worksheet targetSheet = targetWorkbook.Worksheets.Add("Sheet1");
                
                
                // 复制样式
                foreach (CellStyle style in sourceWorkbook.Styles)
                {
                    targetWorkbook.Styles.Add(style);
                }
                for (int col = 1; col <= sourceSheet.Columns.Length; col++)
                {
                    targetSheet.Columns[col - 1].ColumnWidth = sourceSheet.Columns[col - 1].ColumnWidth;
                }
                
                for (int j= i*16 + 1; j< 16 * (i+1); j++) // 旧Excel数据
                {

                    // 复制行内容及样式
                    CellRange sourceRange = sourceSheet.Range[j, 1, j, sourceSheet.Columns.Length];
                    CellRange targetRange = targetSheet.Range[j, 1, j, sourceSheet.Columns.Length];
                    // 深拷贝内容
                    sourceRange.Copy(targetRange);
                    // 单独复制行高
                    targetSheet.Rows[targetRow - 1].RowHeight = sourceSheet.Rows[row - 1].RowHeight;

                    // 处理合并单元格
                    foreach (CellRange mergedRange in sourceSheet.MergedCells)
                    {
                        if (mergedRange.Row == row - 1) // Spire的行索引从0开始
                        {
                            targetSheet.Range[targetRow - 1, mergedRange.Column,
                                            targetRow - 1, mergedRange.LastColumn].Merge();
                        }
                    }
                }
                string pdfPath = Path.Combine(outputDir, $"看板{filename}.pdf");// 增加sheet标签
                targetSheet.SaveToPdf(pdfPath);
                //targetWorkbook.SaveToFile(targetPath, FileFormat.Version2016);
            }
            //while (rowIndex <= sheet.LastRowNum)
            //{
            //    // 收集当前卡片的10行数据
            //    var cardRows = new List<IRow>();
            //    for (int i = 0; i < 16 && rowIndex <= sheet.LastRowNum; i++)
            //    {
            //        var row = sheet.GetRow(rowIndex++);
            //        if (row != null) cardRows.Add(row);
            //    }
            //    if (cardRows.Count == 0) continue;
            //    // 获取关键数据
            //    string keyValue = GetKeyValue(cardRows); // 默认值
            //    if (cardRows.Count >= 11)
            //    {
            //        try
            //        {
            //            IRow ninthRow = cardRows[10]; // 第9行（索引8）
            //            ICell keyCell = ninthRow.GetCell(2); // C列（索引2）
            //            keyValue = keyCell?.ToString()?.Trim() ?? "N/A";// 获取到单号
            //        }
            //        catch (Exception ex)
            //        {
            //            Console.WriteLine($"获取关键数据失败: {ex.Message}");
            //        }
            //    }
            //    if (string.IsNullOrEmpty(keyValue)) continue; // 单号为空
            //    // 生成PDF文件名（过滤非法字符）
            //    string safeSheetName = Regex.Replace("看板3-1", @"[\\/:*?""<>|]", "");
            //    string safeKeyValue = Regex.Replace(keyValue, @"[\\/:*?""<>|]", "");
            //    string pdfName = $"{safeSheetName}_{safeKeyValue}_{++cardCount}.pdf";
            //    string pdfPath = Path.Combine(outputDir, pdfName);


            // 生成PDF卡片
            // CreatePdfCard(cardRows, pdfPath);
            // pdfFiles.Add(pdfPath);
            //CreatePdfCard(cardRows, pdfPath);
        }

        //private void ProcessSheet(ISheet sheet, string outputDir, List<string> pdfFiles)
        //{

        //    var mergedRegions = new List<CellRangeAddress>();
        //    for (int i = 0; i < sheet.NumMergedRegions; i++)
        //    {
        //        mergedRegions.Add(sheet.GetMergedRegion(i));
        //    }

        //    int rowIndex = 0;
        //    int cardCount = 0;

        //    while (rowIndex <= sheet.LastRowNum)
        //    {
        //        var cardRows = new List<IRow>();
        //        for (int i = 0; i < 16 && rowIndex <= sheet.LastRowNum; rowIndex++)
        //        {
        //            var row = sheet.GetRow(rowIndex);
        //            if (row != null) cardRows.Add(row);
        //        }

        //        if (cardRows.Count == 0) continue;

        //        var pdfPath = GeneratePdfPath(outputDir, cardRows, ref cardCount);
        //        CreatePdfCard(sheet, cardRows, mergedRegions, pdfPath);
        //        pdfFiles.Add(pdfPath);
        //    }
        //}

        //private void CreatePdfCard(ISheet sheet, List<IRow> rows, List<CellRangeAddress> mergedRegions, string pdfPath)
        //{
        //    IWorkbook workbook = sheet.Workbook;
        //    var pageSize = new Rectangle(210 * MmToPoint, 99 * MmToPoint);
        //    using (var fs = new FileStream(pdfPath, FileMode.Create))
        //    using (var doc = new Document(pageSize))
        //    {
        //        var writer = PdfWriter.GetInstance(doc, fs);
        //        doc.Open();

        //        var maxCols = rows.Max(r => r.LastCellNum);
        //        var table = new PdfPTable(maxCols) { WidthPercentage = 100 };

        //        var styleCache = new StyleCache();

        //        foreach (var row in rows)
        //        {
        //            for (int col = 0; col < maxCols; col++)
        //            {
        //                if (IsMergedCell(row.RowNum, col, mergedRegions)) continue;

        //                var cell = row.GetCell(col);
        //                var cellPhrase = CreatePhrase(cell, styleCache, workbook);
        //                var pdfCell = new PdfPCell(cellPhrase);

        //                ApplyCellStyle(cell, pdfCell, workbook,styleCache );
        //                table.AddCell(pdfCell);
        //            }
        //        }

        //        doc.Add(table);
        //    }
        //}



        //#region 样式处理 deepseek优化后也是复杂 应该是思路错误...需要跟换思路
        //private class StyleCache
        //{
        //    public Dictionary<string, Font> Fonts = new Dictionary<string, Font>();
        //    public Dictionary<string, BaseColor> Colors = new Dictionary<string, BaseColor>();
        //}

        //private Phrase CreatePhrase(ICell cell, StyleCache cache, IWorkbook workbook)
        //{
        //    if (cell == null) return new Phrase();

        //    var font = GetCellFont(cell.CellStyle, cache, workbook);
        //    return new Phrase(cell.ToString(), font);
        //}

        //private Font GetCellFont(ICellStyle style, StyleCache cache, IWorkbook workbook)
        //{
        //    // 新版NPOI获取字体方式
        //    IFont workbookFont = workbook.GetFontAt(style.FontIndex);

        //    // 生成唯一字体标识
        //    var fontKey = $"{workbookFont.FontName}-{workbookFont.FontHeight}-{workbookFont.IsBold}-{workbookFont.Color}";

        //    if (!cache.Fonts.TryGetValue(fontKey, out var pdfFont))
        //    {
        //        var baseFont = BaseFont.CreateFont("STSong-Light", "UniGB-UCS2-H", false);
        //        int styleFlags = Font.NORMAL;
        //        styleFlags |= workbookFont.IsBold ? Font.BOLD : 0;
        //        styleFlags |= workbookFont.IsItalic ? Font.ITALIC : 0;

        //        pdfFont = new Font(
        //            baseFont,
        //            workbookFont.FontHeightInPoints,
        //            styleFlags,
        //            GetFontColor(workbookFont, workbook, cache.Colors)
        //        );
        //        cache.Fonts[fontKey] = pdfFont;
        //    }
        //    return pdfFont;
        //}
        //private BaseColor GetFontColor(IFont font, IWorkbook workbook, Dictionary<string, BaseColor> cache)
        //{
        //    // HSSF和XSSF颜色处理差异
        //    if (workbook is HSSFWorkbook hssfWorkbook)
        //    {
        //        HSSFColor hssfColor = hssfWorkbook.GetCustomPalette().GetColor(font.Color);
        //        return GetHssfColor(hssfColor, cache);
        //    }
        //    else if (workbook is XSSFWorkbook)
        //    {
        //        XSSFColor xssfColor = (font as XSSFFont)?.GetXSSFColor();
        //        return GetXssfColor(xssfColor, cache);
        //    }
        //    return BaseColor.BLACK;
        //}
        //private BaseColor GetFillColor(ICellStyle style, IWorkbook workbook, Dictionary<string, BaseColor> cache)
        //{
        //    short colorIndex = style.FillForegroundColor;
        //    if (colorIndex == 0) colorIndex = style.FillBackgroundColor;

        //    if (workbook is HSSFWorkbook hssfWorkbook)
        //    {
        //        HSSFColor hssfColor = hssfWorkbook.GetCustomPalette().GetColor(colorIndex);
        //        return GetHssfColor(hssfColor, cache);
        //    }
        //    else if (workbook is XSSFWorkbook)
        //    {
        //        XSSFColor xssfColor = (XSSFColor)style.FillForegroundColorColor;
        //        return GetXssfColor(xssfColor, cache);
        //    }
        //    return BaseColor.WHITE;
        //}
        //private BaseColor GetHssfColor(HSSFColor hssfColor, Dictionary<string, BaseColor> cache)
        //{

        //    if (hssfColor == null) return BaseColor.BLACK;

        //    var triplet = hssfColor.GetTriplet();
        //    var colorKey = $"{triplet}-{triplet}-{triplet}";
        //    if (!cache.TryGetValue(colorKey, out var baseColor))
        //    {
        //        baseColor = new BaseColor(Convert.ToInt32(triplet), Convert.ToInt32(triplet), Convert.ToInt32(triplet));
        //        cache[colorKey] = baseColor;
        //    }
        //    return baseColor;
        //}
        //private BaseColor GetXssfColor(XSSFColor xssfColor, Dictionary<string, BaseColor> cache)
        //{
        //    if (xssfColor == null)
        //        return BaseColor.BLACK;

        //    byte[] rgb = xssfColor.GetRgb();//.GetRGB();
        //    int r, g, b;
        //    var colorKey = $"{rgb}-{rgb}-{rgb}";
        //    if (rgb.Length == 4)
        //    {
        //        r = Convert.ToInt32(rgb); // 使用 & 0xFF 确保无符号转换
        //        g = Convert.ToInt32(rgb);
        //        b = Convert.ToInt32(rgb);
        //    }
        //    else // 直接 RGB 格式（3字节）
        //    {
        //        r = Convert.ToInt32(rgb);
        //        g = Convert.ToInt32(rgb);
        //        b = Convert.ToInt32(rgb);
        //    }
        //    if (!cache.TryGetValue(colorKey, out var baseColor))
        //    {
        //        baseColor = new BaseColor(r, g, b);
        //        cache[colorKey] = baseColor;
        //    }
        //    return baseColor;
        //}
        ////private BaseColor GetColor(short colorIndex, Dictionary<string, BaseColor> cache)
        ////{
        ////    var color = workbookFont.Workbook.GetPalette().GetColor(colorIndex);
        ////    var colorKey = $"{color.R}-{color.G}-{color.B}";

        ////    if (!cache.TryGetValue(colorKey, out var baseColor))
        ////    {
        ////        baseColor = new BaseColor(color.R, color.G, color.B);
        ////        cache[colorKey] = baseColor;
        ////    }
        ////    return baseColor;
        ////}

        //private void ApplyCellStyle(ICell cell, PdfPCell pdfCell, IWorkbook workbook, StyleCache cache)
        //{
        //    var style = cell.CellStyle;

        //    // 对齐方式转换
        //    pdfCell.HorizontalAlignment = ConvertAlignment(style.Alignment);
        //    pdfCell.VerticalAlignment = ConvertVerticalAlignment(style.VerticalAlignment);

        //    // 新版背景色处理
        //    pdfCell.BackgroundColor = GetFillColor(style, workbook, cache.Colors);

        //    // 边框处理
        //    ApplyBorders(pdfCell, style);
        //}


        //private int ConvertAlignment(HorizontalAlignment alignment)
        //{
        //    switch (alignment)
        //    {
        //        case HorizontalAlignment.Center: return Element.ALIGN_CENTER;
        //        case HorizontalAlignment.Right: return Element.ALIGN_RIGHT;
        //        default: return Element.ALIGN_LEFT;
        //    }
        //}

        //private int ConvertVerticalAlignment(VerticalAlignment alignment)
        //{
        //    switch (alignment)
        //    {
        //        case VerticalAlignment.Top: return Element.ALIGN_TOP;
        //        case VerticalAlignment.Bottom: return Element.ALIGN_BOTTOM;
        //        default: return Element.ALIGN_MIDDLE;
        //    }
        //}

        ////private BaseColor GetFillColor(ICellStyle style, Dictionary<string, BaseColor> cache)
        ////{
        ////    var color = style.FillForegroundColorColor ?? style.FillBackgroundColorColor;
        ////    if (color == null) return BaseColor.WHITE;

        ////    var key = $"{color.R}-{color.G}-{color.B}";
        ////    if (!cache.TryGetValue(key, out var baseColor))
        ////    {
        ////        baseColor = new BaseColor(color.R, color.G, color.B);
        ////        cache[key] = baseColor;
        ////    }
        ////    return baseColor;
        ////}

        //private void ApplyBorders(PdfPCell pdfCell, ICellStyle style)
        //{
        //    pdfCell.BorderWidth = 0.5f;
        //    pdfCell.BorderColor = BaseColor.BLACK;

        //    // 详细边框设置
        //    var borderWidth = style.BorderLeft == BorderStyle.None ? 0f : 0.5f;
        //    pdfCell.BorderWidthLeft = borderWidth;
        //    pdfCell.BorderWidthRight = borderWidth;
        //    pdfCell.BorderWidthTop = borderWidth;
        //    pdfCell.BorderWidthBottom = borderWidth;
        //}
        //#endregion

        //#region 辅助方法
        //private bool IsMergedCell(int row, int col, List<CellRangeAddress> regions)
        //{
        //    return regions.Any(r =>
        //        r.FirstRow <= row && row <= r.LastRow &&
        //        r.FirstColumn <= col && col <= r.LastColumn &&
        //        !(r.FirstRow == row && r.FirstColumn == col));
        //}

        //private string GeneratePdfPath(string outputDir, List<IRow> rows, ref int counter)
        //{
        //    var keyValue = GetKeyValue(rows);
        //    var safeKey = Regex.Replace(keyValue, @"[^\w]", "_");
        //    return Path.Combine(outputDir, $"看板_{safeKey}_{++counter}.pdf");
        //}

        //private string GetKeyValue(List<IRow> rows)
        //{
        //    try
        //    {
        //        return rows.ElementAtOrDefault(10)?.GetCell(2)?.ToString()?.Trim() ?? "N/A";
        //    }
        //    catch
        //    {
        //        return "N/A";
        //    }
        //}

        //private string CleanFileName(string fileName)
        //{
        //    return Regex.Replace(fileName, @"[\\/:*?""<>|]", "_");
        //}
        //#endregion

        public List<string> ExcelLabelSheetSplitter(string excelPath, List<string> bundleFolders,string basePath)
        {
            var    pdfFiles = new List<string>();
            //string pathEnd  = Path.GetExtension(excelPath).ToLower();
            //string filename = Path.GetFileName(excelPath);
            // 检查是否是“货垛标签”文件
            string fileName = Path.GetFileNameWithoutExtension(excelPath);
            if (!fileName.EndsWith("货垛标签"))
            {
                return pdfFiles; // 如果不是“货垛标签”文件，直接返回空列表
            }


            SpireHelper.ActivateMemoryPatching();
            Workbook workbook = new Workbook();
            workbook.LoadFromFile(excelPath);


            foreach (Worksheet sheet in workbook.Worksheets)
            {
                // 查找与Sheet名称匹配的捆包文件夹
                string sheetName      = sheet.Name;
                var    matchingFolderName = bundleFolders.FirstOrDefault(folder => Path.GetFileName(folder) == sheetName);

                if (matchingFolderName == null)
                {
                    continue; // 如果没有匹配的文件夹，跳过
                }
                var matchingFolder = Path.Combine(basePath, matchingFolderName);
                // 将 Sheet转换为PDF并保存到对应的捆包文件夹
                string pdfPath = Path.Combine(matchingFolder, $"货垛标签_{sheetName}.pdf");
                sheet.SaveToPdf(pdfPath);  // ???为啥不是单个sheet页？？

            }

            workbook.Dispose();
            return pdfFiles;
        }


        public  List<string> ExcelSheetSplitter(string excelPath, List<string> bundleFolders, string basePath)
        {
            var        pdfFiles = new List<string>();


            // 获取文件名并验证是否符合"货垛标签"命名规则
            string fileName = Path.GetFileNameWithoutExtension(excelPath);
            if (!fileName.EndsWith("货垛标签"))
            {
                return pdfFiles; // 如果不是目标文件，直接返回空列表
            }
            if (Path.GetExtension(excelPath).ToLower() == ".xls")
            {
                excelPath = ConvertXlsToXlsx(excelPath);
            }


            // 打开 Excel 文件并处理每个工作表
            using (var package = new ExcelPackage(new FileInfo(excelPath)))
            {
                foreach (var sheet in package.Workbook.Worksheets)
                {
                    string sheetName = sheet.Name;
                    var matchingFolderName = bundleFolders.FirstOrDefault(folder => Path.GetFileName(folder) == sheetName);

                    // 如果没有找到匹配的文件夹，跳过该工作表
                    if (matchingFolderName == null)
                    {
                        continue;
                    }

                    var matchingFolder = Path.Combine(basePath, matchingFolderName);
                    string pdfPath = Path.Combine(matchingFolder, $"货垛标签_{sheetName}.pdf");

                    // **1. 将 Excel 工作表转换为 HTML 内容**
                    UtilHelper utilHelp    = new UtilHelper();
                    string     htmlContent = utilHelp.ConvertSheetToHtml(sheet);

                    // **2. 使用 wkhtmltopdf 将 HTML 转换为 PDF**
                    string wkHtmlToPdfPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wkhtmltopdf.exe");

                    if (!File.Exists(wkHtmlToPdfPath))
                    {
                        throw new FileNotFoundException("wkhtmltopdf.exe not found in the application directory.");
                    }

                    // 临时保存 HTML 文件
                    string tempHtmlFile = Path.Combine(matchingFolder, $"货垛标签_{sheetName}.html");
                    File.WriteAllText(tempHtmlFile, htmlContent);

                    // 设置命令行参数来调用 wkhtmltopdf 执行转换
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = wkHtmlToPdfPath,
                        Arguments = $"\"{tempHtmlFile}\" \"{pdfPath}\"",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    // 执行命令行进程
                    using (var process = Process.Start(startInfo))
                    {
                        process.WaitForExit(); // 等待进程完成
                    }

                    // 删除临时 HTML 文件
                    File.Delete(tempHtmlFile);

                    // 将生成的 PDF 文件路径添加到返回列表
                    pdfFiles.Add(pdfPath);
                }
            }

            return pdfFiles;
        }



        //public static string ConvertXlsToXlsx(string xlsPath)
        //{
        //    string   xlsxPath = Path.ChangeExtension(xlsPath, ".xlsx");
        //    Workbook workbook = new Workbook();
        //    workbook.LoadFromFile(xlsPath);
        //    workbook.SaveToFile(xlsxPath, ExcelVersion.Version2007);
        //    return xlsxPath;
        //}


        public static string ConvertXlsToXlsx(string xlsPath)
        {
            // 获取新的 .xlsx 文件路径
            string xlsxPath = Path.ChangeExtension(xlsPath, ".xlsx");

            // 读取 .xls 文件
            using (FileStream fs = new FileStream(xlsPath, FileMode.Open, FileAccess.Read))
            {
                // 读取 .xls 文件
                HSSFWorkbook hssfWorkbook = new HSSFWorkbook(fs);

                // 创建一个新的 .xlsx 工作簿
                XSSFWorkbook xssfWorkbook = new XSSFWorkbook();

                // 将 .xls 工作簿中的每个 Sheet 复制到 .xlsx 工作簿中
                for (int i = 0; i < hssfWorkbook.NumberOfSheets; i++)
                {
                    var sheet    = hssfWorkbook.GetSheetAt(i);
                    var newSheet = xssfWorkbook.CreateSheet(sheet.SheetName);

                    // 复制每一行数据
                    for (int j = 0; j <= sheet.LastRowNum; j++)
                    {
                        var row = sheet.GetRow(j);
                        if (row != null)
                        {
                            var newRow = newSheet.CreateRow(j);

                            for (int k = 0; k < row.Cells.Count; k++)
                            {
                                var cell    = row.GetCell(k);
                                var newCell = newRow.CreateCell(k);

                                // 复制单元格的值
                                if (cell != null)
                                {
                                    newCell.SetCellValue(cell.ToString());
                                }
                            }
                        }
                    }
                }

                // 保存为 .xlsx 文件
                using (FileStream outFile = new FileStream(xlsxPath, FileMode.Create, FileAccess.Write))
                {
                    xssfWorkbook.Write(outFile);
                }
            }

            return xlsxPath;
        }


        public static string ConvertXlsToXlsxPro(string xlsPath)
        {
            // 获取新的 .xlsx 文件路径
            string xlsxPath = Path.ChangeExtension(xlsPath, ".xlsx");

            // 读取 .xls 文件
            using (FileStream fs = new FileStream(xlsPath, FileMode.Open, FileAccess.Read))
            {
                HSSFWorkbook hssfWorkbook = new HSSFWorkbook(fs);
                XSSFWorkbook xssfWorkbook = new XSSFWorkbook();

                for (int i = 0; i < hssfWorkbook.NumberOfSheets; i++)
                {
                    var oldSheet = hssfWorkbook.GetSheetAt(i);
                    var newSheet = xssfWorkbook.CreateSheet(oldSheet.SheetName);

                    // 复制合并单元格
                    for (int j = 0; j < oldSheet.NumMergedRegions; j++)
                    {
                        var mergedRegion = oldSheet.GetMergedRegion(j);
                        newSheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(
                            mergedRegion.FirstRow, mergedRegion.LastRow, mergedRegion.FirstColumn, mergedRegion.LastColumn));
                    }

                    // 复制行
                    for (int j = 0; j <= oldSheet.LastRowNum; j++)
                    {
                        var oldRow = oldSheet.GetRow(j);
                        if (oldRow == null) continue;

                        var newRow = newSheet.CreateRow(j);
                        newRow.Height = oldRow.Height; // 复制行高

                        // 复制单元格
                        for (int k = 0; k < oldRow.LastCellNum; k++)
                        {
                            var oldCell = oldRow.GetCell(k);
                            if (oldCell == null) continue;

                            var newCell = newRow.CreateCell(k);

                            // 复制单元格值
                            switch (oldCell.CellType)
                            {
                                case CellType.String:
                                    newCell.SetCellValue(oldCell.StringCellValue);
                                    break;
                                case CellType.Numeric:
                                    newCell.SetCellValue(oldCell.NumericCellValue);
                                    break;
                                case CellType.Boolean:
                                    newCell.SetCellValue(oldCell.BooleanCellValue);
                                    break;
                                case CellType.Formula:
                                    newCell.SetCellFormula(oldCell.CellFormula);
                                    break;
                                case CellType.Blank:
                                    newCell.SetCellValue("");
                                    break;
                                default:
                                    newCell.SetCellValue(oldCell.ToString());
                                    break;
                            }

                            // 复制单元格样式
                            var newCellStyle = xssfWorkbook.CreateCellStyle();
                            newCellStyle.CloneStyleFrom(oldCell.CellStyle);
                            newCell.CellStyle = newCellStyle;
                        }
                    }
                }

                // 保存为 .xlsx 文件
                using (FileStream outFile = new FileStream(xlsxPath, FileMode.Create, FileAccess.Write))
                {
                    xssfWorkbook.Write(outFile);
                }
            }

            return xlsxPath;
        }



    }

}