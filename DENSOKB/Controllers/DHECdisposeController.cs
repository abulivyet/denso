using DENSOKB.Helper;
using DENSOKB.Libs;
using DENSOKB.Models;
using HelpLog.CommomMethod;
using Newtonsoft.Json.Linq;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Web.Script.Serialization;

namespace DENSOKB.Controllers
{
    /// <summary>
    ///  东风本田处理类
    /// </summary>
    public class DHECdisposeController : ApiController
    {
        private DHECdisposeService DHECdisposeService = new DHECdisposeService();
        private string KBUrl = "\\DHEC\\看板";
        private string citation = "\\DHEC\\传票";
        private orderimportService orderimport =  new orderimportService();
        /// <summary>
        /// 日计划分组查询（分页）
        /// </summary>
        /// <param name="paginationobj"></param>
        /// <returns></returns>
        [JwtAuthorize]
        [HttpPost]
        public object GetPageDayPlanList(Pagination paginationobj)
        {
            paginationobj.sidx = "planleavedate";
            paginationobj.sord = "desc";
            DataTable data = DHECdisposeService.getPageTB(paginationobj, paginationobj.queryJson);
            Dictionary<string, object> jo = new Dictionary<string, object>();
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            jo.Add("success", true);
            jo.Add("total", paginationobj.records);
            jo.Add("data", PulicHelper.ConvertDataTableToListOfDictionaries(data));
            return jo;
        }
        // 根据选择行数据
        [JwtAuthorize]
        [HttpPost]
        public HttpResponseMessage download(JArray jarray)
        {
            XErrorLogToolMDM.WriteLog(" DHEC下载开始");
            List<string> urlList             = new List<string>();
            List<string> DownloadList        = new List<string>();
            try
            {
                FileInfo[] files;
                if (jarray.Count <= 0)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest,
                    new { success = false, message = "请选择数据" });
                    //return BadRequest("请选择数据");
                }
                // ch1 确认表，ch2 箱标签，ch7取套票
                // ----------校验 取车组+出荷时间必须一致 这两个是表头
                bool iscar = isokcarno(jarray);
                if (!iscar)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest,
                    new { success = false, message = "取车组不一致" });
                    //return BadRequest("取车组+出荷时间不一致");
                    //return Request.CreateErrorResponse(HttpStatusCode.NoContent, "未找到解析后的数据，请重新解析");
                    //return Request.CreateErrorResponse(HttpStatusCode.NotFound, string.Format("取货车组+出荷日期不同"));
                }

                // 排序 

                // ----------找看板  根据以上条件 测试 选择两行 可能会有5个传票号 需要把所有传票号找到 注意 plancode 包含sp的没有看板 需要根据图片生成
                string sqlwhere = setWhere(jarray);
                //  --------- 2025-06-05 客户敲定排序原则：看板排序：车组、落点、计划代码、品番、计划到货日期、计划到货时间、 订单号（送货单号）
                DataTable KBdt = DHECdisposeService.getdayplan(sqlwhere); // 所有字段的看板（传票号） 用于生成A4
                if (KBdt.Rows.Count == 0) { throw new Exception("KBdt未查询到dhec_dayplan，条件为:" + sqlwhere); }
                DataTable okdt = DHECdisposeService.getnotPageTB(sqlwhere);  //确认表 所需数据 合并数据  部分数据需要从原单位表里找   
                if (okdt.Rows.Count == 0) { throw new Exception("okdt未查询到dhec_dayplan,条件为:" + sqlwhere); }
                //----------- 生成确认表  整理数据 调小毕写公共方法
                if (!string.IsNullOrEmpty(jarray[0]["check1"].ToString()) || !string.IsNullOrEmpty(jarray[0]["check7"].ToString())) // 勾选了确认表
                {
                    // 准备数据
                    string qrbUrl = getdeteilList(okdt);
                    XErrorLogToolMDM.WriteLog("确认表生成完成");
                    // 生成PDF
                    if (!string.IsNullOrEmpty(qrbUrl))
                    {
                        urlList.Add(qrbUrl);
                    }
                    if (!string.IsNullOrEmpty(jarray[0]["check1"].ToString()))
                    {
                        DownloadList.Add(qrbUrl);
                    }
                }

                if (!string.IsNullOrEmpty(jarray[0]["check2"].ToString()) || !string.IsNullOrEmpty(jarray[0]["check7"].ToString()))
                {
                    XErrorLogToolMDM.WriteLog("看板开始");
                    //----------- 合并看板  sp的不参与合并 单独生成
                    DataRow[] spKB = KBdt.Select("plancode like '%SP%'", "pn,drops,plandate,plantime,s_code ASC");
                    string outpaht = "";
                    //传票路径
                    string citationfilePath = "";

                    if (spKB.Length > 0)
                    {
                        XErrorLogToolMDM.WriteLog("看板SP");
                        outpaht = SPA4(spKB);

                        //传票
                        List<string> list = new List<string>();

                        string basePath = ConfigurationManager.AppSettings["fileUpload"].ToString() + citation;
                        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        // 拼接完整路径
                        citationfilePath = Path.Combine(basePath, timestamp + ".pdf");
                        foreach (var item in spKB)
                        {
                            
                            string pathName = item["s_code"].ToString();
                            string[] PDFfiles = Directory.GetFiles(basePath, "*.pdf");

                            XErrorLogToolMDM.WriteLog("根据name" + pathName + "获取看板");
                            XErrorLogToolMDM.WriteLog("目录下文件数" + PDFfiles.Length);

                            foreach (var file in PDFfiles)
                            {
                                // 只匹配文件名（不带路径）等于 pathName.pdf
                                if (Path.GetFileName(file).Equals(pathName + ".pdf", StringComparison.OrdinalIgnoreCase))
                                {
                                    list.Add(file); // 加入集合
                                }
                            }

                        }
                        FileInfo[] citationfiles = list.Select(path => new FileInfo(path)).ToArray();

                        SpireHelper.Merge(2, 1, citationfiles, citationfilePath, true, 0, 0, true);
                        XErrorLogToolMDM.WriteLog("看板完成:" + citationfilePath);

                    }
                    XErrorLogToolMDM.WriteLog("看板");
                    //DataRow[] NoSp = KBdt.Select("plancode not like '%SP%'", "pn,drops,plandate,plantime+0,s_code+0 ");
                    var NoSp = KBdt.AsEnumerable()
                        .Where(r => !r.Field<string>("plancode").Contains("SP"))
                        .OrderBy(r => r.Field<string>("pn"))
                        .ThenBy(r => r.Field<string>("drops"))
                        .ThenBy(r => r.Field<DateTime>("plandate"))
                        .ThenBy(r => long.Parse(r.Field<string>("plantime")))  // 数字排序
                        .ThenBy(r => long.Parse(r.Field<string>("s_code")))    // 数字排序
                        .ToArray();


                    foreach (var row in NoSp)
                    {
                        System.Diagnostics.Debug.WriteLine(row["pn"]);
                    }


                    if (NoSp.Length > 0)
                        {
                            string nospUrl = NoSPA4(NoSp, out outpaht);
                        XErrorLogToolMDM.WriteLog("看板完成:" + outpaht);
                        }

                        if (!string.IsNullOrEmpty(outpaht))
                        {
                            urlList.Add(outpaht);
                        }
                        if (!string.IsNullOrEmpty(citationfilePath))
                        {
                            urlList.Add(citationfilePath);
                        }
                        if (!string.IsNullOrEmpty(jarray[0]["check2"].ToString()))
                        {
                            DownloadList.Add(outpaht);
                        }
                    
                    //----------- 取套票 SpireHelper.MergeFiles()  // 多个文件生成一个PDF
                    if (!string.IsNullOrEmpty(jarray[0]["check7"].ToString()))
                    {
                        try
                        {
                            string AllUrl = ConfigurationManager.AppSettings["fileUpload"].ToString() + "\\DHEC\\TP\\" + "套票.pdf";

                            SpireHelper.MergeFiles(urlList, AllUrl);
                            urlList.Add(AllUrl);
                            DownloadList.Add(AllUrl);
                            XErrorLogToolMDM.WriteLog("套票下载");
                        }
                        catch (Exception e)
                        {
                            XErrorLogToolMDM.WriteLog("东风本田获取套票错误" + e.Message);
                        }
                    }
                    if (DownloadList.Count == 1)
                    {
                        DHECdisposeService.DownloadCountPlus(jarray[0]["carno"].ToString());
                        DownloadPdf(DownloadList[0], Path.GetFileNameWithoutExtension(DownloadList[0]));
                    }



                    //----------- 下载
                    foreach (var item in jarray)
                    {
                        DHECdisposeService.DownloadCountPlus(item["carno"].ToString());
                    }
                    //PulicHelper.DownloadFilesAsZip(DownloadList, $"{KBUrl}\\out\\DHEC_看板");
                }
            }
            catch (Exception ex)
            {

                XErrorLogToolMDM.WriteLog("东风本田下载错误" + ex.Message);
                XErrorLogToolMDM.WriteLog(ex.InnerException.ToString());
                XErrorLogToolMDM.WriteLog(ex.StackTrace);
                return Request.CreateResponse(HttpStatusCode.BadRequest,
                   new { success = false, message = "东风本田下载错误" + ex.Message });
                // return BadRequest(string.Format("东风本田下载错误" + ex.Message));
                //eturn Request.CreateErrorResponse(HttpStatusCode.NotFound, string.Format("东风本田下载错误" + ex.Message));
            }
            return Request.CreateResponse(HttpStatusCode.OK) ;
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
        
        public string setWhere(JArray jarray)
        {
            string sqlwhere = "";
            sqlwhere += " and (";
            var row = 0;
            foreach (var item in jarray)
            {
                if (row > 0)
                {
                    sqlwhere += " or ( 1=1";
                }
                else
                {
                    sqlwhere += "( 1=1 ";
                }
                if (!string.IsNullOrEmpty(item["carno"].ToString()))
                {
                    sqlwhere += string.Format(" and carno='{0}'", item["carno"].ToString());
                }
                //if (!string.IsNullOrEmpty(item["plancode"].ToString()))
                //{
                //    sqlwhere += string.Format(" and plancode='{0}'", item["plancode"].ToString());
                //}
                //if (!string.IsNullOrEmpty(item["pn"].ToString()))
                //{
                //    sqlwhere += string.Format(" and pn='{0}'", item["pn"].ToString());
                //}
                //if (!string.IsNullOrEmpty(item["plandate"].ToString()))
                //{
                //    sqlwhere += string.Format(" and plandate='{0}'", item["plandate"].ToString());
                //}
                //if (!string.IsNullOrEmpty(item["plantime"].ToString()))
                //{
                //    sqlwhere += string.Format(@" and CASE
                //            WHEN LENGTH(plantime) = 3 THEN STR_TO_DATE(CONCAT('0', plantime), '%H%i')
                //            WHEN LENGTH(plantime) = 4 THEN STR_TO_DATE(plantime, '%H%i')
                //        END = '{0}'", item["plantime"].ToString());
                //}
                sqlwhere += ")";
                row++;
            }
            sqlwhere += ")";
            return sqlwhere;
        }
        /// <summary>
        ///  校验是不是一个确认表
        ///  取货车组和出荷日期一致为一个确认表
        /// </summary>
        /// <param name="jarray"></param>
        /// <returns></returns>
        public bool isokcarno(JArray jarray)
        {
            bool isok = true;
            string carno = jarray[0]["carno"].ToString();
            //string plandate = jarray[0]["plandate"].ToString() ;
            foreach (var item in jarray)
            {
                if (!item["carno"].ToString().Equals(carno))
                {
                    isok = false;
                    break;
                }
            }
            return isok;
        }

        /// <summary>
        ///  非sp A4穿透
        /// </summary>
        /// <returns></returns>
        public string NoSPA4(DataRow[] NoSp,out string url)
        {
            string mess = "";
            url = "";
            //FileInfo[] files;
            List<FileInfo> files = new List<FileInfo>();
            string filesPsth = ConfigurationManager.AppSettings["fileUpload"].ToString()+ KBUrl;
            if (!Directory.Exists(filesPsth))
            {
                Directory.CreateDirectory(filesPsth);
                XErrorLogToolMDM.WriteLog($"已创建目录：{filesPsth}");
            }
            try
            { 
            //{XErrorLogToolMDM.WriteLog(NoSp.Length)
                foreach (var item in NoSp)
                {
                //XErrorLogToolMDM.WriteLog("获取S_CODE");
                    string sCode = item["s_code"].ToString();
                    bool found = false;
                // 获取目录下所有文件
                //XErrorLogToolMDM.WriteLog("获取目录下所有文件:" + filesPsth);
                DirectoryInfo dir = new DirectoryInfo(filesPsth);
                    foreach (FileInfo file in dir.GetFiles("*", SearchOption.AllDirectories))
                    {
                        // 比较不包含扩展名的文件名
                        if (Path.GetFileNameWithoutExtension(file.Name).Equals(sCode, StringComparison.OrdinalIgnoreCase))
                        {
                            files.Add(file);
                            found = true;
                            break;
                        }

                    }
                    // if (!found)
                    // {
                    //     mess = string.Format("文件【{0}】未找到解析文件，请先解析PDF", sCode);
                    // }
                }

                if (files.Count == 0) { throw new Exception("未找到看板文件，请检查看板文件是否上传。"); }
                //FileInfo[] fileinfo = SpireHelper.SortA4(files.ToArray(), 2, 2);
                url = filesPsth+"\\out\\看板.pdf";
                SpireHelper.Merge(2, 2, files.ToArray(), url, true,0,0,true);
            XErrorLogToolMDM.WriteLog("合并看板路径");
                //SpireHelper.Merge(2, 2, fileinfo, url);
                    }
            catch (Exception e)
            {
                XErrorLogToolMDM.WriteLog(e.Message);
                XErrorLogToolMDM.WriteLog(e.StackTrace);
                XErrorLogToolMDM.WriteLog(e.InnerException.ToString());
                throw;
            }
            return mess;
        }
        /// <summary>
        /// sp自己参照图片做模板
        /// </summary>
        /// <param name="spKB"></param>
        /// <returns></returns>
        public string SPA4(DataRow[] spKB)
        {
            /* E3 供应商名称；E5 零件号；E7零件名称；E9对应车型；E11数量；E13到货日
             * 二维码：零件号+空格+电装品番+14个数字（还不知道）             
            */
            //string outPath = "";
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string confirmName = ConfigurationManager.AppSettings["DHECSP"].ToString();// 模板存储
            string confirmTemplatePath = System.IO.Path.Combine(baseDirectory, confirmName); // 模板地址
            string fileUpload= ConfigurationManager.AppSettings["fileUpload"].ToString(); // 文件存储路径
            string extension = System.IO.Path.GetExtension(confirmName).ToLower();
            string filesPsth = ConfigurationManager.AppSettings["fileUpload"].ToString() + KBUrl;
            List<string> fileInfos = new List<string>();
            foreach (var item in spKB)
            {
                string confinrmOutName = item["pn"].ToString()+"_"+ item["plandate"].ToString() + "SP标签.pdf";
                
                string newPdfPath = filesPsth +"\\"+ item["pn"].ToString()  + "_SP标签.pdf";
                string pnName = "";
                string vehModel = "";
                string plandate = item["plandate"].ToString();
                // 先转成 DateTime，再格式化
                if (DateTime.TryParse(plandate, out DateTime dt))
                {
                    plandate = dt.ToString("yyyy-MM-dd");  // 输出：2025-02-17
                }
                DataTable oriItem = PulicHelper.GetOri("DHEC", "", item["pn"].ToString(), "A*SP**");
                if (oriItem.Rows.Count > 0) {
                    pnName = oriItem.Rows[0]["part_name"].ToString();
                    vehModel = oriItem.Rows[0]["veh_model"].ToString();
                }
                //string pnName = 
                SpireHelper.FillPdf(confirmTemplatePath, newPdfPath, item["pn"].ToString(), pnName, vehModel, item["p_num"].ToString(), plandate);
                fileInfos.Add(newPdfPath);
            }
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            // 拼接完整路径
            string filePath = Path.Combine(filesPsth, timestamp + ".pdf");
            SpireHelper.MergeFiles(fileInfos, filePath);
            return filePath;
        }

        public string getdeteilList(DataTable spKB)
        {
            /*计划代码  A4AE01
             * 落点 DD1E1
             * 零件号 143106MA J012M3
             * 电装品番 GN229900-03300T
             * 收容数 12
             * 拖/台数 = 收容数*托/箱数
             * 托/箱数 32
             * 箱数…台尾数 =CONCATENATE(TRUNC(L11/E11),"…",ROUND(MOD(L11,E11),2))
             * 箱数 =纳入个数/收容数
             * 托数..台尾数 =CONCATENATE(TRUNC(L11/F11),"…",ROUND(MOD(L11,F11),2))
             * 托数…箱尾数 =CONCATENATE(TRUNC(I11/G11),"…",ROUND(MOD(I11,G11),1))
             * 纳入个数
             * 传票日期
             * 传票时间
             
             */

            List<CHAMOrdWeekWithOrig> detailList = new List<CHAMOrdWeekWithOrig>();
            string fileUrl = "";
            string outPath = "";
            CHAMconfirm cHAMconfirm = new CHAMconfirm();
            try
            {
                for (int i = 0; i < spKB.Rows.Count; i++)
                {
                    var item = spKB.Rows[i];
                    ; CHAMOrdWeekWithOrig detail = new CHAMOrdWeekWithOrig();
                    detail.projectCode = item["plancode"].ToString();                   //计划代码
                    detail.movingInCode = item["drops"].ToString();                      //落点
                    detail.materialCode = item["pn"].ToString();                          //零件号
                    detail.DensoNum = item["denso_num"].ToString();                      //电装品番
                    detail.BoxCap =string.IsNullOrEmpty(item["box_cap"].ToString())?0: Convert.ToInt32(item["box_cap"].ToString());         //收容数
                    detail.PalletBox = string.IsNullOrEmpty(item["pallet_box"].ToString())?0: Convert.ToInt32(item["pallet_box"].ToString());   //拖 / 台数
                    detail.quantity = item["p_num"].ToString();                         //纳入个数
                    detail.cpdate = item["plandate"].ToString();                        //传票日期
                    detail.orderCode = "-";// item["s_code"].ToString();

                    var raw = item["plantime"].ToString(); // "1500"

                    if (!string.IsNullOrWhiteSpace(raw) && raw.Length == 4)
                    {
                        detail.cptime = raw.Substring(0, 2) + ":" + raw.Substring(2, 2); // "15:00"
                    }
                    else if (!string.IsNullOrWhiteSpace(raw) && raw.Length == 3) {
                        raw = "0" + raw;
                        detail.cptime = raw.Substring(0, 2) + ":" + raw.Substring(2, 2); // "15:00"
                    }
                    else
                    {
                        detail.cptime = raw;
                    }

                    //detail.cptime = item["plantime"].ToString();                         //传票时间
                    detailList.Add(detail);
                }
                 fileUrl = ConfigurationManager.AppSettings["CHAMQRB"].ToString();// 文件模板路径 CHAMQRB
                 var filepath = ConfigurationManager.AppSettings["fileUpload"].ToString() + "\\DHEC\\download"; // 文件输出路径 fileUpload
                if (!Directory.Exists(filepath))
                {
                    Directory.CreateDirectory(filepath);
                }

                string fileName = spKB.Rows[0]["fileName"].ToString();
                //通过filename查找传票做成者和出荷日期
                string createPerson = DHECdisposeService.getCreatePerson(fileName);
                cHAMconfirm.summonsCreatePerson = createPerson; // 传票做成者
                // cHAMconfirm.shipmentDate        = spKB.Rows[0]["planreachdate"].ToString();
                
                DateTime date = DateTime.Parse(spKB.Rows[0]["planreachdate"].ToString());
                cHAMconfirm.shipmentDate  = date.ToString("yyyy-MM-dd");

                // 生成文件
                cHAMconfirm.title           = "DHEC 制品集货・出货异品・欠品照合确认表";
                cHAMconfirm.cusName             = "DHEC（东本发动机）";
                cHAMconfirm.carGroup            = spKB.Rows[0]["carno"].ToString();
                cHAMconfirm.cusCode             = "11001500";
                // cHAMconfirm.summonsCreatePerson = "";
                cHAMconfirm.summonsCreateTime   = DateTime.Now.ToString("yyyy-MM-dd");
                cHAMconfirm.details             = detailList;
                cHAMconfirm.confirmRemark = orderimport.GetConfirmBatch("东风本田","");// spKB.Rows[0]["confirmRemark"].ToString();
                cHAMconfirm.type                = "东风本田";
                Console.WriteLine(cHAMconfirm.confirmRemark);
                outPath= PulicHelper.EditConfirmExcel(fileUrl, filepath+ "\\确认表.xls", cHAMconfirm);
                XErrorLogToolMDM.WriteLog("确认表文件路径:" + outPath);
            }
            catch (Exception e)
            {
                throw e;
            }
            return outPath;
        }

        public string createQRB()
        {
            string url = "";

            return url;
        }

        /// <summary>
        ///  东风本田处理
        /// </summary>
        /// <param name="ftceDto"></param>
        /// <returns></returns>
        [HttpPost]
        public ResParameter CompressedFileDHEC(FtceDto ftceDto)
        {
            ResParameter rp = new ResParameter();
            try
            {
                #region 第一步 拆分
                string filePath = ftceDto.zipPath;
                string outpath = ftceDto.basePath + KBUrl;
                string citationPath = ftceDto.basePath + citation;
                XErrorLogToolMDM.WriteLog("东风拆分1" + outpath);
                // 检查并创建目录
                if (!Directory.Exists(outpath))
                {
                    Directory.CreateDirectory(outpath);
                    Console.WriteLine($"已创建目录：{outpath}");
                }
                XErrorLogToolMDM.WriteLog("东风拆分2" + outpath);

                //先判断文件内是否包含零件明示卡  包含 量产
                if (SpireHelper.GetStartPage(filePath, "零件明示卡") > 0)
                {
                    SpireHelper.PdfSplit(
                        filePath,
                        outpath,
                        SpireHelper.GetStartPage(filePath, "零件明示卡"),
                        2,
                        2,
                        SpireSplit.EnumNameRule.Customize,
                        new Rectangle(110, 30, 176, 40),
                        new Rectangle(8, 9, 821, 572));
                }
                else {
                    SpireHelper.PdfSplit(
                            filePath,
                            citationPath,
                            SpireHelper.GetStartPage(filePath, "送货传票"),
                            2,
                            1,
                            SpireSplit.EnumNameRule.Customize,
                            new Rectangle(50, 200, 160, 50),
                            new Rectangle(8, 9, 821, 572));

                    //记录传票号和订单号
                    List<Rectangle> rectangles = new List<Rectangle>();
                    Rectangle summonNo = new Rectangle(148, 30, 80, 12);
                    Rectangle orderCode = new Rectangle(258, 143, 61, 12);
                    rectangles.Add(summonNo);
                    rectangles.Add(orderCode);
                    Dictionary<int, List<string>> dictionary = SpireHelper.GetTextFromPages(filePath, rectangles, SpireHelper.GetStartPage(filePath, "送货传票"));


                    DHECdisposeService.InsertSummonOrder(dictionary);
                }
                XErrorLogToolMDM.WriteLog("东风拆分完成3" + outpath);
                #endregion
                #region 第二步 排序

                #endregion
                rp.code = "200";
                rp.info = string.IsNullOrEmpty(rp.info) ? "成功" : rp.info;
            }
            catch (Exception e)
            {
                rp.code = "500";
                rp.info = "解析失败" +e.Message;
            }
            // 无法生成确认表，需要再导出时选择数据生成
            return rp;
        }
        /// <summary>
        /// 量产订单导入
        /// </summary>
        /// <param name="filePath"> 上传成功后Excel的路径</param>
        public static void setWorkOrder(string filePath)
        {
            try
            {
                using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    IWorkbook workbook;
                    if (filePath.EndsWith(".xlsx"))
                        workbook = new XSSFWorkbook(stream); // .xlsx 格式
                    else
                        workbook = new HSSFWorkbook(stream); // .xls 格式

                    ISheet sheet = workbook.GetSheetAt(0); // 第一个工作表
                    List<OrdWeekOrder> chainList = new List<OrdWeekOrder>();
                    List<int> clist = new List<int>();
                    int numMerged = sheet.NumMergedRegions;
                    //// 遍历当前 Sheet 的所有合并区域
                    //for (int i = 0; i < sheet.NumMergedRegions; i++)
                    //{
                    //    CellRangeAddress mergedRegion = sheet.GetMergedRegion(i);
                    //    int startRow = mergedRegion.FirstRow;
                    //    int endRow = mergedRegion.LastRow;
                    //    int startCol = mergedRegion.FirstColumn;
                    //    int endCol = mergedRegion.LastColumn;
                    //    var row = sheet.GetRow(startRow);
                    //    var cell = row?.GetCell(startCol);
                    //    var content = cell?.ToString() ?? string.Empty;

                    //}
                    for (int rowIdx = 1; rowIdx <= sheet.LastRowNum; rowIdx++)
                    {
                        OrdWeekOrder chain = new OrdWeekOrder();
                        IRow row = sheet.GetRow(rowIdx);

                        var cell_0 = row?.GetCell(0);
                        var cellValue0 = cell_0?.ToString() ?? string.Empty;
                        chain.o_number = cellValue0;// 序号
                        var cell_1 = row?.GetCell(1);
                        var cellValue1 = cell_1?.ToString() ?? string.Empty;
                        chain.sup_code = cellValue1;// 供应商编号
                        var cell_2 = row?.GetCell(2);
                        var cellValue2 = cell_2?.ToString() ?? string.Empty;
                        chain.r_location = cellValue2; //收货地点
                        var cell_3 = row?.GetCell(3);
                        var cellValue3 = cell_3?.ToString() ?? string.Empty;
                        chain.online_v = cellValue3;//上线地点
                        var cell_4 = row?.GetCell(4);
                        var cellValue4 = cell_4?.ToString() ?? string.Empty;
                        chain.part_code = cellValue4;//零件号
                        var cell_5 = row?.GetCell(5);
                        var cellValue5 = cell_5?.ToString() ?? string.Empty;
                        chain.part_color = cellValue5;//零件颜色
                        var cell_6 = row?.GetCell(6);
                        var cellValue6 = cell_6?.ToString() ?? string.Empty;
                        chain.part_name = cellValue6;//零件名称
                        var cell_7 = row?.GetCell(7);
                        var cellValue7 = cell_7?.ToString() ?? string.Empty;
                        chain.plan_code = cellValue7;//计划代码
                        var cell_8 = row?.GetCell(8);
                        var cellValue8 = cell_8?.ToString() ?? string.Empty;
                        try
                        {
                            chain.r_date = ConvertExcelDate(cellValue8);//厂内收货日期
                        }
                        catch
                        {
                            chain.r_date = null;
                        }
                        var cell_9 = row?.GetCell(9);
                        var cellValue9 = cell_9?.ToString() ?? string.Empty;
                        chain.r_time = cellValue9;//厂内收货时间
                        var cell_10 = row?.GetCell(10);
                        var cellValue10 = cell_10?.ToString() ?? string.Empty;
                        try
                        {
                            chain.c_date = ConvertExcelDate(cellValue10);//收货日期
                        }
                        catch
                        {
                            chain.c_date = null;
                        }
                        var cell_11 = row?.GetCell(11);
                        var cellValue11 = cell_11?.ToString() ?? string.Empty;
                        chain.c_time = cellValue11;//收货时间
                        var cell_12 = row?.GetCell(12);
                        var cellValue12 = cell_12?.ToString() ?? string.Empty;
                        try
                        {
                            chain.order_num = Convert.ToDecimal(cellValue12);//订货数量
                        }
                        catch
                        {
                            chain.order_num = null;
                        }
                        var cell_13 = row?.GetCell(13);
                        var cellValue13 = cell_13?.ToString() ?? string.Empty;
                        chain.p_code = cellValue13;//传票号码
                        var cell_14 = row?.GetCell(14);
                        var cellValue14 = cell_14?.ToString() ?? string.Empty;
                        chain.o_version = cellValue14;//版本
                        var cell_15 = row?.GetCell(15);
                        var cellValue15 = cell_15?.ToString() ?? string.Empty;
                        chain.o_week = cellValue15;//周次
                        var cell_16 = row?.GetCell(16);
                        var cellValue16 = cell_16?.ToString() ?? string.Empty;
                        chain.o_class = cellValue16;//分类
                        var cell_17 = row?.GetCell(17);
                        var cellValue17 = cell_17?.ToString() ?? string.Empty;
                        chain.rule = cellValue17;//交货规则
                        var cell_18 = row?.GetCell(18);
                        var cellValue18 = cell_18?.ToString() ?? string.Empty;
                        chain.remarks = cellValue18;//备注
                        var cell_19 = row?.GetCell(19);
                        var cellValue19 = cell_19?.ToString() ?? string.Empty;
                        chain.order_code = cellValue19;//订单编号
                        var cell_20 = row?.GetCell(20);
                        var cellValue20 = cell_20?.ToString() ?? string.Empty;
                        chain.carmodel = cellValue20;//车型
                        chainList.Add(chain);
                    }

                    orderimportService.insertweekorder(chainList);
                }
            }
            catch (Exception ex)
            {
                XErrorLogToolMDM.WriteLog("东风本田订单上传数据错误：" + ex.Message);
            }
        }
        public static DateTime? ConvertExcelDate(string dateString)
        {
            try
            {
                if (string.IsNullOrEmpty(dateString))
                    return null;

                // 先尝试系统自动解析（可识别多种本地格式）
                if (DateTime.TryParse(dateString, out DateTime result))
                    return result;

                // 明确支持的格式
                string[] formats = new[]
                {
    "M/d/yy", "MM/d/yy", "M/dd/yy", "MM/dd/yy",  // ✅ 支持 7/28/25
    "M-d-yy", "MM-d-yy", "M-dd-yy", "MM-dd-yy",
    "yyyy/MM/dd", "yyyy-MM-dd", "yyyyMMdd",
    "M/d/yyyy", "MM/dd/yyyy"                      // ✅ 防止 7/28/2025 也能过
};

                if (DateTime.TryParseExact(dateString,
                    formats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out result))
                {
                    return result;
                }

                // 如果是 Excel OA 数字日期
                if (double.TryParse(dateString, out double oaDate))
                {
                    return DateTime.FromOADate(oaDate);
                }

                return null;

            }
            catch (Exception e)
            {
                XErrorLogToolMDM.WriteLog("日期转换出问题，日期为：" + dateString);
                return null;

            }
            return null;
        }
        /// <summary>
        ///  日取货计划导入
        /// </summary>
        /// <param name="filePath"></param>
        public static void setDHECPlan(orderImport orderImport )
        {
            try
            {
                string filePath   = orderImport.redundancy1;
                string fileName   = orderImport.redundancy3;
                var    jsonResult = new JObject();
                var    dataArray  = new JArray();
                using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    IWorkbook workbook;
                    if (filePath.EndsWith(".xlsx"))
                        workbook = new XSSFWorkbook(stream); // .xlsx 格式
                    else
                        workbook = new HSSFWorkbook(stream); // .xls 格式
                    ISheet sheet = workbook.GetSheetAt(0); // 第一个工作表
                                                           // 获取列名行（第二行）
                    IRow headerRow = sheet.GetRow(1);
                    if (headerRow == null) return ;

                    // 构建列名列表
                    var columnNames = new List<string>();
                    for (int i = 0; i < headerRow.LastCellNum; i++)
                    {
                        var cell = headerRow.GetCell(i);
                        columnNames.Add(cell?.ToString()?.Trim() ?? $"Column_{i + 1}");
                    }

                    // 处理数据行（从第三行开始）
                    for (int rowIndex = 2; rowIndex <= sheet.LastRowNum; rowIndex++)
                    {
                        IRow dataRow = sheet.GetRow(rowIndex);
                        if (dataRow == null) continue;

                        var rowObject = new JObject();
                        for (int colIndex = 0; colIndex < columnNames.Count; colIndex++)
                        {
                            var cellValue = dataRow.GetCell(colIndex)?.ToString() ?? string.Empty;
                            if (columnNames[colIndex]== "计划到达日期"|| columnNames[colIndex] == "计划离开日期"||
                                columnNames[colIndex] == "作成时间"||columnNames[colIndex] == "计划到货日期"||columnNames[colIndex] == "调整到货日期")
                            {
                                cellValue = ConvertExcelDate(cellValue.ToString()).ToString();
                            }

                            if (columnNames[colIndex] == "作成时间") {
                                var cell = dataRow.GetCell(colIndex);
                                //string value2;

                                if (cell == null)
                                {
                                    cellValue = string.Empty;
                                }
                                else if (cell.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(cell))
                                {
                                    cellValue = cell.DateCellValue.ToString("yyyy/MM/dd"); // 或者你需要的格式
                                }
                                else
                                {
                                    cellValue = cell.ToString();
                                }
                            }
                            rowObject[columnNames[colIndex]] = cellValue;
                        }
                        rowObject["fileName"] = fileName;
                        dataArray.Add(rowObject);
                    }
                }
                DHECdisposeService.insertDayPlan(dataArray);
                
            }
            catch (Exception ex)
            {
                XErrorLogToolMDM.WriteLog("东风本田取货计划上传数据错误：" + ex.Message);
            }
        }

        [HttpPost]
        public ResParameter DeleteRecords([FromBody] List<int> ids)
        {
            try
            {
                if (ids == null || ids.Count == 0)
                {
                    return ResParameter.Fail("未接收到删除 id 列表");
                }

                DHECdisposeService.DeleteWeekProject(ids);
                return ResParameter.Success("删除成功");
            }
            catch (Exception e)
            {
                XErrorLogToolMDM.WriteLog("DHEC 删除错误: " + e.Message);
                return ResParameter.Fail(e.Message);
            }
        }
    }
}