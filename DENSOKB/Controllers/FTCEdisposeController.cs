using DENSOKB.Helper;
using DENSOKB.Libs;
using DENSOKB.Models;
using HelpLog.CommomMethod;
using iText.Layout.Element;
using iTextSharp.text.pdf;
using NPOI.HSSF.UserModel;
using NPOI.HSSF.Util;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Org.BouncyCastle.Asn1.X9;
using SharpCompress.Archives;
//using Spire.Additions.Xps.Schema;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Http;
using Path = System.IO.Path;

namespace DENSOKB.Controllers
{
    public class FTCEdisposeController: ApiController
    {
        
        orderimportService orderservice = new orderimportService();
        
        private LiberOfficeHelper liberOfficeHelper = new LiberOfficeHelper();
        

        #region FTCE订单处理，上传的是RAR格式

        /// <summary>
        /// 处理ftce 量产
        /// </summary>
        /// <param name="ftceDto"></param>
        /// <returns></returns>
        [HttpPost]
        public ResParameter CompressedFileFTCE(FtceDto ftceDto)
        {
            try
            {
                // 检查 zipPath 是否有效
                if (string.IsNullOrEmpty(ftceDto.zipPath) || !File.Exists(ftceDto.zipPath))
                {
                    return ResParameter.Fail("未找到zip文件");
                }
                
                string                  orderCode  = "";
                string                  fDate      = "";
                string                  bundleList = "";
                Dictionary<string, int> boardCount = null;
                List<OrderInfo>         infos      = null;

                using (var fileStream = new FileStream(ftceDto.zipPath, FileMode.Open, FileAccess.Read))
                {
                    List<FtceDetail> detail            = new List<FtceDetail>(); //创建集合用于后面储存地址
                    List<string>     bundleFolderPaths = null;
                    List<string>     tempFiles         = new List<string>();
                    //初始压缩包名称
                    string zipName = Path.GetFileNameWithoutExtension(ftceDto.zipPath);
                    orderCode = zipName;
                    //原始压缩包路径 FTCE
                    string zipDic = Path.GetDirectoryName(ftceDto.zipPath);
                    //string basePath = Path.Combine(zipDic, zipName);
                    //在原始路径下创建压缩包同名文件夹
                    string extractPath = Path.Combine(ftceDto.basePath,"FTCE", zipName);
                    Directory.CreateDirectory(extractPath);

                    
                    //解压
                    using (var archive = SharpCompress.Archives.Rar.RarArchive.Open(fileStream))
                    {
                        foreach (var entry in archive.Entries.Where(x => !x.IsDirectory))
                        {
                            var filePath = Path.Combine(extractPath, entry.Key);
                            entry.WriteToFile(filePath);
                            tempFiles.Add(filePath);
                        }
                    }

                    // 先处理所有捆包List文件
                    foreach (var tempFile in tempFiles.Where(f =>
                                 (Path.GetExtension(f).ToLower() == ".xlsx" ||
                                  Path.GetExtension(f).ToLower() == ".xls") &&
                                 Path.GetFileNameWithoutExtension(f).EndsWith("捆包List")))
                    {
                        try
                        {
                            bundleList = tempFile;
                            XErrorLogToolMDM.WriteLog("开始处理捆包"+DateTime.Now);

                            //创建一个字典   key 订单号\分1     value 看板张数  从第六行第六列开始读 
                            boardCount = GetBoardCount(tempFile);
                            bundleFolderPaths = boardCount.Keys.ToList();


                            //返回的是捆包中的捆包中订单号集合 112202 112203
                            CreateBundleFoldersFromExcel(tempFile, extractPath);


                            infos =  ReadList(tempFile);



                            //捆包excel转pdf
                            string baleFileName = Path.GetFileNameWithoutExtension(tempFile);
                            string balePdfDic = Path.GetDirectoryName(tempFile);
                            string balePdfPath = Path.Combine(balePdfDic, $"{baleFileName}.pdf");
                            XErrorLogToolMDM.WriteLog("捆包EXCEL转pdf开始"+DateTime.Now);
                            LiberOfficeHelper.ExcelOutOfPdf(tempFile, balePdfPath);
                            XErrorLogToolMDM.WriteLog("捆包EXCEL转pdf结束"+DateTime.Now);
                            //有几个订单号创建几个明细对象 后面插数据库用
                            bundleFolderPaths.ForEach(orerCode =>
                            {
                                FtceDetail ftceDetail = new FtceDetail();
                                ftceDetail.orderCode = orerCode;
                                ftceDetail.mainId    = ftceDto.mainId;
                                ftceDetail.bale      = balePdfPath;
                                detail.Add(ftceDetail);
                            });
                        }
                        catch (Exception ex)
                        {
                            Directory.Delete(extractPath, true);
                            return ResParameter.Fail(ex.Message);
                        }
                    }
                    XErrorLogToolMDM.WriteLog(bundleFolderPaths.ToString());
                    //根据捆包创建文件夹 拆分货垛标签
                    if (bundleFolderPaths != null)
                    {
                        foreach (var tempFile in tempFiles)
                        {
                            var ext = Path.GetExtension(tempFile).ToLower();
                            XErrorLogToolMDM.WriteLog("临时文件名称" + tempFile);
                            if (ext == ".xlsx" || ext == ".xls" &&
                                Path.GetFileNameWithoutExtension(tempFile).EndsWith("货垛标签"))
                            {
                                XErrorLogToolMDM.WriteLog("开始处理货垛标签"+DateTime.Now);
                                try
                                {
                                    XErrorLogToolMDM.WriteLog("处理货垛标签excel转pdf"+DateTime.Now);
                                    List<string> pdfFiles = liberOfficeHelper.ExcelLabelSheetSplitter(tempFile, bundleFolderPaths,extractPath);
                                    XErrorLogToolMDM.WriteLog("货垛标签转pdf结束"+DateTime.Now);
                                    XErrorLogToolMDM.WriteLog("detail"+ detail.Count);
                                    XErrorLogToolMDM.WriteLog("pdfFiles" + pdfFiles.Count);
                                    //保存货垛标签pdf路径到明细
                                    for (int i = 0; i < detail.Count; i++)
                                    {
                                        pdfFiles.ForEach(x =>
                                        {
                                            
                                            string stacklabel = Path.GetFileNameWithoutExtension(x);
                                            XErrorLogToolMDM.WriteLog(detail[i].orderCode.Replace("_", "") + "============" + stacklabel);
                                            //if (stacklabel.IndexOf(detail[i].orderCode.Replace("_","")) > -1)
                                            XErrorLogToolMDM.WriteLog("stacklabel");
                                            if (stacklabel == detail[i].orderCode.Replace("_", "")) 
                                            {
                                                XErrorLogToolMDM.WriteLog(detail[i].orderCode.Replace("_", "") + "匹配成功" + stacklabel);
                                                detail[i].Stacklabel = x;
                                            }
                                        });
                                    }
                                }
                                catch (Exception ex)
                                {
                                    //Directory.Delete(extractPath, true);
                                    return ResParameter.Fail(ex.Message);
                                }
                            }
                        }
                    }
                    XErrorLogToolMDM.WriteLog("处理huoduobiaoqian文件完成");
                    for (int i = 0; i < tempFiles.Count; i++)
                    {
                        XErrorLogToolMDM.WriteLog(tempFiles[i]);
                    }
                    //然后处理看板文件 
                    foreach (var tempFile in tempFiles.Where(f =>
                                 (Path.GetExtension(f).ToLower() == ".xlsx" ||
                                  Path.GetExtension(f).ToLower() == ".xls") &&
                                 Path.GetFileNameWithoutExtension(f).EndsWith("看板")))
                    {
                        try
                        {
                            XErrorLogToolMDM.WriteLog("开始处理看板"+DateTime.Now);
                            //存放看板文件的路径
                            string boardPdfDic = Path.GetDirectoryName(tempFile);
                            string boardPdfName = Path.GetFileNameWithoutExtension(tempFile);
                            string boardPdfPath = Path.Combine(boardPdfDic, $"{boardPdfName}.pdf");
                            XErrorLogToolMDM.WriteLog("看板64rows开始"+DateTime.Now);
                            //构建插入分页符后的excel路径
                            string board64Row = Path.Combine(boardPdfDic, $"{boardPdfName}64Row.xls");
                            XErrorLogToolMDM.WriteLog("看板64rows结束"+DateTime.Now);
                            RebuildPageBreaks(tempFile, board64Row);
                            //先转把看板excel转为pdf
                            XErrorLogToolMDM.WriteLog("看板转pdf开始"+DateTime.Now);
                            LiberOfficeHelper.ExcelOutOfPdf(board64Row, boardPdfPath);
                            SpireHelper.DrawLineFTEC(boardPdfPath);
                            XErrorLogToolMDM.WriteLog("看板转pdf结束"+DateTime.Now);
                            //根据捆包数组创建对应文件夹
                            List<string> listTempBoard =  new List<string>();
                            string boardFolderPath = Path.Combine(boardPdfDic, "board");
                            
                            string tempBoardAllPath = Path.Combine(boardPdfDic, "tempBoardAllPath");    //切割完的临时看板文件
                            if (Directory.Exists(tempBoardAllPath))
                            {
                                Directory.Delete(tempBoardAllPath, true);
                            }
                            Directory.CreateDirectory(tempBoardAllPath);
                            XErrorLogToolMDM.WriteLog("看板切割开始"+DateTime.Now);
                            //使用spire对pdf进行切割，切割后根据文件名放入对应的文件夹
                            SpireHelper.PdfSplit(boardPdfPath,tempBoardAllPath,0,4,1,SpireSplit.EnumNameRule.CustomizeAuto,new Rectangle(60,152,95,30),new Rectangle(16,26,559,790));
                            XErrorLogToolMDM.WriteLog("看板切割结束"+DateTime.Now);
                            int    index       = 0;
                            string currentCode = "";
                            int sumIndex = 0;
                            int currentIndex = 0;
                            //string folderName = "";
                            foreach (string bundleFolderPath in bundleFolderPaths)
                            {
                                string boardName      = bundleFolderPath.Split('_')[0];
                                if (string.IsNullOrEmpty(currentCode) || currentCode != boardName)
                                {
                                    currentCode  = boardName;
                                    currentIndex = 0;
                                    sumIndex = 0;
                                }

                                string tempFolderPath = Path.Combine(boardFolderPath,bundleFolderPath);
                                listTempBoard.Add(tempFolderPath);
                                Directory.CreateDirectory(tempFolderPath);
                                
                                //找tempBoardAllPath目录下以bundleFolderPath开头的文件
                                //看板中的名称不包含分1 分2 所以需要把bundleFolderPath中的下划线后面的内容去掉
                                
                                string[] tempBoardFiles = Directory.GetFiles(tempBoardAllPath, $"{boardName}*.pdf");
                                FileInfo[] fileInfos = tempBoardFiles
                                    .OrderBy(path => GetPageNumberOrZero(path)) // 排序
                                    .Select(path => new FileInfo(path))         // 转换
                                    .ToArray();
                                int count = boardCount[bundleFolderPath];
                                
                                
                                FileInfo[] fileInfosSubset = fileInfos
                                    .Skip(sumIndex) // 跳过前 currentIndex 个元素
                                    .Take(count)        // 取接下来的 count 个元素
                                    .ToArray();         // 转换为数组

                                currentIndex = count  ;
                                sumIndex += currentIndex;

                                string boardPath = Path.Combine(tempFolderPath, $"{bundleFolderPath}小纸张.pdf");
                                string boardA4Path = Path.Combine(tempFolderPath, $"{bundleFolderPath}A4.pdf");
                                XErrorLogToolMDM.WriteLog("看板拼接小纸张开始"+DateTime.Now);
                                SpireHelper.Merge(1,1,fileInfosSubset,boardPath,false, 20f);
                                XErrorLogToolMDM.WriteLog("看板拼接小纸张结束" +DateTime.Now);
                                XErrorLogToolMDM.WriteLog("看板拼接A4穿透开始" +DateTime.Now);
                                SpireHelper.Merge(4,1,fileInfosSubset,boardA4Path,true);
                                XErrorLogToolMDM.WriteLog("看板拼接A4穿透结束" +DateTime.Now);

                                detail[index].Board = boardPath;
                                detail[index].A4Board = boardA4Path;
                                currentCode = boardName;
                                index++;
                            }
                        }
                        catch (Exception ex)
                        {
                            //Directory.Delete(extractPath, true);
                            return ResParameter.Fail(ex.Message);
                        }
                    }

                    //处理受领书
                    XErrorLogToolMDM.WriteLog("处理首领书开始" +DateTime.Now);
                    //handleReceiptExcel
                    string receiptPath = ""; //受领书路径

                    for (int i = 0; i < tempFiles.Count; i++)
                    {
                        XErrorLogToolMDM.WriteLog(tempFiles[i]);
                    }
                    XErrorLogToolMDM.WriteLog("解压文件数量"+tempFiles.Count);

                    foreach (var tempFile in tempFiles.Where(f =>
                                 (Path.GetExtension(f).ToLower() == ".xlsx" ||
                                  Path.GetExtension(f).ToLower() == ".xls") &&
                                 Path.GetFileNameWithoutExtension(f).EndsWith("纳受领书")))
                    {
                        try
                        {
                            receiptPath = tempFile;
                            //拿到当前登陆人  日期    捆包数量
                            string outReceiptExcelPath = Path.Combine(extractPath, "TempOrderExcel", Path.GetFileName(tempFile));
                            Directory.CreateDirectory(Path.Combine(extractPath, "TempOrderExcel"));
                            fDate =  HandleReceiptExcel(tempFile, bundleFolderPaths, outReceiptExcelPath,infos);
                            string xlsxDic  = Path.Combine(extractPath, "TempOrderExcel");
                            string xlsxName = Path.GetFileNameWithoutExtension(tempFile) + ".xlsx";
                            string xlsxPath = Path.Combine(xlsxDic, xlsxName);
                            string pdfPath  = Path.Combine(xlsxDic, Path.GetFileNameWithoutExtension(tempFile) + ".pdf");
                            XErrorLogToolMDM.WriteLog("开始转换纳受领书");
                            LiberOfficeHelper.ExcelOutOfPdf(xlsxPath, pdfPath);
                            SpireHelper.DrawLineInPdf(pdfPath);
                            XErrorLogToolMDM.WriteLog("处理首领书结束" +DateTime.Now);

                            detail.ForEach(x => { x.Receipt = pdfPath; });
                        }
                        catch (Exception ex)
                        {
                            //Directory.Delete(extractPath, true);
                            //return (false, $"捆包List文件处理失败: {ex.Message}");
                        }
                    }

                    XErrorLogToolMDM.WriteLog("处理确认表开始" +DateTime.Now);
                    string baseDirectory       = AppDomain.CurrentDomain.BaseDirectory;
                    string confirmName = ConfigurationManager.AppSettings["FTCEQRB"].ToString();// 模板地址
                    string confirmTemplatePath = System.IO.Path.Combine(baseDirectory, confirmName);
                    //extractPath
                    string confinrmOutName = zipName + "确认书.xls";
                    string confirmOutDic   = extractPath;
                    string conFirmOutPath  = Path.Combine(confirmOutDic, confinrmOutName);
                    XErrorLogToolMDM.WriteLog("确认表excel转pdf开始" +DateTime.Now);

                    // bundleFolderPaths

                    //确认表
                    try
                    {
                        liberOfficeHelper.handleConfirmExcel(confirmTemplatePath, receiptPath, conFirmOutPath, detail, ftceDto, bundleList, "10001600");
                        orderservice.EditConfirmRemark(ftceDto.mainId.ToString(), "FTCE", "");
                        XErrorLogToolMDM.WriteLog("确认表excel转pdf结束" + DateTime.Now);
                    }
                    catch (Exception e)
                    {
                        return ResParameter.Fail(e.Message);
                    }

                    try
                    {
                        //将明细信息存入数据库
                        orderservice.InsertDetail(detail);
                    }
                    catch (Exception e)
                    {
                        return ResParameter.Fail(e.Message);
                    }
                    //删除临时 文件 文件夹
                    List<string> tempFileList =  new List<string>();
                    tempFileList.Add("tempBoardAllPath");
                    PulicHelper.DeleteExcelFiles(extractPath,tempFileList);

                    string   formattedDate = "";
                    DateTime parsedDate;
                    var      dateParts = fDate.Split('/');
                    string   month     = dateParts[0].PadLeft(2, '0'); // 补充月份
                    string   day       = dateParts[1].PadLeft(2, '0'); // 补充日期
                    string   year      = dateParts[2];                 // 年份已经是两位，不需要处理

                    // 重新构建日期字符串
                    //string fDateNew = $"{month}/{day}/{year}";
                    //bool isValid = DateTime.TryParseExact(fDateNew, "MM/dd/yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate);
        
                    //if (isValid)
                    //{
                    //    // 格式化为 "yyyy/MM/dd"
                    //     formattedDate = parsedDate.ToString("yyyy/MM/dd");
                    //    Console.WriteLine(formattedDate);  
                    //}
                    //else
                    //{
                    //    Console.WriteLine("无法解析日期");
                    //}
                    
                    var result = ResParameter.Success(
                        data: null,
                        ordercode: orderCode,
                        bin: "",
                        fdate: fDate
                    );
                    return result; // ResParameter.Success();
                }
            }
            catch (Exception ex)
            {
                return ResParameter.Fail(ex.Message);
            }
        }


