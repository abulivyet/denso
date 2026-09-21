using DENSOKB.Helper;
using DENSOKB.Libs;
using DENSOKB.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpCompress.Archives;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Script.Serialization;
using System.Web;
using HelpLog.CommomMethod;
using System.Configuration;

namespace DENSOKB.Controllers
{
    public class orderImportController : ApiController
    {
        orderimportService orderservice = new orderimportService();

        //FileHelper filehelper = new FileHelper();
        private LiberOfficeHelper liberOfficeHelper = new LiberOfficeHelper();
        CHAMdisposeController               _chaMdisposeController       = new CHAMdisposeController();

        uploadController up = new uploadController();
        [HttpPost]
        public Object GetPage(Pagination paginationobj)
        {
            return null;
        }

        public DataTable getNotHead()
        {
            return null;
        }

        /// <summary>
        /// 导入列表查询
        /// </summary>
        /// <param name="page">页码</param>
        /// <param name="start">起始页</param>
        /// <param name="limit">每页条数</param>
        /// <param name="queryJson">查询参数</param>
        /// <returns></returns>
        [JwtAuthorize]
        [HttpPost]
        public object GetPageList(Pagination paginationobj)
        {
            paginationobj.sidx = "createdate";
            paginationobj.sord = "desc";
            DataTable                  data       = orderservice.getPageTB(paginationobj, paginationobj.queryJson);
            Dictionary<string, object> jo         = new Dictionary<string, object>();
            JavaScriptSerializer       serializer = new JavaScriptSerializer();
            jo.Add("success", true);
            jo.Add("total", paginationobj.records);
            jo.Add("data", PulicHelper.ConvertDataTableToListOfDictionaries(data));
            return jo;
        }

        

        private static readonly string[] AllowedExtensions = { ".rar", ".zip", ".xls", ".xlsx", ".pdf" };

