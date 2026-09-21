using DENSOKB.Libs;
using DENSOKB.Models;
using HelpLog.CommomMethod;
using iText.Forms.Form.Element;
using iText.Kernel.Pdf.Canvas;
using iText.Layout.Element;
using iText.Layout.Element;
using iTextSharp.text;
using iTextSharp.text.pdf;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using QRCoder;
using Spire.Pdf;
using Spire.Pdf.General;
using Spire.Pdf.Graphics;
using System;
using System;
using System.Collections;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using unoidl.com.sun.star.beans;
using unoidl.com.sun.star.container;
using unoidl.com.sun.star.frame;
using unoidl.com.sun.star.lang;
using unoidl.com.sun.star.script.provider;
using unoidl.com.sun.star.sheet;
using unoidl.com.sun.star.style;
using unoidl.com.sun.star.uno;
using unoidl.com.sun.star.util;
using unoidl.com.sun.star.view;
using DateTime = System.DateTime;
using Exception = System.Exception;
using List = iText.Layout.Element.List;
using Path = System.IO.Path;
using PdfDocument = Spire.Pdf.PdfDocument;
using PdfTemplate = Spire.Pdf.Graphics.PdfTemplate;
using Rectangle = System.Drawing.Rectangle;

namespace DENSOKB.Helper
{
    public class LiberOfficeHelper
    {
        orderimportService ordService = new orderimportService();

        public List<string> ExcelLabelSheetSplitter2(string zipPath, List<string> bundleFolders, string basePath)
        {
            var    pdfFiles = new List<string>();
            string fileName = System.IO.Path.GetFileNameWithoutExtension(zipPath);

            // 只处理“货垛标签”文件
            if (!fileName.EndsWith("货垛标签"))
            {
                return pdfFiles;
            }

            // LibreOffice 路径
            string libreOfficePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libreoffice",
                "program",
                "soffice.exe");

            // 遍历每个捆包文件夹对应的 Sheet
            foreach (string sheetName in bundleFolders)
            {
                string matchingFolder = System.IO.Path.Combine(basePath, sheetName);

                if (!Directory.Exists(matchingFolder))
                    continue;

                // PDF 输出路径
                string pdfPath = System.IO.Path.Combine(matchingFolder, $"货垛标签_{sheetName}.pdf");

                // 生成导出单个 Sheet 的命令
                string arguments =
                    $"--headless --convert-to pdf:\"calc_pdf_Export:FilterOptions=2,2,0,1,1\" \"{zipPath}\" --outdir \"{matchingFolder}\"";

                // 设置 ProcessStartInfo 来启动 LibreOffice
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName               = libreOfficePath,
                    Arguments              = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true
                };

                // 启动进程并等待完成
                using (Process process = new Process { StartInfo = psi })
                {
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error  = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    // 检查输出路径的 PDF 文件是否生成
                    if (File.Exists(pdfPath))
                    {
                        pdfFiles.Add(pdfPath);
                    }
                    else
                    {
                        Console.WriteLine($"Sheet {sheetName} 转换失败: {error}");
                    }
                }
            }


