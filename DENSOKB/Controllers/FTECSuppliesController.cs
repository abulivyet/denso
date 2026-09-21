using DENSOKB.Helper;
using DENSOKB.Libs;
using DENSOKB.Models;
using iTextSharp.text;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using QRCoder;
using Spire.Pdf;
using Spire.Pdf.Graphics;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Web.Http;
using uno;

namespace DENSOKB.Controllers
{
    public class FTECSuppliesController: ApiController
    {
        private LiberOfficeHelper liberOfficeHelper = new LiberOfficeHelper();
        orderimportService        ordService        =  new orderimportService();
        
        /// <summary>
        /// 处理ftce补给品
        /// </summary>
        /// <param name="FtceDto"></param>
        /// <returns></returns>
        [HttpPost]
        public ResParameter CompressedFileFTCESupplies(FtceDto FtceDto)
        {
            try
            {
                //检查excel路径是否有效
                if (string.IsNullOrEmpty(FtceDto.zipPath) || !File.Exists(FtceDto.zipPath))
                {
                    return ResParameter.Fail("未找到excel文件");
                }

                return DisposeFtceSupply(FtceDto);
                // return ResParameter.Success("ok");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                // throw;
                return ResParameter.Fail(e.Message);
            }

        }
        
        
        public ResParameter DisposeFtceSupply(FtceDto FtceDto)
        {
            try
            {
                string           baseDirectory = AppDomain.CurrentDomain.BaseDirectory; //根目录
                List<inpdetail2> inpdetail2s   = new List<inpdetail2>();
                string           fDate         = "";
            

            //先创建一个同名文件夹
            string sourceExcelName = Path.GetFileNameWithoutExtension(FtceDto.zipPath);
            string sourcezipPath = FtceDto.basePath;
            string newFolderPath = Path.Combine(sourcezipPath,"FTCE", sourceExcelName);
            List<Dictionary<string, string>> originalunit = new List<Dictionary<string, string>>();
            if (!Directory.Exists(newFolderPath))
            {
                Directory.CreateDirectory(newFolderPath);
            }
            //先将.xls转为.xlsx
            LiberOfficeHelper.ConvertXlsToXlsx(FtceDto.zipPath, newFolderPath);
            string xlsxPath = Path.Combine(newFolderPath, sourceExcelName + ".xlsx");
            //把excel根据sheet页拆分到刚创建的文件夹中
            LiberOfficeHelper.SplitExcelByDeletingOtherSheets(xlsxPath, newFolderPath);
            string blankReceiptPath = Path.Combine(newFolderPath, "空白纳入受领书 (FTMS).xlsx");

            string AcceptancePath = Path.Combine(newFolderPath, "纳入受领书.pdf");
            if (!string.IsNullOrEmpty(AcceptancePath))    //保存受领书
            {
                for (int i = 0; i < 3; i++)
                {
                    inpdetail2  inpdetail2  = new inpdetail2();
                    inpdetail2.filetype   = "受领书";
                    inpdetail2.Stacklabel = AcceptancePath;
                    inpdetail2.rowcode    = 5+i;
                    inpdetail2s.Add(inpdetail2);
                }

            }
            
            //受领书转PDF
            LiberOfficeHelper.ExcelOutOfPdf(blankReceiptPath, AcceptancePath);
            
            string tempPdfPath;
            using (FileStream blankReceipt = new FileStream(blankReceiptPath, FileMode.Open, FileAccess.Read))
            {
                IWorkbook blankReceiptBook = new XSSFWorkbook(blankReceipt);
                ISheet confirmSheet = blankReceiptBook.GetSheetAt(0);
                originalunit = TraverseCellsFromC9(confirmSheet);     //根据纳受领书和原单位表计算需要多少箱 多少外箱单
                saveFtceSupOrderInfo(originalunit);
                string newOutBoxDic = disposeBoxPDF(originalunit, newFolderPath);
                tempPdfPath = liberOfficeHelper.SetOfOutBoxPDF(newOutBoxDic,inpdetail2s);  //生成成套外箱单    保存外箱单路径   

            }
            //处理货垛标签
            string stackzipPath = Path.Combine(baseDirectory, ConfigurationManager.AppSettings["FTCEMDD"].ToString());// 模板地址);
            liberOfficeHelper.StackLabel(originalunit, stackzipPath,newFolderPath,inpdetail2s);    //保存货垛标签路径
            
            //确认表
            // string q = ConfigurationManager.AppSettings["FTCEQRB"].ToString();
            string confirmTemplatePath = Path.Combine(baseDirectory, ConfigurationManager.AppSettings["FTCEQRB"].ToString());
            string conFirmOutPath = Path.Combine(newFolderPath,sourceExcelName +"确认表.xls");
            //originalunit
            string orderCode = liberOfficeHelper.FtceSuppleConfirm(originalunit,confirmTemplatePath,conFirmOutPath,inpdetail2s,FtceDto);
            ordService.EditConfirmRemark(FtceDto.mainId.ToString(), "FTCE", "");

                foreach (var t in inpdetail2s)
            {
                t.ordercode     = orderCode;
                t.mainId = FtceDto.mainId;
            };
            ordService.InsertDetail2(inpdetail2s);
            
            List<string> tempFileList =  new List<string>();
            tempFileList.Add("ScalePdf");
            //tempFileList.Add("TemporaryOuterBoxLabel");
            tempFileList.Add("tempPDF");
            PulicHelper.DeleteExcelFiles(newFolderPath,tempFileList);
            
            var result = ResParameter.Success(
                data: null,
                ordercode: orderCode,
                bin: "",
                fdate: originalunit[0]["shipment"]
            );
            
            return result; // ResParameter.Success();

            // return ResParameter.Success();

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            

        }


        public void saveFtceSupOrderInfo(List<Dictionary<string, string>> originalunit)
        {
            try
            {
                List<OrderInfo> orderInfos = new List<OrderInfo>();
                foreach (Dictionary<string,string> dictionary in originalunit)
                {
                    OrderInfo info = new OrderInfo();
                    info.ShipmentDate       = dictionary["shipment"];                             // 出货日期
                    info.ArrivalDueDate     = null;                                                 // 到货纳期
                    info.OrderCode          = dictionary["orderNo"];                              // 订单号
                    info.InclusionNumber    = "";                                                 // 纳入番号
                    info.CustomerPartNumber = dictionary["custNum"];                              // 客户品番
                    info.DnPartNumber       = PulicHelper.GetDnPartNumber(dictionary["custNum"]); // DN品番
                    info.BackNumber         = dictionary["backNum"];                              // 背番号
                    info.Capacity           = int.Parse(dictionary["boxCap"]);                   // 收容数
                    info.BoardCount         = int.Parse(dictionary["boxNum"]);                   // 看板张数
                    info.InclusionQuantity  = int.Parse(dictionary["shippingQty"]);              // 纳入个数
                    info.Category           = "补给品";                                            // 分类
                    orderInfos.Add(info);
                }
                PulicHelper.SaveOrderInfos(orderInfos);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public List<Dictionary<string,string>> TraverseCellsFromC9(ISheet sheet)
        {
            // 起始位置是C9，在NPOI中C -> 2（列索引），9 -> 8（行索引）
            int startRow = 8;  // 行索引，从 0 开始，所以 9 行为索引 8
            int startCol = 2;  // 列索引，C 列为 2
            int sOrderNoRow = 5;
            int sOrderNoCell = 10;
            int bOrderNoRow = 17; //订单号
            int bOrderNoCell = 10; //订单号
            int supplierRowNum = 3;
            int supplierCellNum = 2;
            int shipmentRowNum = 4;
            int shipmentRowCell = 7;
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();

            IRow orderNoRow =  sheet.GetRow(sOrderNoRow);
            ICell orderNoCell =  orderNoRow.GetCell(sOrderNoCell);
            string salesOrderNo = orderNoCell.ToString();   //销售订单号
            //ICell OrderNoCell = orderNoRow.GetCell(6);    //订单号
            IRow bOrderRow = sheet.GetRow(bOrderNoRow);   //订单号
            ICell OrderNoCell = bOrderRow.GetCell(bOrderNoCell);
            IRow shipmentRow = sheet.GetRow(shipmentRowNum); //出货日期

            IRow supplierRow = sheet.GetRow(supplierRowNum);
            ICell supplierCell = supplierRow.GetCell(supplierCellNum);
            ICell shipmentCell = shipmentRow.GetCell(shipmentRowCell);  //出货日期
            ICell supplierCodeCell =  shipmentRow.GetCell(6); //供应商代码
            string supplier = supplierCell.ToString();   //供应商
            //string shipment = shipmentCell.ToString(); //出货日
            //System.DateTime shipmentDate = System.DateTime.Parse(shipmentCell); // 将 Excel 单元格的值转换为 DateTime 类型
            //string shipment = shipmentDate.ToString("yyyy/MM/dd"); // 自定义日期格式
            string shipment = null;
            if (shipmentCell.CellType == CellType.Numeric)
            {
                shipment = Convert.ToDateTime(shipmentCell.DateCellValue)
                .ToString("yyyy/MM/dd");
            }
            else
            {
                shipment = shipmentCell.ToString();
            }
            string supplierCode = supplierCodeCell.ToString(); //供应商代码
            string orderNo = OrderNoCell.ToString(); //订单号


            // 遍历从 C9 开始的所有单元格，直到遇到空单元格
            for (int rowIndex = startRow; rowIndex < sheet.LastRowNum; rowIndex++)
            {
                IRow row = sheet.GetRow(rowIndex);

                if (row == null)
                {
                    continue; // 如果行为空，跳过
                }

                ICell cell = row.GetCell(startCol);
                if (cell == null || cell.CellType == CellType.Blank)
                {
                    // 如果单元格为空或为空值，则停止遍历
                    break;
                }

                ICell cell9 =  row.GetCell(9);
                ICell cell8 = row.GetCell(8);
                string densoNum = cell.ToString();     //品番
                string shippingQty = cell9.ToString();  //出货数量
                if (string.IsNullOrEmpty(shippingQty)) {
                    shippingQty = cell8.ToString();
                }
                //需要一个方法根据品番查询数据库
                
                Dictionary<string, string> rowData = new Dictionary<string, string>();
                rowData["densoNum"] = densoNum;
                rowData["shippingQty"] = shippingQty;
                rowData["salesOrderNo"] = salesOrderNo;
                rowData["supplier"] = supplier;
                rowData["shipment"] = shipment;
                rowData["orderNo"] = orderNo;
                rowData["supplierCode"] = supplierCode;
                results.Add(rowData);
                // startCol++;
            }
            //遍历纳受领书 根据原单位表计算箱数
            results.ForEach(x =>
            {
                string sql = $"select part_name,box_cap,pallet_pct,veh_model,cust_num,denso_num,back_num,loc_num  from originalunit t where t.cust_name = 'FTEC' and recv_port = 'SP' and replace(t.cust_num,'-','')  = '{x["densoNum"]}'";
                DataTable data = DataHelper.Query(sql);
                if (data.Rows.Count == 0) {
                    throw new Exception("原单位表未找到该品番");
                }
                // 将 DataTable 转换为 Dictionary
                Dictionary<string, string> dbData = new Dictionary<string, string>();
                foreach (DataRow row in data.Rows) {
                    x["boxCap"]    = row["box_cap"].ToString();    //箱收容数
                    x["part_name"] = row["part_name"].ToString();  //品番名称
                    x["palletPct"] = row["pallet_pct"].ToString(); //托占比
                    x["vehModel"] =  row["veh_model"].ToString();                   //车型
                    //制品名称
                    x["custNum"]  = row["cust_num"].ToString();  //客户品番
                    x["densoNum"] = row["denso_num"].ToString(); //电装品番
                    x["backNum"]  = row["back_num"].ToString();  //背番号
                    x["locNum"]  = row["loc_num"].ToString(); //库位号




                }
            });
            //计算箱数
            results.ForEach(x =>
            {
                if (x.ContainsKey("boxCap") && x.ContainsKey("shippingQty"))
                {
                    int boxCap = int.Parse(x["boxCap"]);
                    int shippingQty = int.Parse(x["shippingQty"]);
                    decimal palletPct = decimal.Parse(x["palletPct"]);
                    int boxNum = (int)Math.Ceiling((double)shippingQty / boxCap);    //向上取整  箱数
                    int aliquotBoxNum = shippingQty / boxCap; //整箱数
                    int quantityOutBox = shippingQty % boxCap; //余数
                    decimal stackNum = boxNum * palletPct; //托数
                    x["boxNum"]         = boxNum.ToString();
                    x["aliquotBoxNum"]  = boxNum.ToString();
                    x["quantityOutBox"] = quantityOutBox.ToString(); 
                    x["stackNum"]       = stackNum.ToString();
                    x["shippingQty"]    = shippingQty.ToString();
                }
            });
            //Console.WriteLine("1111");
            return results;

        }


        //处理外箱
        // public string disposeBox(List<Dictionary<string, string>> list, string newFolderPath)
        // {
        //     string baseDirectory = AppDomain.CurrentDomain.BaseDirectory; //根目录
        //     string outBoxTemplatePath = Path.Combine(baseDirectory, ConfigurationManager.AppSettings["FTCEWXD"].ToString());   //新模板
        //     string outBoxPath = Path.Combine(newFolderPath, "外箱 箱单标识.xlsx");
        //     string newOutBoxDic = Path.Combine(Path.GetDirectoryName(outBoxPath), "TemporaryOuterBoxLabel");
        //     Directory.CreateDirectory(newOutBoxDic);
        //
        //     try
        //     {
        //         //打开模板文件
        //         using (FileStream readStream = new FileStream(outBoxTemplatePath, FileMode.Open, FileAccess.Read))
        //         {
        //             IWorkbook outBoxBook      = new XSSFWorkbook(readStream);
        //             ISheet    outBoxBookSheet = outBoxBook.GetSheetAt(0);
        //
        //             int index = 0;
        //             
        //             for (int i = 0; i < list.Count; i++)
        //             {
        //                 string densoNum       = list[i]["custNum"].Replace("-", ""); // 品番
        //                 string part_name      = list[i]["part_name"];                //品名
        //                 int    box_cap        = int.Parse(list[i]["boxCap"]);
        //                 int    boxNum         = int.Parse(list[i]["boxNum"]);         // 箱数
        //                 int    aliquotBoxNum  = int.Parse(list[i]["aliquotBoxNum"]);  // 整箱数
        //                 int    quantityOutBox = int.Parse(list[i]["quantityOutBox"]); // 端数
        //                 string salesOrderNo   = list[i]["salesOrderNo"];              // 销售订单号
        //                 string suppler        = list[i]["supplier"];                  // 供应商
        //                 string shipment       = list[i]["shipment"];                  // 出货日期
        //                 string orderNo        = list[i]["orderNo"];                   // 订单号
        //
        //                 string paddedBoxCap = box_cap.ToString().PadLeft(6, '0'); //二维码信息
        //
        //                 // 生成二维码字符串
        //                 string QRcode = $"{densoNum} {paddedBoxCap}";
        //                 byte[] qrImageBytes;
        //
        //                 using (var qrGenerator = new QRCodeGenerator())
        //                 using (var qrData = qrGenerator.CreateQrCode(QRcode, QRCodeGenerator.ECCLevel.Q))
        //                 using (var qrCode = new QRCode(qrData))
        //                 using (var originalQr =
        //                        qrCode.GetGraphic(10, Color.Black, Color.White, drawQuietZones: false)) // 关键：禁用白边
        //                 using (var resized = new Bitmap(80, 80))                                       // 创建缩放目标图片
        //                 using (var g = Graphics.FromImage(resized))
        //                 {
        //                     // 设置绘图参数
        //                     g.SmoothingMode     = SmoothingMode.AntiAlias;
        //                     g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        //                     g.PixelOffsetMode   = PixelOffsetMode.HighQuality;
        //
        //                     // 绘制缩放后的二维码
        //                     g.DrawImage(originalQr, 0, 0, 80, 80);
        //
        //                     // 将结果转为 byte[]
        //                     using (var ms = new MemoryStream())
        //                     {
        //                         resized.Save(ms, ImageFormat.Png);
        //                         qrImageBytes = ms.ToArray();
        //                     }
        //                 }
        //                 
        //                 
        //                 //填充数据
        //                 for (int j = 1; j <= boxNum; j++)
        //                 {
        //                     // 插入二维码图片
        //                     int      pictureIdx = outBoxBook.AddPicture(qrImageBytes, PictureType.PNG);
        //                     IDrawing drawing    = outBoxBookSheet.CreateDrawingPatriarch();
        //                     var      anchor     = new XSSFClientAnchor(512, 0, 1023, 255, 8, 2+index*19, 9, 4+index*19);
        //         
        //                     IPicture picture = drawing.CreatePicture(anchor, pictureIdx);
        //                     // 获取当前图片的尺寸（图片原始宽度、高度）
        //                     double originalWidth  = picture.GetImageDimension().Width;
        //                     double originalHeight = picture.GetImageDimension().Height;
        //         
        //                     // 设置宽度为原来的一半，保持高度不变
        //                     double newWidth  = originalWidth / 2;
        //                     double newHeight = originalHeight;
        //         
        //                     // 设置新的大小
        //                     picture.Resize(newWidth / originalWidth, newHeight / originalHeight);
        //
        //                     IRow row5  = outBoxBookSheet.GetRow(5 +index *19) ?? outBoxBookSheet.CreateRow(5+index*19);
        //                     IRow row6  = outBoxBookSheet.GetRow(6 +index *19) ?? outBoxBookSheet.CreateRow(6+index*19);
        //                     IRow row7  = outBoxBookSheet.GetRow(7 +index*19)            ?? outBoxBookSheet.CreateRow(7+index*19);
        //                     IRow row11 = outBoxBookSheet.GetRow(10+index*19)           ?? outBoxBookSheet.CreateRow(10+index*19);
        //
        //                     ICell cell2   = row5.GetCell(2  ) ?? row5.CreateCell(2  );
        //                     ICell cell7   = row5.GetCell(8  ) ?? row5.CreateCell(8  );
        //                     ICell cell62  = row6.GetCell(2  ) ?? row6.CreateCell(2  );
        //                     ICell cell72  = row7.GetCell(2  ) ?? row7.CreateCell(2  );
        //                     ICell cell74  = row7.GetCell(4  ) ?? row7.CreateCell(4  );
        //                     ICell cell77  = row7.GetCell(7  ) ?? row7.CreateCell(7  );
        //                     ICell cell111 = row11.GetCell(1 ) ?? row11.CreateCell(1 ); //品番
        //                     ICell cell113 = row11.GetCell(3 ) ?? row11.CreateCell(3 );            //品名
        //                     ICell cell117 = row11.GetCell(7 ) ?? row11.CreateCell(7 );            //数量
        //
        //                     cell2.SetCellValue(suppler);      // 供应商
        //                     cell7.SetCellValue(salesOrderNo); // 销售订单号
        //                     cell62.SetCellValue(shipment);    // 出货日期
        //                     cell72.SetCellValue(orderNo);     // 订单号
        //                     cell74.SetCellValue(j);           // 当前是第几箱
        //                     cell77.SetCellValue(boxNum);      // 总箱数
        //                     cell111.SetCellValue(densoNum);
        //                     cell113.SetCellValue(part_name);
        //                     if (j == boxNum && quantityOutBox > 0)
        //                     {
        //                         cell117.SetCellValue(quantityOutBox); //端数
        //                     }
        //                     else
        //                     {
        //                         cell117.SetCellValue(box_cap); //收容数
        //                     }
        //                     
        //                     index++;
        //                 }
        //                 
        //                 
        //             }
        //             
        //             
        //             DeleteAllRowsAfter(outBoxBookSheet, 19 * index);
        //             outBoxBook.SetPrintArea(
        //                 0,              // 工作表索引
        //                 0,              // 起始列（0 表示列 A）
        //                 8,              // 结束列（5 表示列 F）
        //                 0,              // 起始行（0 表示第 1 行）
        //                  19 * index -1  // 结束行（19 表示第 20 行）
        //             );
        //             
        //             // 保存修改后的文件到新文件流
        //             using (FileStream writeStream = new FileStream("D:\\合众唯一\\南沙电装\\02代码\\DENSOKB\\DENSOKB\\FTCE补给品\\test.xlsx", FileMode.Create, FileAccess.Write))
        //             {
        //                 outBoxBook.Write(writeStream);
        //             }
        //
        //             outBoxBook.Close();
        //             
        //             /// <param name="inputFile"></param> 输入路径
        //             /// <param name="outputFile"></param>输出路径
        //             /// <param name="direction"></param>方向 0 纵向 1 横向
        //             /// <param name="margin"></param>页边距
        //             /// <param name="autoscaling"></param>自动缩放
        //             /// <param name="scaling"></param>缩放比例
        //             //转pdf
        //             LiberOfficeHelper.ExcelOutOfPdf("D:\\合众唯一\\南沙电装\\02代码\\DENSOKB\\DENSOKB\\FTCE补给品\\test.xlsx","D:\\合众唯一\\南沙电装\\02代码\\DENSOKB\\DENSOKB\\FTCE补给品\\test.pdf"
        //                 ,new uno.Any((short)0),new uno.Any((short)0),new Any((short)1),new Any((short)100),new Any((short)1));
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         throw;
        //     }
        //
        //     return newOutBoxDic;
        // }
        public string disposeBoxPDF(List<Dictionary<string, string>> list, string newFolderPath)
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string outBoxTemplatePath = Path.Combine(baseDirectory, ConfigurationManager.AppSettings["FTCEWXDPDF"].ToString());

            string outBoxPath = System.IO.Path.Combine(newFolderPath, "外箱 箱单标识.xlsx");
            string newOutBoxDic = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(outBoxPath), "TemporaryOuterBoxLabel");
            Directory.CreateDirectory(newOutBoxDic);

            int totalBoxNum = 0;
            int index = 1;

            foreach (var item in list)
            {
                totalBoxNum += int.Parse(item["boxNum"]);
            }

            for (int i = 0; i < list.Count; i++)
            {
                string densoNum = list[i]["custNum"].Replace("-", "");    // 品番
                string densoNumQR = list[i]["custNum"];    // 品番
                string part_name = list[i]["part_name"];//品名
                int box_cap = int.Parse(list[i]["boxCap"]);
                int boxNum = int.Parse(list[i]["boxNum"]);   // 箱数
                int aliquotBoxNum = int.Parse(list[i]["aliquotBoxNum"]);   // 整箱数
                int quantityOutBox = int.Parse(list[i]["quantityOutBox"]);   // 端数
                string salesOrderNo = list[i]["salesOrderNo"];   // 销售订单号
                string suppler = list[i]["supplier"];   // 供应商
                string shipment = list[i]["shipment"]; // 出货日期
                string orderNo = list[i]["orderNo"]; // 订单号

                string paddedBoxCap = "";

                for (int j = 1; j <= boxNum; j++)
                {
                    // 构建唯一的文件名
                    string uniqueFileName = $"{System.IO.Path.GetFileNameWithoutExtension(outBoxPath)}_{i}_{j}.pdf";

                    string newOutBoxPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(outBoxPath), "TemporaryOuterBoxLabel", uniqueFileName);

                    try
                    {

                        PdfDocument doc = new PdfDocument();
                        doc.LoadFromFile(outBoxTemplatePath); // 你的模板路径

                        // 3. 获取第一页（或指定页）
                        PdfPageBase page = doc.Pages[0]; // 调整页码 if needed

                        

                        //orderNo 订单号
                        float orderNox = 112f;
                        float orderNoy = 107f;
                        PdfTrueTypeFont font = new PdfTrueTypeFont(new System.Drawing.Font("SimHei", 12f, FontStyle.Bold), true);
                        PdfBrush brush = PdfBrushes.Black; // 黑色文本
                        page.Canvas.DrawString(orderNo, font, brush, orderNox, orderNoy);

                        //salesOrderNo 销售订单号
                        float salesOrderNox = 455f;
                        float salesOrderNoy = 81f;
                        page.Canvas.DrawString(salesOrderNo, font, brush, salesOrderNox, salesOrderNoy);

                        //shipment 出货日
                        float shipmentx = 112f;
                        float shipmenty = 92f;
                        page.Canvas.DrawString(shipment, font, brush, shipmentx, shipmenty);

                        //j
                        float jx = 278f;
                        float jy = 107f;
                        page.Canvas.DrawString(index.ToString(), font, brush, jx, jy);

                        float boxNumx = 452f;
                        float boxNumy = 107f;
                        page.Canvas.DrawString(totalBoxNum.ToString(), font, brush, boxNumx, boxNumy);

                        //densoNum  品番
                        float densoNumx = 78f;
                        float densoNumy = 145f;
                        page.Canvas.DrawString(densoNum, font, brush, densoNumx, densoNumy);

                        //part_name 品名
                        float part_namex = 190f;
                        float part_namey = 145f;
                        page.Canvas.DrawString(part_name, font, brush, part_namex, part_namey);

                        float numberx = 440f;
                        float numbery = 145f;

                        if (j == boxNum && quantityOutBox > 0)
                        {
                            page.Canvas.DrawString(quantityOutBox.ToString(), font, brush, numberx, numbery);
                            //cell117.SetCellValue(quantityOutBox);   //端数
                            index += 1;
                            paddedBoxCap = quantityOutBox.ToString().PadLeft(6, '0');
                        }
                        else
                        {
                            page.Canvas.DrawString(box_cap.ToString(), font, brush, numberx, numbery);
                            index += 1;
                            //cell117.SetCellValue(box_cap);   //收容数
                            paddedBoxCap = box_cap.ToString().PadLeft(6, '0');
                        }



                        if (densoNumQR == "13080-0P030") { densoNumQR = densoNumQR + "-A"; }
                        densoNumQR = (densoNumQR ?? string.Empty).PadRight(15, '\u3000');
                        // 生成二维码字符串
                        string QRcode = $"{densoNumQR}{paddedBoxCap}";
       
                        byte[] qrImageBytes;

                        using (var qrGenerator = new QRCodeGenerator())
                        using (var qrData = qrGenerator.CreateQrCode(QRcode, QRCodeGenerator.ECCLevel.Q))
                        using (var qrCode = new QRCode(qrData))
                        using (var originalQr = qrCode.GetGraphic(10, Color.Black, Color.White, drawQuietZones: false))
                        using (var resized = new Bitmap(60, 60)) // 创建缩放目标图片
                        using (var g = Graphics.FromImage(resized))
                        {
                            // 设置高质量绘图参数
                            g.SmoothingMode = SmoothingMode.AntiAlias;
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                            // 绘制缩放后的二维码
                            g.DrawImage(originalQr, 0, 0, 60, 60);

                            // 将结果转为 byte[]
                            using (var ms = new MemoryStream())
                            {
                                resized.Save(ms, ImageFormat.Png);
                                qrImageBytes = ms.ToArray();
                            }
                        }


                        // 4. 将二维码 byte[] 转为 Spire.PDF 的 PdfImage
                        PdfImage qrCodeImage;
                        using (var ms = new MemoryStream(qrImageBytes))
                        {
                            qrCodeImage = PdfImage.FromStream(ms);
                        }

                        float desiredX = 496f;
                        float desiredY = 182f;

                        // 方法 1：直接使用坐标（假设 Y 是从顶部计算）
                        float yFromBottom = page.ActualSize.Height - desiredY - qrCodeImage.Height;
                        page.Canvas.DrawImage(qrCodeImage, desiredX, yFromBottom);

                        doc.SaveToFile(newOutBoxPath);
                        doc.Close();

                        Console.WriteLine($"二维码已插入，文件保存到: {newOutBoxPath}");
                    }
                    catch (System.Exception ex)
                    {
                        throw;
                    }
                }
            }
            return newOutBoxDic;
        }