        private static readonly string[] AllowedMimeTypes =
        {
            "application/x-rar-compressed",
            "application/zip",
            "application/vnd.rar",
            "application/vnd.ms-excel",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/pdf"
        };
        [HttpPost]
        public async Task<baseMessage> delimport(List<orderImport> orderList)
        {
            baseMessage result = new baseMessage();
            int row = 0;
            try
            {
                string condition = string.Join(" , ", orderList.Select(t => $"'{t.id}'"));
                string sql = string.Format(" delete from orderimport where id in ({0});", condition);
                sql += string.Format(" delete from ord_inp_detail2 where mainId in({0})", condition);
                 row = DataHelper.ExecuteSql(sql);
            }
            catch (Exception e)
            {
                return new baseMessage() { success = false, result = $"失败，原因：{e.Message}" };
            }
            return new baseMessage() { success = true, result = $"操作成功{row}行" };
        }
        [HttpPost]
        public async Task<baseMessage> fileNameCheck()
        {
            baseMessage result = new baseMessage();
            string username = "";
            // 获取 HttpContext 的请求对象
            HttpRequest request = HttpContext.Current.Request;
            //string type = request.Form["type"]; // 从 FormData 获取 type 参数
            var fileList = request.Files;
            var provider = new MultipartMemoryStreamProvider();
            DataTable dt = orderservice.getNotPageTB(null);
            string mess = "";
            try
            {
                var formData = await Request.Content.ReadAsMultipartAsync(provider);

                foreach (var file in provider.Contents)
                {
                    var typename = file.Headers.ContentDisposition.Name?.Trim('\"');
                    var fileName = file.Headers.ContentDisposition.FileName?.Trim('\"');

                    // 获取文件字节数组
                    var fileBytes = file.ReadAsByteArrayAsync().Result;

                    // 计算 MD5
                    string md5Hash = PulicHelper.GetMD5(fileBytes);

                    DataRow[] dr= dt.Select(" filehash='" + md5Hash + "'");
                    if (dr.Length>0)
                    {
                        mess += string.Format("文件{0}已上传过;", fileName);
                    }
                }
                if (!string.IsNullOrEmpty(mess))
                {
                    return new baseMessage() { success = false, result = mess };
                }
            }
            catch (Exception ex)
            {
                throw;
            }

            return new baseMessage() { success = true, result = "" }; ;

        }
        // 异步上传多人多附件上传应该会好点，已测试Task<baseMessage>高老师前台可以接收返回值
        [HttpPost]
        public async Task<baseMessage> UploadFile()
        {
            baseMessage result = new baseMessage();
            string username = "";
            // 获取 HttpContext 的请求对象
            HttpRequest request = HttpContext.Current.Request;
            //string type = request.Form["type"]; // 从 FormData 获取 type 参数
            var fileList = request.Files;
            var provider = new MultipartMemoryStreamProvider();
            try
            {
                var formData = await Request.Content.ReadAsMultipartAsync(provider);

                foreach (var file in provider.Contents)
                {
                    var typename = file.Headers.ContentDisposition.Name?.Trim('\"');
                    if (typename == "username")
                    {
                        username = await file.ReadAsStringAsync();
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }


            string apiPath = "";

            //    throw;
            //}

            //    throw;
            //}


            string uploadPath = HttpContext.Current.Server.MapPath("~/Uploads");
            Directory.CreateDirectory(uploadPath);
            List<orderImport> orderList = new List<orderImport>();
            foreach (var file in provider.Contents)
            {
                var typename = file.Headers.ContentDisposition.Name?.Trim('\"');
                if (typename == "username")
                {
                    continue;
                }

                var fileName = file.Headers.ContentDisposition.FileName?.Trim('\"');
                if (string.IsNullOrEmpty(fileName))
                {
                    return new baseMessage() { success = false, result = "无效的文件名" };
                }

                var         fileData      = await file.ReadAsByteArrayAsync();
                var         fileExtension = Path.GetExtension(fileName).ToLower();
                var fileBytes = file.ReadAsByteArrayAsync().Result;
                orderImport order         = new orderImport();
                order.inportdate   = DateTime.Now;
                order.createdate   = DateTime.Now;
                order.createperson = username;
                order.redundancy3  = fileName;
                order.filehash = PulicHelper.GetMD5(fileBytes);
                order.redundancy4 = Guid.NewGuid().ToString();
                order.handlestate = "1";  // 默认1
                switch (fileExtension)
                {
                    case ".rar":
                        //await ProcessCompressedFileFTCE(fileData, uploadPath);

                        order.redundancy1 = SaveFileToFolder(fileName, fileData, uploadPath, "FTCE");
                        order.redundancy2 = "量产";
                        order.cuscode     = "FTCE";
                        order.cusname     = "长春";
                        string datePart = fileName.Split('_')[1];
                        DateTime dt = DateTime.ParseExact(datePart, "yyyyMMdd", null);
                        order.redundancy7 = dt;
                        order.podate = dt;
                        //order
                        apiPath = ConfigurationManager.AppSettings["FTCElcPath"].ToString();
                        break;
                    case ".zip":
                        order.redundancy1 = SaveFileToFolder(fileName, fileData, uploadPath, "FTET");
                        if (Path.GetFileName(fileName).IndexOf("BJ") > -1)
                        {
                            order.redundancy2 = "补给";
                        }
                        else
                        {
                            order.redundancy2 = "量产";
                        }

                        order.cuscode = "FTET";
                        order.cusname = "一汽发动机";
                        apiPath       = ConfigurationManager.AppSettings["FTETPath"].ToString();
                        //FTET订单
                        // await ProcessCompressedFileFTCE(fileData, fileExtension, newPath);
                        break;
                    case ".xls":
                        if (fileName.IndexOf("FTMS") > -1)
                        {
                            //FTMS 为 FTCE补给订单
                            order.redundancy1 = SaveFileToFolder(fileName, fileData, uploadPath, "FTCE补给订单");
                            order.redundancy2 = "补给";
                            order.cuscode     = "FTCE";
                            order.cusname     = "长春";
                            apiPath           = ConfigurationManager.AppSettings["FTCEbjPath"].ToString();
                        }
                        // 一汽发动机 ftet的直连配货 数据需要存入数据库
                        if (fileName.IndexOf("直链配货") > -1)
                        {
                            order.redundancy1 = SaveFileToFolder(fileName, fileData, uploadPath, "FTCE直链配货");
                            order.redundancy2 = "直链配货";
                            order.cuscode     = "FTET";
                            order.cusname     = "一汽发动机";
                            FTETdisposeController.setZL(order.redundancy1);
                            order.handlestate = "2";// 不需要解析 上传记录存成功
                        }
                        if (fileName.IndexOf("GHAC") > -1 )
                        {
                            string fileType = string.Empty;
                           
                            // 广本周订单导入 数据需要写入数据库
                            string fileSavePath =  SaveFileToFolder(fileName, fileData, uploadPath, "GHAC");
                            order.redundancy2 = "周订单";
                            order.cuscode = "GHAC";
                            order.cusname = "广州本田";
                            order.redundancy1 = fileSavePath;//SaveFileToFolder(fileName, fileData, uploadPath, "GHAC");

                            GHACController ghac = new GHACController();

                            object obj = new
                            {
                                filePath = fileSavePath,
                                fileType = "EXCEL",
                                username = username
                            };

                            ghac.GHACUpload(JObject.Parse(JsonConvert.SerializeObject(obj)));

                            result = new baseMessage() { success = true, result = "文件上传完成" };
                            break;

                            //return result;

                        }
                        if ((fileName.IndexOf("DHEC") > -1 || fileName.IndexOf("GHEC") > -1) && fileName.IndexOf("order") > -1)
                        {
                            order.redundancy1 = SaveFileToFolder(fileName, fileData, uploadPath, "DHEC订单");
                            order.redundancy2 = "量产订单";
                            order.cuscode     = "DHEC";
                            order.cusname     = "东风本田";
                            //东风本田订单导入 数据写入数据库
                            DHECdisposeController.setWorkOrder(order.redundancy1);
                            order.handlestate = "2";// 不需要解析 上传记录存成功
                        }
                        if ((fileName.IndexOf("DHEC") > -1 || fileName.IndexOf("GHEC") > -1 ) && fileName.IndexOf("PLAN") > -1)  // 东风取货日计划（取货计划）
                        {
                            order.redundancy1 = SaveFileToFolder(fileName, fileData, uploadPath, "取货计划");
                            order.redundancy2 = "取货计划";
                            order.cuscode = "DHEC";
                            order.cusname = "东风本田";
                            //东风本田订单导入 数据写入数据库
                            DHECdisposeController.setDHECPlan(order);
                            order.handlestate = "2";// 不需要解析 上传记录存成功
                        }
                        if ( fileName.IndexOf("集货计划")>-1)
                        {
                            // 一汽物流集货计划导入
                            order.redundancy1 = SaveFileToFolder(fileName, fileData, uploadPath, "取货计划");
                            order.redundancy2 = "集货计划";
                            order.cuscode = "FTET";
                            order.cusname = "一汽发动机";
                            order.handlestate = "2";// 不需要解析 上传记录存成功
                            FTETdisposeController.ExecPlanExcel(order.redundancy1);
                        }

                        // if (fileName.IndexOf("物流集货计划") > -1)
                        // {
                        //     
                        // }

                        break;
                    case ".xlsx":
                        if (fileName.IndexOf("FTMS") > -1)
                        {
                            //FTMS 为 FTCE补给订单
                            order.redundancy1 = SaveFileToFolder(fileName, fileData, uploadPath, "FTCE补给订单");
                            order.redundancy2 = "补给";
                            order.cuscode = "FTCE";
                            order.cusname = "长春";
                            apiPath = ConfigurationManager.AppSettings["FTCEbjPath"].ToString();
                        }
                        else if (fileName.IndexOf("集货计划") > -1)
                        {
                            // 一汽物流集货计划导入
                            order.redundancy1 = SaveFileToFolder(fileName, fileData, uploadPath, "取货计划");
                            order.redundancy2 = "集货计划";
                            order.cuscode = "FTET";
                            order.cusname = "一汽发动机";
                            order.handlestate = "2";// 不需要解析 上传记录存成功
                            FTETdisposeController.ExecPlanExcel(order.redundancy1);
                        }
                        else if (fileName.IndexOf("TFTE") > -1 || fileName.IndexOf("FTET") > -1)
                        {
                            //FTET补给订单  
                            order.redundancy1 = SaveFileToFolder(fileName, fileData, uploadPath, "FTET补给订单");
                            order.redundancy2 = "补给";
                            order.cuscode = "FTET";
                            order.cusname = "一汽发动机";
                        }
                        else if (fileName.IndexOf("DHEC") > -1)  //客户提供的是xls文件不会走断代码 需要确认谁写的，什么情况下会走
                        {
                            if (fileName.IndexOf("PLAN") > -1)
                            {
                                XErrorLogToolMDM.WriteLog("DHEC上传取货计划");
                                //DHEC取货计划
                                order.redundancy1 = SaveFileToFolder(fileName, fileData, uploadPath, "DHEC\\取货计划");
                                order.redundancy2 = "取货计划";
                                order.cuscode = "DHEC";
                                order.cusname = "东风本田";
                            }
                            else
                            {
                                XErrorLogToolMDM.WriteLog("DHEC上传量产订单");

                                //DHEC量产订单
                                order.redundancy1 = SaveFileToFolder(fileName, fileData, uploadPath, "DHEC\\订单");
                                order.redundancy2 = "量产";
                                order.cuscode = "DHEC";
                                order.cusname = "东风本田";
                            }
                        }
                        else if (fileName.IndexOf("GHAC") > -1)
                        {
                            //*********可能是周订单 可能是取货计划，需要再次区分
                            string fileType = string.Empty;

                            // 广本周订单导入 数据需要写入数据库
                            string fileSavePath = SaveFileToFolder(fileName, fileData, uploadPath, "GHAC");

                            GHACController ghac = new GHACController();

                            object obj = new
                            {
                                filePath = fileSavePath,
                                fileType = "EXCEL"
                            };

                            ghac.GHACUpload(JObject.Parse(JsonConvert.SerializeObject(obj)));

                            result = new baseMessage() { success = true, result = "文件上传完成" };
                            return result;
                        }
                        else if (fileName.IndexOf("销售订单工作台整单导出") > -1)
                        {
                            order.redundancy1 = SaveFileToFolder(fileName, fileData, uploadPath, "CHAM\\周订单");
                            order.redundancy2 = "周订单";
                            order.cuscode = "CHAM";
                            order.cusname = "佛山本田";
                            _chaMdisposeController.saveWeekOrder(fileData);
                        }
                        else if (fileName.IndexOf("取货计划") > -1 || fileName.IndexOf("SP计划")>-1) {

                            string fileType = string.Empty;

                            // 广本周订单导入 数据需要写入数据库
                            string fileSavePath = SaveFileToFolder(fileName, fileData, uploadPath, "GHAC");

                            order.redundancy1 = fileSavePath;
                            order.redundancy2 = "取货计划";
                            order.cuscode = "GHAC";
                            order.cusname = "广州本田";

                            GHACController ghac = new GHACController();

                            object obj = new
                            {
                                filePath = fileSavePath,
                                fileType = "EXCEL",
                                username = username
                            };

                            ghac.GHACUpload(JObject.Parse(JsonConvert.SerializeObject(obj)));

                            result = new baseMessage() { success = true, result = "文件上传完成" };
                        }
                            break;
                    case ".pdf":
                        {
                            ///上传
                            string tempPdf = SaveFileToFolder(fileName, fileData, uploadPath, "TEMP");
                            //判断pdf类型
                            SpireHelper.enumPdfType pdfType = SpireHelper.getPdfType(tempPdf);

                            GHACController ghac = new GHACController();
                            object obj;

                            switch (pdfType)
                            {
                                case SpireHelper.enumPdfType.NONE:
                                    result = new baseMessage() { success = false, result = "不能识别的PDF类型" };
                                    //return result;
                                    break;


                                case SpireHelper.enumPdfType.CHAM_SUMMONS:
                                    order.redundancy1 = SaveFileToFolder(fileName, fileData, uploadPath, "CHAM\\传票");
                                    order.redundancy2 = "传票";
                                    order.cuscode = "CHAM";
                                    order.cusname = "佛山本田";
                                    // _chaMdisposeController.DisposeSummonPdf(order.redundancy1);
                                    apiPath = ConfigurationManager.AppSettings["CHAMDisposeSummonPdf"].ToString();
                                    break;
                                case SpireHelper.enumPdfType.CHAM_INSTRUCTIONS:
                                    order.redundancy1 = SaveFileToFolder(fileName, fileData, uploadPath, "CHAM\\送货单");
                                    order.redundancy2 = "送货单";
                                    order.cuscode = "CHAM";
                                    order.cusname = "佛山本田";

                                    // 调用接口
                                    apiPath = ConfigurationManager.AppSettings["CHAMPath"].ToString();

                                    //_chaMdisposeController.disposeDeliveryNoteImport(order);
                                    break;
                                case SpireHelper.enumPdfType.CHAM_T:
                                    order.redundancy1 = SaveFileToFolder(fileName, fileData, uploadPath, "CHAM\\托标签\\temp");
                                    order.redundancy2 = "托标签";
                                    order.cuscode = "CHAM";
                                    order.cusname = "佛山本田";
                                    // _chaMdisposeController.DisposeStackLabel(order.redundancy1);
                                    apiPath = ConfigurationManager.AppSettings["CHAMDisposeStackLabel"].ToString();
                                    break;
                                case SpireHelper.enumPdfType.CHAM_D:
                                    order.redundancy1 = SaveFileToFolder(fileName, fileData, uploadPath, "CHAM\\看板");
                                    order.redundancy2 = "看板";
                                    order.cuscode = "CHAM";
                                    order.cusname = "佛山本田";
                                    // _chaMdisposeController.DisposeBoard(order.redundancy1);
                                    apiPath = ConfigurationManager.AppSettings["CHAMDisposeBoard"].ToString();
                                    
                                    break;
                                case SpireHelper.enumPdfType.GHAC_SUMMONS:
                                    //传票和取货计划书暂时无法分别
                                    order.redundancy1 = SaveFileToFolder(fileName, fileData, uploadPath, "GHAC");
                                    obj = new
                                    {
                                        filePath = order.redundancy1,
                                        fileType = "SUMMONS"
                                    };
                                    order.redundancy2 = "取货计划";
                                    order.cuscode = "GHAC";
                                    order.cusname = "广州本田";
                                    order.handlestate = "2";// 不需要解析 上传记录存成功
                                    ghac.GHACUpload(JObject.Parse(JsonConvert.SerializeObject(obj)));
                                    result = new baseMessage() { success = true, result = "文件上传完成" };
                                   // return result;
                                    break;
                                case SpireHelper.enumPdfType.GHAC_INSTRUCTIONS:
                                    //传票和取货计划书暂时无法分别
                                    order.redundancy1 = SaveFileToFolder(fileName, fileData, uploadPath, "GHAC");
                                    obj = new
                                    {
                                        filePath = order.redundancy1,
                                        fileType = "INSTRUCTIONS"
                                    };
                                    order.redundancy2 = "传票";
                                    order.cuscode = "GHAC";
                                    order.cusname = "广州本田";
                                    order.handlestate = "2";// 不需要解析 上传记录存成功
                                    ghac.GHACUpload(JObject.Parse(JsonConvert.SerializeObject(obj)));
                                    result = new baseMessage() { success = true, result = "文件上传完成" };
                                   // return result;
                                    break;
                                case SpireHelper.enumPdfType.GHAC_PLS:
                                case SpireHelper.enumPdfType.GHAC_PLS2:
                                    order.redundancy1 = SaveFileToFolder(fileName, fileData, uploadPath, "GHAC");
                                    obj = new
                                    {
                                        filePath = order.redundancy1,
                                        fileType = "PLS"
                                    };
                                    order.redundancy2 = "看板";
                                    order.cuscode = "GHAC";
                                    order.cusname = "广州本田";
                                    order.handlestate = "2";// 不需要解析 上传记录存成功
                                    ghac.GHACUpload(JObject.Parse(JsonConvert.SerializeObject(obj)));
                                    result = new baseMessage() { success = true, result = "文件上传完成" };
                                   // return result;
                                    break;                                
                                case SpireHelper.enumPdfType.GHAC_T:
                                    order.redundancy1 = SaveFileToFolder(fileName, fileData, uploadPath, "GHAC");
                                    obj = new
                                    {
                                        filePath = order.redundancy1,
                                        fileType = "PALLET"
                                    };
                                    order.redundancy2 = "托标签";
                                    order.cuscode = "GHAC";
                                    order.cusname = "广州本田";
                                    order.handlestate = "2";// 不需要解析 上传记录存成功
                                    ghac.GHACUpload(JObject.Parse(JsonConvert.SerializeObject(obj)));
                                    result = new baseMessage() { success = true, result = "文件上传完成" };
                                   // return result;
                                    break;
                                case SpireHelper.enumPdfType.GHAC_TRACE:
                                    order.redundancy1 = SaveFileToFolder(fileName, fileData, uploadPath, "GHAC");
                                    order.redundancy2 = "追溯标签";
                                    order.cuscode = "GHAC";
                                    order.cusname = "广州本田";
                                    obj = new
                                    {
                                        filePath = order.redundancy1,
                                        fileType = "TRACE"
                                    };
                                    ghac.GHACUpload(JObject.Parse(JsonConvert.SerializeObject(obj)));
                                    result = new baseMessage() { success = true, result = "文件上传完成" };
                                   // return result;
                                    break;


                                case SpireHelper.enumPdfType.DHEC_D:
                                    //DHEC看板
                                    order.redundancy1 = SaveFileToFolder(fileName, fileData, uploadPath, "DHEC\\看板");
                                    order.redundancy2 = "看板";
                                    order.cuscode = "DHEC";
                                    order.cusname = "东风本田";
                                    order.handlestate = "2";// 不需要解析 上传记录存成功
                                    //apiPath = ConfigurationManager.AppSettings["DHECPath"].ToString();
                                    break;
                                case SpireHelper.enumPdfType.DHEC_D_SP:
                                    //DHEC看板
                                    order.redundancy1 = SaveFileToFolder(fileName, fileData, uploadPath, "DHEC\\传票");
                                    order.redundancy2 = "传票";
                                    order.cuscode = "DHEC";
                                    order.cusname = "东风本田";
                                    order.handlestate = "2";// 不需要解析 上传记录存成功
                                    //apiPath = ConfigurationManager.AppSettings["DHECPath"].ToString();
                                    break;
                                case SpireHelper.enumPdfType.GHAC_01:
                                    order.redundancy1 = SaveFileToFolder(fileName, fileData, uploadPath, "GHAC");
                                    order.redundancy2 = "01版";
                                    order.cuscode = "GHAC";
                                    order.cusname = "广州本田";
                                    obj = new
                                    {
                                        filePath = order.redundancy1,
                                        fileType = "GHAC01"
                                    };
                                    ghac.GHACUpload(JObject.Parse(JsonConvert.SerializeObject(obj)));
                                    result = new baseMessage() { success = true, result = "文件上传完成" };
                                    break;

                                default:
                                    break;
                            }                           
                        }                                              

                        break;
                    default:
                        break;
                }

                orderList.Add(order);
            }

            var filteredList = orderList
    .Where(order =>
        !string.IsNullOrEmpty(order.cusname) ||
        !string.IsNullOrEmpty(order.cuscode) ||
        !string.IsNullOrEmpty(order.redundancy1))
    .ToList();
            //批量写库
            orderservice.Setorderimport(filteredList);
            result = new baseMessage() { success = true, result = "文件上传完成" };
            List<orderImport> newList = orderimportService.getOrderDB(filteredList);
            // 数据处理 不用等返回结果
            _ = Task.Run(async () =>
            {
                try
                {
                    await up.analysisAPI_batch(newList);
                }
                catch (Exception ex)
                {
                    XErrorLogToolMDM.WriteLog($"后台错误: {ex.Message}");
                }
            });
            return result;
        }
        /// <summary>
        /// 多个文件重新解析
        /// </summary>
        /// <param name="orD">handlestate 需要传1，handlemess传‘重新解析’</param>
        /// <returns></returns>
        public baseMessage order_Re_analyze(List<orderImport> reouseList)
        {
            baseMessage bmess = new baseMessage();
            try
            {
                // 修改状态
                orderimportService.setState(reouseList);
                orderimportService.DeleteDetail2(reouseList);
                // 每一条数据发送一次数据处理
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await up.analysisAPI_batch(reouseList);
                    }
                    catch (Exception ex)
                    {
                        XErrorLogToolMDM.WriteLog($"后台错误: {ex.Message}");
                    }
                });
                bmess.message = "已重新发送解析";
                bmess.success = true;
            }
            catch (Exception e)
            {
                bmess.message = "重新解析失败" + e.Message;
                bmess.success = false;
            }
            return bmess;

        }
        

