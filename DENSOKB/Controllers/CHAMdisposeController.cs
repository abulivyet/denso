using DENSOKB.Helper;
using DENSOKB.Libs;
using DENSOKB.Models;
using HelpLog.CommomMethod;
using ICSharpCode.SharpZipLib.Zip;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Newtonsoft.Json.Linq;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using SharpCompress.Common;
using Spire.Pdf;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.AccessControl;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Script.Serialization;
using static DENSOKB.Helper.SpireSplit;
using Path = System.IO.Path;
using Rectangle = System.Drawing.Rectangle;


namespace DENSOKB.Controllers
{
    public class CHAMdisposeController: ApiController
    {
        CHAMService chamService =  new CHAMService();
        orderimportService orderservice = new orderimportService();
        // SpireHelper _spireHelper = new SpireHelper();

        [JwtAuthorize]
        [HttpPost]
        public object GetPageWeekList(Pagination paginationobj)
        {
            paginationobj.sidx = "court_summons,demanded_date";
            paginationobj.sord = "desc";
            DataTable data = chamService.getPageTB(paginationobj, paginationobj.queryJson);
            Dictionary<string, object> jo = new Dictionary<string, object>();
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            jo.Add("success", true);
            jo.Add("total", paginationobj.records);
            jo.Add("data", PulicHelper.ConvertDataTableToListOfDictionaries(data));
            return jo;
        }
        /// <summary>
        /// 出货指示查询
        /// </summary>
        /// <param name="paginationobj"></param>
        /// <returns></returns>
        [JwtAuthorize]
        [HttpPost]
        public object GetPageList(Pagination paginationobj)
        {
            paginationobj.sidx = "createdate";
            paginationobj.sord = "desc";
            JObject jqueryJo= DataHelper.ToJObject(paginationobj.queryJson);
            DataTable data = chamService.getPageTBout(paginationobj, jqueryJo.ToString());
            Dictionary<string, object> jo = new Dictionary<string, object>();
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            jo.Add("success", true);
            jo.Add("total", paginationobj.records);
            jo.Add("data", PulicHelper.ConvertDataTableToListOfDictionaries(data));
            return jo;
        }
        [JwtAuthorize]
        public HttpResponseMessage DownloadFile(OrderDownload orderDownload)
        {
            try
            {
                string filePath = string.Empty;
                //文件路径
                DataTable dt= orderservice.getinp_detail2(orderDownload.id.ToString());
                string type = string.Empty; 
                // 1 传票，2托看板，3箱标签，4指示书
                switch (orderDownload.type)
                {
                    case "1":
                        type = "传票";
                            break;
                    case "2":
                        type = "托标签";
                        break;
                    case "3":
                        type = "看板";
                        break;
                    case "4":
                        type = "出货指示单";
                        break;
                }
                DataRow[] dr= dt.Select($"filetype='{type}'");
                if (dr.Length == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "未找到解析后的数据，请重新解析");
                }
                filePath = dr[0]["Stacklabel"].ToString();
                // 检查文件是否存在
                if (!File.Exists(filePath))
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "未找到文件");
                }
                // 获取文件信息
                var fileInfo = new FileInfo(filePath);
                var fileName = fileInfo.Name;
                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var encodedFileName = HttpUtility.UrlEncode(fileName);
                // 设置响应头
                var response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StreamContent(fileStream);
                response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = encodedFileName
                };

                orderimportService.DownloadCountPlus(orderDownload.id.ToString());

                return response;
            }
            catch (Exception ex)
            {
                // 错误处理
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }


        public DataTable getdetailForMainid(List<OrderDownload> mainid)

        {
            try
            {
                var strSql = new StringBuilder();
                strSql.Append("Select * from ord_inp_detail2 where 1=1");
                string condition = string.Join(" or ", mainid.Select(t => $" mainId='{t.id}' "));
                strSql.Append($" and ({condition})");
                strSql.Append(" order by CAST(r1 AS SIGNED)");
                return DataHelper.Query(strSql.ToString());
            }
            catch (Exception e)
            {
                throw e;
            }
        }


        [JwtAuthorize]
        public HttpResponseMessage DownloadFileZipRowid(List<OrderDownload> orderDList)
        {
            string fileUpload = ConfigurationManager.AppSettings["fileUpload"].ToString();
            bool TP = false;
            try
            {
                if (orderDList.Count == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "未选择数据");
                }
                List<inpdetail2> inpdetail2List = new List<inpdetail2>();// 可能前台只勾选取套票，所以得用List接收
                DataTable detailDT = getdetailForMainid(orderDList);

                if (detailDT.Rows.Count == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.InternalServerError, "未找到解析后的数据，请重新解析");
                }

                foreach (var entity in orderDList)
                {
                    string type = string.Empty;
                    if (entity.type.IndexOf("1") > -1) { type += "传票;"; }
                    if (entity.type.IndexOf("2") > -1) { type += "托标签;"; }
                    if (entity.type.IndexOf("3") > -1) { type += "看板;"; }
                    if (entity.type.IndexOf("4") > -1) { type += "出货指示单;"; }
                    if (entity.type.IndexOf("5") > -1) { 
                        type += "确认表;";
                        orderservice.EditConfirmRemark(entity.id.ToString(), "佛山本田", "");
                    }
                    // 勾选单个文件
                    if (!string.IsNullOrEmpty(type))
                    {
                        string[] types = type.Split(';');
                        string condition = string.Join(" or ", types.Select(t => $"filetype='{t}'"));
                        DataRow[] oneFile = detailDT.Select($" mainid='{entity.id}' and ({condition})");
                        foreach (var item in oneFile)
                        {
                            inpdetail2 inpMD = new inpdetail2();
                            inpMD.Stacklabel = item["Stacklabel"].ToString();
                            inpMD.mainId = entity.id.ToString();
                            inpdetail2List.Add(inpMD);
                        }
                    }
                    if (entity.type.IndexOf("7") > -1) TP = true;
                    // 勾选套票
                    if (TP)
                    {
                        List<string> listPsth = new List<string>();
                        DataRow[] MDRow = detailDT.Select($" mainid='{entity.id}'");
                        foreach (var item in MDRow)
                        {
                            listPsth.Add(item["Stacklabel"].ToString());
                        }

                        var zipfilename = System.IO.Path.GetFileNameWithoutExtension(orderDList[0].zipfilename);
                        string TPpath = SpireHelper.MergeFiles(listPsth, fileUpload+"\\CHAM\\TP\\"+zipfilename + "_套票.pdf");
                        if (!string.IsNullOrEmpty(TPpath))
                        {

                            inpdetail2 inp = new inpdetail2();
                            inp.Stacklabel = TPpath;
                            inp.mainId = entity.id.ToString();
                            inpdetail2List.Add(inp);
                        }
                    }
                    orderimportService.DownloadCountPlus(entity.id.ToString());

                }
                string mess = "";
                // 返回压缩包
                bool isok = returnZip(orderDList, inpdetail2List, out mess);

            }
            catch (Exception ex)
            {
                XErrorLogToolMDM.WriteLog("导出失败，" + ex.Message);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "导出失败，" + ex.Message);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }
        
        
        public bool returnZip(List<OrderDownload> orderDList, List<inpdetail2> inpdetail2List, out string message)
            {
                bool isok = true;
                string tempDir = null;
                message = "";
                try
                {
                    tempDir = Path.Combine(HttpContext.Current.Server.MapPath("~/Uploads"), "Temp_" + Guid.NewGuid().ToString());
                    Directory.CreateDirectory(tempDir);
                    
                    foreach (var order in orderDList)
                    {
                        FileInfo f_temp      = new FileInfo(order.zipfilename);
                        string   saveTempDir = Path.Combine(tempDir, f_temp.Name.Replace(f_temp.Extension, ""));


                        Directory.CreateDirectory(saveTempDir);

                        List<inpdetail2> itemlist = inpdetail2List.Where(t => t.mainId == order.id.ToString()).ToList();

                        foreach (inpdetail2 row in itemlist)
                        {
                            FileInfo Stacklabel = new FileInfo(row.Stacklabel);
                            Stacklabel.CopyTo(Path.Combine(saveTempDir, Stacklabel.Name),true);
                        }
                    }
                    
                    DirectoryInfo dir_tempDir = new DirectoryInfo(tempDir);

                    FileInfo [] finalFiles = dir_tempDir.GetFiles("*.*", SearchOption.AllDirectories);

                    if(finalFiles.Length>1)
                    {
                        //zip打包

                        string ZipPath = Path.Combine(HttpContext.Current.Server.MapPath("~/Uploads"), $"{DateTime.Now:yyyyMMddHHmmss}.zip");

                        System.IO.Compression.ZipFile.CreateFromDirectory(tempDir, ZipPath);

                        DownloadZip(ZipPath, Path.GetFileNameWithoutExtension(ZipPath));

                        File.Delete(ZipPath);
                    }
                    else
                    {

                        DownloadPdf(finalFiles[0].FullName, Path.GetFileNameWithoutExtension(finalFiles[0].FullName));
                        //直接返回文件
                    }

                    Directory.Delete(tempDir, true);

                }
                catch (Exception e)
                {
                    message = e.Message;
                    isok = false;
                }
                return isok;
            }
        
        private static void DownloadZip(string filePath, string zipName)
        {
            HttpResponse response = HttpContext.Current.Response;
            response.Clear();
            response.ContentType = "application/zip";
            response.AppendHeader("Content-Disposition", $"attachment; filename={HttpUtility.UrlEncode(zipName)}.zip");
            response.TransmitFile(filePath);
        
            response.Flush();
        }

        private static void DownloadPdf(string filePath, string zipName)
        {
            HttpResponse response = HttpContext.Current.Response;
            response.Clear();
            response.ContentType = "application/pdf";
            response.AppendHeader("Content-Disposition", $"attachment; filename={HttpUtility.UrlEncode(zipName)}.pdf");
            response.TransmitFile(filePath);

            response.Flush();
        }
        

            /// <summary>
            /// 佛山本田
            /// </summary>
            /// <param name="chamInputDto"></param>
            /// <returns></returns>
            [HttpPost]
        public ResParameter CompressedFileCHAM(ChamInputDto chamInputDto)
        {
            try
            {
                return disposeDeliveryNote(chamInputDto);
                // return ResParameter.Success("ok");
            }
            catch (Exception e)
            {
                
                Console.WriteLine(e);
                return ResParameter.Fail(e.Message);
            }

            
        }

        [HttpPost]
        public ResParameter DeleteRecords([FromBody] List<int> ids)
        {
            try
            {
                if (ids == null)
                { 
                    return ResParameter.Fail("未接收到数据"); 
                }
                chamService.DeleteWeekProject(ids);
                return ResParameter.Success("操作成功");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return ResParameter.Fail(e.Message);
            }


        }



        /// <summary>
        /// 保存周订单
        /// </summary>
        /// <param name="fileData"></param>
        public async Task saveWeekOrder(byte [] fileData)
        {
            try
            {
                var chamOrdWeeks = new List<CHAMOrdWeek>();
                
                using (var memoryStream = new MemoryStream(fileData))
                {
                    XSSFWorkbook workbook = new XSSFWorkbook(memoryStream);

                    ISheet sheet = workbook.GetSheetAt(0);

                    // 获取行数和列数
                    int rowCount = sheet.PhysicalNumberOfRows;
                    for (int rowIndex = 1; rowIndex < rowCount; rowIndex++)
                    {
                        IRow row = sheet.GetRow(rowIndex);
                        
                        if (row == null) continue;  // 如果某行为空，则跳过
                        Console.Write(GetCellStringValue(row.GetCell(0)));
                        var chamOrdWeek = new CHAMOrdWeek
                        {
                            ordStatus        = GetCellStringValue(row.GetCell(0)),
                            orderCode        = GetStringCellValue(row.GetCell(1)),
                            publishDate      = GetCellStringValue(row.GetCell(2)),
                            cusName          = GetStringCellValue(row.GetCell(3)),
                            courtSummons     = GetStringCellValue(row.GetCell(4)),
                            materialCode     = GetStringCellValue(row.GetCell(5)),
                            materialName     = GetStringCellValue(row.GetCell(6)),
                            projectCode      = GetStringCellValue(row.GetCell(7)),
                            demandedDate     = GetDateTimeCellValue(row.GetCell(8)),
                            receivingTime    = GetStringCellValue(row.GetCell(9)),
                            quantity         = GetStringCellValueNum(row.GetCell(10)),
                            unit             = GetStringCellValue(row.GetCell(11)),
                            orderType        = GetStringCellValue(row.GetCell(12)),
                            receivingAddress = GetStringCellValue(row.GetCell(13)),
                            movingInCode     = GetStringCellValue(row.GetCell(14))
                        };

                        chamOrdWeeks.Add(chamOrdWeek);
                        
                    }
                }
                
                //需要先判断这个excel是不是上传过 
                //根据订单号和传票
                foreach (CHAMOrdWeek item in chamOrdWeeks)
                {
                    CHAMOrdWeek oldWeekOrder = chamService.GetOldWeekOrder(item.orderCode, item.courtSummons);
                    if (oldWeekOrder == null)
                    {
                        chamService.InsertOrdWeek(item);
                    }
                    else
                    {
                        bool updated = false;

                        if (DateTime.TryParse(item.ordStatus, out DateTime date))
                        {
                            oldWeekOrder.demandeDatedNew = item.ordStatus;
                            updated                      = true;
                        }

                        if (oldWeekOrder.quantity != item.quantity)
                        {
                            oldWeekOrder.quantity = item.quantity;
                            updated               = true;
                        }

                        if (updated)
                        {
                            chamService.UpdateWeekOrder(oldWeekOrder);
                        }
                        
                    }
                }
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }

        /// <summary>
        /// 处理CHAM托标签
        /// </summary>
        /// <param name="inputPth"></param>
        [HttpPost]
        public ResParameter DisposeStackLabel(ChamInputDto chamInputDto)
        {
            try
            {
                XErrorLogToolMDM.WriteLog("托标签开始处理");
                string basePath = "";
                if (!string.IsNullOrEmpty(chamInputDto.zipPath)) {
                    // 获取文件所在的文件夹
                    string tempFolder = Path.GetDirectoryName(chamInputDto.zipPath);

                    // 再往上一级，得到“托标签”文件夹路径
                    basePath = Directory.GetParent(tempFolder).FullName;
                }

                //创建临时文件夹
                string uniqueFolderName = "temp_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string outputFolder = Path.Combine(basePath, uniqueFolderName);
                Directory.CreateDirectory(outputFolder);

                SpireHelper.PdfSplit(chamInputDto.zipPath, outputFolder, 0, 1, 1, EnumNameRule.Customize, new Rectangle(200, 80, 141, 30), new Rectangle());
                XErrorLogToolMDM.WriteLog("托标签切割文件夹" + outputFolder);
                string[] splitFiles = Directory.GetFiles(outputFolder, "*.pdf");
                if (splitFiles.Length == 0)
                {
                    throw new Exception("PDF 拆分失败：未生成拆分文件。");
                }

                List<CHAMStackLabel> cHAMStackLabels = new List<CHAMStackLabel>();
                foreach (var item in splitFiles)
                {
                    CHAMStackLabel cHAMStackLabel = new CHAMStackLabel();
                    //读取托标签中的出货指示号
                    string stackLabelCode = SpireHelper.GetText(item, new Rectangle(200, 84, 150, 15), 0);  //托标签号
                    string shippingCode = SpireHelper.GetText(item, new Rectangle(200, 119, 150, 15), 0); //出货指示单号
                    string fileName = Path.GetFileNameWithoutExtension(item);
                    cHAMStackLabel.shippingCode = shippingCode;
                    cHAMStackLabel.stackLabelCode = stackLabelCode;
                    cHAMStackLabel.inputPath = item;
                    cHAMStackLabel.stackLabelName = fileName;

                    cHAMStackLabels.Add(cHAMStackLabel);
                }

                chamService.InsertStackLabel(cHAMStackLabels);
                XErrorLogToolMDM.WriteLog("托标签拆分完成");
                return ResParameter.Success();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return ResParameter.Fail(e.Message);
                // throw;
            }

        }

        public ResParameter disposeDeliveryNote(ChamInputDto chamDto)
        {
            // 检查输入文件是否存在
            if (!File.Exists(chamDto.zipPath))
            {
                throw new FileNotFoundException($"输入的PDF文件不存在。 {chamDto.zipPath}", chamDto.zipPath);
            }
            try
            {
                //D:\合众唯一\南沙电装\02代码\DENSOKB\DENSOKB\Uploads\CHAM\送货单\指示书.pdf
                string dicPaath = Path.GetDirectoryName(chamDto.zipPath);
                string uniqueFolderName = "temp_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                string basePath = Path.Combine(dicPaath, uniqueFolderName);
                //出货指示号
                var fileFelieryNoteCode = new StringBuilder();
                string filepDate = "";//new StringBuilder();
                var fileBin = new StringBuilder();
                //string outDate = null;

                SpireHelper.PdfSplit(chamDto.zipPath, basePath, 0, 1, 1, SpireSplit.EnumNameRule.Customize, new Rectangle(200, 80, 141, 30), new Rectangle());


                List <inpdetail2>  list = new List<inpdetail2>();
                var pageDateList = new List<(int PageIndex, DateTime Date)>();

                PdfDocument doc = new PdfDocument();
                doc.LoadFromFile(chamDto.zipPath);

                // 1. 先提取每页日期
                for (int i = 0; i < doc.Pages.Count; i++)
                {
                    string text = SpireHelper.GetText(chamDto.zipPath, new Rectangle(200, 114, 80, 30), i);   //原114

                    // 假设提取出的日期是 "2025-10-30"
                    var match = System.Text.RegularExpressions.Regex.Match(text, @"\d{4}-\d{2}-\d{2}");
                    if (match.Success)
                    {
                        DateTime date = DateTime.Parse(match.Value);
                        //outDate = match.Value;
                        pageDateList.Add((i, date));
                    }
                    else
                    {
                        // 如果未识别到日期，可按需要决定是否跳过
                        Console.WriteLine($"第 {i + 1} 页未找到日期");
                    }
                }

                // 2. 根据日期排序（升序或降序）
                var sortedPages = pageDateList.OrderBy(x => x.Date).ToList();

                //for (int i = 0; i < doc.Pages.Count; i++)
               for (int index = 0; index < sortedPages.Count; index++)
               {
                  int i = sortedPages[index].PageIndex;
                  //{

                    string extractTextFromPdf = SpireHelper.GetText(chamDto.zipPath, new Rectangle(0, 0, 594, 790), i);

                    List<ShipmentItem> items = ParseShipmentItems(extractTextFromPdf);
                    List<CHAMOrdWeek> ordWeeks = findSummons(items);   //   获取到传票号 集合中没有的是补给品 

                    if (items.Count > ordWeeks.Count)
                    {
                        //有补给品
                        orderImport orderImport = new orderImport();
                        orderImport.id = int.Parse(chamDto.mainId);
                        orderImport.hassupplies = "是";

                        chamService.updateSupply(orderImport);
                    }
                    else {
                        orderImport orderImport = new orderImport();
                        orderImport.id = int.Parse(chamDto.mainId);
                        orderImport.hassupplies = "否";

                        chamService.updateSupply(orderImport);
                    }

                        // 暂存每组文件（一个文件夹就是一组）看板
                        List<List<string>> groupPagesList = new List<List<string>>();
                    List<string> Summons = new List<string>();

                    // // 正则提取页码
                    Regex regex = new Regex(@"_Page_(\d+)\.pdf$", RegexOptions.IgnoreCase);
                    foreach (CHAMOrdWeek chamOrdWeek in ordWeeks)
                    {
                        try
                        {
                            string courtSummons = chamOrdWeek.courtSummons;  //传票号
                            Summons.Add(courtSummons);

                            //根据传票号找到对应的看板路径
                            string boardFolderPath = FindBoardBySummons(courtSummons);
                            // 如果找到了有效路径则加入列表

                            if (!string.IsNullOrEmpty(boardFolderPath) && Directory.Exists(boardFolderPath))
                            {
                                // 获取该文件夹下的所有 PDF 文件
                                string[] pdfFiles = Directory.GetFiles(boardFolderPath, "*.pdf");

                                if (pdfFiles.Length == 0)
                                {
                                    Console.WriteLine($"文件夹中没有找到PDF文件：{boardFolderPath}");
                                    throw new Exception($"获取看板文件失败，文件夹中没有找到PDF文件：{boardFolderPath}");
                                }

                                //  提取该组的 PDF 文件，并按页码排序
                                var sortedGroup = pdfFiles
                                    .Select(path =>
                                    {
                                        var match = regex.Match(Path.GetFileName(path));
                                        int page = match.Success ? int.Parse(match.Groups[1].Value) : int.MaxValue;
                                        return (page, path);
                                    })
                                    .OrderBy(x => x.page)
                                    .Select(x => x.path)
                                    .ToList();
                                groupPagesList.Add(sortedGroup);
                            }
                            else
                            {
                                Console.WriteLine($"无效路径或文件夹不存在：{boardFolderPath}");
                                throw new Exception($"获取看板文件失败，无效路径或文件夹不存在：{boardFolderPath}");
                            }

                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                        }
                    }



                    FileInfo[] files = groupPagesList
                        .SelectMany(group => group)         // 将 List<List<string>> 扁平化为 List<string>
                        .Select(path => new FileInfo(path)) // 转为 FileInfo
                        .ToArray();                         // 转为数组

                    //获取出货指示单编号
                    string delieryNoteCode = SpireHelper.GetText(chamDto.zipPath, new Rectangle(193, 85, 300, 20), i);
                    XErrorLogToolMDM.WriteLog("出货指示号:" + delieryNoteCode);
                    fileFelieryNoteCode.Append(delieryNoteCode).Append("  ");
                    string pDate = SpireHelper.GetText(chamDto.zipPath, new Rectangle(196, 108, 76, 35), i);
                    filepDate = pDate.ToString();
                    string bin = SpireHelper.GetText(chamDto.zipPath, new Rectangle(435, 112, 30, 35), i);
                    fileBin.Append(bin).Append(" ");
                    string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    string SummonFileName = delieryNoteCode.Substring(1, 7) + "传票.pdf";
                    string SummonOutPath = Path.Combine(chamDto.basePath, "CHAM", delieryNoteCode.Replace("-", "").Replace("#", ""), SummonFileName);
                    Directory.CreateDirectory(Path.Combine(chamDto.basePath, "CHAM", delieryNoteCode.Replace("-", "").Replace("#", "")));
                    DisposeSummons(Summons, SummonOutPath);
                    //保存传票信息  明细表
                    inpdetail2 detailSummons = new inpdetail2();
                    detailSummons.mainId = chamDto.mainId;
                    detailSummons.ordercode = delieryNoteCode;
                    detailSummons.filetype = "传票";
                    detailSummons.rowcode = 4;
                    detailSummons.Stacklabel = SummonOutPath;

                    string fileName = delieryNoteCode.Substring(1, 7) + "看板.pdf";
                    string boardOutPath = Path.Combine(chamDto.basePath, "CHAM", delieryNoteCode.Replace("-","").Replace("#",""), fileName);
                    Directory.CreateDirectory(Path.Combine(chamDto.basePath, "CHAM", delieryNoteCode.Replace("-", "").Replace("#", "")));
                    SpireHelper.Merge(4, 1, files, boardOutPath, true);
                   
                    
                    //生成确认表
                    string confirmPath = DispostConfirm(items, delieryNoteCode, chamDto, SummonOutPath);
                    inpdetail2 detailConfirm = new inpdetail2();
                    detailConfirm.mainId = chamDto.mainId;
                    detailConfirm.ordercode = delieryNoteCode;
                    detailConfirm.filetype = "确认表";
                    detailConfirm.rowcode = 1;
                    detailConfirm.Stacklabel = confirmPath;
                    list.Add(detailConfirm);


                    //根据出货知道单编号找到对应的托标签
                    List<CHAMStackLabel> chamStackLabels = chamService.FindStackLabelByShippingCode(delieryNoteCode);
                    //明细表保存托标签路径
                    foreach (CHAMStackLabel chamStackLabelItem in chamStackLabels)
                    {
                        inpdetail2 detailStackLabel = new inpdetail2();
                        detailStackLabel.mainId = chamDto.mainId;
                        detailStackLabel.ordercode = delieryNoteCode;
                        detailStackLabel.filetype = "托标签";
                        detailStackLabel.rowcode = 2;
                        detailStackLabel.Stacklabel = chamStackLabelItem.inputPath;
                        list.Add(detailStackLabel);
                        XErrorLogToolMDM.WriteLog("明细行保存托标签路径" + detailStackLabel.Stacklabel);
                    };

                    //支持多页出货指示    去basePath中找出货指示号为extractTextFromPdf的
                    string[] detaildeliveryNoteFiles = Directory.GetFiles(basePath);
                    var foundFile = detaildeliveryNoteFiles.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals(delieryNoteCode, StringComparison.OrdinalIgnoreCase));

                    inpdetail2 detaildeliveryNote = new inpdetail2();
                    detaildeliveryNote.mainId = chamDto.mainId;
                    detaildeliveryNote.ordercode = delieryNoteCode;
                    detaildeliveryNote.filetype = "出货指示单";
                    detaildeliveryNote.rowcode = 3;
                    detaildeliveryNote.Stacklabel = foundFile;
                    list.Add(detaildeliveryNote);
                    list.Add(detaildeliveryNote);

                    
                    list.Add(detailSummons);

                    //保存看板路径 
                    inpdetail2 detailBoard = new inpdetail2();
                    detailBoard.mainId = chamDto.mainId;
                    detailBoard.ordercode = delieryNoteCode;
                    detailBoard.filetype = "看板";
                    detailBoard.rowcode = 5;
                    detailBoard.Stacklabel = boardOutPath;
                    list.Add(detailBoard);

                }

                orderimportService orderimportService = new orderimportService();
                orderimportService.InsertDetail2(list);

                var result = ResParameter.Success(
                    data: null,
                    ordercode: fileFelieryNoteCode.ToString(),
                    bin: fileBin.ToString(),//bin,
                    fdate: filepDate.ToString()//pDate
                );

                return result; // ResParameter.Success();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return ResParameter.Fail(e.Message);
            }
            
        }

        /// <summary>
        /// 找到对应的传票文件进行穿透
        /// </summary>
        /// <param name="summonsPaths"></param>
        public void DisposeSummons(List<string> summonsPaths,string outPutPath)
        {
            try
            {
                DataTable  table     = chamService.getCourtSummons(summonsPaths);
                if (table.Rows.Count == 0) { throw new Exception("根据周订单未找到对应传票号，DisposeSummons"); }
                FileInfo[] summons   = ConvertDataTableToFileInfos(table);
                FileInfo[]        fileInfos = SpireHelper.SortA4(summons, 2, 1);
                SpireHelper.Merge(2,1,fileInfos,outPutPath,false);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        public FileInfo[] ConvertDataTableToFileInfos(DataTable table)
        {
            if (table == null || table.Rows.Count == 0) return new FileInfo[0];

            List<FileInfo> files = new List<FileInfo>();

            foreach (DataRow row in table.Rows)
            {
                if (row["path"] != DBNull.Value)
                {
                    string path = row["path"].ToString();

                    // 可选：判断路径是否有效且文件存在
                    if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    {
                        files.Add(new FileInfo(path));
                    }
                }
            }

            return files.ToArray();
        }


        /// <summary>
        /// 处理确认表
        /// </summary>
        /// <param name="items"></param>
        public string DispostConfirm(List<ShipmentItem> items,string delieryNoteCode,ChamInputDto chamInputDto,string SummonOutPath)
        {
            string confirmPath       = ConfigurationManager.AppSettings["CHAMQRB"].ToString();
            string summonsCreateTime = items[0].OutDate; //SpireHelper.GetText(SummonOutPath,new Rectangle(24,188,53,15),0).Replace("-","/");
            // List<CHAMOrdWeekWithOrig> list        = new List<CHAMOrdWeekWithOrig>();
            CHAMconfirm  chaMconfirm = new CHAMconfirm();
            chaMconfirm.summonsCreatePerson = chamInputDto.createPerson;
            chaMconfirm.summonsCreateTime = DateTime.Now.ToString("yyyy-MM-dd");
            //summonsCreateTime;//SpireHelper.GetText(SummonOutPath,new Rectangle(24,180,53,15),0);    //传票做成日期
            try
            {
                chaMconfirm.title        = "CHAM 制品集货・出货异品・欠品照合确认表";
                chaMconfirm.cusName      = "CHAM（佛山本田）";
                chaMconfirm.carGroup     = "-";
                chaMconfirm.cusCode      = "40005150";
                chaMconfirm.shipmentDate = summonsCreateTime;//DateTime.Now.ToString("yyyy/MM/dd");
                chaMconfirm.type         = "佛山本田";
                
                //查询备注信息
                string confirmRemark = orderservice.GetConfirmRemark(chamInputDto.mainId);
                chaMconfirm.confirmRemark = confirmRemark;
                
                foreach (ShipmentItem shipmentItem in items)
                {
                    var demandedDate = shipmentItem.InDate.ToString();
                    var chamOrdWeek = chamService.getChamOrdWeeks(
                        shipmentItem.PartNumber, shipmentItem.Line, demandedDate, shipmentItem.Quantity.ToString());
                    if (chamOrdWeek == null)
                    {
                        chamOrdWeek = chamService.getChamOrdWeeks(
                            shipmentItem.PartNumber, shipmentItem.Line, demandedDate);
                    }


                    if (chamOrdWeek != null)
                    {
                        chamOrdWeek.quantity = shipmentItem.Quantity.ToString();
                        chamOrdWeek.cpdate = chamOrdWeek.demandedDate?.ToString("yyyy/MM/dd")   ?? "";
                        chamOrdWeek.cptime = chamOrdWeek.receivingTime;
                        chamOrdWeek.orderCode = chamOrdWeek.courtSummons;
                        chaMconfirm.details.Add(chamOrdWeek);
                        // list.Add(chamOrdWeek);
                    }
                }

                string baseDirectory = delieryNoteCode.Replace("-", "").Replace("#", "");
                string fileName = delieryNoteCode.Substring(1, 7) + "确认表.xls";
                //string fileName = Path.Combine(HttpContext.Current.Server.MapPath("~/Uploads"), $"{DateTime.Now:yyyyMMddHHmmss}.xls");
                string confirmOutPath = Path.Combine(chamInputDto.basePath,"CHAM", baseDirectory, fileName);
                Directory.CreateDirectory(Path.Combine(chamInputDto.basePath,"CHAM", baseDirectory));
                string editConfirmExcel = PulicHelper.EditConfirmExcel(confirmPath, confirmOutPath,chaMconfirm);
                PulicHelper.saveConfirm(chamInputDto.mainId,confirmOutPath,editConfirmExcel);
                List<OrderInfo> orderInfos = new List<OrderInfo>();
                foreach (var item in chaMconfirm.details)
                {
                    OrderInfo orderInfo = new OrderInfo();
                    orderInfo.ShipmentDate = chaMconfirm.shipmentDate;
                    orderInfo.ArrivalDueDate = chaMconfirm.shipmentDate;
                    orderInfo.OrderCode = item.orderCode;
                    orderInfo.CustomerPartNumber = item.materialCode;
                    orderInfo.DnPartNumber = item.DensoNum;
                    orderInfo.Capacity = item.BoxCap ?? 0;
                    double quantityVal = PulicHelper.ParseToDoubleOrZero(item.quantity);
                    orderInfo.BoardCount = int.Parse(quantityVal.ToString());
                    orderInfo.InclusionQuantity = int.Parse(item.quantity);
                    orderInfo.Category = "CHAM";
                    orderInfo.planCode = item.projectCode;
                    orderInfo.ReceiptPlace = item.movingInCode;
                    orderInfos.Add(orderInfo);
                }

                //保存出货指示信息之前需要先清除之前的


                PulicHelper.DeleteOrderInfos(orderInfos);
                PulicHelper.SaveOrderInfos(orderInfos);

                return editConfirmExcel;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
            
        }

        


        /// <summary>
        /// 通过传票号找到对应的看板
        /// </summary>
        /// <param name="summonsCode"></param>
        /// <returns></returns>
        public string FindBoardBySummons(string summonsCode)
        {
            try
            {
                return chamService.FindBoardBySummons(summonsCode);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
        }


        /// <summary>
        /// 上传传票后对传票进行拆分
        /// </summary>
        /// <param name="summonPdfPath"></param>
        /// <exception cref="FileNotFoundException"></exception>
        [HttpPost]
        public ResParameter DisposeSummonPdf(ChamInputDto chamInputDto)
        {
            try
            {
                if (!File.Exists(chamInputDto.zipPath))
                    throw new FileNotFoundException("PDF 文件不存在", chamInputDto.zipPath);
                
                string baseFolder   = Path.GetDirectoryName(chamInputDto.zipPath);
                string baseFileName = Path.GetFileNameWithoutExtension(chamInputDto.zipPath);
                string outPutPath   = Path.Combine(baseFolder, baseFileName);
                Directory.CreateDirectory(outPutPath);
                //  extractRegion = new RectangleF(75, 240, 210, 35);
                //SpireHelper.PdfSplit(chamInputDto.zipPath, outPutPath, 0, 2, 1,SpireSplit.EnumNameRule.Customize
                //    , new System.Drawing.Rectangle(108,246,300,10), new System.Drawing.Rectangle(0, 38, 842, 516));
                SpireHelper.PdfSplit(chamInputDto.zipPath, outPutPath, 0, 2, 1, SpireSplit.EnumNameRule.Customize
                    , new System.Drawing.Rectangle(75, 240, 210, 30), new System.Drawing.Rectangle(0, 35, 842, 540));
                //将文件夹内部的文件保存至数据库
                chamService.saveCourtSummons(outPutPath);
                return ResParameter.Success();
            }
            catch (Exception ex)
            {
                return ResParameter.Fail(ex.Message);
                // throw;
            }
        }
        
        //上传的看板进行裁切
        [HttpPost]
        public ResParameter DisposeBoard(ChamInputDto chamInputDto)
        {
            try
            {
                List<CHAMBoard> chamBoards = new List<CHAMBoard>();
                // CHAMBoard    chamBoard = new CHAMBoard();
                // chamBoard.boardPath = chamInputDto.zipPath; //暂时注释
                // string boardName = Path.GetFileNameWithoutExtension(chamInputDto.zipPath);
                // chamBoard.boardName = boardName;         //这里暂时注掉
                string boardName = "tempBoard"; // 
                string outputDir = Path.Combine(Path.GetDirectoryName(chamInputDto.zipPath), boardName);
                // chamBoard.boardDictory = outputDir;//暂时注释
                Directory.CreateDirectory(outputDir);
                SpireHelper.PdfSplit(chamInputDto.zipPath,outputDir,0,4,1,SpireSplit.EnumNameRule.CustomizeAuto,
                    new Rectangle(210, 55, 175, 18),
                    
                    new Rectangle(0, 16, 597, 796));
                
                //读取新创建的临时文件中的看板名称 根据名称创建新的文件夹 并且把对应的看板移动至对应文件夹
                string[]        tempPdfFiles    = Directory.GetFiles(outputDir, "*.pdf");
                HashSet<string> uniqueFileNames = new HashSet<string>();  //去重 并且只取前14位的传票号
                
                foreach (var file in tempPdfFiles)
                {
                    string fileName          = Path.GetFileName(file);  // 获取文件名（不包含路径）
                    string truncatedFileName = fileName.Length > 14 ? fileName.Substring(0, 14) : fileName;
                    uniqueFileNames.Add(truncatedFileName);  // 添加到 HashSet 去重
                }
                
                // 获取目标目录的路径
                string directoryPath = Path.GetDirectoryName(chamInputDto.zipPath);

                // 遍历去重后的文件名并创建文件夹
                foreach (var uniqueName in uniqueFileNames)
                {
                    string newFolderPath = Path.Combine(directoryPath, uniqueName);

                    // 创建文件夹（如果文件夹不存在）
                    if (!Directory.Exists(newFolderPath))
                    {
                        Directory.CreateDirectory(newFolderPath);
                        Console.WriteLine($"文件夹已创建: {newFolderPath}");
                    }
                    else
                    {
                        Console.WriteLine($"文件夹已存在: {newFolderPath}");
                    }

                    // 将对应的 PDF 文件移动到对应的文件夹
                    foreach (var file in tempPdfFiles)
                    {
                        string fileName          = Path.GetFileName(file);  // 获取文件名（不包含路径）
                        string truncatedFileName = fileName.Length > 14 ? fileName.Substring(0, 14) : fileName;

                        // 如果文件名的前14位和当前的 uniqueName 匹配，进行移动
                        if (truncatedFileName == uniqueName)
                        {
                            string destinationFilePath = Path.Combine(newFolderPath, fileName);

                            if (File.Exists(destinationFilePath))
                            {
                                //  覆盖旧文件
                                File.Delete(destinationFilePath);
                            }

                            File.Move(file, destinationFilePath);
                            Console.WriteLine($"文件 {fileName} 已移动到: {destinationFilePath}");

                        }
                    }



                        CHAMBoard chamBoard = new CHAMBoard();
                    // chamBoard.boardPath = chamInputDto.zipPath;
                    chamBoard.boardDictory = newFolderPath; 
                    chamBoard.boardCode = uniqueName;
                    chamService.saveBoard(chamBoard);
                    
                }

                if (Directory.Exists(outputDir))
                {
                    // 删除所有文件
                    foreach (string file in Directory.GetFiles(outputDir))
                    {
                        File.Delete(file);
                    }
                }

                return ResParameter.Success();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                // throw;
                return ResParameter.Fail(e.Message);
            }

        }
        

        //根据部品番号 线别 搬入日期 部品数量结合周订单找出船票号
        private List<CHAMOrdWeek> findSummons(List<ShipmentItem> shipmentItem)
        {
            List<CHAMOrdWeek> ordWeek = chamService.FindSummons(shipmentItem);
            return ordWeek;
        }

        
        
        
        
        
        /// <summary>
        /// 出货指示单内容转实体
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public CHAMDeliveryNote Parse(string text)
        {
            CHAMDeliveryNote CHAMdeliveryNote = new CHAMDeliveryNote();

            CHAMdeliveryNote.deliveryNoteCode = Regex.Match(text, @"出货指示号：\s*(\S+)").Groups[1].Value;
            CHAMdeliveryNote.PickupDate       = Regex.Match(text, @"取货日期：\s*(\S+)").Groups[1].Value;
            CHAMdeliveryNote.route            = Regex.Match(text, @"路线：\s*(\S+)").Groups[1].Value;
            CHAMdeliveryNote.trip             = Regex.Match(text, @"趟次：\s*(\S+)").Groups[1].Value;
            CHAMdeliveryNote.SupplierCode     = Regex.Match(text, @"供应商编号：\s*(\S+)").Groups[1].Value;
            CHAMdeliveryNote.SupplierName     = Regex.Match(text, @"供应商简称：\s*(\S+)").Groups[1].Value;
            CHAMdeliveryNote.PlanPickUpTime   = Regex.Match(text, @"计划取货时间：\s*(\S+\s+\S+)").Groups[1].Value;
            CHAMdeliveryNote.tCount           = int.Parse(Regex.Match(text, @"托数：(\d+)").Groups[1].Value);
            CHAMdeliveryNote.PlanArrivalTime  = Regex.Match(text, @"计划到货时间：\s*(\S+\s+\S+)").Groups[1].Value;

            return CHAMdeliveryNote;
        }


        /// <summary>
        /// 读取出货指示单中的明细信息
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private List<ShipmentItem> ParseShipmentItems(string text)
        {
            var result = new List<ShipmentItem>();

            // 拆分每一行
            var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            // 第一遍循环：提取取货日期
            string pickupDate = null;

            foreach (var line in lines)
            {
                var trimmed = Regex.Replace(line.Trim(), @"\s+", " ");
                if (trimmed.Contains("计划到货时间"))
                {
                    // 匹配日期部分，忽略后面的时间
                    var match = Regex.Match(trimmed, @"计划到货时间：\s*(\d{4}-\d{2}-\d{2})");
                    if (match.Success)
                    {
                        pickupDate = match.Groups[1].Value;
                        break;  // 找到日期后可以跳出循环
                    }
                }
            }

            foreach (var line in lines)
            {
                // 去除多余空格
                var trimmed = Regex.Replace(line.Trim(), @"\s+", " ");

                // 检查格式是否符合预期（用CHAM和日期判断）
                if (!Regex.IsMatch(trimmed, @"CHAM\d+AM \d{4}-\d{2}-\d{2}")) continue;

                var parts = trimmed.Split(' ');

                if (parts.Length < 7) continue; // 忽略字段不足的行

                var partNumber = $"{parts[0]} {parts[1]}";
                var chamIndex  = Array.FindIndex(parts, p => p.StartsWith("CHAM"));
                var partName   = string.Join(" ", parts.Skip(2).Take(chamIndex - 2));
                var lineCode   = parts[chamIndex];
                var inDate     = DateTime.Parse(parts[chamIndex + 1]);
                var boxCount   = int.Parse(parts[chamIndex      + 2]);
                var quantity   = int.Parse(parts[chamIndex      + 3]);

                result.Add(new ShipmentItem
                {
                    PartNumber = partNumber,
                    PartName   = partName,
                    Line       = lineCode,
                    InDate     = inDate,
                    BoxCount   = boxCount,
                    Quantity   = quantity,
                    OutDate = pickupDate 
                });
            }
            return result;
        }



        private string GetStringCellValue(ICell cell)
        {
            return cell?.ToString().Trim();
        }
        
        private string GetStringCellValueNum(ICell cell)
        {
            if (cell == null) return string.Empty;

            // 获取单元格文本并去除空格与逗号
            return cell.ToString().Trim().Replace(",", "");
        }

        private DateTime? GetDateTimeCellValue(ICell cell)
        {
            if (cell == null) return null;
            if (cell.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(cell))
            {
                return cell.DateCellValue;
            }
            if (cell.CellType == CellType.String)
            {
                DateTime parsedDate;
                // 尝试将字符串解析为 DateTime
                if (DateTime.TryParse(cell.ToString(), out parsedDate))
                {
                    return parsedDate;
                }
            }
            return null;
        }

        private double? GetDoubleCellValue(ICell cell)
        {
            if (cell == null) return null;
            if (cell.CellType == CellType.Numeric)
            {
                return cell.NumericCellValue;
            }
            return null;
        }
        
        private decimal? GetDecimalCellValue(ICell cell)
        {
            if (cell == null) return null;
            if (cell.CellType == CellType.Numeric)
            {
                return (decimal)cell.NumericCellValue;
            }
            return null;
        }
        
        public static string GetCellStringValue(ICell cell)
        {
            if (cell == null) return null;

            switch (cell.CellType)
            {
                case CellType.Numeric:
                    if (DateUtil.IsCellDateFormatted(cell))
                    {
                        // 是日期，返回标准格式字符串
                        return cell.DateCellValue.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    else
                    {
                        // 是数字
                        return cell.NumericCellValue.ToString(CultureInfo.InvariantCulture);
                    }

                case CellType.String:
                    return cell.StringCellValue?.Trim();

                case CellType.Boolean:
                    return cell.BooleanCellValue.ToString();

                case CellType.Blank:
                    return string.Empty;

                default:
                    return cell.ToString().Trim();
            }
        }
        
        /// <summary>
        /// 提取传票号
        /// </summary>
        /// <param name="pdfText"></param>
        /// <returns></returns>
        private string ExtractDeliveryNoteNumber(string pdfText)
        {
            // 按行分割文本
            var lines = pdfText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
    
            // 确保至少有两行数据
            if (lines.Length < 2)
                return string.Empty;

            // 倒数第二行
            var targetLine = lines[lines.Length - 2];

            // 使用正则提取14位数字
            var match = Regex.Match(targetLine, @"\b\d{14}\b");
            if (match.Success)
            {
                return match.Value;
            }

            return string.Empty;
        }

    }
}