        /// <summary>
        /// 读取捆包excel文件，返回一个字典，key为列标题，value为订单信息列表
        /// </summary>
        /// <param name="excelFilePath"></param>
        /// <returns></returns>
        public List<OrderInfo> ReadList(string excelFilePath)
        {
            try
            {
                var result = new Dictionary<string, List<OrderInfo>>();  // 单一字典存储合计值
                List<OrderInfo> orderInfos = new List<OrderInfo>();
                IWorkbook workbook;

                using (var fileStream = new FileStream(excelFilePath, FileMode.Open, FileAccess.Read))
                {
                    var ext = Path.GetExtension(excelFilePath).ToLower();
                    if (ext == ".xlsx")
                    {
                        workbook = new XSSFWorkbook(fileStream);
                    }
                    else
                    {
                        workbook = new HSSFWorkbook(fileStream);
                    }

                    // 获取第一个工作表
                    var sheet = workbook.GetSheetAt(0);

                    string codeLeft = sheet.GetRow(3).GetCell(14).ToString().Substring(0, 4);
                    string codeRight = "";
                    // 获取第六行数据，作为列标题（key）
                    var headerRow = sheet.GetRow(5);

                    // 获取列的key（从第六列开始）
                    //var columnKeys = new List<string>();
                    for (int col = 6; col < headerRow.Cells.Count; col++)  // 从第六列开始
                    {
                        var cellValue = headerRow.GetCell(col)?.ToString();
                        //codeRight = cellValue.Contains("\\") ? cellValue.Substring(0, 6) : cellValue;

                        if (string.IsNullOrWhiteSpace(cellValue))
                        {
                            // 为空直接跳过
                            continue;
                        }
                        codeRight = cellValue
                                    .Replace(" ", "")
                                    .Replace("\r", "")
                                    .Replace("\n", "")
                                    .Trim()
                                    .Substring(0, 6);

                        //if (codeRight.Contains("\\"))
                        //{
                        //    codeRight = codeRight.Substring(0, 6);
                        //}


                        if (!string.IsNullOrEmpty(cellValue))
                        {
                            //columnKeys.Add(cellValue.Replace("\\", "_"));
                            string key = cellValue;
                            //List<OrderInfo> orderInfos = new List<OrderInfo>();

                            for (int i = 6; i < sheet.LastRowNum; i++)
                            {
                                IRow rowi = sheet.GetRow(i);
                                if (!string.IsNullOrEmpty(rowi.GetCell(0).ToString()) && !string.IsNullOrEmpty(rowi.GetCell(col).ToString()))
                                {
                                    OrderInfo orderInfo = new OrderInfo();
                                    orderInfo.CustomerPartNumber = rowi.GetCell(0).ToString();
                                    orderInfo.BackNumber = rowi.GetCell(1).ToString();
                                    orderInfo.Capacity = int.Parse(rowi.GetCell(2).ToString());
                                    orderInfo.BoardCount = int.Parse(rowi.GetCell(col).ToString());
                                    orderInfo.InclusionQuantity = orderInfo.Capacity * orderInfo.BoardCount;
                                    orderInfo.OrderCode = codeLeft + codeRight;
                                    if (cellValue.Contains("\\"))
                                    {
                                        orderInfo.Category = cellValue.Replace(" ", "")
                                    .Replace("\r", "")
                                    .Replace("\n", "")
                                    .Trim().Split('\\')[1];
                                    }
                                    orderInfo.kCode = cellValue.Replace(" ", "")
                                    .Replace("\r", "")
                                    .Replace("\n", "")
                                    .Trim();
                                    if (orderInfo.BoardCount != 0) {
                                        orderInfos.Add(orderInfo);
                                    }
                                    
                                }
                            }

                            result[key] = orderInfos;

                        }
                    }

                    return orderInfos;

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }


        public Dictionary<string, int> GetBoardCount(string excelFilePath)
        {
            try
            {
                var result = new Dictionary<string, int>();  // 单一字典存储合计值
                IWorkbook workbook;

                using (var fileStream = new FileStream(excelFilePath, FileMode.Open, FileAccess.Read))
                {
                    var ext = Path.GetExtension(excelFilePath).ToLower();
                    if (ext == ".xlsx")
                    {
                        workbook = new XSSFWorkbook(fileStream);
                    }
                    else
                    {
                        workbook = new HSSFWorkbook(fileStream);
                    }

                    // 获取第一个工作表
                    var sheet = workbook.GetSheetAt(0);

                    // 获取第六行数据，作为列标题（key）
                    var headerRow = sheet.GetRow(5); // 第六行是索引5，0开始

                    // 获取列的key（从第六列开始）
                    var columnKeys = new List<string>();
                    for (int col = 6; col < headerRow.Cells.Count; col++)  // 从第六列开始
                    {
                        var cellValue = headerRow.GetCell(col)?.ToString();
                        if (!string.IsNullOrEmpty(cellValue))
                        {
                            columnKeys.Add(cellValue.Replace("\\", "_"));
                        }
                    }

                    // 计算每一列的合计
                    for (int col = 6; col < columnKeys.Count + 6; col++) // 从第六列开始
                    {
                        int columnSum = 0;

                        // 从第七行开始，读取每一列的值
                        for (int rowIndex = 6; rowIndex <= sheet.LastRowNum; rowIndex++)  // 从第七行开始
                        {
                            var row = sheet.GetRow(rowIndex);
                            if (row != null)
                            {
                                var cellValue = row.GetCell(col)?.ToString();
                                if (int.TryParse(cellValue, out int value))
                                {
                                    columnSum += value;  // 累加该列的值
                                }
                            }
                        }
                        if (columnSum == 0) { continue; }
                        result[columnKeys[col - 6]] = columnSum; // 将列合计存入字典
                    }
                }

                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static int GetPageNumberOrZero(string path)
        {
            string fileName = Path.GetFileNameWithoutExtension(path);
            var    match    = Regex.Match(fileName, @"_Page_(\d+)");
            if (match.Success)
            {
                return int.Parse(match.Groups[1].Value);
            }
            return 0; // 原始文件没有页码
        }


        /// <summary>
        /// 清除所有分页符并每隔64行插入一个分页符
        /// </summary>
        //public void RebuildPageBreaks(string inputPath, string outputPath, int linesPerPage = 63)
        //{
        //    using (FileStream fs = new FileStream(inputPath, FileMode.Open, FileAccess.Read))
        //    {
        //        IWorkbook workbook = new HSSFWorkbook(fs); // 或者 XSSFWorkbook 用于 .xlsx 文件

        //        int numberOfSheets = workbook.NumberOfSheets;

        //        for (int sheetIndex = 0; sheetIndex < numberOfSheets; sheetIndex++)
        //        {
        //            ISheet sheet = workbook.GetSheetAt(sheetIndex);

        //            // 1. 清除所有分页符
        //            var rowBreaks = sheet.RowBreaks;
        //            foreach (int breakRow in rowBreaks)
        //            {
        //                sheet.RemoveRowBreak(breakRow);
        //            }

        //            // 2. 插入新的分页符（每 linesPerPage 行）
        //            int lastRow = sheet.LastRowNum;
        //            for (int row = linesPerPage; row < lastRow; row += linesPerPage + 1)
        //            {
        //                sheet.SetRowBreak(row);
        //            }
        //        }

        //        // 3. 保存修改后的文件
        //        using (FileStream outFs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
        //        {
        //            workbook.Write(outFs);
        //        }
        //    }
        //}

        public void RebuildPageBreaks(string inputPath, string outputPath, int linesPerPage = 63)
        {
            using (FileStream fs = new FileStream(inputPath, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook = new HSSFWorkbook(fs); // 适用于 .xls，若为 .xlsx 请用 XSSFWorkbook

                for (int sheetIndex = 0; sheetIndex < workbook.NumberOfSheets; sheetIndex++)
                {
                    ISheet sheet = workbook.GetSheetAt(sheetIndex);

                    //  清除所有分页符
                    foreach (int breakRow in sheet.RowBreaks.ToArray())
                    {
                        sheet.RemoveRowBreak(breakRow);
                    }


                    //int targetColumnIndex = 13; // 你想调整的列索引
                    //int currentWidth = sheet.GetColumnWidth(targetColumnIndex);
                    //sheet.SetColumnWidth(targetColumnIndex, (int)(currentWidth * 2)); // 增加20%

                    //int nColIndex = 14; // N列（例如第5列，索引从0开始）
                    int gColIndex = 6; // G列（第7列）

                    int baseRow1 = 1; // 从第2行开始（索引为1）

                    while (baseRow1 <= sheet.LastRowNum)
                    {
                        // ---------- 第 2~9 行的第 N 列添加右边框 ----------
                        //for (int i = 0; i <= 20; i++)
                        //{
                        //    int rowIndex = baseRow1 + i;
                        //    if (rowIndex > sheet.LastRowNum) break; 

                        //    IRow row = sheet.GetRow(rowIndex) ?? sheet.CreateRow(rowIndex);
                        //    ICell cell = row.GetCell(nColIndex) ?? row.CreateCell(nColIndex);

                        //    ICellStyle style = workbook.CreateCellStyle();
                        //    style.CloneStyleFrom(cell.CellStyle);
                        //    style.BorderLeft = BorderStyle.Thin;
                        //    style.LeftBorderColor = HSSFColor.Red.Index;
                        //    cell.CellStyle = style;
                        //}

                        // ---------- 第 14~15 行的第 G 列添加右边框 ----------
                        for (int i = 9; i <=13 ; i++)
                        {
                            int rowIndex = baseRow1 + i;
                            if (rowIndex > sheet.LastRowNum) break;

                            IRow row = sheet.GetRow(rowIndex) ?? sheet.CreateRow(rowIndex);
                            ICell cell = row.GetCell(gColIndex) ?? row.CreateCell(gColIndex);

                            ICellStyle style = workbook.CreateCellStyle();
                            style.CloneStyleFrom(cell.CellStyle);
                            style.BorderRight = BorderStyle.Thin;
                            style.RightBorderColor = HSSFColor.Black.Index;
                            cell.CellStyle = style;
                        }

                        baseRow1 += 16; // 进入下一组
                    }




                    // 5. 插入新的分页符
                    int lastRow = sheet.LastRowNum;
                    for (int row = linesPerPage; row <= lastRow; row += linesPerPage + 1)
                    {
                        sheet.SetRowBreak(row);
                    }


                    IDataFormat format = workbook.CreateDataFormat();

                    for (int baseRow = 5; baseRow <= sheet.LastRowNum; baseRow += 16)
                    {
                        int[] offsetRows = { 0, 2 }; // 偏移量对应第6行、第8行（baseRow + 0, baseRow + 2）

                        foreach (int offset in offsetRows)
                        {
                            int rowIndex = baseRow + offset;
                            IRow row = sheet.GetRow(rowIndex);
                            if (row == null) continue;

                            ICell cell = row.GetCell(0); // 第一列（列索引0）
                            if (cell == null) continue;

                            if (cell.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(cell))
                            {
                                ICellStyle newStyle = workbook.CreateCellStyle();
                                newStyle.CloneStyleFrom(cell.CellStyle);
                                newStyle.DataFormat = format.GetFormat("yyyy/m/d hh:mm");
                                cell.CellStyle = newStyle;
                            }
                        }
                    }

                }



                using (FileStream outFs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                {
                    workbook.Write(outFs);
                }
            }
        }







        #endregion

        /// <summary>
        /// 读取捆包excel  创建对应文件夹
        /// </summary>
        /// <param name="excelFilePath"></param>
        /// <param name="basePath"></param>
        /// <exception cref="Exception"></exception>
        private List<string> CreateBundleFoldersFromExcel(string excelFilePath, string basePath)
        {
            List<string> list = new List<string>();
            IWorkbook    workbook;
            using (var fileStream = new FileStream(excelFilePath, FileMode.Open, FileAccess.Read))
            {
                var ext = Path.GetExtension(excelFilePath).ToLower();
                if (ext == ".xlsx")
                {
                    workbook = new XSSFWorkbook(fileStream);
                }
                else
                {
                    workbook = new HSSFWorkbook(fileStream);
                }
            }

            // 获取第一个工作表
            var sheet = workbook.GetSheetAt(0);

            //ICellStyle verticalStyle = workbook.CreateCellStyle();
            // 获取第 6 行
            var targetRow = sheet.GetRow(5);
            if (targetRow == null)
            {
                throw new Exception("Excel 文件第 6 行为空，无法读取捆包编号");
            }

            // 从G列开始遍历
            int startCol = 6;
            for (int col = startCol; col < targetRow.LastCellNum; col++)
            {
                var cell = targetRow.GetCell(col);
                if (cell == null || string.IsNullOrWhiteSpace(cell.ToString()))
                {
                    continue; // 跳过空单元格
                }

                // 获取捆包编号
                var bundleNumber = cell.ToString().Trim().Replace("\\", "_");
                list.Add(bundleNumber);

                string text = cell.ToString().Trim();
                string verticalText = string.Join("\n", text.ToCharArray());
                cell.SetCellValue(verticalText);

                ICellStyle style = workbook.CreateCellStyle();
                style.WrapText = true;
                style.Alignment = HorizontalAlignment.Center;     // 水平居中
                style.VerticalAlignment = VerticalAlignment.Center; // 垂直居中

                style.BorderTop = BorderStyle.Thin;
                style.BorderBottom = BorderStyle.Thin;
                style.BorderLeft = BorderStyle.Thin;
                style.BorderRight = BorderStyle.Thin;

                // 可选：设置边框颜色（默认黑）
                style.TopBorderColor = IndexedColors.Black.Index;
                style.BottomBorderColor = IndexedColors.Black.Index;
                style.LeftBorderColor = IndexedColors.Black.Index;
                style.RightBorderColor = IndexedColors.Black.Index;

                cell.CellStyle = style;
                // 创建以捆包编号命名的文件夹

            }

            //多余的地方才切掉

            int totalRows   = sheet.LastRowNum + 1;
            int headerRows  = 6;
            int rowsPerPage = 13;

            for (int startRow = headerRows; startRow < totalRows; startRow += rowsPerPage)
            {
                IRow   row           = sheet.GetRow(startRow);
                string firstCellText = row?.GetCell(0)?.ToString()?.Trim();

                if (string.IsNullOrEmpty(firstCellText))
                {
                    // 从该行开始删除至最后
                    for (int i = sheet.LastRowNum; i >= startRow; i--)
                    {
                        IRow r = sheet.GetRow(i);
                        if (r != null)
                            sheet.RemoveRow(r);
                    }
                    break; 
                }
            }

            
            // workbook.Close();
            string tempPath = excelFilePath + ".tmp";

            using (FileStream fsOut = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
            {
                workbook.Write(fsOut);
            }

            // 替换原文件
            File.Delete(excelFilePath);
            File.Move(tempPath, excelFilePath);
            return list;
        }
        
        /// <summary>
        /// 处理首领书.xls
        /// </summary>
        /// <param name="tempFile"></param>
        /// <param name="bundleFolderPaths"></param>
        /// <param name="outReceiptzipPath"></param>
        public string HandleReceiptExcel(string tempFile, List<string> bundleFolderPaths, string outReceiptzipPath,List<OrderInfo> infos)
        {
            try
            {   //先转成.xlsx再添加文本框 会导致格式有问题      先添加文本框 虽然乱码 但是再转成xlsx之后就正常了？？？ 
                System.DateTime currentDate   = System.DateTime.Now;
                string          formattedDate = $"{currentDate:yy.MM.dd}";
                int             num           = bundleFolderPaths.Count;

                IWorkbook workbook;
               
                using (var fileStream = new FileStream(tempFile, FileMode.Open, FileAccess.Read))
                {
                    workbook = new HSSFWorkbook(fileStream);
                }

                IDataFormat format = workbook.CreateDataFormat();
                HSSFSheet sheet = (HSSFSheet)workbook.GetSheetAt(0);


                ICell cellInDate = sheet.GetRow(3).GetCell(10);
                ICell cellOutDate = sheet.GetRow(9).GetCell(27);


                if (cellInDate!= null && cellInDate.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(cellInDate))
                {
                    ICellStyle newStyle = workbook.CreateCellStyle();
                    newStyle.CloneStyleFrom(cellInDate.CellStyle);
                    newStyle.DataFormat = format.GetFormat("yyyy/m/d");
                    cellInDate.CellStyle = newStyle;
                }

                if (cellOutDate != null && cellOutDate.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(cellOutDate))
                {
                    ICellStyle newStyle = workbook.CreateCellStyle();
                    newStyle.CloneStyleFrom(cellOutDate.CellStyle);
                    newStyle.DataFormat = format.GetFormat("yyyy/m/d");
                    cellOutDate.CellStyle = newStyle;
                }


                ICell cell926  = sheet.GetRow(9).GetCell(27);
                ICell  cell931  = sheet.GetRow(9).GetCell(32);
                string fDate = cell926?.ToString() ?? string.Empty;
                string fTime = cell931?.ToString() ?? string.Empty;

                int startRow = 12; 
                int startCol = 5;  
                List<OrderInfo> orderInfos = new List<OrderInfo>();
                //出荷日期
                ICell  shipmentDateCell = sheet.GetRow(9).GetCell(27);
                string shipmentDateStr  = string.Empty;
                //到货纳期
                ICell  ArrivalDueDateCell = sheet.GetRow(3).GetCell(10);
                string ArrivalDueDateStr = string.Empty;
                //订单号
                ICell OrderCode = sheet.GetRow(6).GetCell(27);
                //纳入番号
                ICell InclusionNumber = sheet.GetRow(1).GetCell(27);

                foreach (var item in infos)
                {
                    if (shipmentDateCell != null)
                    {
                        if (shipmentDateCell.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(shipmentDateCell))
                        {
                            // 如果单元格是日期类型
                            DateTime date = shipmentDateCell.DateCellValue;
                            shipmentDateStr = date.ToString("yyyy-MM-dd");
                        }
                        else if (shipmentDateCell.CellType == CellType.String)
                        {

                            if (DateTime.TryParseExact(shipmentDateCell.StringCellValue, "MM/dd/yy",
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    System.Globalization.DateTimeStyles.None,
                                    out DateTime parsedDate))
                            {
                                shipmentDateStr = parsedDate.ToString("yyyy-MM-dd");
                            }
                        }
                    }
                    item.ShipmentDate = shipmentDateStr;
                    if (ArrivalDueDateCell != null)
                    {
                        if (ArrivalDueDateCell.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(ArrivalDueDateCell))
                        {
                            // 如果单元格是日期类型
                            DateTime date = ArrivalDueDateCell.DateCellValue;
                            ArrivalDueDateStr = date.ToString("yyyy-MM-dd");
                        }
                        else if (ArrivalDueDateCell.CellType == CellType.String)
                        {

                            if (DateTime.TryParseExact(ArrivalDueDateCell.StringCellValue, "MM/dd/yy",
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    System.Globalization.DateTimeStyles.None,
                                    out DateTime parsedDate))
                            {
                                ArrivalDueDateStr = parsedDate.ToString("yyyy-MM-dd");
                            }
                        }
                    }
                    item.ArrivalDueDate = ArrivalDueDateStr;
                    //item.OrderCode = OrderCode?.ToString() ?? string.Empty;
                    item.InclusionNumber = InclusionNumber?.ToString().Replace("*", "") ?? string.Empty;
                    string dnPartNumber = PulicHelper.GetDnPartNumber(item.CustomerPartNumber);
                    item.DnPartNumber = dnPartNumber;
                }


                PulicHelper.SaveOrderInfos(infos);
                
                
                // 获取 HSSFPatriarch 用于绘制文本框
                HSSFPatriarch patriarch = (HSSFPatriarch)sheet.DrawingPatriarch;
                if (patriarch == null)
                {
                    patriarch = (HSSFPatriarch)sheet.CreateDrawingPatriarch();
                }

                // 创建字体
                HSSFFont font = (HSSFFont)workbook.CreateFont();
                font.FontName           = "Times New Roman"; 
                font.FontHeightInPoints = 10;
                font.Charset            = (byte)134;


                HSSFFont font2 = (HSSFFont)workbook.CreateFont();
                font2.FontName = "Times New Roman";
                font2.FontHeightInPoints = 15;
                font2.Charset = (byte)134;

                //  在 G40 上方添加 formattedDate 的文本框
                HSSFClientAnchor anchorG40 = new HSSFClientAnchor(
                    50, 175, 512, 225,
                    6, 38, 8, 38
                );
                HSSFTextbox        textboxG40  = (HSSFTextbox)patriarch.CreateTextbox(anchorG40);
                HSSFRichTextString richTextG40 = new HSSFRichTextString(formattedDate);
                richTextG40.ApplyFont(font);
                // 设置文本框无边框
                textboxG40.LineStyle = LineStyle.None;
                textboxG40.String = richTextG40;


                //  在 AO39 添加 num 的文本框
                HSSFClientAnchor anchorAO39 = new HSSFClientAnchor(
                    0, 100, 512, 200,
                    40, 38, 42, 38
                );
                HSSFTextbox textboxAO39 = (HSSFTextbox)patriarch.CreateTextbox(anchorAO39);
                //HSSFRichTextString richTextAO39 = new HSSFRichTextString(num?.ToString() ?? "N/A");
                HSSFRichTextString richTextAO39 = new HSSFRichTextString(num.ToString());
                richTextAO39.ApplyFont(font2);
                // 设置文本框无边框
                textboxAO39.LineStyle = LineStyle.None; 
                textboxAO39.String = richTextAO39;

                // 保存修改后的文件
                using (var fileStream = new FileStream(outReceiptzipPath, FileMode.Create, FileAccess.Write))
                {
                    workbook.Write(fileStream);
                }
                string DirectoryName= System.IO.Path.GetDirectoryName(outReceiptzipPath);
                //string fileName =  System.IO.Path.GetFileNameWithoutExtension(outReceiptzipPath);
                //string XLSXPath = System.IO.Path.Combine(DirectoryName,)
                LiberOfficeHelper.ConvertXlsToXlsx(outReceiptzipPath, DirectoryName);
                return fDate;

            }
            catch (System.Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        
        
    }
    
    
}