        [HttpPost]
        public async Task<baseMessage> UploadFile1()
        {
            if (!Request.Content.IsMimeMultipartContent())
            {
                // 根据实际情况修改返回值，方便前端接收
                return new baseMessage() { success = false, result = "不支持的媒体类型" };
                //return Request.CreateErrorResponse(HttpStatusCode.UnsupportedMediaType, "不支持的媒体类型");
            }

            //  考虑点：高老师前台控制不住，后台需要控制文件大小，文件类型，是否有效文件，还需要支持多文件上传
            try
            {
                var provider = new MultipartMemoryStreamProvider();
                await Request.Content.ReadAsMultipartAsync(provider);

                string uploadPath = HttpContext.Current.Server.MapPath("~/Uploads");
                Directory.CreateDirectory(uploadPath);

                foreach (var file in provider.Contents)
                {
                    var fileName = file.Headers.ContentDisposition.FileName?.Trim('\"');
                    if (string.IsNullOrEmpty(fileName))
                    {
                        return new baseMessage() { success = false, result = "无效的文件名" };
                        //return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "无效的文件名");
                    }

                    var fileData      = await file.ReadAsByteArrayAsync();
                    var fileExtension = Path.GetExtension(fileName).ToLower();
                    var mimeType      = file.Headers.ContentType.MediaType;

                    // 基础验证
                    if (!ValidateFile(fileExtension, mimeType, fileData.Length))
                    {
                        return new baseMessage() { success = false, result = "文件验证失败" };
                        //return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "文件验证失败");
                    }

                    // 
                    // 处理压缩文件；写入数据库需要解压完读到Excel数据才能写，PDF......
                    if (fileExtension == ".zip" || fileExtension == ".rar")
                    {
                        // 根据文件名称 实现不同逻辑 1028且压缩包是长春（ftce)量产；一汽发动机量产/补给品
                        var result = await ProcessCompressedFile1028(fileData, fileExtension, uploadPath);
                        if (!result.Success)
                        {
                            return new baseMessage() { success = false, result = result.Message };
                            //return Request.CreateErrorResponse(HttpStatusCode.BadRequest, result.Message);
                        }
                    }
                    else if (fileExtension == ".xls" || fileExtension == ".xlsx")
                    {
                        // 非压缩包处理逻辑 长春补给品；佛山本田Excel；东风本田；广州本田
                        SaveFile(fileData, uploadPath, fileName);
                    }
                    else
                    {
                        // pdf 处理逻辑；佛山本田；东风本田；广州本田
                    }
                }

                return new baseMessage() { success = false, result = "文件上传完成" };
                //return Request.CreateResponse(HttpStatusCode.OK, "文件处理完成");
            }
            catch (Exception ex)
            {
                return new baseMessage() { success = false, result = $"处理错误: {ex.Message}" };
                // return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, $"处理错误: {ex.Message}");
            }
        }