        public string disposeBox(List<Dictionary<string,string>> list,string newFolderPath) {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;     //根目录
            // string outBoxTemplatePath = System.IO.Path.Combine(baseDirectory, "UploadFiles", "外箱 箱单标识.xlsx");
            string outBoxTemplatePath = Path.Combine(baseDirectory, ConfigurationManager.AppSettings["FTCEWXD"].ToString());
            //newFolderPath = "D:\\合众唯一\\南沙电装\\02代码\\DENSOKB\\DENSOKB\\Uploads";
            string outBoxPath = System.IO.Path.Combine(newFolderPath, "外箱 箱单标识.xlsx");
            string newOutBoxDic = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(outBoxPath), "TemporaryOuterBoxLabel");
            Directory.CreateDirectory(newOutBoxDic);
            for (int i = 0; i < list.Count; i++)
            {
                string densoNum = list[i]["custNum"].Replace("-","");    // 品番
                string part_name = list[i]["part_name"];//品名
                int box_cap = int.Parse(list[i]["boxCap"]);
                int boxNum = int.Parse(list[i]["boxNum"]);   // 箱数
                int aliquotBoxNum = int.Parse(list[i]["aliquotBoxNum"]);   // 整箱数
                int quantityOutBox = int.Parse(list[i]["quantityOutBox"]);   // 端数
                string salesOrderNo = list[i]["salesOrderNo"];   // 销售订单号
                string suppler = list[i]["supplier"];   // 供应商
                string shipment = list[i]["shipment"]; // 出货日期
                string orderNo = list[i]["orderNo"]; // 订单号

                string paddedBoxCap = box_cap.ToString().PadLeft(6, '0');
                //Console.WriteLine(paddedBoxCap);

                // 生成二维码字符串
                string QRcode = $"{densoNum} {paddedBoxCap}";
                byte[] qrImageBytes;

                using (var qrGenerator = new QRCodeGenerator())
                using (var qrData = qrGenerator.CreateQrCode(QRcode, QRCodeGenerator.ECCLevel.Q))
                using (var qrCode = new QRCode(qrData))
                using (var originalQr = qrCode.GetGraphic(10, Color.Black, Color.White, drawQuietZones: false)) 
                using (var resized = new Bitmap(80, 80)) // 创建缩放目标图片
                using (var g = Graphics.FromImage(resized))
                {
                    // 设置高质量绘图参数
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    // 绘制缩放后的二维码
                    g.DrawImage(originalQr, 0, 0, 80, 80);

                    // 将结果转为 byte[]
                    using (var ms = new MemoryStream())
                    {
                        resized.Save(ms, ImageFormat.Png);
                        qrImageBytes = ms.ToArray();
                    }
                }


                for (int j = 1; j <= boxNum; j++)
                {
                    // 构建唯一的文件名
                    string uniqueFileName = $"{System.IO.Path.GetFileNameWithoutExtension(outBoxPath)}_{i}_{j}.xlsx";
                    
                    string newOutBoxPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(outBoxPath), "TemporaryOuterBoxLabel", uniqueFileName);

                    try
                    {
                        // 打开复制的文件进行修改
                        using (FileStream readStream = new FileStream(outBoxTemplatePath, FileMode.Open, FileAccess.Read))
                        {
                            IWorkbook outBoxBook = new XSSFWorkbook(readStream);
                            ISheet outBoxBookSheet = outBoxBook.GetSheetAt(0);

                            // 插入二维码图片
                            int pictureIdx = outBoxBook.AddPicture(qrImageBytes, PictureType.PNG);
                            IDrawing drawing = outBoxBookSheet.CreateDrawingPatriarch();

                            var anchor = new XSSFClientAnchor(512, 0, 1023, 255, 8, 2, 9, 4);

                            IPicture picture = drawing.CreatePicture(anchor, pictureIdx);
                            // 获取当前图片的尺寸（图片原始宽度、高度）
                            double originalWidth = picture.GetImageDimension().Width;
                            double originalHeight = picture.GetImageDimension().Height;

                            // 设置宽度为原来的一半，保持高度不变
                            double newWidth = originalWidth / 2;
                            double newHeight = originalHeight;

                            // 设置新的大小
                            picture.Resize(newWidth / originalWidth, newHeight / originalHeight);
                            //picture.Resize(0.5);

                            // 填充数据
                            IRow row5 = outBoxBookSheet.GetRow(5) ?? outBoxBookSheet.CreateRow(5);
                            IRow row6 = outBoxBookSheet.GetRow(6) ?? outBoxBookSheet.CreateRow(6);
                            IRow row7 = outBoxBookSheet.GetRow(7) ?? outBoxBookSheet.CreateRow(7);
                            IRow row11 = outBoxBookSheet.GetRow(10) ?? outBoxBookSheet.CreateRow(10);

                            ICell cell2 = row5.GetCell(2) ?? row5.CreateCell(2);
                            ICell cell7 = row5.GetCell(8) ?? row5.CreateCell(8);
                            ICell cell62 = row6.GetCell(2) ?? row6.CreateCell(2);
                            ICell cell72 = row7.GetCell(2) ?? row7.CreateCell(2);
                            ICell cell74 = row7.GetCell(4) ?? row7.CreateCell(4);
                            ICell cell77 = row7.GetCell(7) ?? row7.CreateCell(7);
                            ICell cell111 = row11.GetCell(1) ?? row11.CreateCell(1); //品番
                            ICell cell113 = row11.GetCell(3) ?? row11.CreateCell(3); //品名
                            ICell cell117 = row11.GetCell(7) ?? row11.CreateCell(7); //数量

                            cell2.SetCellValue(suppler);   // 供应商
                            cell7.SetCellValue(salesOrderNo); // 销售订单号
                            cell62.SetCellValue(shipment); // 出货日期
                            cell72.SetCellValue(orderNo); // 订单号
                            cell74.SetCellValue(j);  // 当前是第几箱
                            cell77.SetCellValue(boxNum); // 总箱数
                            cell111.SetCellValue(densoNum);
                            cell113.SetCellValue(part_name);
                            if (j == boxNum && quantityOutBox > 0)
                            {
                                cell117.SetCellValue(quantityOutBox);   //端数
                            }
                            else {
                                cell117.SetCellValue(box_cap);   //收容数
                            }

                            // 保存修改后的文件到新文件流
                            using (FileStream writeStream = new FileStream(newOutBoxPath, FileMode.Create, FileAccess.Write))
                            {
                                outBoxBook.Write(writeStream);
                            }

                            outBoxBook.Close();
                        }

                    }
                    catch (System.Exception ex)
                    {
                       throw;
                    }
                }
            }

            return newOutBoxDic;
        }
        
        public void DeleteAllRowsAfter(ISheet sheet, int startRow)
        {
            int lastRow = sheet.LastRowNum;

            for (int i = lastRow; i >= startRow; i--)
            {
                IRow row = sheet.GetRow(i);
                if (row != null)
                {
                    sheet.RemoveRow(row);
                }
            }
        }


    }
}