            return pdfFiles;
        }

        /// <summary>
        /// 此方法的作用是将货垛标签excel先转为pdf，然后再根据sheet分割excel到对应的文件夹
        /// </summary>
        /// <param name="zipPath"></param>
        /// <param name="bundleFolders"></param>
        /// <param name="basePath"></param>
        /// <returns></returns>
        public List<string> ExcelLabelSheetSplitter(string zipPath, List<string> bundleFolders, string basePath)
        {
            var    pdfFiles = new List<string>();
            string fileName = System.IO.Path.GetFileNameWithoutExtension(zipPath);

            // 只处理“货垛标签”文件
            if (!fileName.EndsWith("货垛标签"))
            {
                return pdfFiles;
            }

            // LibreOffice 路径
            string libreOfficePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libreoffice",
                "program",
                "soffice.exe");
            XErrorLogToolMDM.WriteLog($"LibreOffice 路径: {libreOfficePath}");

            // 临时文件夹用于存放整个 Excel 转换后的 PDF
            string tempFolder = System.IO.Path.Combine(basePath, "TempExcelSheets");
            Directory.CreateDirectory(tempFolder);

            // 将整个 Excel 转换为 PDF
            string tempPdfPath = System.IO.Path.Combine(tempFolder, $"{fileName}.pdf");

            LiberOfficeHelper.ExcelOutOfPdf(zipPath, tempPdfPath);
            XErrorLogToolMDM.WriteLog("转换完成");
            if (!File.Exists(tempPdfPath))
            {
                XErrorLogToolMDM.WriteLog($"Excel 转换为 PDF 失败: ");
                return pdfFiles;
            }

            string labelPath = Path.Combine(basePath, "托标签");
            Directory.CreateDirectory (labelPath);
            XErrorLogToolMDM.WriteLog("创建托标签文件夹");
            SpireHelper.PdfSplit(tempPdfPath, labelPath, 0,1,1,SpireSplit.EnumNameRule.Customize,new Rectangle(150,440,406,75),new Rectangle());
            XErrorLogToolMDM.WriteLog("分割托标签");
            string[] files = Directory.GetFiles(labelPath, "*.pdf", SearchOption.AllDirectories);
            pdfFiles.AddRange(files);

            // 清理临时文件夹
            //if (Directory.Exists(tempFolder))
            //{
            //    Directory.Delete(tempFolder, true);
            //}

            return pdfFiles;
        }

        /// <summary>
        /// 转化成文件转化成PDF
        /// </summary>
        /// <param name="filePath">文件地址</param>
        /// <param name="targetPath">目标文件地址</param>
        /// <exception cref="LibreOfficeFailedException"></exception>
        public static void ToPdf(string filePath, string targetPath)
        {
            // 判断targetPath是否已经是目录（不以扩展名结尾）
            bool isDirectory = !Path.HasExtension(targetPath) ||
                               targetPath.EndsWith("\\") ||
                               targetPath.EndsWith("/");

            string directory;
            if (isDirectory)
            {
                directory = targetPath;  // targetPath本身就是目录
            }
            else
            {
                directory = Path.GetDirectoryName(targetPath);  // targetPath是完整文件路径
            }

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string libreOfficePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libreoffice",
                "program",
                "soffice.exe"); // GetLibreOfficePath();

            string cammd = string.Format("--headless --convert-to pdf --outdir \"{0}\" --nologo \"{1}\"", directory, filePath);
            XErrorLogToolMDM.WriteLog(cammd);
            ProcessStartInfo processStartInfo = new ProcessStartInfo(libreOfficePath, cammd);
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.UseShellExecute = false;
            processStartInfo.CreateNoWindow = true;
            processStartInfo.WorkingDirectory = Environment.CurrentDirectory;

            Process process = new Process() { StartInfo = processStartInfo };
            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                //throw new LibreOfficeFailedException(process.ExitCode);
            }
        }




        //public static void ConvertXlsToXlsx(string inputPath, string outputDir)
        //{
        //    //string libreOfficePath = @"C:\Program Files\LibreOffice\program\soffice.exe";
        //    string libreOfficePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libreoffice",
        //        "program", "soffice.exe");
        //    string arguments =
        //        $"--headless --convert-to xlsx:\"Calc MS Excel 2007 XML\" \"{inputPath}\" --outdir \"{outputDir}\"";

        //    ProcessStartInfo processInfo = new ProcessStartInfo
        //    {
        //        FileName               = libreOfficePath,
        //        Arguments              = arguments,
        //        UseShellExecute        = false,
        //        RedirectStandardOutput = true,
        //        CreateNoWindow         = true
        //    };

        //    using (Process process = Process.Start(processInfo))
        //    {
        //        process.WaitForExit();
        //        string output = process.StandardOutput.ReadToEnd();
        //        Console.WriteLine(output);
        //    }
        //}


        public static void ConvertXlsToXlsx(string inputFile, string outputDir)
        {
            // 验证输入文件
            if (!File.Exists(inputFile))
            {
                throw new FileNotFoundException($"输入文件不存在: {inputFile}");
            }

            // 确保输出目录存在
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // 获取输入文件的文件名（不含扩展名）
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(inputFile);

            // 拼接输出文件完整路径（扩展名改为 .xlsx）
            string outputFile = Path.Combine(outputDir, fileNameWithoutExt + ".xlsx");

            // 如果输出文件已存在，先删除（避免被占用）
            if (File.Exists(outputFile))
            {
                try
                {
                    File.Delete(outputFile);
                }
                catch (Exception ex)
                {
                    throw new Exception($"无法删除已存在的输出文件，可能被其他程序占用: {outputFile}", ex);
                }
            }

            // 连接到 LibreOffice
            XComponentContext localContext = uno.util.Bootstrap.bootstrap();
            XMultiComponentFactory multiComponentFactory = localContext.getServiceManager();
            XComponentLoader componentLoader = (XComponentLoader)multiComponentFactory.createInstanceWithContext(
                "com.sun.star.frame.Desktop", localContext);

            // 打开 Excel 文件
            PropertyValue[] loadProps = new PropertyValue[1];
            loadProps[0] = new PropertyValue();
            loadProps[0].Name = "Hidden";
            loadProps[0].Value = new uno.Any(true);

            XComponent xComponent = componentLoader.loadComponentFromURL(
                "file:///" + inputFile.Replace("\\", "/"), "_blank", 0, loadProps);

            // 准备保存参数
            PropertyValue[] saveProps = new PropertyValue[2];

            // 设置过滤器名称（xlsx 格式）
            saveProps[0] = new PropertyValue();
            saveProps[0].Name = "FilterName";
            saveProps[0].Value = new uno.Any("Calc MS Excel 2007 XML"); // 或 "MS Excel 2007 XML"

            // 可选：设置覆盖现有文件
            saveProps[1] = new PropertyValue();
            saveProps[1].Name = "Overwrite";
            saveProps[1].Value = new uno.Any(true);

            // 保存为 xlsx
            XStorable xStorable = (XStorable)xComponent;
            xStorable.storeToURL("file:///" + outputFile.Replace("\\", "/"), saveProps);

            // 关闭文档
            XCloseable xCloseable = (XCloseable)xComponent;
            xCloseable.close(false);
        }

        /// <summary>
        /// 编辑确认表
        /// </summary>
        /// <param name="ConfirmTemplate"></param>
        /// <param name="ReceiptPath"></param>
        public void handleConfirmExcel(string ConfirmTemplate, string  ReceiptPath, string confirmOoutPath,
            List<FtceDetail>                  detail,          FtceDto ftceDto,     string bundleList,string cusCode)
        {
            try
            {
                if (!File.Exists(ReceiptPath))
                {
                    throw new FileNotFoundException("生成确认表失败,文件未找到", ReceiptPath);
                }

                //读取捆包List 判断要生成多少张确认表
                if (!File.Exists(bundleList))
                {
                    throw new FileNotFoundException("生成确认表失败，文件未找到", bundleList);
                }

                List<ConfirmBundle> confirmBundles = new List<ConfirmBundle>();
                string inDate = "";
                using (FileStream fs = new FileStream(bundleList, FileMode.Open, FileAccess.Read))
                {
                    HSSFWorkbook workbook = new HSSFWorkbook(fs);
                    ISheet       sheet    = workbook.GetSheetAt(0);

                    int inDataRow = 3;
                    int cellInDate = 14;
                    IRow rowData = sheet.GetRow(inDataRow);
                    ICell cellDate = rowData.GetCell(cellInDate);
                    inDate =  cellDate.StringCellValue.Substring(0,4);
                    //if (!string.IsNullOrEmpty(cellDate).ToString) { }




                    int  rowBegin  = 5;
                    int  cellBegin = 6;
                    IRow row       = sheet.GetRow(rowBegin);

                    for (int i = cellBegin; i < row.LastCellNum; i++)
                    {
                        ConfirmBundle confirmBundle = new ConfirmBundle();
                        if (row.GetCell(i) == null || string.IsNullOrWhiteSpace(row.GetCell(i).ToString()))
                        {
                            continue;
                        }

                        confirmBundle.orderCode = row.GetCell(i).ToString().Replace(" ", "")        
                                                        .Replace("\n", "")       
                                                        .Replace("\r", "")       
                                                        .Replace("\t", "")       
                                                        .Trim(); 

                        for (int j = rowBegin + 1; j < sheet.LastRowNum; j++)
                        {
                            Bundle bundle = new Bundle();
                            IRow   rowj   = sheet.GetRow(j);
                            if (rowj == null)
                            {
                                continue;
                            }

                            if (rowj.GetCell(i) == null || string.IsNullOrWhiteSpace(rowj.GetCell(i).ToString()))
                            {
                                continue;
                            }

                            bundle.quantity = Convert.ToInt32(rowj.GetCell(i).ToString());
                            for (int k = 0; k < cellBegin; k++)
                            {
                                switch (k)
                                {
                                    case 0:
                                        bundle.itemCode = rowj.GetCell(k).ToString();
                                        break;
                                    case 1:
                                        bundle.referenceNumber = rowj.GetCell(k).ToString();
                                        break;
                                    case 2:
                                        bundle.capacityPerBox = Convert.ToInt32(rowj.GetCell(k).ToString());
                                        break;
                                    case 3:
                                        bundle.totalBoxes = Convert.ToInt32(rowj.GetCell(k).ToString());
                                        break;
                                    case 4:
                                        bundle.shelfNumber = rowj.GetCell(k).ToString();
                                        break;
                                    case 5:
                                        bundle.palletNumber = rowj.GetCell(k).ToString();
                                        //if (string.IsNullOrEmpty(bundle.densoNum) || string.IsNullOrEmpty(bundle.itemName)) {
                                        //    throw new Exception("yuan");
                                        //}
                                        confirmBundle.bundles.Add(bundle);
                                        break;
                                }
                            }
                        }

                        confirmBundles.Add(confirmBundle);
                    }
                }

                string                           receipt       = "";      //发生订单号
                System.DateTime                  outDate       = default; //出荷日期
                string                           formattedDate = "";
                string                           week          = ""; //周四

                // 使用NPOI读取Excel文件
                using (FileStream fs = new FileStream(ReceiptPath, FileMode.Open, FileAccess.Read))
                {
                    HSSFWorkbook workbook = new HSSFWorkbook(fs);

                    // 获取第一个工作表
                    ISheet sheet = workbook.GetSheetAt(0);

                    IRow row  = sheet.GetRow(1); // 第2行
                    IRow row9 = sheet.GetRow(9);
                    if (row != null)
                    {
                        ICell cell = row.GetCell(12); // M列
                        if (cell != null)
                        {
                            receipt = cell.StringCellValue;
                        }
                    }

                    if (row9 != null)
                    {
                        ICell cell27 = row9.GetCell(27);
                        if (cell27 != null)
                        {
                            //string value = cell27.StringCellValue;
                            outDate       = cell27.DateCellValue;
                            formattedDate = outDate.ToString("yyyy/MM/dd");
                            string dayOfWeek = outDate.DayOfWeek.ToString();
                            week = ConvertToChineseDayOfWeek(outDate.DayOfWeek);
                        }
                    }
                    
                }

                foreach (ConfirmBundle confirmBundle in confirmBundles)
                {
                    //获取当前年份
                    string currentYear = DateTime.Now.Year.ToString();
                    string bianci = inDate + confirmBundle.orderCode.Replace("\\", "");


                    foreach (Bundle bundle in confirmBundle.bundles)
                    {
                        string strSql = @"select part_name,denso_num,veh_model
                                              from originalunit where cust_name = 'FTEC' and cust_num = '{0}'  and recv_port = '号口'";
                        strSql = string.Format(strSql, bundle.itemCode);
                        DataTable dataTable = DataHelper.Query(strSql);
                        if (dataTable.Rows.Count > 0)
                        {
                            bundle.itemName = dataTable.Rows[0]["part_name"].ToString();
                            bundle.densoNum = dataTable.Rows[0]["denso_num"].ToString();
                            bundle.vehModel = dataTable.Rows[0]["veh_model"].ToString();
                        }
                        else
                        {
                            throw new Exception("未查询到该品番信息 " + bundle.itemCode);
                            //bundle.itemName = "";
                            //bundle.densoNum = "";
                            //bundle.vehModel = "";
                        }
                    }

                    // foreach (Bundle bundle in confirmBundle.bundles)
                    // {
                    try
                    {
                        // 打开模板文件
                        using (FileStream confirmTemp = new FileStream(ConfirmTemplate, FileMode.Open, FileAccess.Read))
                        {
                            IWorkbook confirmT;
                            string    extension = Path.GetExtension(ConfirmTemplate).ToLower();
                            if (extension == ".xls")
                            {
                                confirmT = new HSSFWorkbook(confirmTemp); // Excel 2003 及以下版本
                            }
                            else if (extension == ".xlsx")
                            {
                                confirmT = new XSSFWorkbook(confirmTemp); // Excel 2007 及以上版本
                            }
                            else
                            {
                                throw new NotSupportedException(
                                    "Unsupported file format. Only .xls and .xlsx are supported.");
                            }

                            ISheet confirmSheet = confirmT.GetSheetAt(0);

                            IRow row4 = confirmSheet.GetRow(4);
                            ICell  cell45 = row4.GetCell(5);
                            cell45.SetCellValue(bianci);

                            // 纳入受领编号 
                            IRow  cRow3   = confirmSheet.GetRow(3);
                            ICell cCellF4 = cRow3.GetCell(5);
                            cCellF4.SetCellValue(receipt);

                            //星期
                            ICell cCellI3 = cRow3.GetCell(8);
                            cCellI3.SetCellValue(week);

                            IRow  cRow5   = confirmSheet.GetRow(5);

                            ICell cellCusName = cRow5.GetCell(1);
                            cellCusName.SetCellValue(cusCode);

                            ICell cCellF5 = cRow5.GetCell(5);
                            cCellF5.SetCellValue(formattedDate);
                            
                            // cRow5.GetCell()


                            IRow  row6   = confirmSheet.GetRow(6);
                            ICell cell61 = row6.GetCell(1);
                            cell61.SetCellValue(ftceDto.createPerson);
                            ICell cell65 = row6.GetCell(5);
                            cell65.SetCellValue(DateTime.Now.ToString("yyyy/MM/dd"));

                            int  templateRowIndex = 12; // 第 13 行（索引从 0 开始）
                            IRow templateRow      = confirmSheet.GetRow(templateRowIndex);
                            if (templateRow == null)
                            {
                                throw new InvalidOperationException(
                                    $"Template row at index {templateRowIndex} does not exist.");
                            }

                            // 插入开始位置：第 14 行（模板行之后）
                            int startRowIndex   = templateRowIndex + 1;
                            int currentRowIndex = startRowIndex;


                            for (var i = 0; i < confirmBundle.bundles.Count - 1; i++)
                            {
                                // 检查现有行数，确保不会覆盖
                                if (currentRowIndex < confirmSheet.LastRowNum)
                                {
                                    // 将 currentRowIndex 及以下的行向下移动 1 行，为新行腾出空间
                                    confirmSheet.ShiftRows(currentRowIndex, confirmSheet.LastRowNum, 1);
                                }

                                // 创建新行
                                IRow newRow = confirmSheet.CreateRow(currentRowIndex);
                                newRow.Height = templateRow.Height; // 保留行高

                                // 复制模板行的单元格
                                for (int j = 0; j < templateRow.LastCellNum; j++)
                                {
                                    ICell templateCell = templateRow.GetCell(j, MissingCellPolicy.CREATE_NULL_AS_BLANK);
                                    ICell newCell      = newRow.CreateCell(j);

                                    // 复制样式和类型
                                    newCell.CellStyle = templateCell.CellStyle;
                                    newCell.SetCellType(templateCell.CellType);

                                    // 复制模板单元格的值
                                    if (templateCell.CellType != CellType.Blank)
                                    {
                                        switch (templateCell.CellType)
                                        {
                                            case CellType.String:
                                                newCell.SetCellValue(templateCell.StringCellValue);
                                                break;
                                            case CellType.Numeric:
                                                newCell.SetCellValue(templateCell.NumericCellValue);
                                                break;
                                            case CellType.Boolean:
                                                newCell.SetCellValue(templateCell.BooleanCellValue);
                                                break;
                                            case CellType.Formula:
                                                newCell.SetCellFormula(templateCell.CellFormula);
                                                break;
                                            default:
                                                newCell.SetCellValue(templateCell.ToString());
                                                break;
                                        }
                                    }
                                }

                                // 复制模板行的合并区域
                                for (int k = 0; k < confirmSheet.NumMergedRegions; k++)
                                {
                                    var mergedRegion = confirmSheet.GetMergedRegion(k);
                                    if (mergedRegion != null && mergedRegion.FirstRow == templateRowIndex)
                                    {
                                        var newMergedRegion = new NPOI.SS.Util.CellRangeAddress(
                                            currentRowIndex,
                                            currentRowIndex,
                                            mergedRegion.FirstColumn,
                                            mergedRegion.LastColumn);
                                        confirmSheet.AddMergedRegion(newMergedRegion);
                                    }
                                }

                                // 移到下一行
                                currentRowIndex++;
                            }
                            
                            //设置值
                            int sumBoxNum = 0;
                            for (var j = 0; j < confirmBundle.bundles.Count; j++)
                            {
                                IRow rowi = confirmSheet.GetRow(templateRowIndex + j);
                                if (rowi != null)
                                {
                                    ICell cell0 = rowi.GetCell(0); //车型
                                    cell0.SetCellValue(confirmBundle.bundles[j].vehModel);
                                    ICell cell1 = rowi.GetCell(1); //制品名称
                                    cell1.SetCellValue(confirmBundle.bundles[j].itemName);
                                    ICell cell2 = rowi.GetCell(2); //客户品番
                                    cell2.SetCellValue(confirmBundle.bundles[j].itemCode);
                                    ICell cell3 = rowi.GetCell(3); //电装品番
                                    cell3.SetCellValue(confirmBundle.bundles[j].densoNum);
                                    ICell cell5 = rowi.GetCell(5); //背番号
                                    cell5.SetCellValue(confirmBundle.bundles[j].referenceNumber);
                                    ICell cell6 = rowi.GetCell(6); //收容数
                                    cell6.SetCellValue(confirmBundle.bundles[j].capacityPerBox);
                                    ICell cell7 = rowi.GetCell(7); //看板张数
                                    cell7.SetCellValue(confirmBundle.bundles[j].quantity);
                                    sumBoxNum += confirmBundle.bundles[j].quantity;
                                    ICell cell8 = rowi.GetCell(8); //纳入数量
                                    cell8.SetCellValue(confirmBundle.bundles[j].capacityPerBox * confirmBundle.bundles[j].quantity);
                                }
                            }
                            
                            // 将合计值写入合计单元格（合计行的 H 列）
                            IRow sumRow = confirmSheet.GetRow(templateRowIndex    + confirmBundle.bundles.Count) ??
                                          confirmSheet.CreateRow(templateRowIndex + confirmBundle.bundles.Count);
                            ICell sumCell = sumRow.GetCell(7) ?? sumRow.CreateCell(7); // H 列是索引 7
                            sumCell.SetCellType(CellType.Numeric);                     // 显式设置单元格为数字类型
                            sumCell.SetCellValue(sumBoxNum);

                            if (confirmSheet is XSSFSheet xssfSheet)
                            {
                                var drawing = xssfSheet.CreateDrawingPatriarch() as XSSFDrawing;
                                if (drawing != null)
                                {
                                    foreach (var shape in drawing.GetShapes())
                                    {
                                        if (shape is XSSFSimpleShape simpleShape)
                                        {
                                            var anchor = simpleShape.GetAnchor() as XSSFClientAnchor;
                                            if (anchor != null && anchor.Row1 >= startRowIndex)
                                            {
                                                int insertedRowCount = currentRowIndex - startRowIndex;
                                                anchor.Row1 += insertedRowCount;
                                                anchor.Row2 += insertedRowCount;
                                            }
                                        }
                                    }
                                }
                            }
                            else if (confirmSheet is HSSFSheet hssfSheet)
                            {
                                var drawing = hssfSheet.CreateDrawingPatriarch() as HSSFPatriarch;
                                if (drawing != null)
                                {
                                    foreach (var shape in drawing.Children)
                                    {
                                        if (shape is HSSFSimpleShape simpleShape)
                                        {
                                            var anchor = simpleShape.Anchor as HSSFClientAnchor;
                                            if (anchor != null && anchor.Row1 >= startRowIndex)
                                            {
                                                int insertedRowCount = currentRowIndex - startRowIndex;
                                                anchor.Row1 += insertedRowCount;
                                                anchor.Row2 += insertedRowCount;
                                            }
                                        }
                                    }
                                }
                            }

                           
                            
                            string confirmItemName = confirmBundle.orderCode.Replace("\\","_");
                            
                            string confirmDic      = System.IO.Path.GetDirectoryName(confirmOoutPath);
                            string confirmName     = System.IO.Path.GetFileNameWithoutExtension(confirmOoutPath);
                            
                            string confirmItemDic = Path.Combine(confirmDic, confirmName);
                            Directory.CreateDirectory(confirmItemDic);

                            string confirmExcelPath = System.IO.Path.Combine(confirmDic, confirmName,confirmItemName+".xls" );
                            
                            // 保存修改后的文件
                            using (FileStream outputFs =
                                   new FileStream(confirmExcelPath, FileMode.Create, FileAccess.Write))
                            {
                                confirmT.Write(outputFs);
                            }
                            
                            string confirmPdfPath = System.IO.Path.Combine(confirmDic, confirmName,confirmItemName+".pdf" );
                            // string confirmPath     = System.IO.Path.Combine(confirmDic, confirmName     + ".pdf");
                            ExcelOutOfPdf1(confirmExcelPath, confirmPdfPath);
                            PulicHelper.saveConfirm(ftceDto.mainId, confirmExcelPath, confirmPdfPath);
                            // detail.ForEach(item => { item.comfirm = confirmPdfPath; });
                            detail.Where(item => item.orderCode == confirmItemName)
                                .ToList()
                                .ForEach(item => { item.comfirm = confirmPdfPath; });
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.WriteLine($"发生错误: {ex.Message}");
                        throw;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }


        // 获取单元格值的辅助方法，处理合并单元格
        private string GetCellValue(ICell cell, ISheet sheet)
        {
            if (cell == null)
                return string.Empty;

            // 检查是否是合并单元格
            if (cell.IsMergedCell)
            {
                // 遍历工作表中的合并区域
                for (int i = 0; i < sheet.NumMergedRegions; i++)
                {
                    NPOI.SS.Util.CellRangeAddress region = sheet.GetMergedRegion(i);
                    if (region.IsInRange(cell.RowIndex, cell.ColumnIndex))
                    {
                        // 获取合并区域的第一个单元格
                        IRow  firstRow  = sheet.GetRow(region.FirstRow);
                        ICell firstCell = firstRow.GetCell(region.FirstColumn);
                        return GetCellValueInternal(firstCell);
                    }
                }
            }

            return GetCellValueInternal(cell);
        }

        // 内部方法：根据单元格类型获取值
        private string GetCellValueInternal(ICell cell)
        {
            if (cell == null)
                return string.Empty;

            switch (cell.CellType)
            {
                case CellType.String:
                    return cell.StringCellValue;
                case CellType.Numeric:
                    if (DateUtil.IsCellDateFormatted(cell))
                        return cell.DateCellValue.ToString("yyyy-MM-dd");
                    return cell.NumericCellValue.ToString();
                case CellType.Boolean:
                    return cell.BooleanCellValue.ToString();
                case CellType.Formula:
                    return cell.CachedFormulaResultType == CellType.String
                        ? cell.StringCellValue
                        : cell.NumericCellValue.ToString();
                default:
                    return string.Empty;
            }
        }

        // 获取列范围名称（例如 F-K, L-U 等）
        private string GetColumnRangeName(int startCol)
        {
            if (startCol == 5) return "F-K";
            if (startCol == 12) return "L-U";
            if (startCol == 23) return "X-Z";
            if (startCol == 26) return "AA-AC";
            if (startCol == 29) return "AD-AG";
            if (startCol == 33) return "AH-AJ";
            return string.Empty;
        }

        private string ConvertToChineseDayOfWeek(DayOfWeek dayOfWeek)
        {
            switch (dayOfWeek)
            {
                case DayOfWeek.Monday:    return "周一";
                case DayOfWeek.Tuesday:   return "周二";
                case DayOfWeek.Wednesday: return "周三";
                case DayOfWeek.Thursday:  return "周四";
                case DayOfWeek.Friday:    return "周五";
                case DayOfWeek.Saturday:  return "周六";
                case DayOfWeek.Sunday:    return "周日";
                default:                  return "未知";
            }
        }

        private string GetCellStringValue(ICell cell)
        {
            if (cell == null)
                return "";

            switch (cell.CellType)
            {
                case CellType.String:
                    return cell.StringCellValue;

                case CellType.Numeric:
                    return cell.NumericCellValue.ToString();

                case CellType.Boolean:
                    return cell.BooleanCellValue.ToString();

                case CellType.Formula:
                    return cell.ToString(); // 你也可以改成 cell.CellFormula

                default:
                    return cell.ToString();
            }
        }

        private int GetCellIntValue(ICell cell)
        {
            if (cell == null)
                return 0;

            try
            {
                switch (cell.CellType)
                {
                    case CellType.Numeric:
                        return (int)cell.NumericCellValue; // 强制转换为整数

                    case CellType.Formula:
                        if (cell.CachedFormulaResultType == CellType.Numeric)
                            return (int)cell.NumericCellValue;

                        if (cell.CachedFormulaResultType == CellType.String)
                        {
                            if (int.TryParse(cell.StringCellValue, out int formulaStrResult))
                                return formulaStrResult;
                        }

                        break;

                    case CellType.String:
                        if (int.TryParse(cell.StringCellValue, out int stringResult)) // 直接转换为 int
                            return stringResult;
                        break;
                }
            }
            catch
            {
                // 如果有异常（如无法转换），返回0
            }

            return 0;
        }

        // public void DisposeFtceSupply(FtceDto FtceDto)
        // {
        //     string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;     //根目录
        //     List<inpdetail2> inpdetail2s  = new List<inpdetail2>();
        //
        //     //先创建一个同名文件夹
        //     string sourceExcelName = System.IO.Path.GetFileNameWithoutExtension(FtceDto.zipPath);
        //     string sourcezipPath = FtceDto.basePath;
        //     string newFolderPath = System.IO.Path.Combine(sourcezipPath, sourceExcelName);
        //     List<Dictionary<string, string>> originalunit = new List<Dictionary<string, string>>();
        //     if (!Directory.Exists(newFolderPath))
        //     {
        //         Directory.CreateDirectory(newFolderPath);
        //     }
        //     //先将.xls转为.xlsx
        //     ConvertXlsToXlsx(FtceDto.zipPath, newFolderPath);
        //     string xlsxPath = System.IO.Path.Combine(newFolderPath, sourceExcelName + ".xlsx");
        //     //把excel根据sheet页拆分到刚创建的文件夹中
        //     SplitExcelByDeletingOtherSheets(xlsxPath, newFolderPath);
        //     string blankReceiptPath = System.IO.Path.Combine(newFolderPath, "空白纳入受领书 (FTMS).xlsx");
        //
        //     string AcceptancePath = System.IO.Path.Combine(newFolderPath, "纳入受领书.pdf");
        //     if (!string.IsNullOrEmpty(AcceptancePath))    //保存受领书
        //     {
        //         inpdetail2  inpdetail2  = new inpdetail2();
        //         inpdetail2.filetype   = "受领书";
        //         inpdetail2.Stacklabel = AcceptancePath;
        //         inpdetail2.rowcode    = 5;
        //         inpdetail2s.Add(inpdetail2);
        //     }
        //     
        //     //受领书转PDF
        //     ExcelOutOfPdf(blankReceiptPath, AcceptancePath);
        //     
        //     string tempPdfPath;
        //     using (FileStream blankReceipt = new FileStream(blankReceiptPath, FileMode.Open, FileAccess.Read))
        //     {
        //         IWorkbook blankReceiptBook = new XSSFWorkbook(blankReceipt);
        //         ISheet confirmSheet = blankReceiptBook.GetSheetAt(0);
        //         originalunit = TraverseCellsFromC9(confirmSheet);     //根据纳受领书和原单位表计算需要多少箱 多少外箱单
        //         string newOutBoxDic = disposeBox(originalunit, newFolderPath);
        //         tempPdfPath = SetOfOutBox(newOutBoxDic,inpdetail2s);  //生成成套外箱单    保存外箱单路径   
        //     }
        //处理货垛标签
        // string stackzipPath = System.IO.Path.Combine(baseDirectory, ConfigurationManager.AppSettings["FTCEMDD"].ToString());// 模板地址);
        // StackLabel(originalunit, stackzipPath,newFolderPath,inpdetail2s);    //保存货垛标签路径
        //
        // //确认表
        // // string q = ConfigurationManager.AppSettings["FTCEQRB"].ToString();
        // string confirmTemplatePath = System.IO.Path.Combine(baseDirectory, ConfigurationManager.AppSettings["FTCEQRB"].ToString());
        // string conFirmOutPath = System.IO.Path.Combine(newFolderPath, "确认表.xls");
        // //originalunit
        // string orderCode = FtceSuppleConfirm(originalunit,confirmTemplatePath,conFirmOutPath,inpdetail2s);
        //
        // foreach (var t in inpdetail2s)
        // {
        //     t.ordercode     = orderCode;
        //     t.mainId = FtceDto.mainId;
        // };
        // ordService.InsertDetail2(inpdetail2s);


        // }
        /// <summary>
        /// ftce补给品确认表
        /// </summary>
        /// <param name="originalunit"></param>
        public string FtceSuppleConfirm(List<Dictionary<string, string>> originalunit, string ConfirmTemplate,
            string confirmOoutPath, List<inpdetail2> inpdetail2s, FtceDto ftceDto)
        {
            try
            {
                // 打开模板文件
                using (FileStream confirmTemp = new FileStream(ConfirmTemplate, FileMode.Open, FileAccess.Read))
                {
                    IWorkbook confirmT; //= new XSSFWorkbook(confirmTemp);

                    // 判断文件扩展名，选择合适的 Workbook 实现
                    string extension = Path.GetExtension(ConfirmTemplate).ToLower();
                    if (extension == ".xls")
                    {
                        confirmT = new HSSFWorkbook(confirmTemp); // Excel 2003 及以下版本
                    }
                    else if (extension == ".xlsx")
                    {
                        confirmT = new XSSFWorkbook(confirmTemp); // Excel 2007 及以上版本
                    }
                    else
                    {
                        throw new NotSupportedException("Unsupported file format. Only .xls and .xlsx are supported.");
                    }

                    //
                    //  获取第一个工作表（Sheet1）
                    ISheet confirmSheet = confirmT.GetSheetAt(0);
                    confirmSheet.DisplayGridlines = false;

                    // 纳入受领编号 
                    string receipt = originalunit[0]["orderNo"];
                    IRow   cRow3   = confirmSheet.GetRow(3);
                    ICell  cCellF4 = cRow3.GetCell(5);
                    cCellF4.SetCellValue(receipt);

                    string custName = "FTEC补给品";
                    ICell cell1 = cRow3.GetCell(1);
                    cell1.SetCellValue(custName);

                    //星期
                    string          week    = "";      //周四
                    System.DateTime outDate = default; //出荷日期
                    outDate = Convert.ToDateTime(originalunit[0]["shipment"]);
                    string formattedDate = outDate.ToString("yyyy/MM/dd");

                    //出荷日期
                    IRow  cRow5   = confirmSheet.GetRow(5);
                    ICell cellCusCode = cRow5.GetCell(1);
                    cellCusCode.SetCellValue("10001601");
                    ICell cCellF5 = cRow5.GetCell(5);
                    cCellF5.SetCellValue(formattedDate);

                    //周几
                    week = ConvertToChineseDayOfWeek(outDate.DayOfWeek);
                    ICell cCellI3 = cRow3.GetCell(8);
                    cCellI3.SetCellValue(week);


                    IRow  row6   = confirmSheet.GetRow(6);
                    ICell cell61 = row6.GetCell(1);
                    cell61.SetCellValue(ftceDto.createPerson);
                    ICell cell65 = row6.GetCell(5);
                    cell65.SetCellValue(DateTime.Now.ToString("yyyy/MM/dd"));


                    int  templateRowIndex = 12; // 第 13 行（索引从 0 开始）
                    IRow templateRow      = confirmSheet.GetRow(templateRowIndex);
                    if (templateRow == null)
                    {
                        throw new InvalidOperationException(
                            $"Template row at index {templateRowIndex} does not exist.");
                    }

                    // 插入开始位置：第 14 行（模板行之后）
                    int startRowIndex   = templateRowIndex + 1;
                    int currentRowIndex = startRowIndex;


                    for (var i = 0; i < originalunit.Count - 1; i++)
                    {
                        // 检查现有行数，确保不会覆盖
                        if (currentRowIndex < confirmSheet.LastRowNum)
                        {
                            // 将 currentRowIndex 及以下的行向下移动 1 行，为新行腾出空间
                            confirmSheet.ShiftRows(currentRowIndex, confirmSheet.LastRowNum, 1);
                        }

                        // 创建新行
                        IRow newRow = confirmSheet.CreateRow(currentRowIndex);
                        newRow.Height = templateRow.Height; // 保留行高

                        // 复制模板行的单元格
                        for (int j = 0; j < templateRow.LastCellNum; j++)
                        {
                            ICell templateCell = templateRow.GetCell(j, MissingCellPolicy.CREATE_NULL_AS_BLANK);
                            ICell newCell      = newRow.CreateCell(j);

                            // 复制样式和类型
                            newCell.CellStyle = templateCell.CellStyle;
                            newCell.SetCellType(templateCell.CellType);

                            // 复制模板单元格的值
                            if (templateCell.CellType != CellType.Blank)
                            {
                                switch (templateCell.CellType)
                                {
                                    case CellType.String:
                                        newCell.SetCellValue(templateCell.StringCellValue);
                                        break;
                                    case CellType.Numeric:
                                        newCell.SetCellValue(templateCell.NumericCellValue);
                                        break;
                                    case CellType.Boolean:
                                        newCell.SetCellValue(templateCell.BooleanCellValue);
                                        break;
                                    case CellType.Formula:
                                        newCell.SetCellFormula(templateCell.CellFormula);
                                        break;
                                    default:
                                        newCell.SetCellValue(templateCell.ToString());
                                        break;
                                }
                            }
                        }

                        // 复制模板行的合并区域
                        for (int k = 0; k < confirmSheet.NumMergedRegions; k++)
                        {
                            var mergedRegion = confirmSheet.GetMergedRegion(k);
                            if (mergedRegion != null && mergedRegion.FirstRow == templateRowIndex)
                            {
                                var newMergedRegion = new NPOI.SS.Util.CellRangeAddress(
                                    currentRowIndex,
                                    currentRowIndex,
                                    mergedRegion.FirstColumn,
                                    mergedRegion.LastColumn);
                                confirmSheet.AddMergedRegion(newMergedRegion);
                            }
                        }

                        // 移到下一行
                        currentRowIndex++;
                    }

                    Dictionary<string, int> columnMap = new Dictionary<string, int>
                    {
                        { "vehModel", 0 }, //车型
                        //制品名称
                        { "custNum", 2 },     //客户品番
                        { "densoNum", 3 },    //电装品番
                        { "backNum", 5 },     //背番号
                        { "boxCap", 6 },      //收容数
                        { "boxNum", 7 },      //发箱数    看板张数?
                        { "shippingQty", 8 }, //发住个数  纳入数量?
                        //托码
                        // { "loc_num", 10 },  //库位号
                        { "part_name", 1 }, // 品名
                        //{ "recv_area", 8 } //接入地代码
                    };

                    //设置值
                    int sumBoxNum = 0;
                    for (var j = 0; j < originalunit.Count; j++)
                    {
                        IRow rowi = confirmSheet.GetRow(templateRowIndex + j);
                        if (rowi != null)
                        {
                            // 设置数据值（仅覆盖 columnMap 指定的列）
                            foreach (var kvp in columnMap)
                            {
                                string key      = kvp.Key;
                                int    colIndex = kvp.Value;
                                ICell  dataCell = rowi.GetCell(colIndex) ?? rowi.CreateCell(colIndex);
                                object value    = originalunit[j].ContainsKey(key) ? originalunit[j][key] : null;
                                if (key == "boxNum")
                                {
                                    sumBoxNum += int.Parse(value?.ToString() ?? "0");
                                }

                                if (value is double d)
                                {
                                    dataCell.SetCellValue(d); // 写入为数字
                                }
                                else if (value is int i)
                                {
                                    dataCell.SetCellValue((double)i); // 写入为数字
                                }
                                else if (value is decimal m)
                                {
                                    dataCell.SetCellValue((double)m); // 写入为数字
                                }
                                else
                                {
                                    dataCell.SetCellValue(value?.ToString() ?? ""); // 写入为字符串
                                }
                            }
                        }
                    }


                    // 将合计值写入合计单元格（合计行的 H 列）
                    IRow sumRow = confirmSheet.GetRow(templateRowIndex    + originalunit.Count) ??
                                  confirmSheet.CreateRow(templateRowIndex + originalunit.Count);
                    ICell sumCell = sumRow.GetCell(7) ?? sumRow.CreateCell(7); // H 列是索引 7
                    sumCell.SetCellType(CellType.Numeric);                     // 显式设置单元格为数字类型
                    sumCell.SetCellValue(sumBoxNum);

                    if (confirmSheet is XSSFSheet xssfSheet)
                    {
                        var drawing = xssfSheet.CreateDrawingPatriarch() as XSSFDrawing;
                        if (drawing != null)
                        {
                            foreach (var shape in drawing.GetShapes())
                            {
                                if (shape is XSSFSimpleShape simpleShape)
                                {
                                    var anchor = simpleShape.GetAnchor() as XSSFClientAnchor;
                                    if (anchor != null && anchor.Row1 >= startRowIndex)
                                    {
                                        int insertedRowCount = currentRowIndex - startRowIndex;
                                        anchor.Row1 += insertedRowCount;
                                        anchor.Row2 += insertedRowCount;
                                    }
                                }
                            }
                        }
                    }
                    else if (confirmSheet is HSSFSheet hssfSheet)
                    {
                        var drawing = hssfSheet.CreateDrawingPatriarch() as HSSFPatriarch;
                        if (drawing != null)
                        {
                            foreach (var shape in drawing.Children)
                            {
                                if (shape is HSSFSimpleShape simpleShape)
                                {
                                    var anchor = simpleShape.Anchor as HSSFClientAnchor;
                                    if (anchor != null && anchor.Row1 >= startRowIndex)
                                    {
                                        int insertedRowCount = currentRowIndex - startRowIndex;
                                        anchor.Row1 += insertedRowCount;
                                        anchor.Row2 += insertedRowCount;
                                    }
                                }
                            }
                        }
                    }

                    //string confirmRemark = ordService.GetConfirmRemark(ftceDto.mainId);
                    //string type          = "丰田";
                    //if (!string.IsNullOrEmpty(confirmRemark))
                    //{
                    //    for (int i = 0; i <= confirmSheet.LastRowNum; i++)
                    //    {
                    //        IRow row = confirmSheet.GetRow(i);
                    //        if (row == null) continue;

                    //        ICell cell = row.GetCell(0); // 第一列
                    //        if (cell != null && cell.ToString().Trim().Contains("备注"))
                    //        {
                    //            // 设置文本内容
                    //            ICell remarkCell = row.GetCell(1) ?? row.CreateCell(1);
                    //            remarkCell.SetCellValue(confirmRemark);

                    //            if (type == "丰田")
                    //            {
                    //                ICellStyle style = confirmT.CreateCellStyle();
                    //                style.WrapText = true;

                    //                IFont font = confirmT.CreateFont();
                    //                font.FontHeightInPoints = 36;
                    //                style.SetFont(font);
                    //                style.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Top;

                    //                remarkCell.CellStyle = style;

                    //                // int lineCount = Math.Max(1, (int)Math.Ceiling((double)confirmRemark.Length / 82));
                    //                row.HeightInPoints = 4 * 50f;

                    //                break;
                    //            }
                    //            else if (type == "佛山本田")
                    //            {
                    //                ICellStyle style = confirmT.CreateCellStyle();
                    //                style.WrapText = true;

                    //                IFont font = confirmT.CreateFont();
                    //                font.FontHeightInPoints = 18;
                    //                style.SetFont(font);
                    //                style.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Top;
                    //                remarkCell.CellStyle = style;

                    //                // int lineCount = Math.Max(1, (int)Math.Ceiling((double)confirmRemark.Length / 188));
                    //                row.HeightInPoints = 4 * 50f;

                    //                break;
                    //            }
                    //            else if (type == "东风本田")
                    //            {
                    //                ICellStyle style = confirmT.CreateCellStyle();
                    //                style.WrapText = true;

                    //                IFont font = confirmT.CreateFont();
                    //                font.FontHeightInPoints = 18;
                    //                style.SetFont(font);
                    //                style.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Top;
                    //                remarkCell.CellStyle = style;

                    //                // int lineCount = Math.Max(1, (int)Math.Ceiling((double)confirmRemark.Length / 188));
                    //                row.HeightInPoints = 4 * 45f;

                    //                break;
                    //            }
                    //        }
                    //    }
                    //}

                    // 保存修改后的文件
                    using (FileStream outputFs = new FileStream(confirmOoutPath, FileMode.Create, FileAccess.Write))
                    {
                        confirmT.Write(outputFs);
                        outputFs.Flush(); // 显式刷新
                    }
                }

                //将生成的excel转成横板pdf
                string confirmPdfName = System.IO.Path.GetFileNameWithoutExtension(confirmOoutPath);
                string confirmPdfDic  = System.IO.Path.GetDirectoryName(confirmOoutPath);
                string confirmPdfPath = System.IO.Path.Combine(confirmPdfDic, confirmPdfName + ".pdf");
                ExcelOutOfPdf1(confirmOoutPath, confirmPdfPath);

                PulicHelper.saveConfirm(ftceDto.mainId, confirmOoutPath, confirmPdfPath);


                if (!string.IsNullOrEmpty(confirmPdfPath)) //保存受领书
                {
                    inpdetail2 inpdetail2 = new inpdetail2();
                    inpdetail2.filetype   = "确认表";
                    inpdetail2.Stacklabel = confirmPdfPath;
                    // inpdetail2.ordercode  = originalunit[1]["orderNo"];
                    inpdetail2.rowcode = 1;
                    inpdetail2s.Add(inpdetail2);
                }

                return originalunit[0]["orderNo"];
            }
            catch (System.Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }


        public void ArrangePdfWithLabelsVertically(string inputPdfPath, string outputPdfPath)
        {
            try
            {
                // 读取输入的PDF文件
                PdfReader reader = new PdfReader(inputPdfPath);

                // 创建一个新的文件输出流
                using (FileStream fs = new FileStream(outputPdfPath, FileMode.Create))
                {
                    // 创建PDF文档对象
                    Document document = new Document(iTextSharp.text.PageSize.A4);

                    // 创建PdfWriter实例，将输出流传递给它
                    PdfWriter writer = PdfWriter.GetInstance(document, fs);

                    // 打开文档进行写入
                    document.Open();

                    // 获取每页的宽高
                    float a4Width  = iTextSharp.text.PageSize.A4.Width;
                    float a4Height = iTextSharp.text.PageSize.A4.Height;

                    // 计算每个标签的位置（每页放3个标签，纵向排列）
                    // 假设每个标签的宽度占A4宽度，高度占A4高度的1/3
                    float labelWidth  = a4Width;      // 标签的宽度占满整个A4宽度
                    float labelHeight = a4Height / 3; // 标签的高度是A4的1/3

                    // 遍历所有的页面
                    for (int i = 1; i <= reader.NumberOfPages; i++)
                    {
                        // 获取当前页面
                        PdfImportedPage page = writer.GetImportedPage(reader, i);

                        // 设置页面的坐标系统，以便我们能够放置多个标签
                        // 页面的起始坐标 (0, 0) 即为左下角
                        PdfContentByte cb = writer.DirectContent;

                        // 第一行的标签位置 (上)
                        cb.AddTemplate(page, 0, labelHeight * 2, labelWidth, labelHeight, 0, 0); // 上方标签

                        // 第二行的标签位置 (中)
                        cb.AddTemplate(page, 0, labelHeight, labelWidth, labelHeight, 0, 0); // 中间标签

                        // 第三行的标签位置 (下)
                        cb.AddTemplate(page, 0, 0, labelWidth, labelHeight, 0, 0); // 下方标签
                    }

                    // 关闭文档
                    document.Close();
                }

                Console.WriteLine("PDF 标签排列完成。输出文件: " + outputPdfPath);
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("生成标签排列 PDF 时发生错误: " + ex.Message);
            }
        }


        public void MergeThreePdfsVertically(
            string inputFile1,
            string inputFile2,
            string inputFile3,
            string outputFile)
        {
            if (string.IsNullOrEmpty(inputFile1) || string.IsNullOrEmpty(inputFile2) ||
                string.IsNullOrEmpty(inputFile3) || string.IsNullOrEmpty(outputFile))
            {
                throw new System.ArgumentException("All file paths must be provided.");
            }

            // A4 页面大小 (595 x 842 点)
            float a4Width       = 595f;
            float a4Height      = 842f;
            float sectionHeight = a4Height / 3; // 每个页面目标高度 (约 280.67 点)

            using (iText.Kernel.Pdf.PdfWriter writer = new iText.Kernel.Pdf.PdfWriter(outputFile))
            using (iText.Kernel.Pdf.PdfDocument mergedPdf = new iText.Kernel.Pdf.PdfDocument(writer))
            {
                // 创建一个新的 A4 页面
                iText.Kernel.Pdf.PdfPage newPage = mergedPdf.AddNewPage(
                    new iText.Kernel.Geom.PageSize(a4Width, a4Height));
                iText.Kernel.Pdf.Canvas.PdfCanvas canvas = new iText.Kernel.Pdf.Canvas.PdfCanvas(newPage);

                // 处理三个 PDF 文件
                string[] inputFiles = new string[] { inputFile1, inputFile2, inputFile3 };
                for (int i = 0; i < 3; i++)
                {
                    using (iText.Kernel.Pdf.PdfReader reader = new iText.Kernel.Pdf.PdfReader(inputFiles[i]))
                    using (iText.Kernel.Pdf.PdfDocument pdfDoc = new iText.Kernel.Pdf.PdfDocument(reader))
                    {
                        // 获取第一个页面
                        iText.Kernel.Pdf.PdfPage    page         = pdfDoc.GetPage(1);
                        iText.Kernel.Geom.Rectangle originalRect = page.GetPageSize();

                        // 计算缩放比例，使页面高度适应 A4 的三分之一
                        float scale       = sectionHeight           / originalRect.GetHeight();
                        float scaledWidth = originalRect.GetWidth() * scale;

                        // 计算水平居中偏移量
                        float xOffset = (a4Width - scaledWidth) / 2;

                        // 创建 PdfFormXObject 来包装页面内容
                        iText.Kernel.Pdf.Xobject.PdfFormXObject pageCopy =
                            page.CopyAsFormXObject(mergedPdf);

                        // 计算垂直位置（从顶部开始，依次向下排列）
                        float yOffset = a4Height - (i + 1) * sectionHeight;

                        // 应用缩放并绘制页面内容，水平居中
                        canvas.SaveState();
                        canvas.ConcatMatrix(scale, 0, 0, scale, xOffset, yOffset);
                        canvas.AddXObjectAt(pageCopy, 0, 0);
                        canvas.RestoreState();
                    }
                }

                // 设置新页面的 MediaBox 和 CropBox
                newPage.SetMediaBox(new iText.Kernel.Geom.Rectangle(0, 0, a4Width, a4Height));
                newPage.SetCropBox(new iText.Kernel.Geom.Rectangle(0, 0, a4Width, a4Height));
            }
        }

        public string BoardPdf(string tempPath)
        {
            return "";
        }

        /// <summary>
        /// 将一个excel根据sheet页拆分成多个excel
        /// </summary>
        /// <param name="zipPath"></param>
        /// <param name="outputDirectory"></param>
        public static void SplitExcelByDeletingOtherSheets(string zipPath, string outputDirectory)
        {
            // 连接到 LibreOffice 服务
            XComponentContext      localContext          = uno.util.Bootstrap.bootstrap();
            XMultiComponentFactory multiComponentFactory = localContext.getServiceManager();
            XComponentLoader componentLoader = (XComponentLoader)multiComponentFactory.createInstanceWithContext(
                "com.sun.star.frame.Desktop", localContext);

            // 确保输出目录存在
            Directory.CreateDirectory(outputDirectory);

            // 先获取所有工作表名称（不修改原文件）
            PropertyValue[] loadProperties = new PropertyValue[1];
            loadProperties[0] = new PropertyValue { Name = "Hidden", Value = new uno.Any(true) };

            // 首次加载仅获取sheet名称
            XComponent tempDoc = componentLoader.loadComponentFromURL(
                "file:///" + zipPath.Replace("\\", "/"),
                "_blank", 0, loadProperties);
            XSpreadsheetDocument tempSpreadsheet = (XSpreadsheetDocument)tempDoc;
            string[]             sheetNames      = tempSpreadsheet.getSheets().getElementNames();
            tempDoc.dispose();

            // 处理每个工作表
            for (int i = 0; i < sheetNames.Length; i++)
            {
                // 每次处理都重新加载原始文件
                XComponent document = componentLoader.loadComponentFromURL(
                    "file:///" + zipPath.Replace("\\", "/"),
                    "_blank", 0, loadProperties);
                XSpreadsheetDocument spreadsheetDocument = (XSpreadsheetDocument)document;
                XSpreadsheets        sheets              = spreadsheetDocument.getSheets();

                try
                {
                    // 获取当前要保留的工作表名称
                    string targetSheetName = sheetNames[i];

                    // 逆向删除其他工作表（避免索引变化问题）
                    string[] allNames = sheets.getElementNames();
                    for (int j = allNames.Length - 1; j >= 0; j--)
                    {
                        if (allNames[j] != targetSheetName)
                        {
                            sheets.removeByName(allNames[j]);
                        }
                    }

                    // 构建输出路径
                    string safeName   = Regex.Replace(targetSheetName, @"[\\/:*?""<>|]", "_");
                    string outputPath = System.IO.Path.Combine(outputDirectory, $"{safeName}.xlsx");
                    string outputUrl  = "file:///" + outputPath.Replace("\\", "/");

                    // 保存单工作表文档
                    XStorable       storable        = (XStorable)document;
                    PropertyValue[] storeProperties = new PropertyValue[1];
                    storeProperties[0] = new PropertyValue
                    {
                        Name  = "FilterName",
                        Value = new uno.Any("Calc MS Excel 2007 XML")
                    };

                    storable.storeToURL(outputUrl, storeProperties);
                    Console.WriteLine($"已保存: {outputPath}");
                }
                finally
                {
                    // 确保文档被关闭
                    document.dispose();
                }
            }
        }


        public string disposeBox3(List<Dictionary<string, string>> list, string newFolderPath)
        {
            string baseDirectory      = AppDomain.CurrentDomain.BaseDirectory; //根目录
            string outBoxTemplatePath = System.IO.Path.Combine(baseDirectory, "UploadFiles", "demo.xlsx");
            //newFolderPath = "D:\\合众唯一\\南沙电装\\02代码\\DENSOKB\\DENSOKB\\Uploads";
            string outBoxPath = System.IO.Path.Combine(newFolderPath, "外箱 箱单标识.xlsx");
            string newOutBoxDic =
                System.IO.Path.Combine(System.IO.Path.GetDirectoryName(outBoxPath), "TemporaryOuterBoxLabel");
            Directory.CreateDirectory(newOutBoxDic);
            for (int i = 0; i < list.Count; i++)
            {
                string densoNum       = list[i]["densoNum"].Replace("-", ""); // 品番
                string part_name      = list[i]["part_name"];                 //品名
                int    box_cap        = int.Parse(list[i]["boxCap"]);
                int    boxNum         = int.Parse(list[i]["boxNum"]);         // 箱数
                int    aliquotBoxNum  = int.Parse(list[i]["aliquotBoxNum"]);  // 整箱数
                int    quantityOutBox = int.Parse(list[i]["quantityOutBox"]); // 端数
                string salesOrderNo   = list[i]["salesOrderNo"];              // 销售订单号
                string suppler        = list[i]["supplier"];                  // 供应商
                string shipment       = list[i]["shipment"];                  // 出货日期
                string orderNo        = list[i]["orderNo"];                   // 订单号

                for (int j = 1; j <= boxNum; j += 3)
                {
                    // 构建唯一的文件名
                    string uniqueFileName = $"{System.IO.Path.GetFileNameWithoutExtension(outBoxPath)}_{i}_{j}.xlsx";
                    string newOutBoxPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(outBoxPath),
                        "TemporaryOuterBoxLabel", uniqueFileName);

                    try
                    {
                        // 打开复制的文件进行修改
                        using (FileStream readStream =
                               new FileStream(outBoxTemplatePath, FileMode.Open, FileAccess.Read))
                        {
                            IWorkbook outBoxBook      = new XSSFWorkbook(readStream);
                            ISheet    outBoxBookSheet = outBoxBook.GetSheetAt(0);

                            for (int k = 0; k < 3; k++)
                            {
                                // 填充数据
                                IRow row5 = outBoxBookSheet.GetRow(5 + 20 * k) ?? outBoxBookSheet.CreateRow(5 + 20 * k);
                                IRow row6 = outBoxBookSheet.GetRow(6 + 20 * k) ?? outBoxBookSheet.CreateRow(6 + 20 * k);
                                IRow row7 = outBoxBookSheet.GetRow(7 + 20 * k) ?? outBoxBookSheet.CreateRow(7 + 20 * k);
                                IRow row11 = outBoxBookSheet.GetRow(10    + 20 * k) ??
                                             outBoxBookSheet.CreateRow(10 + 20 * k);

                                ICell cell2   = row5.GetCell(2)  ?? row5.CreateCell(2);
                                ICell cell7   = row5.GetCell(8)  ?? row5.CreateCell(8);
                                ICell cell62  = row6.GetCell(2)  ?? row6.CreateCell(2);
                                ICell cell72  = row7.GetCell(2)  ?? row7.CreateCell(2);
                                ICell cell74  = row7.GetCell(4)  ?? row7.CreateCell(4);
                                ICell cell77  = row7.GetCell(7)  ?? row7.CreateCell(7);
                                ICell cell111 = row11.GetCell(1) ?? row11.CreateCell(1); //品番
                                ICell cell113 = row11.GetCell(3) ?? row11.CreateCell(3); //品名
                                ICell cell117 = row11.GetCell(7) ?? row11.CreateCell(7); //数量

                                cell2.SetCellValue(suppler);      // 供应商
                                cell7.SetCellValue(salesOrderNo); // 销售订单号
                                cell62.SetCellValue(shipment);    // 出货日期
                                cell72.SetCellValue(orderNo);     // 订单号
                                cell74.SetCellValue(j + k);       // 当前是第几箱
                                cell77.SetCellValue(boxNum);      // 总箱数
                                cell111.SetCellValue(densoNum);
                                cell113.SetCellValue(part_name);
                                if (j + k == boxNum && quantityOutBox > 0)
                                {
                                    cell117.SetCellValue(quantityOutBox); //端数
                                    break;
                                }
                                else
                                {
                                    cell117.SetCellValue(box_cap); //收容数
                                }
                            }

                            // 保存修改后的文件到新文件流
                            using (FileStream writeStream =
                                   new FileStream(newOutBoxPath, FileMode.Create, FileAccess.Write))
                            {
                                outBoxBook.Write(writeStream);
                            }

                            outBoxBook.Close();
                        }

                        Console.WriteLine($"成功生成文件: {newOutBoxPath}");
                    }

                    catch (System.Exception ex)
                    {
                        Console.WriteLine($"处理文件 {newOutBoxPath} 时发生错误: {ex.Message}");
                    }
                }
            }

            return newOutBoxDic;
        }

        public string SetOfOutBoxPDF(string folderPath, List<inpdetail2> inpdetail2s)
        {
            //先判断folderPath路径是否存在
            if (!Directory.Exists(folderPath))
            {
                throw new System.Exception("路径不存在");
            }

            string[] filesname = Directory.GetFiles(folderPath, "*.pdf");
            //FileInfo[] files = filesname
            //                           .Select(filePath => new FileInfo(filePath))
            //                           .ToArray(); 
             FileInfo[] files = filesname
            .Select(filePath => new FileInfo(filePath))
            .OrderBy(file => {
                var match = Regex.Match(file.Name, @"_(\d+)_(\d+)");
                if (match.Success &&
                    int.TryParse(match.Groups[1].Value, out int firstNum) &&
                    int.TryParse(match.Groups[2].Value, out int secondNum))
                {
                    return (firstNum, secondNum);
                }
                return (0, 0);
            })
            .ToArray();
            string fileDic = System.IO.Path.GetDirectoryName(folderPath);
            string tempPDFPath = System.IO.Path.Combine(fileDic, "tempPDF");
            Directory.CreateDirectory(tempPDFPath);


            //裁切pdf并保存
            string scalePdfCutDic = "ScalePdf";
            string scalePdfCutPath = System.IO.Path.Combine(tempPDFPath, scalePdfCutDic);

            if (!Directory.Exists(scalePdfCutPath))
            {
                Directory.CreateDirectory(scalePdfCutPath);
            }




            string finalBoxBoardName = "小纸张.pdf"; //temp小纸张改为小纸张
            string finalBoxBoardDic = "final";
            string finalDic = System.IO.Path.Combine(fileDic, finalBoxBoardDic);
            if (!Directory.Exists(finalDic))
            {
                Directory.CreateDirectory(finalDic);
            }

            string finalBoxBoardPath = System.IO.Path.Combine(finalDic, finalBoxBoardName);
            //string boxBoardPath = MergeAndScaleBoxBoard(scalePdfCutPath, finalBoxBoardPath);
            SpireHelper.Merge(1, 1, files, finalBoxBoardPath, false, 0, 0);
            // if (!string.IsNullOrEmpty(boxBoardPath))
            // {
            //     inpdetail2 board = new inpdetail2();
            //     board.filetype   = "小纸张";
            //     board.Stacklabel = boxBoardPath;
            //     board.rowcode    = 3;
            //     inpdetail2s.Add(board);
            // }

            if (!string.IsNullOrEmpty(finalBoxBoardPath))
            {
                inpdetail2 board = new inpdetail2();
                board.filetype = "小纸张";
                board.Stacklabel = finalBoxBoardPath;
                board.rowcode = 3;
                inpdetail2s.Add(board);
            }

            string finalBoxA4BoardName = "A4穿透.pdf";
            string finalBoxA4BoardPath = System.IO.Path.Combine(fileDic, finalBoxBoardDic, finalBoxA4BoardName);

            var sorted = filesname
    .Select(file => new
    {
        FilePath = file,
        SerialNumber = System.IO.Path.GetFileNameWithoutExtension(file)
            .Split('_')
            .Where(part => int.TryParse(part, out _))
            .Take(2)
            .Select(int.Parse)
            .ToArray()
    })
    .Where(x => x.SerialNumber.Length == 2)
    .OrderBy(x => x.SerialNumber[0])
    .ThenBy(x => x.SerialNumber[1])
    .Select(x => x.FilePath)
    .ToList();

            SpireHelper.Merge(3, 1, files, finalBoxA4BoardPath, true, 0, 0);
            //MergeAndScaleBoxBoardA4(scalePdfCutPath, finalBoxA4BoardPath);
            //MergeAndScalePdf(scalePdfCutPath, finalBoxA4BoardPath);

            if (!string.IsNullOrEmpty(finalBoxA4BoardPath))
            {
                inpdetail2 A4board = new inpdetail2();
                A4board.filetype = "A4";
                A4board.rowcode = 4;
                A4board.Stacklabel = finalBoxA4BoardPath;
                inpdetail2s.Add(A4board);
            }

            return tempPDFPath;
        }


        public string SetOfOutBox(string folderPath, List<inpdetail2> inpdetail2s)
        {
            //先判断folderPath路径是否存在
            if (!Directory.Exists(folderPath))
            {
                throw new System.Exception("路径不存在");
            }

            string[] files       = Directory.GetFiles(folderPath, "*.xlsx*");
            string   fileDic     = System.IO.Path.GetDirectoryName(folderPath);
            string   tempPDFPath = System.IO.Path.Combine(fileDic, "tempPDF");
            Directory.CreateDirectory(tempPDFPath);
            foreach (var file in files)
            {
                string tempFileName = System.IO.Path.GetFileNameWithoutExtension(file) + ".pdf";
                string tempPdfName  = System.IO.Path.Combine(tempPDFPath, tempFileName);
                ExcelOutOfPdf(file, tempPdfName);
            }

            //裁切pdf并保存
            string scalePdfCutDic  = "ScalePdf";
            string scalePdfCutPath = System.IO.Path.Combine(fileDic, scalePdfCutDic);

            if (!Directory.Exists(scalePdfCutPath))
            {
                Directory.CreateDirectory(scalePdfCutPath);
            }

            RectangleF rectangleF = new RectangleF(0, 0, 348, 595);
            CutPdfAndSave(tempPDFPath, scalePdfCutPath);


            string finalBoxBoardName = "小纸张.pdf"; //temp小纸张改为小纸张
            string finalBoxBoardDic  = "final";
            string finalDic          = System.IO.Path.Combine(fileDic, finalBoxBoardDic);
            if (!Directory.Exists(finalDic))
            {
                Directory.CreateDirectory(finalDic);
            }

            string finalBoxBoardPath = System.IO.Path.Combine(finalDic, finalBoxBoardName);
            string boxBoardPath      = MergeAndScaleBoxBoard(scalePdfCutPath, finalBoxBoardPath);
            // if (!string.IsNullOrEmpty(boxBoardPath))
            // {
            //     inpdetail2 board = new inpdetail2();
            //     board.filetype   = "小纸张";
            //     board.Stacklabel = boxBoardPath;
            //     board.rowcode    = 3;
            //     inpdetail2s.Add(board);
            // }

            if (!string.IsNullOrEmpty(finalBoxBoardPath))
            {
                inpdetail2 board = new inpdetail2();
                board.filetype   = "小纸张";
                board.Stacklabel = finalBoxBoardPath;
                board.rowcode    = 3;
                inpdetail2s.Add(board);
            }

            string finalBoxA4BoardName = "A4穿透.pdf";
            string finalBoxA4BoardPath = System.IO.Path.Combine(fileDic, finalBoxBoardDic, finalBoxA4BoardName);
            MergeAndScaleBoxBoardA4(scalePdfCutPath, finalBoxA4BoardPath);
            //MergeAndScalePdf(scalePdfCutPath, finalBoxA4BoardPath);

            if (!string.IsNullOrEmpty(finalBoxA4BoardPath))
            {
                inpdetail2 A4board = new inpdetail2();
                A4board.filetype   = "A4";
                A4board.rowcode    = 4;
                A4board.Stacklabel = finalBoxA4BoardPath;
                inpdetail2s.Add(A4board);
            }

            return tempPDFPath;
        }

        //裁切并缩放

        /// <summary>
        /// 裁切 PDF 文件中的每一页，保留指定区域
        /// </summary>
        /// <param name="inputPdfFolderPath">包含 PDF 文件的文件夹路径</param>
        /// <param name="outputFolderPath">输出文件夹路径</param>
        /// <param name="cropRect">要保留的区域（单位是 point，1 inch = 72 point）</param>
        public void CutPdfAndSave(string inputPdfFolderPath, string outputFolderPath, RectangleF cropRect)
        {
            if (!Directory.Exists(outputFolderPath))
                Directory.CreateDirectory(outputFolderPath);

            string[] pdfFiles = Directory.GetFiles(inputPdfFolderPath, "*.pdf");
            foreach (string pdfFile in pdfFiles)
            {
                PdfDocument originalDoc = new PdfDocument();
                originalDoc.LoadFromFile(pdfFile);

                PdfDocument newDoc = new PdfDocument();

                for (int i = 0; i < originalDoc.Pages.Count; i++)
                {
                    var         originalPage = originalDoc.Pages[i];
                    SizeF       pageSize     = new SizeF(cropRect.Width, cropRect.Height);
                    PdfPageBase newPage      = newDoc.Pages.Add(pageSize);

                    // 在新页面上绘制指定区域
                    PdfTemplate template = originalPage.CreateTemplate();
                    newPage.Canvas.SetClip(cropRect); // 设置绘图剪裁区域
                    newPage.Canvas.DrawTemplate(template, new PointF(-cropRect.X, -cropRect.Y));
                }

                string fileName   = Path.GetFileName(pdfFile);
                string outputPath = Path.Combine(outputFolderPath, fileName);
                newDoc.SaveToFile(outputPath);
                Console.WriteLine($"已保存裁剪后的 PDF：{outputPath}");
            }
        }

        //裁切保存
        public void CutPdfAndSave(string inputPdfPath, string outputFolderPath)
        {
            try
            {
                string[] pdfFiles = Directory.GetFiles(inputPdfPath, "*.pdf");
                if (pdfFiles.Length == 0)
                {
                    Console.WriteLine("未找到任何 PDF 文件");
                    return;
                }

                foreach (var file in pdfFiles)
                {
                    string fileName       = System.IO.Path.GetFileName(file);
                    string outputFilePath = System.IO.Path.Combine(outputFolderPath, fileName); // 输出路径

                    using (iText.Kernel.Pdf.PdfReader reader = new iText.Kernel.Pdf.PdfReader(file))
                    using (iText.Kernel.Pdf.PdfWriter writer = new iText.Kernel.Pdf.PdfWriter(outputFilePath))
                    using (iText.Kernel.Pdf.PdfDocument pdfDoc = new iText.Kernel.Pdf.PdfDocument(reader, writer))
                    {
                        int pageCount = pdfDoc.GetNumberOfPages();

                        for (int i = 1; i <= pageCount; i++)
                        {
                            var                         page         = pdfDoc.GetPage(i);
                            iText.Kernel.Geom.Rectangle originalRect = page.GetPageSize();

                            iText.Kernel.Geom.Rectangle cropRect = new iText.Kernel.Geom.Rectangle(
                                0,
                                originalRect.GetHeight() * 0.6f,
                                originalRect.GetWidth(),
                                originalRect.GetHeight() * 0.4f
                            );

                            // 设置裁切区域
                            page.SetCropBox(cropRect);
                            page.SetMediaBox(cropRect);
                        }
                    }

                    Console.WriteLine($"PDF裁切完成并保存至: {outputFilePath}");
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("裁切 PDF 时发生错误: " + ex.Message);
            }
        }


        //A4穿透
        public void MergeAndScalePdf(string inputFolderPath, string outputPdfPath)
        {
            try
            {
                string[] inputFiles = Directory.GetFiles(inputFolderPath, "*.pdf");


                // A4 页面大小 (595x842)
                float a4Width       = 595f;
                float a4Height      = 842f;
                float sectionHeight = a4Height / 3; // 每个页面目标高度 (约 280.67 点)

                using (iText.Kernel.Pdf.PdfWriter writer = new iText.Kernel.Pdf.PdfWriter(outputPdfPath))
                using (iText.Kernel.Pdf.PdfDocument mergedPdf = new iText.Kernel.Pdf.PdfDocument(writer))
                {
                    int                               pageCounter = 0;
                    iText.Kernel.Pdf.PdfPage          newPage     = mergedPdf.AddNewPage(iText.Kernel.Geom.PageSize.A4);
                    iText.Kernel.Pdf.Canvas.PdfCanvas canvas      = new iText.Kernel.Pdf.Canvas.PdfCanvas(newPage);

                    for (int i = 0; i < inputFiles.Length; i++)
                    {
                        using (iText.Kernel.Pdf.PdfReader reader = new iText.Kernel.Pdf.PdfReader(inputFiles[i]))
                        using (iText.Kernel.Pdf.PdfDocument pdfDoc = new iText.Kernel.Pdf.PdfDocument(reader))
                        {
                            int pageCount = pdfDoc.GetNumberOfPages();

                            for (int j = 1; j <= pageCount; j++)
                            {
                                iText.Kernel.Pdf.PdfPage    page         = pdfDoc.GetPage(j);
                                iText.Kernel.Geom.Rectangle originalRect = page.GetPageSize();

                                // 计算缩放比例，使页面高度适应 A4 的三分之一
                                float scale       = sectionHeight           / originalRect.GetHeight();
                                float scaledWidth = originalRect.GetWidth() * scale;

                                // 计算水平居中偏移量
                                float xOffset = (a4Width - scaledWidth) / 2;

                                // 创建 PdfFormXObject 来包装页面内容
                                iText.Kernel.Pdf.Xobject.PdfFormXObject pageCopy = page.CopyAsFormXObject(mergedPdf);

                                // 计算垂直位置（从顶部开始，依次向下排列）
                                float yOffset = a4Height - ((pageCounter % 3) + 1) * sectionHeight;

                                // 应用缩放并绘制页面内容，水平居中
                                canvas.SaveState();
                                canvas.ConcatMatrix(scale, 0, 0, scale, xOffset, yOffset);
                                canvas.AddXObjectAt(pageCopy, 0, 0);
                                canvas.RestoreState();

                                // 每添加一个页面，就更新页面计数器
                                pageCounter++;

                                // 每当页面计数器达到 3 时，换到新的一页
                                if (pageCounter % 3 == 0)
                                {
                                    newPage = mergedPdf.AddNewPage(iText.Kernel.Geom.PageSize.A4);
                                    canvas  = new iText.Kernel.Pdf.Canvas.PdfCanvas(newPage);
                                }
                            }
                        }
                    }

                    // 设置新页面的 MediaBox 和 CropBox
                    newPage.SetMediaBox(new iText.Kernel.Geom.Rectangle(0, 0, a4Width, a4Height));
                    newPage.SetCropBox(new iText.Kernel.Geom.Rectangle(0, 0, a4Width, a4Height));
                }

                Console.WriteLine($"PDF拼接与缩放完成。输出文件: {outputPdfPath}");
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("拼接与缩放 PDF 时发生错误: " + ex.Message);
            }
        }

        public void MergeAndScaleBoxBoardA4(string inputFolderPath, string outputPdfPath)
        {
            try
            {
                string[] inputFiles = Directory.GetFiles(inputFolderPath, "*.pdf");

                var sorted = inputFiles
                    .Select(file => new
                    {
                        FilePath = file,
                        SerialNumber = System.IO.Path.GetFileNameWithoutExtension(file)
                            .Split('_')
                            .Where(part => int.TryParse(part, out _))
                            .Take(2)
                            .Select(int.Parse)
                            .ToArray()
                    })
                    .Where(x => x.SerialNumber.Length == 2)
                    .OrderBy(x => x.SerialNumber[0])
                    .ThenBy(x => x.SerialNumber[1])
                    .Select(x => x.FilePath)
                    .ToList();

                // A4 页面大小 (595x842)
                float a4Width       = 595f;
                float a4Height      = 842f;
                float sectionHeight = a4Height / 3f;

                using (var writer = new iText.Kernel.Pdf.PdfWriter(outputPdfPath))
                using (var mergedPdf = new iText.Kernel.Pdf.PdfDocument(writer))
                {
                    int tagsPerPage = 3;
                    int totalTags   = sorted.Count;
                    int totalPages  = (int)Math.Ceiling((double)totalTags / tagsPerPage);

                    for (int pageNum = 0; pageNum < totalPages; pageNum++)
                    {
                        var newPage = mergedPdf.AddNewPage(iText.Kernel.Geom.PageSize.A4);
                        var canvas  = new iText.Kernel.Pdf.Canvas.PdfCanvas(newPage);

                        for (int slot = 0; slot < tagsPerPage; slot++)
                        {
                            int tagIndex = pageNum + slot * totalPages;
                            if (tagIndex >= totalTags) break;

                            using (var reader = new iText.Kernel.Pdf.PdfReader(sorted[tagIndex]))
                            using (var pdfDoc = new iText.Kernel.Pdf.PdfDocument(reader))
                            {
                                var page         = pdfDoc.GetPage(1); // 默认取第一页
                                var originalRect = page.GetPageSize();

                                float scale       = sectionHeight           / originalRect.GetHeight();
                                float scaledWidth = originalRect.GetWidth() * scale;
                                float xOffset     = (a4Width - scaledWidth) / 2;
                                float yOffset     = a4Height - (slot + 1) * sectionHeight;

                                var pageCopy = page.CopyAsFormXObject(mergedPdf);

                                canvas.SaveState();
                                canvas.ConcatMatrix(scale, 0, 0, scale, xOffset, yOffset);
                                canvas.AddXObjectAt(pageCopy, 0, 0);
                                canvas.RestoreState();
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("拼接与缩放 PDF 时发生错误: " + ex.Message);
            }
        }


        //拼接小纸张
        public string MergeAndScaleBoxBoard(string inputFolderPath, string outputPdfPath)
        {
            try
            {
                string[] inputFiles = Directory.GetFiles(inputFolderPath, "*.pdf");
                var sortedPdfFiles = inputFiles
                    .Select(file => new
                    {
                        FilePath = file,
                        // 使用正则表达式提取文件名中的数字部分（后缀 _x_x）
                        SerialNumber = System.IO.Path.GetFileNameWithoutExtension(file)
                            .Split('_')                               // 将文件名按 '_' 分割
                            .Where(part => int.TryParse(part, out _)) // 确保分割后的部分是有效的数字
                            .Take(2)                                  // 只取后缀部分的两个数字
                            .Select(n => int.Parse(n))                // 将这两个部分转换为整数
                            .ToArray()                                // 转换为整数数组
                    })
                    .Where(x => x.SerialNumber.Length == 2) // 确保每个文件至少有两个数字部分
                    .OrderBy(x => x.SerialNumber[0])        // 按第一个数字排序
                    .ThenBy(x => x.SerialNumber[1])         // 如果第一个数字相同，再按第二个数字排序
                    .Select(x => x.FilePath)
                    .ToArray();

                using (iText.Kernel.Pdf.PdfWriter writer = new iText.Kernel.Pdf.PdfWriter(outputPdfPath))
                using (iText.Kernel.Pdf.PdfDocument mergedPdf = new iText.Kernel.Pdf.PdfDocument(writer))
                {
                    foreach (var inputFile in sortedPdfFiles)
                    {
                        using (iText.Kernel.Pdf.PdfReader reader = new iText.Kernel.Pdf.PdfReader(inputFile))
                        using (iText.Kernel.Pdf.PdfDocument pdfDoc = new iText.Kernel.Pdf.PdfDocument(reader))
                        {
                            int pageCount = pdfDoc.GetNumberOfPages();

                            for (int i = 1; i <= pageCount; i++)
                            {
                                var                         page         = pdfDoc.GetPage(i);
                                iText.Kernel.Geom.Rectangle originalRect = page.GetPageSize();

                                // 将裁切后的页面添加到合并文档
                                pdfDoc.CopyPagesTo(i, i, mergedPdf);
                            }
                        }
                    }
                }

                //
                // string finalBoxBoardName = "小纸张2.pdf";
                // string finalBoxBoardDic = System.IO.Path.GetDirectoryName(outputPdfPath);
                // string finalBoxBoardPath = System.IO.Path.Combine(finalBoxBoardDic, finalBoxBoardName);
                // ScalePdf(outputPdfPath, finalBoxBoardPath);
                // return finalBoxBoardPath;
                return outputPdfPath;
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("拼接与缩放 PDF 时发生错误: " + ex.Message);
                throw;
            }
        }

        //pdf 缩放
        public void ScalePdf(string inputFilePath, string outputFilePath)
        {
            // 读取输入的PDF文件
            PdfReader reader = new PdfReader(inputFilePath);

            // 创建一个新的文件输出流
            using (FileStream fs = new FileStream(outputFilePath, FileMode.Create))
            {
                // 创建PDF文档对象
                Document document = new Document();

                // 创建PdfWriter实例，将输出流传递给它
                PdfWriter writer = PdfWriter.GetInstance(document, fs);

                // 打开文档进行写入
                document.Open();

                // 获取A4纸的高度
                float a4Height = iTextSharp.text.PageSize.A4.Height;

                // 计算目标高度为A4的三分之一
                float targetHeight = a4Height / 3;

                // 遍历所有的页面
                for (int i = 1; i <= reader.NumberOfPages; i++)
                {
                    // 获取当前页面
                    PdfImportedPage page = writer.GetImportedPage(reader, i);

                    // 计算缩放比例
                    float scale = targetHeight / reader.GetPageSize(i).Height;

                    // 设置目标页面大小
                    document.SetPageSize(new iTextSharp.text.Rectangle(reader.GetPageSize(i).Width * scale,
                        targetHeight));

                    // 开始一个新页面并将当前页面内容缩放绘制进去
                    document.NewPage();

                    PdfContentByte cb = writer.DirectContent;
                    cb.AddTemplate(page, scale, 0, 0, scale, 0, 0);
                }

                // 关闭文档
                document.Close();
            }

            Console.WriteLine("PDF scaling complete. Output file: " + outputFilePath);
        }


        /// <summary>
        ///  公共Excel转PDF方法
        /// </summary>
        /// <param name="zipPath">原始文件路径带文件名+扩展名</param>
        /// <param name="fileName">原始文件名称不带扩展名</param>
        public string ExcelOutPdf(string zipPath, string fileName)
        {
            string outPath  = "";
            string basePath = System.IO.Path.GetDirectoryName(zipPath); // 临时文件路径
            string libreOfficePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libreoffice",
                "program",
                "soffice.exe");
            XErrorLogToolMDM.WriteLog($"LibreOffice 路径: {libreOfficePath}");

            // 临时文件夹用于存放整个 Excel 转换后的 PDF
            string tempFolder = System.IO.Path.Combine(basePath, "TempExcelSheets"); //临时文件名称
            Directory.CreateDirectory(tempFolder);

            // 将整个 Excel 转换为 PDF
            string tempPdfPath = System.IO.Path.Combine(tempFolder, $"{fileName}.pdf");
            //string arguments = $"--headless --convert-to pdf \"{zipPath}\" --outdir \"{tempFolder}\"";
            string arguments =
                $"--headless --convert-to pdf:calc_pdf_Export:{{\"FitToPagesWidth\":\"1\", \"FitToPagesHeight\":\"1\"}} \"{zipPath}\" --outdir \"{tempFolder}\"";
            XErrorLogToolMDM.WriteLog($"命令行: {arguments}");

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName               = libreOfficePath,
                Arguments              = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            };
            XErrorLogToolMDM.WriteLog("开始调用");
            using (Process process = new Process { StartInfo = psi })
            {
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error  = process.StandardError.ReadToEnd();
                process.WaitForExit();
                XErrorLogToolMDM.WriteLog($"转换完成");
                if (!File.Exists(tempPdfPath))
                {
                    Console.WriteLine($"Excel 转换为 PDF 失败: {error}");
                    return "";
                }

                outPath = tempPdfPath;
            }

            return outPath;
        }

        public bool ExcelOutOfPdf_H(string inputFile, string outputFile)
        {
            bool isok = true;
            XErrorLogToolMDM.WriteLog("生成确认表pdf");
            XErrorLogToolMDM.WriteLog("链接libreoffice");
            // 连接到 LibreOffice
            XComponentContext      localContext          = uno.util.Bootstrap.bootstrap();
            XMultiComponentFactory multiComponentFactory = localContext.getServiceManager();
            XComponentLoader componentLoader =
                (XComponentLoader)multiComponentFactory.createInstanceWithContext("com.sun.star.frame.Desktop",
                    localContext);

            // 打开 Excel 文件
            PropertyValue[] loadProps = new PropertyValue[1];
            loadProps[0]       = new PropertyValue();
            loadProps[0].Name  = "Hidden";
            loadProps[0].Value = new uno.Any(true);

            XComponent xComponent = componentLoader.loadComponentFromURL(
                "file:///" + inputFile.Replace("\\", "/"), "_blank", 0, loadProps);
            try
            {
                // 获取当前控制器
                XController xController = ((XModel)xComponent).getCurrentController();

                // 设置页面方向为纵向
                PropertyValue[] pageProps = new PropertyValue[7];
                // 方向 0 纵向
                pageProps[0] = new PropertyValue { Name = "PrintPageOrientation", Value = new uno.Any((short)0) };
                // 缩放至一页
                pageProps[1] = new PropertyValue { Name = "FitToPagesWidth", Value  = new uno.Any((short)1) };
                pageProps[2] = new PropertyValue { Name = "FitToPagesHeight", Value = new uno.Any((short)1) };
                //// 禁用自动缩放
                //pageProps[3] = new PropertyValue { Name = "ScaleToPages", Value = new uno.Any((short)1) };
                // 页边距（单位：1/100毫米）
                pageProps[4] = new PropertyValue { Name = "LeftMargin", Value  = new uno.Any(1000) }; // 1cm
                pageProps[5] = new PropertyValue { Name = "RightMargin", Value = new uno.Any(500) };  // 0.5cm
                pageProps[6] = new PropertyValue { Name = "TopMargin", Value   = new uno.Any(0) };    // 0.5cm
                // 应用页面设置
                XPrintable xPrintable = (XPrintable)xController.getModel();
                xPrintable.setPrinter(pageProps);

                // 导出为PDF
                PropertyValue[] exportProps = new PropertyValue[1];
                exportProps[0]       = new PropertyValue();
                exportProps[0].Name  = "FilterName";
                exportProps[0].Value = new uno.Any("calc_pdf_Export");

                XStorable xStorable = (XStorable)xComponent;
                xStorable.storeToURL("file:///" + outputFile.Replace("\\", "/"), exportProps);
                XErrorLogToolMDM.WriteLog("生成确认表pdf完成");
            }
            catch (unoidl.com.sun.star.uno.Exception ex)
            {
                isok = false;
            }
            finally
            {
                // 关闭文档
                XCloseable xCloseable = (XCloseable)xComponent;
                xCloseable.close(false);
            }

            return isok;
        }

        //没法控制页面缩放
        public void ExcelOutPdf2(string zipPath, string fileName)
        {
            string basePath = System.IO.Path.GetDirectoryName(zipPath); // 临时文件路径
            string libreOfficePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libreoffice",
                "program", "soffice.exe");
            XErrorLogToolMDM.WriteLog($"LibreOffice 路径: {libreOfficePath}");

            // 临时文件夹用于存放整个 Excel 转换后的 PDF
            string tempFolder = System.IO.Path.Combine(basePath, "TempExcelSheets");
            Directory.CreateDirectory(tempFolder);

            // 将整个 Excel 转换为 PDF
            string tempPdfPath = System.IO.Path.Combine(tempFolder, $"{fileName}.pdf");
            // 设置页面方向为竖状 (Portrait) 和整页匹配
            string arguments =
                $"--headless --convert-to pdf:calc_pdf_Export:{{\"PageOrientation\":\"Portrait\",\"FitToPage\":true}} \"{zipPath}\" --outdir \"{tempFolder}\"";
            XErrorLogToolMDM.WriteLog($"命令行: {arguments}");

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName               = libreOfficePath,
                Arguments              = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            };
            XErrorLogToolMDM.WriteLog("开始调用");
            using (Process process = new Process { StartInfo = psi })
            {
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error  = process.StandardError.ReadToEnd();
                process.WaitForExit();
                XErrorLogToolMDM.WriteLog($"转换完成");
                if (!File.Exists(tempPdfPath))
                {
                    Console.WriteLine($"Excel 转换为 PDF 失败: {error}");
                    return;
                }
            }
        }

        public static void ExcelOutOfPdf(string inputFile, string outputFile)
        {
            // 连接到 LibreOffice
            XComponentContext      localContext          = uno.util.Bootstrap.bootstrap();
            XMultiComponentFactory multiComponentFactory = localContext.getServiceManager();
            XComponentLoader componentLoader =
                (XComponentLoader)multiComponentFactory.createInstanceWithContext("com.sun.star.frame.Desktop",
                    localContext);

            // 打开 Excel 文件
            PropertyValue[] loadProps = new PropertyValue[1];
            loadProps[0]       = new PropertyValue();
            loadProps[0].Name  = "Hidden";
            loadProps[0].Value = new uno.Any(true);

            XComponent xComponent = componentLoader.loadComponentFromURL(
                "file:///" + inputFile.Replace("\\", "/"), "_blank", 0, loadProps);

            // 获取当前控制器
            XController xController = ((XModel)xComponent).getCurrentController();

            // 设置页面方向为纵向
            PropertyValue[] pageProps = new PropertyValue[4];
            pageProps[0]       = new PropertyValue();
            pageProps[0].Name  = "PrintPageOrientation";
            pageProps[0].Value = new uno.Any((short)0); // 0=纵向, 1=横向


            // 2. 禁用自动缩放
            pageProps[1]       = new PropertyValue();
            pageProps[1].Name  = "ScaleToPages";
            pageProps[1].Value = new uno.Any((short)1); // 0=禁用


            // 4. 设置页边距（可选）
            pageProps[3]       = new PropertyValue();
            pageProps[3].Name  = "LeftMargin";
            pageProps[3].Value = new uno.Any(1000); // 1厘米（单位：1/100毫米）

            // 应用页面设置
            XPrintable xPrintable = (XPrintable)xController.getModel();
            xPrintable.setPrinter(pageProps);

            // 导出为PDF
            PropertyValue[] exportProps = new PropertyValue[1];
            exportProps[0]       = new PropertyValue();
            exportProps[0].Name  = "FilterName";
            exportProps[0].Value = new uno.Any("calc_pdf_Export");

            XStorable xStorable = (XStorable)xComponent;
            xStorable.storeToURL("file:///" + outputFile.Replace("\\", "/"), exportProps);

            // 关闭文档
            XCloseable xCloseable = (XCloseable)xComponent;
            xCloseable.close(false);
        }


        public void StackLabel(List<Dictionary<string, string>> originalunit, string stackzipPath, string newFolderPath,List<inpdetail2> inpdetail2s)
        {
            decimal totalStackNum = 0;
            originalunit.ForEach(x =>
            {
                decimal stackNum = decimal.Parse(x["stackNum"]);
                totalStackNum += stackNum;
            });

            // 供应商代码
            string supplierCode = originalunit[0]["supplierCode"];
            // 供应商
            string supplier = originalunit[0]["supplier"];
            // 销售订单号
            string salesOrderNo = originalunit[0]["salesOrderNo"];
            // 采购订单号
            string  orderNo         = originalunit[0]["orderNo"];
            decimal cellingStackNum = Math.Ceiling(totalStackNum); // 总垛数

            string newOutDic = System.IO.Path.Combine(newFolderPath, "stack");
            Directory.CreateDirectory(newOutDic);

            // 为每个货垛生成新的文件
            for (int i = 1; i <= cellingStackNum; i++)
            {
                string newFileName = $"货垛标签_{i}.xlsx";
                string newOutPath  = System.IO.Path.Combine(newOutDic, newFileName);

                try
                {
                    // 复制模板文件到新的文件路径
                    File.Copy(stackzipPath, newOutPath, overwrite: true);

                    // 打开新复制的文件进行修改
                    using (FileStream readStream = new FileStream(newOutPath, FileMode.Open, FileAccess.Read))
                    {
                        IWorkbook workbook = new XSSFWorkbook(readStream);
                        ISheet    sheet    = workbook.GetSheetAt(0);

                        // 填充数据
                        IRow  rowSupplierCode  = sheet.GetRow(4)            ?? sheet.CreateRow(4);
                        ICell cellSupplierCode = rowSupplierCode.GetCell(1) ?? rowSupplierCode.CreateCell(1);
                        cellSupplierCode.SetCellValue(supplierCode); // 供应商代码

                        IRow  rowSupplier  = sheet.GetRow(6)        ?? sheet.CreateRow(6);
                        ICell cellSupplier = rowSupplier.GetCell(1) ?? rowSupplier.CreateCell(1);
                        cellSupplier.SetCellValue(supplier); // 供应商名

                        IRow  rowSalesOrderNo  = sheet.GetRow(9)            ?? sheet.CreateRow(9);
                        ICell cellSalesOrderNo = rowSalesOrderNo.GetCell(1) ?? rowSalesOrderNo.CreateCell(1);
                        cellSalesOrderNo.SetCellValue(salesOrderNo); // 销售订单号

                        IRow  rowOrderNo  = sheet.GetRow(11)      ?? sheet.CreateRow(11);
                        ICell cellOrderNo = rowOrderNo.GetCell(1) ?? rowOrderNo.CreateCell(1);
                        cellOrderNo.SetCellValue(orderNo); // 采购订单号

                        IRow  rowStockIndex  = sheet.GetRow(14)         ?? sheet.CreateRow(14);
                        ICell cellStockIndex = rowStockIndex.GetCell(1) ?? rowStockIndex.CreateCell(1);
                        cellStockIndex.SetCellValue(i); // 填充当前货垛索引

                        IRow  rowTotalStockNum  = sheet.GetRow(14)             ?? sheet.CreateRow(14);
                        ICell cellTotalStockNum = rowTotalStockNum.GetCell(20) ?? rowTotalStockNum.CreateCell(20);
                        cellTotalStockNum.SetCellValue(cellingStackNum.ToString()); // 填充总垛数

                        // 保存修改后的文件
                        using (FileStream writeStream = new FileStream(newOutPath, FileMode.Create, FileAccess.Write))
                        {
                            workbook.Write(writeStream);
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Console.WriteLine($"Error processing file {newFileName}: {ex.Message}");
                }
            }

            //将货垛标签中的excel文件转pdf
            string[] pdfFiles = Directory.GetFiles(newOutDic, "*.xlsx");
            pdfFiles.ToList().ForEach(x =>
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(x);
                string pdfPath  = System.IO.Path.Combine(newOutDic, $"{fileName}.pdf");
                ExcelOutOfPdf(x, pdfPath);
            });
            string stackPdfPath = System.IO.Path.Combine(newFolderPath, "码垛单.pdf");

            inpdetail2 inpdetail2 = new inpdetail2();
            if (!string.IsNullOrEmpty(stackPdfPath))
            {
                inpdetail2.filetype   = "码垛单";
                inpdetail2.Stacklabel = stackPdfPath;
                inpdetail2.rowcode    = 2;
                inpdetail2s.Add(inpdetail2);
            }

            MergePdfFiles(newOutDic, stackPdfPath);
        }


        public void MergePdfFiles(string pdfPath, string outputFilePath)
        {
            // 创建一个 PdfWriter 用于输出 PDF 文件
            using (iText.Kernel.Pdf.PdfWriter writer = new iText.Kernel.Pdf.PdfWriter(outputFilePath))
            {
                // 创建一个 PdfDocument 用于合并文件
                using (iText.Kernel.Pdf.PdfDocument pdfDoc = new iText.Kernel.Pdf.PdfDocument(writer))
                {
                    // 获取 pdfPath 下的所有 PDF 文件
                    string[] pdfFiles = Directory.GetFiles(pdfPath, "*.pdf");

                    // 遍历每个 PDF 文件
                    foreach (var pdfFile in pdfFiles)
                    {
                        // 打开每个 PDF 文件
                        using (iText.Kernel.Pdf.PdfDocument srcDoc =
                               new iText.Kernel.Pdf.PdfDocument(new iText.Kernel.Pdf.PdfReader(pdfFile)))
                        {
                            // 将源 PDF 的页面复制到目标 PDF
                            srcDoc.CopyPagesTo(1, srcDoc.GetNumberOfPages(), pdfDoc);
                        }
                    }
                }
            }

            Console.WriteLine("PDF 合并完成！");
        }

        public void ExcelOutOfPdf1(string inputFile, string outputFile)
        {
            // 连接到 LibreOffice
            XErrorLogToolMDM.WriteLog("---- 准备Excel转PDF---------");
            XComponentContext localContext = null;
            try
            {
                localContext = uno.util.Bootstrap.bootstrap();
            }
            catch (System.Exception ex)
            {
                XErrorLogToolMDM.WriteLog($"Bootstrap失败: {ex.ToString()}");
            }

            XMultiComponentFactory multiComponentFactory = localContext.getServiceManager();
            XComponentLoader componentLoader =
                (XComponentLoader)multiComponentFactory.createInstanceWithContext("com.sun.star.frame.Desktop",
                    localContext);
            try
            {
                XErrorLogToolMDM.WriteLog("---- 准备Excel文件---------");
                // 打开 Excel 文件
                PropertyValue[] loadProps = new PropertyValue[1];
                loadProps[0]       = new PropertyValue();
                loadProps[0].Name  = "Hidden";
                loadProps[0].Value = new uno.Any(true);
                XErrorLogToolMDM.WriteLog("---- 准备文件地址" + inputFile);
                XComponent xComponent = componentLoader.loadComponentFromURL(
                    "file:///" + inputFile.Replace("\\", "/"), "_blank", 0, loadProps);
                XErrorLogToolMDM.WriteLog("---- 打开文件" + inputFile);
                // 获取当前控制器
                XController xController = ((XModel)xComponent).getCurrentController();

                // 设置页面方向为纵向
                PropertyValue[] pageProps = new PropertyValue[4];
                pageProps[0]       = new PropertyValue();
                pageProps[0].Name  = "PrintPageOrientation";
                pageProps[0].Value = new uno.Any((short)1); // 0=纵向, 1=横向

                // 设置整页匹配
                pageProps[1]       = new PropertyValue();
                pageProps[1].Name  = "ScaleToPages";
                pageProps[1].Value = new uno.Any((short)1); // 1页宽

                pageProps[2]       = new PropertyValue();
                pageProps[2].Name  = "ScaleToPages";
                pageProps[2].Value = new uno.Any((short)1); // 1页高

                // 2. 禁用自动缩放
                pageProps[1]       = new PropertyValue();
                pageProps[1].Name  = "ScaleToPages";
                pageProps[1].Value = new uno.Any((short)1); // 0=禁用

                // 3. 设置手动缩放比例
                // pageProps[2] = new PropertyValue();
                // pageProps[2].Name = "ScaleToPages";
                // pageProps[2].Value = new uno.Any((short)50); // 缩放百分比

                // 4. 设置页边距（可选）
                pageProps[3]       = new PropertyValue();
                pageProps[3].Name  = "LeftMargin";
                pageProps[3].Value = new uno.Any(1000); // 1厘米（单位：1/100毫米）

                // 应用页面设置
                XPrintable xPrintable = (XPrintable)xController.getModel();
                xPrintable.setPrinter(pageProps);

                // 导出为PDF
                PropertyValue[] exportProps = new PropertyValue[1];
                exportProps[0]       = new PropertyValue();
                exportProps[0].Name  = "FilterName";
                exportProps[0].Value = new uno.Any("calc_pdf_Export");

                XStorable xStorable = (XStorable)xComponent;
                xStorable.storeToURL("file:///"     + outputFile.Replace("\\", "/"), exportProps);
                XErrorLogToolMDM.WriteLog("---- 输出" + outputFile);
                // 关闭文档
                XCloseable xCloseable = (XCloseable)xComponent;
                xCloseable.close(false);
            }
            catch (unoidl.com.sun.star.uno.Exception e)
            {
                XErrorLogToolMDM.WriteLog("---- 准备Excel转PDF失败：" + e.Message);
                throw e;
            }
        }

        public static void ExcelOutOfPdf(
            string  inputFile,
            string  outputFile,
            uno.Any direction,      // 0 = 纵向, 1 = 横向
            uno.Any margin,         // 页边距（单位：1/100 毫米）
            uno.Any autoscaling,    // 是否启用自动缩放（0 = 否）
            uno.Any scalingPercent, // 缩放百分比（如 50）
            uno.Any pageWidth) // 页面宽度（页数，如 1）
        {
            // 1. 初始化 LibreOffice 上下文
            XComponentContext      localContext          = uno.util.Bootstrap.bootstrap();
            XMultiComponentFactory multiComponentFactory = localContext.getServiceManager();
            XComponentLoader componentLoader = (XComponentLoader)multiComponentFactory
                .createInstanceWithContext("com.sun.star.frame.Desktop", localContext);

            // 2. 加载 Excel 文件（隐藏模式）
            PropertyValue[] loadProps = new PropertyValue[1];
            loadProps[0] = new PropertyValue { Name = "Hidden", Value = new uno.Any(true) };

            XComponent xComponent = componentLoader.loadComponentFromURL(
                "file:///" + inputFile.Replace("\\", "/"), "_blank", 0, loadProps);

            XController xController = ((XModel)xComponent).getCurrentController();

            // 3. 构建打印属性
            var pagePropsList = new List<PropertyValue>();

            // 设置页面方向
            pagePropsList.Add(new PropertyValue
            {
                Name  = "PrintPageOrientation",
                Value = direction
            });

            // 设置边距
            pagePropsList.Add(new PropertyValue
            {
                Name  = "LeftMargin",
                Value = margin
            });

            // 4. 处理缩放配置
            if (autoscaling.Equals(new uno.Any((short)0))) // 禁用自动缩放
            {
                // 设置页面宽度为 1 页
                pagePropsList.Add(new PropertyValue
                {
                    Name  = "ScaleToPagesX",
                    Value = pageWidth
                });

                // 高度不限（或可设为1页高）
                pagePropsList.Add(new PropertyValue
                {
                    Name  = "ScaleToPagesY",
                    Value = new uno.Any((short)0)
                });

                // 指定缩放比例（如 50%）
                pagePropsList.Add(new PropertyValue
                {
                    Name  = "ScaleFactor",
                    Value = scalingPercent
                });
            }

            // 应用打印设置
            XPrintable xPrintable = (XPrintable)xController.getModel();
            xPrintable.setPrinter(pagePropsList.ToArray());

            // 5. 导出为 PDF
            PropertyValue[] exportProps = new PropertyValue[1];
            exportProps[0] = new PropertyValue
            {
                Name  = "FilterName",
                Value = new uno.Any("calc_pdf_Export")
            };

            XStorable xStorable = (XStorable)xComponent;
            xStorable.storeToURL("file:///" + outputFile.Replace("\\", "/"), exportProps);

            // 6. 关闭文档
            XCloseable xCloseable = (XCloseable)xComponent;
            xCloseable.close(false);
        }
    }
}