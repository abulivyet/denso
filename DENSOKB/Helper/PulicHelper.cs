using DENSOKB.Models;
using HelpLog.CommomMethod;
using ICSharpCode.SharpZipLib.Zip;
using MySql.Data.MySqlClient;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace DENSOKB.Helper
{
    public class PulicHelper
    {

        
        /// <summary>
        /// 生成确认表
        /// </summary>
        /// <param name="chamConfirmPath"></param>  模板路径
        /// <param name="confirmOutPath"></param>   输出路径
        /// <param name="chaMconfirm"></param>   
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static string EditConfirmExcel(string chamConfirmPath, string confirmOutPath, CHAMconfirm chaMconfirm)
        {
            string confirmPath = "";
            try
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string templatePath = Path.Combine(baseDirectory, chamConfirmPath);
                if (!File.Exists(templatePath))
                {
                    throw new FileNotFoundException("生成确认表失败,模板文件未找到", templatePath);
                }

                try
                {
                    // 打开模板文件
                    using (FileStream confirmTemp = new FileStream(templatePath, FileMode.Open, FileAccess.Read))
                    {
                        IWorkbook confirmT;
                        string extension = Path.GetExtension(templatePath).ToLower();
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

                        ISheet confirmSheet = confirmT.GetSheetAt(0);

                        //标题
                        IRow row0 = confirmSheet.GetRow(0);
                        ICell cellTitle = row0.GetCell(0);
                        cellTitle.SetCellValue(chaMconfirm.title);

                        IRow row1 = confirmSheet.GetRow(1);
                        ICell cellCusName = row1.GetCell(2);
                        cellCusName.SetCellValue(chaMconfirm.cusName);  //客户名称
                        ICell cellCarGrop = row1.GetCell(6);
                        cellCarGrop.SetCellValue(chaMconfirm.carGroup);  //取货车组

                        IRow row2 = confirmSheet.GetRow(2);
                        ICell cellCusCode = row2.GetCell(2);
                        cellCusCode.SetCellValue(chaMconfirm.cusCode); //客户编码
                        ICell cellShipmentDate = row2.GetCell(6);
                        cellShipmentDate.SetCellValue(chaMconfirm.shipmentDate);  //出荷日期

                        DateTime date;
                        if (DateTime.TryParse(chaMconfirm.shipmentDate, out date)) {
                            chaMconfirm.Week = ConvertToChineseDayOfWeek(date.DayOfWeek);
                        }

                        ICell cellWeek = row1.GetCell(9);
                        cellWeek.SetCellValue(chaMconfirm.Week);


                        IRow row3 = confirmSheet.GetRow(3);
                        ICell cellSummonsCreatePerson = row3.GetCell(2);
                        cellSummonsCreatePerson.SetCellValue(chaMconfirm.summonsCreatePerson);
                        ICell cellSummonsCreateTime = row3.GetCell(6);
                        cellSummonsCreateTime.SetCellValue(chaMconfirm.summonsCreateTime);

                        IRow row4 = confirmSheet.GetRow(4);
                        ICell cellCsGoodCreatePerson = row4.GetCell(2);
                        cellCsGoodCreatePerson.SetCellValue(chaMconfirm.csGoodCreatePerson);
                        ICell cellCsGoodCreateTime = row4.GetCell(6);
                        cellCsGoodCreateTime.SetCellValue(chaMconfirm.csGoodCreateTime);

                        IRow row5 = confirmSheet.GetRow(5);
                        ICell cellTpCheckCreatePerson = row5.GetCell(2);
                        cellTpCheckCreatePerson.SetCellValue(chaMconfirm.tpCheckCreatePerson);
                        ICell cellTpCheckCreateTime = row5.GetCell(6);
                        cellTpCheckCreateTime.SetCellValue(chaMconfirm.tpCheckCreateTime);


                        int templateRowIndex = 9; // 模板行
                        IRow templateRow = confirmSheet.GetRow(templateRowIndex);
                        if (templateRow == null)
                        {
                            throw new InvalidOperationException(
                                $"模板行 {templateRowIndex} 不存在.");
                        }

                        // 插入开始位置：模板行之后
                        int startRowIndex = templateRowIndex + 1;
                        int currentRowIndex = startRowIndex;  //当前行

                        // foreach (var item in list)
                        for (int j = 1; j < chaMconfirm.details.Count; j++)
                        {
                            // 检查现有行数，确保不会覆盖
                            if (currentRowIndex <= confirmSheet.LastRowNum)
                            {
                                // 将 currentRowIndex 及以下的行向下移动 1 行，为新行腾出空间
                                confirmSheet.ShiftRows(currentRowIndex, confirmSheet.LastRowNum, 1);
                            }

                            // 创建新行
                            IRow newRow = confirmSheet.CreateRow(currentRowIndex);
                            newRow.Height = templateRow.Height; // 保留行高

                            // 复制模板行的单元格
                            for (int i = 0; i < templateRow.LastCellNum; i++)
                            {
                                ICell templateCell = templateRow.GetCell(i, MissingCellPolicy.CREATE_NULL_AS_BLANK);
                                ICell newCell = newRow.CreateCell(i);

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
                            for (int i = 0; i < confirmSheet.NumMergedRegions; i++)
                            {
                                var mergedRegion = confirmSheet.GetMergedRegion(i);
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


                            currentRowIndex++;
                        }

                        // 插入值

                        int sumBoxNum = 0;
                        int sumQuentity = 0;

                        for (var i = 0; i < chaMconfirm.details.Count; i++)
                        {
                            IRow row = confirmSheet.GetRow(templateRowIndex + i);
                            if (row != null)
                            {
                                ICell cell0 = row.GetCell(0);  //计划代码
                                ICell cell1 = row.GetCell(1);  //落点
                                ICell cell2 = row.GetCell(2);  //零件号
                                ICell cell3 = row.GetCell(3);  //电装品番
                                ICell cell4 = row.GetCell(4);  //收容书
                                ICell cell5 = row.GetCell(5);  //托 台数
                                ICell cell6 = row.GetCell(6);  // 托 箱数
                                ICell cell7 = row.GetCell(7);  // 箱数 台尾数
                                ICell cell8 = row.GetCell(8);  // 箱数 
                                ICell cell9 = row.GetCell(9);  //托数 台尾数
                                ICell cell10 = row.GetCell(10);  //托数 箱尾数
                                ICell cell11 = row.GetCell(11);  //纳入个数
                                ICell cell12 = row.GetCell(12); //传票日期
                                ICell cell13 = row.GetCell(13); //传票时间
                                ICell cell14 = row.GetCell(14); //订单号
                                ICell cell15 = row.GetCell(15); //集货张数
                                ICell cell16 = row.GetCell(16); //现品照合
                                ICell cell17 = row.GetCell(17); //托盘个数
                                ICell cell18 = row.GetCell(18); //集货作业者

                                //计算箱数  纳入个数/收容数 向上取整
                                double quantityVal = ParseToDoubleOrZero(chaMconfirm.details[i].quantity);

                                int boxQtyF = chaMconfirm.details[i].BoxCap == 0
                                    ? 0
                                    : (int)Math.Floor(quantityVal / (double)(chaMconfirm.details[i].BoxCap ?? 1));

                                int boxQtyC = chaMconfirm.details[i].BoxCap == 0
                                    ? 0
                                    : (int)Math.Ceiling(quantityVal / (double)(chaMconfirm.details[i].BoxCap ?? 1));

                                // 箱尾数
                                int boxRemainder = chaMconfirm.details[i].BoxCap == 0
                                    ? 0
                                    : (int)(quantityVal % (chaMconfirm.details[i].BoxCap ?? 1));

                                // 托台数
                                int palletNum = chaMconfirm.details[i].BoxCap == 0
                                    ? 0
                                    : (chaMconfirm.details[i].PalletBox ?? 1) * (chaMconfirm.details[i].BoxCap ?? 1);

                                // 托数
                                int palletItemNumInt = palletNum == 0
                                    ? 0
                                    : (int)Math.Floor(quantityVal / palletNum);

                                // 托台尾数
                                int palletItemRem = palletNum == 0
                                    ? 0
                                    : (int)(quantityVal % palletNum);

                                // 箱尾数（基于 boxQtyC）
                                bool box = chaMconfirm.details[i].PalletBox == null;
                                //int palletBoxRem = (box || (chaMconfirm.details[i].PalletBox ?? 0) == 0)
                                //    ? 0
                                //    : boxQtyC % (chaMconfirm.details[i].PalletBox ?? 1);
                                int palletBoxVal = chaMconfirm.details[i].PalletBox ?? 0;
                                int boxCapVal = chaMconfirm.details[i].BoxCap ?? 0;

                                int palletBoxRem = 0;
                                if (!box && palletBoxVal > 0)
                                {
                                    // 先算整箱数（不向上取整）
                                    int fullBoxQty = boxCapVal > 0 ? (int)(quantityVal / boxCapVal) : 0;
                                    // 判断是否有不满的尾箱
                                    bool hasRemBox = boxCapVal > 0 && (quantityVal % boxCapVal) > 0;
                                    // 总箱数中，整箱部分对每托取余
                                    palletBoxRem = fullBoxQty % palletBoxVal;
                                    // 如果有尾箱，尾箱也要算进去
                                    if (hasRemBox) palletBoxRem += 1;
                                }

                                int? palletBox = chaMconfirm.details[i].PalletBox;
                                //int boxQtyF = chaMconfirm.details[i].BoxCap==0 ? 0: (int)Math.Floor(int.Parse(chaMconfirm.details[i].quantity) / (double)(chaMconfirm.details[i].BoxCap ?? 1));
                                //int boxQtyC = chaMconfirm.details[i].BoxCap == 0 ? 0 : (int)Math.Ceiling(int.Parse(chaMconfirm.details[i].quantity) / (double)(chaMconfirm.details[i].BoxCap ?? 1));
                                //箱 台尾数
                                //int boxRemainder = chaMconfirm.details[i].BoxCap == 0 ? 0 : (int)int.Parse(chaMconfirm.details[i].quantity) % chaMconfirm.details[i].BoxCap ?? 1;
                                ////托台数
                                //int palletNum = chaMconfirm.details[i].BoxCap == 0 ? 0 : chaMconfirm.details[i].PalletBox * chaMconfirm.details[i].BoxCap ?? 1;
                                ////托数 台尾数 
                                //int palletItemNumInt = palletNum==0?0:(int)Math.Floor(int.Parse(chaMconfirm.details[i].quantity) / (double)palletNum);   //托数
                                //int palletItemRem = palletNum==0?0:int.Parse(chaMconfirm.details[i].quantity) % palletNum; //托 台尾数
                                //bool box = chaMconfirm.details[i].PalletBox == null;
                                //int palletBoxRem = (box||(int)chaMconfirm.details[i].PalletBox==0)?0: boxQtyC % chaMconfirm.details[i].PalletBox ?? 1;//箱尾数

                                cell0.SetCellValue(chaMconfirm.details[i].projectCode);              //计划代码
                                cell1.SetCellValue(chaMconfirm.details[i].movingInCode);             //落点
                                cell2.SetCellValue(chaMconfirm.details[i].materialCode);             //零件号
                                cell3.SetCellValue(chaMconfirm.details[i].DensoNum);                 //电装品番
                                cell4.SetCellValue((double)chaMconfirm.details[i].BoxCap);           //收容数
                                cell5.SetCellValue(palletNum);                                       //托 台数
                                cell6.SetCellValue(chaMconfirm.details[i].PalletBox.ToString());     //托 箱数
                                cell7.SetCellValue(boxQtyF + "..." + boxRemainder);                  // 箱数 台尾数
                                cell8.SetCellValue(boxQtyC);                                         //箱数 
                                cell9.SetCellValue(palletItemNumInt  + "..." + palletItemRem);       //托 台尾数
                                cell10.SetCellValue(palletItemNumInt + "..." + palletBoxRem);        // 托 箱尾数
                                cell11.SetCellValue(chaMconfirm.details[i].quantity);                //纳入个数
                                cell12.SetCellValue(string.IsNullOrEmpty(chaMconfirm.details[i].cpdate) ? string.Empty : chaMconfirm.details[i].cpdate.ToString()); //传票日期
                                cell13.SetCellValue(string.IsNullOrEmpty(chaMconfirm.details[i].cptime) ? string.Empty : chaMconfirm.details[i].cptime.ToString()); //传票时间
                                cell14.SetCellValue(chaMconfirm.details[i].orderCode);               //订单号
                                cell15.SetCellValue(chaMconfirm.details[i].traceabilityLabel);

                                sumBoxNum += boxQtyC;
                                sumQuentity += int.Parse(chaMconfirm.details[i].quantity);

                                if (i == chaMconfirm.details.Count - 1)
                                {
                                    IRow sumRow = confirmSheet.GetRow(templateRowIndex + i + 1);
                                    ICell cell = sumRow.GetCell(8);
                                    ICell cellSumQ = sumRow.GetCell(11);
                                    cell.SetCellValue(sumBoxNum);
                                    cellSumQ.SetCellValue(sumQuentity);
                                }
                            }


                        }



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

                        if (!string.IsNullOrEmpty(chaMconfirm.confirmRemark)) {
                            //生成备注
                            for (int i = 0; i <= confirmSheet.LastRowNum; i++)
                            {
                                IRow row = confirmSheet.GetRow(i);
                                if (row == null) continue;

                                ICell cell = row.GetCell(0); // 第一列
                                if (cell != null && cell.ToString().Trim().Contains("备注"))
                                {
                                    // 设置文本内容
                                    ICell remarkCell = row.GetCell(1) ?? row.CreateCell(1);
                                    remarkCell.SetCellValue(chaMconfirm.confirmRemark);

                                    if (chaMconfirm.type == "丰田")
                                    {
                                        ICellStyle style = confirmT.CreateCellStyle();
                                        style.WrapText = true;

                                        IFont font = confirmT.CreateFont();
                                        font.FontHeightInPoints = 36;
                                        style.SetFont(font);
                                        style.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Top;
                                        remarkCell.CellStyle    = style;

                                        // int lineCount = Math.Max(1, (int)Math.Ceiling((double)chaMconfirm.confirmRemark.Length / 82));
                                        row.HeightInPoints = 4 * 50f;

                                        break;
                                    }
                                    else if (chaMconfirm.type == "佛山本田")
                                    {
                                        ICellStyle style = confirmT.CreateCellStyle();
                                        style.WrapText = true;

                                        IFont font = confirmT.CreateFont();
                                        font.FontHeightInPoints = 18;
                                        style.SetFont(font);
                                        style.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Top;
                                        remarkCell.CellStyle    = style;

                                        // int lineCount = Math.Max(1, (int)Math.Ceiling((double)chaMconfirm.confirmRemark.Length / 188));
                                        row.HeightInPoints = 4 * 50f;

                                        break;
                                    }
                                    else if (chaMconfirm.type == "东风本田")
                                    {
                                        ICellStyle style = confirmT.CreateCellStyle();
                                        style.WrapText = true;

                                        IFont font = confirmT.CreateFont();
                                        font.FontHeightInPoints = 18;
                                        style.SetFont(font);
                                        style.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Top;
                                        style.BorderTop         = BorderStyle.Thin;
                                        style.BorderBottom      = BorderStyle.Thin;
                                        remarkCell.CellStyle    = style;

                                        // int lineCount = Math.Max(1, (int)Math.Ceiling((double)chaMconfirm.confirmRemark.Length / 188));
                                        row.HeightInPoints = 4 * 30f;

                                        break;
                                    }
                                    else if (chaMconfirm.type == "广州本田")
                                    {
                                        ICellStyle style = confirmT.CreateCellStyle();
                                        style.WrapText = true;

                                        IFont font = confirmT.CreateFont();
                                        font.FontHeightInPoints = 20;
                                        style.SetFont(font);

                                        style.BorderTop         = BorderStyle.Thin;
                                        style.BorderBottom      = BorderStyle.Thin;
                                        style.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Top;
                                        remarkCell.CellStyle    = style;

                                        // int lineCount = Math.Max(1, (int)Math.Ceiling((double)chaMconfirm.confirmRemark.Length / 188));
                                        row.HeightInPoints = 4 * 28f;
                                    }
                                }
                            }
                        }


                        confirmOutPath = SanitizeFilePath(confirmOutPath);
                        // 保存修改后的文件
                        using (FileStream outputFs = new FileStream(confirmOutPath, FileMode.Create, FileAccess.Write))
                        {
                            confirmT.Write(outputFs);
                        }

                        string confirmDic = System.IO.Path.GetDirectoryName(confirmOutPath);
                        string confirmName = System.IO.Path.GetFileNameWithoutExtension(confirmOutPath);
                        confirmPath = System.IO.Path.Combine(confirmDic, confirmName + ".pdf");
                        LiberOfficeHelper liberOfficeHelper = new LiberOfficeHelper();
                        liberOfficeHelper.ExcelOutOfPdf1(confirmOutPath, confirmPath);
                        //spTopdf(confirmOutPath, confirmPath);
                    }

                }
                catch (System.Exception ex)
                {
                    Console.WriteLine($"发生错误: {ex.Message}");
                    XErrorLogToolMDM.WriteLog($"生成确认表发生错误: {ex.Message}");
                }

            }
            catch (Exception e)
            {
                XErrorLogToolMDM.WriteLog(e.Message);
                XErrorLogToolMDM.WriteLog(e.InnerException.ToString());
                XErrorLogToolMDM.WriteLog(e.StackTrace);
                Console.WriteLine(e);
                throw;

            }
            return confirmPath;
        }

        public static string SanitizeFilePath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return filePath;

            // 获取无效的文件名字符
            char[] invalidChars = Path.GetInvalidFileNameChars();

            // 创建 StringBuilder 用于构建清理后的路径
            StringBuilder sb = new StringBuilder(filePath.Length);

            foreach (char c in filePath)
            {
                // 保留路径分隔符（/ 和 \）以及冒号（: 用于驱动器号）
                if (c == '\\' || c == '/' || c == ':' || !invalidChars.Contains(c))
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
        //public static void spTopdf(string confirmOutPath,string  confirmPath)
        //{
        //    // 创建Workbook实例
        //    Spire.Xls.Workbook workbook = new Spire.Xls.Workbook();

        //    // 加载Excel文件
        //    workbook.LoadFromFile(confirmOutPath);

        //    // 设置转换选项
        //    workbook.ConverterSetting.SheetFitToPage = true;

        //    // 保存为PDF文件
        //    workbook.SaveToFile(confirmPath, FileFormat.PDF);


        //    XErrorLogToolMDM.WriteLog("Excel文件已成功转换为PDF！");
        //}

        public static double ParseToDoubleOrZero(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            double result;
            if (double.TryParse(value, out result))
                return result;

            return 0; // 解析失败就返回0
        }
        public static void CopyCellStyle(ICellStyle sourceStyle, ICellStyle targetStyle)
        {
            if (sourceStyle == null || targetStyle == null) return;

            // 边框
            targetStyle.BorderBottom = sourceStyle.BorderBottom;
            targetStyle.BorderLeft   = sourceStyle.BorderLeft;
            targetStyle.BorderRight  = sourceStyle.BorderRight;
            targetStyle.BorderTop    = sourceStyle.BorderTop;

            targetStyle.BottomBorderColor = sourceStyle.BottomBorderColor;
            targetStyle.TopBorderColor    = sourceStyle.TopBorderColor;
            targetStyle.LeftBorderColor   = sourceStyle.LeftBorderColor;
            targetStyle.RightBorderColor  = sourceStyle.RightBorderColor;

            // 对齐
            targetStyle.Alignment         = sourceStyle.Alignment;
            targetStyle.VerticalAlignment = sourceStyle.VerticalAlignment;

            // 字体（需要从 workbook 中再取字体）
            targetStyle.SetFont(sourceStyle.GetFont(null));

            // 背景
            targetStyle.FillForegroundColor = sourceStyle.FillForegroundColor;
            targetStyle.FillBackgroundColor = sourceStyle.FillBackgroundColor;
            targetStyle.FillPattern         = sourceStyle.FillPattern;

            // 数据格式
            targetStyle.DataFormat = sourceStyle.DataFormat;

            // 缩进
            targetStyle.Indention = sourceStyle.Indention;

            // 自动换行
            targetStyle.WrapText = sourceStyle.WrapText;

            // 旋转
            targetStyle.Rotation = sourceStyle.Rotation;

            // 锁定
            targetStyle.IsLocked = sourceStyle.IsLocked;
            targetStyle.IsHidden = sourceStyle.IsHidden;
        }


        /// <summary>
        /// 计算文件字节数组的 MD5
        /// </summary>
        public static string GetMD5(byte[] fileBytes)
        {
            using (var md5 = MD5.Create())
            {
                var hashBytes = md5.ComputeHash(fileBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }


        /// <summary>
        /// 获取文件的 MD5 哈希值（十六进制字符串）
        /// </summary>
        public static string GetFileMD5(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("文件不存在", filePath);

            using (var stream = File.OpenRead(filePath))
            using (var md5 = MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }


        public static void CopyCell(IWorkbook workbook, ICell source, ICell target)
        {
            if (source == null || target == null) return;

            // 复制类型和值
            target.SetCellType(source.CellType);
            switch (source.CellType)
            {
                case CellType.Blank:
                    target.SetCellValue("");
                    break;
                case CellType.Boolean:
                    target.SetCellValue(source.BooleanCellValue);
                    break;
                case CellType.Error:
                    target.SetCellErrorValue(source.ErrorCellValue);
                    break;
                case CellType.Formula:
                    target.SetCellFormula(source.CellFormula);
                    break;
                case CellType.Numeric:
                    target.SetCellValue(source.NumericCellValue);
                    break;
                case CellType.String:
                    target.SetCellValue(source.StringCellValue);
                    break;
            }

            // 复制样式（必须 clone，否则样式会丢失或覆盖）
            ICellStyle newStyle = workbook.CreateCellStyle();
            newStyle.CloneStyleFrom(source.CellStyle);
            target.CellStyle = newStyle;

            // 复制注释（如果有）
            if (source.CellComment != null)
            {
                target.CellComment = source.CellComment;
            }

            // 复制超链接（如果有）
            if (source.Hyperlink != null)
            {
                target.Hyperlink = source.Hyperlink;
            }
        }



        private static string ConvertToChineseDayOfWeek(DayOfWeek dayOfWeek)
        {
            switch (dayOfWeek)
            {
                case DayOfWeek.Monday: return "周一";
                case DayOfWeek.Tuesday: return "周二";
                case DayOfWeek.Wednesday: return "周三";
                case DayOfWeek.Thursday: return "周四";
                case DayOfWeek.Friday: return "周五";
                case DayOfWeek.Saturday: return "周六";
                case DayOfWeek.Sunday: return "周日";
                default: return "未知";
            }
        }


        public static String tablejson(DataTable dt)
        {
            if (dt == null)
            {
                return "";
            }
            try
            {
                if (dt.Rows.Count > 0)
                {
                    //创建json
                    StringBuilder sbjson = new StringBuilder();
                    //sbjson.Append("{");
                    sbjson.Append("[");//拼接字符串 1 没用随便写
                    if (dt != null)
                    {
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            if (i > 0)
                            {
                                sbjson.Append(",");
                                sbjson.Append("{");
                                foreach (DataColumn dc in dt.Columns)
                                {
                                    if (dt.Columns.IndexOf(dc) > 0)
                                    {
                                        sbjson.Append(",");
                                        sbjson.Append("\"" + dc.ColumnName + "\":\"" + dt.Rows[i][dc.ColumnName].ToString().Trim() + "\"");
                                    }
                                    else
                                    {
                                        sbjson.Append("\"" + dc.ColumnName + "\":\"" + dt.Rows[i][dc.ColumnName].ToString().Trim() + "\"");
                                    }
                                }
                                sbjson.Append("}");
                            }
                            else
                            {
                                sbjson.Append("{");
                                foreach (DataColumn dc in dt.Columns)
                                {
                                    if (dt.Columns.IndexOf(dc) > 0)
                                    {
                                        sbjson.Append(",");
                                        sbjson.Append("\"" + dc.ColumnName + "\":\"" + dt.Rows[i][dc.ColumnName].ToString().Trim() + "\"");
                                    }
                                    else
                                    {
                                        sbjson.Append("\"" + dc.ColumnName + "\":\"" + dt.Rows[i][dc.ColumnName].ToString().Trim() + "\"");
                                    }
                                }
                                sbjson.Append("}");
                            }
                        }
                    }
                    sbjson.Append("]");
                    return sbjson.ToString();
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
  
                return ex.Message;
            }

        }

        public static string GetDnPartNumber(string InclusionNumber)
        {
            try
            {
                string strSql = @"select denso_num from originalunit where cust_num = '{0}'";
                strSql = string.Format(strSql, InclusionNumber);
                DataTable table = DataHelper.Query(strSql);
                if (table.Rows.Count <= 0)
                {
                    throw new Exception("查找DN品番失败,请检查纳入番号是否正确");
                }
                return table.Rows[0]["denso_num"].ToString();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public static void DeleteOrderInfos(List<OrderInfo> orderInfos)
        {
            if (orderInfos == null || orderInfos.Count == 0)
                return;

            try
            {
                StringBuilder sb = new StringBuilder();
                List<MySqlParameter> parameters = new List<MySqlParameter>();

                sb.Append("DELETE FROM orderinfo WHERE ordercode IN (");

                for (int i = 0; i < orderInfos.Count; i++)
                {
                    string paramName = $"@ordercode{i}";
                    sb.Append(paramName);

                    if (i < orderInfos.Count - 1)
                        sb.Append(", ");

                    parameters.Add(new MySqlParameter(paramName, orderInfos[i].OrderCode));
                }

                sb.Append(")");

                DataHelper.ExecuteSql(sb.ToString(), parameters.ToArray());
            }
            catch (Exception ex)
            {
                throw new Exception("删除订单信息时出错：" + ex.Message, ex);
            }
        }


        public static void SaveOrderInfos(List<OrderInfo> orderInfos)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                List<MySqlParameter> parameters = new List<MySqlParameter>();

                sb.Append("INSERT INTO orderinfo ");
                sb.Append("(shipmentdate, arrivalduedate, ordercode, inclusionnumber, customerpartnumber, ");
                sb.Append("dnpartnumber, backnumber, capacity, boardcount, inclusionquantity, category, planCode, ReceiptPlace) VALUES ");

                // 拼接多行 values
                for (int i = 0; i < orderInfos.Count; i++)
                {
                    var orderInfo = orderInfos[i];

                    sb.Append($"(@shipmentdate{i}, @arrivalduedate{i}, @ordercode{i}, @inclusionnumber{i}, @customerpartnumber{i}, ");
                    sb.Append($"@dnpartnumber{i}, @backnumber{i}, @capacity{i}, @boardcount{i}, @inclusionquantity{i}, ");
                    sb.Append($"@category{i}, @planCode{i}, @ReceiptPlace{i})");

                    if (i < orderInfos.Count - 1)
                        sb.Append(","); // 非最后一行加逗号

                    // 添加参数（带索引，防止重名）
                    parameters.Add(new MySqlParameter($"@shipmentdate{i}", orderInfo.ShipmentDate));
                    parameters.Add(new MySqlParameter($"@arrivalduedate{i}", orderInfo.ArrivalDueDate));
                    parameters.Add(new MySqlParameter($"@ordercode{i}", orderInfo.OrderCode));
                    parameters.Add(new MySqlParameter($"@inclusionnumber{i}", orderInfo.InclusionNumber));
                    parameters.Add(new MySqlParameter($"@customerpartnumber{i}", orderInfo.CustomerPartNumber));
                    parameters.Add(new MySqlParameter($"@dnpartnumber{i}", orderInfo.DnPartNumber));
                    parameters.Add(new MySqlParameter($"@backnumber{i}", orderInfo.BackNumber));
                    parameters.Add(new MySqlParameter($"@capacity{i}", orderInfo.Capacity));
                    parameters.Add(new MySqlParameter($"@boardcount{i}", orderInfo.BoardCount));
                    parameters.Add(new MySqlParameter($"@inclusionquantity{i}", orderInfo.InclusionQuantity));
                    parameters.Add(new MySqlParameter($"@category{i}", orderInfo.Category));
                    parameters.Add(new MySqlParameter($"@planCode{i}", orderInfo.planCode));
                    parameters.Add(new MySqlParameter($"@ReceiptPlace{i}", orderInfo.ReceiptPlace));
                }

                // 一次执行
                int n = DataHelper.ExecuteSql(sb.ToString(), parameters.ToArray());
                XErrorLogToolMDM.WriteLog("插入orderinfo结果"+n);
            
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        ///  多文件放一个压缩包
        /// </summary>
        /// <param name="filePaths"></param>
        /// <param name="zipName"> 不带扩展名</param>
        public static void DownloadFilesAsZip(List<string> filePaths, string zipName)
        {
            if (filePaths == null || filePaths.Count == 0)
                throw new ArgumentException("文件路径列表不能为空");

            string fileNameWithoutExt = ConfigurationManager.AppSettings["fileUpload"].ToString();
            string subZipName = $"{fileNameWithoutExt}{zipName}.zip";
            string filename = Path.GetFileNameWithoutExtension(zipName);
            // 创建临时压缩文件
            using (ZipOutputStream subZipStream = new ZipOutputStream(File.Create(subZipName)))
            {
                subZipStream.SetLevel(9); // 最高压缩级别
                foreach (var path in filePaths)
                {
                    string filePath = path;
                    if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                        continue;

                    try
                    {
                        ZipEntry entry = new ZipEntry(Path.GetFileName(filePath));
                        entry.DateTime = DateTime.Now;
                        subZipStream.PutNextEntry(entry);

                        using (FileStream fs = File.OpenRead(filePath))
                        {
                            byte[] buffer = new byte[4096];
                            int sourceBytes;
                            do
                            {
                                sourceBytes = fs.Read(buffer, 0, buffer.Length);
                                subZipStream.Write(buffer, 0, sourceBytes);
                            } while (sourceBytes > 0);
                        }
                    }
                    catch (Exception ex)
                    {
                        // 记录错误但继续处理其他文件
                        System.Diagnostics.Debug.WriteLine($"添加文件 {filePath} 到压缩包时出错: {ex.Message}");
                    }
                }
            }

            // 下载压缩包
            DownloadZip(subZipName, filename);

            // 删除临时压缩包
            try
            {
                File.Delete(subZipName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"删除临时文件 {subZipName} 时出错: {ex.Message}");
            }
        }

        public static void DownloadZip(string filePath, string zipName)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("压缩文件不存在");

            HttpResponse response = HttpContext.Current.Response;
            try
            {
                response.Clear();
                response.ContentType = "application/zip";
                response.AppendHeader("Content-Disposition", $"attachment; filename={HttpUtility.UrlEncode(zipName)}.zip");
                response.TransmitFile(filePath);
                response.Flush();
            }
            finally
            {
                response.End();
            }
        }

        // 谢高峰处理不了带\的Datatable
        public static List<Dictionary<string, object>> ConvertDataTableToListOfDictionaries(DataTable dataTable)
        {
            if (dataTable == null || dataTable.Rows.Count == 0)
                return new List<Dictionary<string, object>>();

            return dataTable.AsEnumerable().Select(row =>
            {
                var dict = new Dictionary<string, object>();
                foreach (DataColumn col in dataTable.Columns)
                {
                    // 处理DBNull值，转为null
                    dict[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
                }

                return dict;
            }).ToList();
        }
        
        
        
       public static void DeleteExcelFiles(string baseFolderPath, List<string> subFolders = null)
    {
        try
        {
            // 确保基本文件夹路径存在
            if (Directory.Exists(baseFolderPath))
            {
                // 删除基本路径及其子文件夹中的所有 Excel 文件
                DeleteExcelFilesInDirectory(baseFolderPath);

                // 如果传入了额外的子文件夹路径集合，删除其中的文件和文件夹
                if (subFolders != null && subFolders.Count > 0)
                {
                    foreach (var subFolder in subFolders)
                    {
                        // 拼接完整路径
                        string fullPath = Path.Combine(baseFolderPath, subFolder);

                        if (Directory.Exists(fullPath))
                        {
                            // 删除该子文件夹下的所有文件和文件夹
                            DeleteAllFilesAndFolders(fullPath);
                        }
                        else
                        {
                            Console.WriteLine($"子文件夹 {fullPath} 不存在");
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine($"基本文件夹 {baseFolderPath} 不存在");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"操作失败: {ex.Message}");
        }
    }

    // 删除指定目录下所有 Excel 文件（.xls 和 .xlsx）
    private static void DeleteExcelFilesInDirectory(string folderPath)
    {
        try
        {
                // 获取所有 Excel 文件，包括子文件夹中的文件
                // string[] excelFiles = Directory.GetFiles(folderPath, "*.xls*", SearchOption.AllDirectories);
                // 获取所有 Excel 文件，包括子文件夹中的文件
                //string[] excelFiles = Directory
                //    .GetFiles(folderPath, "*.xls*", SearchOption.AllDirectories)
                //    .Where(f => !Path.GetFileName(f).Contains("确认"))
                //    .ToArray();

                string[] excelFiles = Directory
            .GetFiles(folderPath, "*.xls*", SearchOption.AllDirectories)
            .Where(f =>
                !Path.GetFileName(f).Contains("确认") &&  // 文件名不包含"确认"
                !f.Contains("确认"))                      // 完整路径不包含"确认"（即文件夹名不含"确认"）
            .ToArray();


                foreach (var file in excelFiles)
            {
                try
                {
                    // 删除文件
                    File.Delete(file);
                    Console.WriteLine($"已删除文件: {file}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"删除文件失败: {file}, 错误: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"删除文件夹 {folderPath} 中的文件时出错: {ex.Message}");
        }
    }

    // 删除指定路径下的所有文件和文件夹
    private static void DeleteAllFilesAndFolders(string folderPath)
    {
        try
        {
            // 删除文件夹中的所有文件
            string[] files = Directory.GetFiles(folderPath);
            foreach (var file in files)
            {
                try
                {
                    File.Delete(file);
                    Console.WriteLine($"已删除文件: {file}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"删除文件失败: {file}, 错误: {ex.Message}");
                }
            }

            // 删除子文件夹
            string[] directories = Directory.GetDirectories(folderPath);
            foreach (var dir in directories)
            {
                try
                {
                    Directory.Delete(dir, true);
                    Console.WriteLine($"已删除文件夹: {dir}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"删除文件夹失败: {dir}, 错误: {ex.Message}");
                }
            }

            // 最后删除当前文件夹（如果需要）
            try
            {
                Directory.Delete(folderPath, true);
                Console.WriteLine($"已删除文件夹: {folderPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"删除文件夹失败: {folderPath}, 错误: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"删除 {folderPath} 时出错: {ex.Message}");
        }
    }

        private static void DeleteAllFilesAndFolders(string folderPath, List<string> excludedFolders = null)
        {
            try
            {
                // 首先检查当前文件夹是否在排除列表中
                if (excludedFolders != null && excludedFolders.Contains(folderPath))
                {
                    Console.WriteLine($"跳过排除文件夹: {folderPath}");
                    return;
                }

                // 删除文件夹中的所有文件
                string[] files = Directory.GetFiles(folderPath);
                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                        Console.WriteLine($"已删除文件: {file}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"删除文件失败: {file}, 错误: {ex.Message}");
                    }
                }

                // 递归删除子文件夹
                string[] directories = Directory.GetDirectories(folderPath);
                foreach (var dir in directories)
                {
                    // 检查子文件夹是否在排除列表中
                    if (excludedFolders == null || !excludedFolders.Contains(dir))
                    {
                        DeleteAllFilesAndFolders(dir, excludedFolders); // 递归调用
                    }
                    else
                    {
                        Console.WriteLine($"跳过排除文件夹: {dir}");
                    }
                }

                // 最后尝试删除当前文件夹（如果为空）
                try
                {
                    // 只有当文件夹为空时才删除（因为我们可能跳过了某些排除的子文件夹）
                    if (Directory.GetFiles(folderPath).Length == 0 &&
                        Directory.GetDirectories(folderPath).Length == 0)
                    {
                        Directory.Delete(folderPath);
                        Console.WriteLine($"已删除空文件夹: {folderPath}");
                    }
                    else
                    {
                        Console.WriteLine($"文件夹 {folderPath} 不为空，跳过删除");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"删除文件夹失败: {folderPath}, 错误: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理 {folderPath} 时出错: {ex.Message}");
            }
        }

        public static void saveConfirm(string mainId,string excelPath,string pdfPath)
    {
        try
        {
            string strSQL = @"insert into confirm (mainid,excelpath,pdfpath) values (@mainid,@excelpath,@pdfpath)";
            MySqlParameter[] parameters = new MySqlParameter[]
            {
                new MySqlParameter("@mainid", mainId),
                new MySqlParameter("@excelpath", excelPath),
                new MySqlParameter("@pdfpath", pdfPath),
                new MySqlParameter("@createdate", DateTime.Now)
            };
            DataHelper.ExecuteSql(strSQL, parameters);

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

        public static string GetBoxCap(string custName, string custNum,string type) {
            try
            {
                string strSql = @"select box_cap
                from originalunit where cust_name = '{0}' AND cust_num = '{1}' and veh_model = '{2}'";
                string sql = string.Format(strSql, custName, custNum, type);
                DataTable data = DataHelper.Query(sql);
                if (data.Rows.Count == 0)
                {
                    return "";
                }
                else {
                    return data.Rows[0]["box_cap"].ToString();
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="PN">品番</param>
        /// <param name="orderType">量产 八千代 </param>
        /// <param name="cusNum">#1 #2</param>
        /// <returns></returns>
        public static DataTable GetGHACDensoPN(string PN,string type,string supplier,string cusNum = "",string projectCode = "") {
            try
            {
                string strSql = "";
                string sql = "";
                DataTable dt = null;
                if (string.IsNullOrEmpty(cusNum))
                {
                    strSql = @" select * from originalunit where cust_name = 'GHAC'  and cust_num = '{0}'";
                    sql = string.Format(strSql, PN);
                }
                else {
                    strSql = @"select * from originalunit where cust_name = 'GHAC'  and cust_num = '{0}' and  cust_code = '{1}' and recv_port = '{2}'";

                    if (type == "补给品") { strSql += " and factory like '%SP%'"; }
                    else
                    {
                        if (projectCode.Contains("01")) { strSql += " and factory like '%1%'"; }
                        if (projectCode.Contains("02")) { strSql += " and factory like '%2%'"; }
                        if (projectCode.Contains("03")) { strSql += " and factory like '%3%'"; }
                        if (projectCode.Contains("04")) { strSql += " and factory like '%4%'"; }
                        if (projectCode.Contains("09")) { strSql += " and factory like '%9%'"; }
                    }

                    sql = string.Format(strSql, PN, cusNum, supplier);
                }
                dt = DataHelper.Query(sql);

                XErrorLogToolMDM.WriteLog("查找电装品番");
                XErrorLogToolMDM.WriteLog(sql);
                return dt;

            }
            catch (Exception)
            {

                throw;
            }
        
        }


        public static DataTable GetOri(string custName,string vehModel,string custNum,string factory) {
            try
            {
                StringBuilder strSql = new StringBuilder();
                strSql.Append(@"select * from originalunit where 1=1 ");
                if (!string.IsNullOrEmpty(custName))
                {
                    strSql.Append(" AND cust_name = '" + custName + "'");
                }
                if (!string.IsNullOrEmpty(vehModel))
                {
                    strSql.Append(" AND vehModel = '" + vehModel + "'");
                }
                if (!string.IsNullOrEmpty(custNum))
                {
                    strSql.Append(" AND custNum = '" + custNum + "'");
                }

                if (custName == "GHAC")
                {
                    if (!string.IsNullOrEmpty(factory))
                    {
                        strSql.Append(" AND factory like '%" + factory + "%'");
                    }
                }
                else {
                    if (!string.IsNullOrEmpty(factory))
                    {
                        strSql.Append(" AND factory = '" + factory + "'");
                    }
                }



                return DataHelper.Query(strSql.ToString());

            }
            catch (Exception)
            {

                throw;
            }
        }

        /// <summary>
        /// 获取单元格的字符串值（支持公式、数值、布尔、日期等）
        /// </summary>
        /// <param name="cell">NPOI单元格</param>
        /// <param name="evaluator">公式计算器（可选）</param>
        /// <returns>字符串值</returns>
        public static string GetCellString(ICell cell, IFormulaEvaluator evaluator = null)
        {
            if (cell == null) return string.Empty;

            // 如果传入了 evaluator 并且是公式，先计算公式
            if (cell.CellType == CellType.Formula && evaluator != null)
            {
                var eval = evaluator.Evaluate(cell);
                if (eval == null) return string.Empty;

                switch (eval.CellType)
                {
                    case CellType.String: return eval.StringValue?.Trim();
                    case CellType.Numeric:
                        // 判断是否是日期格式
                        if (DateUtil.IsCellDateFormatted(cell))
                            return cell.DateCellValue.ToString("yyyy-MM-dd HH:mm:ss");
                        return eval.NumberValue.ToString();
                    case CellType.Boolean: return eval.BooleanValue.ToString();
                    default: return eval.FormatAsString();
                }
            }

            // 普通单元格
            switch (cell.CellType)
            {
                case CellType.String: return cell.StringCellValue?.Trim();
                case CellType.Numeric:
                    if (DateUtil.IsCellDateFormatted(cell))
                        return cell.DateCellValue.ToString("yyyy-MM-dd HH:mm:ss");
                    return cell.NumericCellValue.ToString();
                case CellType.Boolean: return cell.BooleanCellValue.ToString();
                case CellType.Blank: return string.Empty;
                default: return cell.ToString().Trim();
            }
        }

    }
}