        private bool ValidateFile(string extension, string mimeType, long fileSize)
        {
            // 大小限制（10MB）
            if (fileSize > 10 * 1024 * 1024) return false;

            return AllowedExtensions.Contains(extension) &&
                   AllowedMimeTypes.Contains(mimeType);
        }

        // 异步处理文件压缩文件
        private async Task<(bool Success, string Message)> ProcessCompressedFile1028(byte[] fileData, string extension,
            string                                                                          basePath)
        {
            try
            {
                using (var stream = new MemoryStream(fileData))
                {
                    List<string> tempFiles   = new List<string>();
                    string       extractPath = Path.Combine(basePath, "temp", Guid.NewGuid().ToString());
                    Directory.CreateDirectory(extractPath);

                    // 处理ZIP文件
                    if (extension == ".zip")
                    {
                        using (var archive = new ZipArchive(stream))
                        {
                            foreach (var entry in archive.Entries)
                            {
                                if (entry.Length == 0) continue;

                                var filePath = Path.Combine(extractPath, entry.FullName);
                                entry.ExtractToFile(filePath);
                                tempFiles.Add(filePath);
                            }
                        }
                    }
                    // 处理RAR文件
                    else if (extension == ".rar")
                    {
                        using (var archive = SharpCompress.Archives.Rar.RarArchive.Open(stream))
                        {
                            foreach (var entry in archive.Entries.Where(x => !x.IsDirectory))
                            {
                                var filePath = Path.Combine(extractPath, entry.Key);
                                entry.WriteToFile(filePath);
                                tempFiles.Add(filePath);
                            }
                        }
                    }

                    // 验证解压文件 大小限制需要写入config
                    foreach (var tempFile in tempFiles)
                    {
                        var ext = Path.GetExtension(tempFile).ToLower();
                        if (!IsValidUncompressedFile(ext))
                        {
                            Directory.Delete(extractPath, true);
                            return (false, $"压缩包包含不支持的文件类型: {ext}");
                        }

                        // 验证文件大小
                        if (new FileInfo(tempFile).Length > 10 * 1024 * 1024)
                        {
                            Directory.Delete(extractPath, true);
                            return (false, "解压文件大小超过限制");
                        }
                    }

                    // 移动有效文件并做处理，准备拆包所需数据
                    foreach (var tempFile in tempFiles)
                    {
                        // 新增的Excel拆分逻辑应在此处开始
                        var ext = Path.GetExtension(tempFile).ToLower();

                        // 如果是Excel文件则进行拆分
                        if (ext == ".xlsx" || ext == ".xls")
                        {
                            try
                            {
                                // 调用Excel拆分方法（需要自行区分长春还是其他啊实现）
                                // var splitResults = filehelper.ConvertExcelToPdfs(tempFile, extractPath);

                                // 思路：处理拆分后的文件；位置移动，清理文件
                                //foreach (var splitFilePath in splitResults)
                                //{
                                //    var splitFileName = $"{Guid.NewGuid()}{Path.GetExtension(splitFilePath)}";
                                //    var splitDestPath = Path.Combine(basePath, splitFileName);
                                //    File.Move(splitFilePath, splitDestPath);  // 移动拆分后的文件
                                //}

                                File.Delete(tempFile); // 删除原始文件 必要时使用
                            }
                            catch (Exception ex)
                            {
                                Directory.Delete(extractPath, true);
                                return (false, $"Excel文件拆分失败: {ex.Message}");
                            }
                        }
                    }

                    // Directory.Delete(extractPath, true);// 删除路径文件以后再说，逻辑写完开启
                    // 处理完成无错误需要在数据库做记录 需要考虑楠哥说的主子表结构怎么写

                    return (true, string.Empty);
                }
            }
            catch (Exception ex)
            {
                return (false, $"解压失败: {ex.Message}");
            }
        }

        private bool IsValidUncompressedFile(string extension)
        {
            switch (extension.ToLower())
            {
                case ".xls":
                case ".xlsx":
                case ".pdf":
                    return true;
                default:
                    return false;
            }
        }

        // 异步处理Excel文件 获取文件 详细拆解在fileHelper中写
        private async void SaveFile(byte[] fileData, string path, string fileName)
        {
            var newFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
            File.WriteAllBytes(Path.Combine(path, newFileName), fileData);
        }
        

        /// <summary>
        /// 创建与上传文件同名的文件夹并保存源文件
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="fileData"></param>
        /// <param name="basePath"></param>
        private string SaveFileToFolder(string fileName, byte[] fileData, string basePath, string Cust = "unknow")
        {
            // 获取文件名不含扩展名的部分
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            // 创建以文件名命名的文件夹路径
            var fileFolderPath = Path.Combine(basePath, Cust);
            // 创建文件夹
            Directory.CreateDirectory(fileFolderPath);

            // 创建完整文件路径并保存
            var fullFilePath = Path.Combine(fileFolderPath, fileName);
            if (File.Exists(fullFilePath))
            {
                //如果存在同名文件 ，则在文件名后加上时间戳
                string timeStamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                string uniqueFileName = $"{fileNameWithoutExtension}_{timeStamp}{Path.GetExtension(fileName)}";
                //string uniqueFileName = $"{fileNameWithoutExtension}{Path.GetExtension(fileName)}";
                fullFilePath = Path.Combine(fileFolderPath, uniqueFileName);
            }

            File.WriteAllBytes(fullFilePath, fileData);
            // 返回新创建的文件夹路径
            return fullFilePath;
        }


        [HttpPost]
        public ResParameter SaveConfirmRemark(ConfirmDto confirmDto)
        {
            if (confirmDto == null)
            {
                return ResParameter.Fail("确认表备注信息不能为空");
            }

            try
            {
                orderservice.SaveConformRemark(confirmDto);
                return ResParameter.Success();
            }
            catch (Exception e)
            {
                return ResParameter.Fail(e.Message);
            }
            
        }

        [HttpGet]
        public ResParameter GetConfirmRemark(string type,string ordercode) {
            try
            {
                if (string.IsNullOrEmpty(type)) {
                    return ResParameter.Fail("客户代码不能为空");
                }
                string category = ""; 
                if (type == "FTET" && !string.IsNullOrEmpty(ordercode)) {
                    category = ordercode.Substring(0, 2);
                }
                string remark = orderservice.GetConfirmBatch(type, category);
                return ResParameter.Success(remark);
            }
            catch (Exception)
            {
                return ResParameter.Fail("获取确认表备注信息失败");
            }
        }


    }
}