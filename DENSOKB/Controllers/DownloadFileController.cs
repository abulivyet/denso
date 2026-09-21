using DENSOKB.Helper;
using DENSOKB.Libs;
using DENSOKB.Models;
using HelpLog.CommomMethod;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using System.Web.Http;

namespace DENSOKB.Controllers
{
    public class DownloadFileController: ApiController
    {
        string apiPath = ConfigurationManager.AppSettings["apiPath"].ToString();//apiPath
        orderimportService orderService = new orderimportService();
        FTETnew _FTETnew = new FTETnew();
        [JwtAuthorize]
        [HttpPost]
        public HttpResponseMessage DownloadFile(OrderDownload orderDownload)
        {
            try
            {
                string filePath = "E:\\合众唯一\\南沙电装\\开发规约※必看.docx";
                // 检查文件是否存在
                if (!File.Exists(filePath))
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "File not found");
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

                return response;
            }
            catch (Exception ex)
            {
                // 错误处理
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        [JwtAuthorize]
        [HttpPost]
        public baseMessage DownloadFileZip(OrderDownload orderDownload)
        {
            baseMessage result = new baseMessage();
            try
            {
                DataTable detailDT= orderService.getinp_detail(orderDownload.id.ToString());
                string zippath = HttpContext.Current.Server.MapPath("~/Uploads")+ orderDownload.zipfilename;
                
                if (detailDT.Rows.Count>0)
                {
                    using (ZipOutputStream zipStream = new ZipOutputStream(File.Create(zippath)))
                    {
                        zipStream.SetLevel(9); // 设置压缩级别（0-9）
                        for (var i = 0; i < detailDT.Rows.Count; i++)
                        {
                            var item = detailDT.Rows[i];
                            string filePaths = "";
                            switch (orderDownload.type)
                            {
                                case "boxMin":  // 小纸张
                                    filePaths = item["board"].ToString();
                                    break;
                                case "boxA4":
                                    filePaths = item["a4board"].ToString();
                                    break;
                            }
                            if (string.IsNullOrEmpty(filePaths)) continue;
                            // filePath 有多个 用分号分开  库里只存绝对路径  需要 从Uploads截取 后边为物理路径
                            string[] filePathArr = filePaths.Split(';');
                            foreach (var filePathcell in filePathArr)
                            {
                                //var endPath = filePathcell.Split(new[] { "Uploads" }, StringSplitOptions.None);
                                //var filePath = apiPath + "/Uploads" + endPath[1].ToString().Replace('\\', '/');
                                var filePath = filePathcell;
                                if (string.IsNullOrEmpty(filePath)) continue;
                                FileInfo fileInfo = new FileInfo(filePath);
                                byte[] buffer = new byte[4096];
                                // 添加文件到压缩包
                                ZipEntry entry = new ZipEntry(Path.GetFileName(filePath));
                                entry.DateTime = DateTime.Now;
                                zipStream.PutNextEntry(entry);

                                using (FileStream fs = File.OpenRead(filePath))
                                {
                                    int sourceBytes;
                                    do
                                    {
                                        sourceBytes = fs.Read(buffer, 0, buffer.Length);
                                        zipStream.Write(buffer, 0, sourceBytes);
                                    } while (sourceBytes > 0);
                                }
                            }
                           

                        }
                        zipStream.Finish();
                    }
                    DownloadZip(zippath, orderDownload.zipfilename);
                    File.Delete(zippath); 
                }
                else
                {
                    result.success = false;
                    result.message = "无上传数据";
                    return result;
                }
                result.success = true;
                result.message = "导出完成";
            }
            catch (Exception ex)
            {
                result.success = false;
                result.message = "导出失败" + ex.Message;
                XErrorLogToolMDM.WriteLog("导出失败，" + ex.Message);
            }
            return result;
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
        ///  行按钮点击文件下载
        /// </summary>
        /// <param name="orderDownload"></param>
        /// <returns></returns>
        [JwtAuthorize]
        [HttpPost]
        public HttpResponseMessage DownloadFileZipNew(OrderDownload orderDownload)
        {
            baseMessage result = new baseMessage();
            try
            {
                DataTable detailDT = orderService.getinp_detail2(orderDownload.id.ToString());
                string zippath = HttpContext.Current.Server.MapPath("~/Uploads") + orderDownload.zipfilename;
                if (detailDT.Rows.Count == 0) {
                    return Request.CreateResponse(HttpStatusCode.InternalServerError, "未找到解析后的数据，请重新解析");
                }

                if (detailDT.Rows.Count > 0)
                {
                    string filePaths = "";
                    switch (orderDownload.type)
                    {
                        case "boxMin":  // 小纸张
                            filePaths = "小纸张";
                            break;
                        case "boxA4":
                            filePaths = "A4";
                            break;
                        case "MDcode":// 码垛单
                            filePaths = "码垛单";
                            break;
                        case "sls":// 纳受领书
                            filePaths = "受领书";
                            break;
                        case "confirm":// 确认单
                            filePaths = "确认表";
                            orderService.EditConfirmRemark(orderDownload.id.ToString(),orderDownload.cuscode,orderDownload.ordercode);
                            break;
                    }
                    
                    //  受领书有三份已经存好了 下载时只需要一个
                    DataRow[] dr = detailDT.Select("filetype='" + filePaths + "'");
                    if (dr.Length > 0)
                    {
                        if (orderDownload.type.Equals("sls") || dr.Length == 1)
                        {
                            DownloadPdf(dr[0]["Stacklabel"].ToString(), Path.GetFileNameWithoutExtension(dr[0]["Stacklabel"].ToString()));
                        }
                        else
                        {
                            List<string> lis = new List<string>();
                            if (dr.Length>0)
                            {
                                // 多个文件合并成一个  
                                foreach (var item in dr)
                                {
                                    lis.Add(item["Stacklabel"].ToString());
                                }
                                string filepath = System.IO.Path.GetDirectoryName(dr[0]["Stacklabel"].ToString());
                                string newpaht= SpireHelper.MergeFiles(lis, filepath+"合并.pdf");
                                DownloadPdf(newpaht, Path.GetFileNameWithoutExtension(newpaht));
                            }
                            
                        }
                        //  分开代码保留
                        //else
                        //{
                        //    HashSet<string> fileNames = new HashSet<string>(); // 用于存储已经添加的文件名

                        //    using (ZipOutputStream zipStream = new ZipOutputStream(File.Create(zippath)))
                        //    {
                        //        for (int i = 0; i < dr.Length; i++)
                        //        {
                        //            var filePath = dr[i]["Stacklabel"].ToString();
                        //            if (string.IsNullOrEmpty(filePath)) continue;

                        //            FileInfo fileInfo = new FileInfo(filePath);
                        //            string   fileName = Path.GetFileName(filePath);
                
                        //            // 如果文件名已经存在，跳过这个文件
                        //            if (fileNames.Contains(fileName))
                        //            {
                        //                continue; 
                        //            }

                        //            fileNames.Add(fileName); // 添加到文件名集合中，避免重复

                        //            byte[] buffer = new byte[4096];
                        //            // 添加文件到压缩包
                        //            ZipEntry entry = new ZipEntry(fileName);
                        //            entry.DateTime = DateTime.Now;
                        //            zipStream.PutNextEntry(entry);

                        //            using (FileStream fs = File.OpenRead(filePath))
                        //            {
                        //                int sourceBytes;
                        //                do
                        //                {
                        //                    sourceBytes = fs.Read(buffer, 0, buffer.Length);
                        //                    zipStream.Write(buffer, 0, sourceBytes);
                        //                } while (sourceBytes > 0);
                        //            }
                        //        }
                        //    }

                        //    DownloadZip(zippath, orderDownload.zipfilename);
                        //}
                    }

                    else
                    {
                        result.success = false;
                        result.message = string.Format("类型[{0}无附件]", filePaths);
                        // return result;
                        return Request.CreateErrorResponse(HttpStatusCode.NotFound, string.Format("类型[{0}无附件]", filePaths));
                    }
                    
                    File.Delete(zippath);
                    
                    orderimportService.DownloadCountPlus(orderDownload.id.ToString());
                }
                else
                {
                    //HttpResponse response = HttpContext.Current.Response;
                    //response.Clear();
                    //response.ContentType = "application/zip";
                    //response.AppendHeader("Content-Disposition", $"attachment;");
             
                    //response.StatusCode = 200;
                    //response.StatusDescription = HttpUtility.UrlEncode("无上传数据", Encoding.UTF8);;
                    //response.Flush();
                    //new System.Web.Mvc.HttpStatusCodeResult(500, "服务器内部错误");
                    result.success = false;
                    result.message = "无上传数据";
                   
                    //return result;
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, "无上传数据");
                }
                result.success = true;
                result.message = "导出完成";
            }
            catch (Exception ex)
            {
                result.success = false;
                result.message = "导出失败" + ex.Message;
                XErrorLogToolMDM.WriteLog("导出失败，" + ex.Message);
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
            //return result;
            return Request.CreateErrorResponse(HttpStatusCode.NotFound, string.Format("导出完成"));
        }
        /// <summary>
        /// 多选行 根据选择记录下载
        /// </summary>
        /// <param name="orderDList">选择的行数据</param>
        /// <returns></returns>
        [JwtAuthorize]
        [HttpPost]
        public baseMessage DownloadFileZipRowid(List<OrderDownload> orderDList)
        {
            baseMessage result = new baseMessage();
            string check7 = "check7";
            string fileUpload = ConfigurationManager.AppSettings["fileUpload"].ToString();
            try
            {
                if (orderDList.Count==0)
                {
                    result.success = false;
                    result.message = "未选择数据";
                    return result;
                }
                List<inpdetail2> inpdetail2List = new List<inpdetail2>();// 可能前台只勾选取套票，所以得用List接收
                DataTable detailDT = orderService.getinp_detailForList(orderDList);

                if (orderDList[0].type.IndexOf("check2") > -1 && orderDList[0].type.IndexOf("check3") > -1)
                {
                    foreach (var entity in orderDList)
                    {
                        DataRow[] MDRow = detailDT.Select($"filetype='码垛单' and mainid='{entity.id}'");
                        DataTable hbmddTB = detailDT.Clone();
                        foreach (DataRow row in MDRow)
                        {
                            hbmddTB.ImportRow(row); // 保留原行状态
                        }
                        // 合并码垛单
                        string TPpath = MergeFiles(hbmddTB, fileUpload, entity.zipfilename + "合并码垛单.pdf");
                        inpdetail2 inpMD = new inpdetail2();
                        inpMD.Stacklabel = TPpath;
                        inpMD.mainId = entity.id.ToString();
                       inpMD.filetype = "码垛单";
                        inpdetail2List.Add(inpMD);
                    }
                    
                }
                HashSet<string> processedIds = new HashSet<string>();

                List<string> boxFilePath = new List<string>();
                for (int i = 0; i < detailDT.Rows.Count; i++)
                {
                    var item = detailDT.Rows[i];
                // 如果 勾选了check2和check3 需要吧码垛单合并成一个文件
                    if (orderDList[0].type.IndexOf("check2") > -1 && orderDList[0].type.IndexOf("check3") > -1)
                    {
                        if (item["filetype"].ToString().Equals("码垛单")) continue;
                    }
                    if (item["filetype"].ToString().Equals("受领书"))
                    {
                        if (processedIds.Contains(item["mainId"].ToString()+ item["filetype"].ToString()))
                        {
                            continue;
                        }
                        else
                        {
                            processedIds.Add(item["mainId"].ToString() + item["filetype"].ToString());
                        }
                       
                    }
                    string a = item["filetype"].ToString();
                    string b = orderDList[0].type;
                    bool aa = item["filetype"].ToString().Equals("小纸张");
                    bool bb = orderDList[0].type.IndexOf("boxA4") > -1;
                    if (item["filetype"].ToString().Equals("小纸张") && orderDList[0].type.IndexOf("boxA4") > -1)
                    {
                        //下载A4,在处理
                        boxFilePath.Add(item["Stacklabel"].ToString());
                        continue;
                    }

                    inpdetail2 inp = new inpdetail2();
                    inp.Stacklabel = item["Stacklabel"].ToString();
                    inp.mainId= item["mainId"].ToString();
                    inp.filetype = item["filetype"].ToString();
                    inpdetail2List.Add(inp);
                }
                #region 小纸张合并转A4
                DirectoryInfo dirTemp = Directory.CreateDirectory(Path.Combine(fileUpload, Guid.NewGuid().ToString()));
                for (int i = 0; i < boxFilePath.Count; i++)
                {
                    DirectoryInfo dirTempSub = dirTemp.CreateSubdirectory(Path.GetFileNameWithoutExtension(boxFilePath[i]));

                    SpireHelper.PdfSplit(boxFilePath[i], dirTempSub.FullName, 0, 1, 1, SpireSplit.EnumNameRule.Auto, new System.Drawing.Rectangle[] { }, new System.Drawing.Rectangle());
                }
                if (boxFilePath.Count > 0) // 有数据处理 无数据不处理
                {
                    FileInfo[] AllBoxFiles = dirTemp.GetFiles("*.*", SearchOption.AllDirectories).OrderBy(f => f.CreationTime).ToArray();
                    string outMergeBoxFilePath = dirTemp.FullName + ".pdf";
                    switch (orderDList[0].cuscode)
                    {
                        case "FTCE":
                            if (orderDList[0].db_type == "量产")
                                SpireHelper.Merge(4, 1, AllBoxFiles, outMergeBoxFilePath, true,0f,20f);
                            else if (orderDList[0].db_type == "补给")
                                SpireHelper.Merge(3, 1, AllBoxFiles, outMergeBoxFilePath, true);
                            break;
                        case "FTET":
                            SpireHelper.Merge(3, 1, AllBoxFiles, outMergeBoxFilePath, true,0f,20f);
                            break;
                        default:
                            break;
                    }
                    inpdetail2 inp2 = new inpdetail2();
                    inp2.Stacklabel = outMergeBoxFilePath;
                    inp2.mainId = string.Empty;
                    inp2.filetype = "小纸张";
                    inpdetail2List.Add(inp2);
                }
                #endregion
                if (orderDList[0].cuscode.Equals("FTET") && orderDList[0].type.IndexOf("check7")>-1)
                {
                    // 套票 即使选多行也只有一个， 在第一个文件增加 核对记录表
                    string excelTemplate = ConfigurationManager.AppSettings["FTETHD"].ToString();
                    string outpath = ConfigurationManager.AppSettings["fileUpload"].ToString() + "\\FTET\\核对记录表.xlsx";
                    XErrorLogToolMDM.WriteLog(string.Format("调用FTETdisposeController.Export传入参数{0}，{1}，{2}", orderDList[0].ordercode, AppDomain.CurrentDomain.BaseDirectory + excelTemplate, outpath));
                    string pafPath = FTETdisposeController.Export(orderDList[0].ordercode, AppDomain.CurrentDomain.BaseDirectory + excelTemplate, outpath);
                    inpdetail2 inp = new inpdetail2();
                    inp.Stacklabel = pafPath;
                    inp.mainId = "0";
                    inp.filetype = "套票";
                    inpdetail2List.Add(inp);
                }
                foreach (var item in orderDList)
                {
                    DataTable tp = orderService.getinp_detaiTP(item);

                    // #region 套票A4
                    //
                    // string[] parts = item.type.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    //
                    // if (parts.Contains("check7")&&parts.Contains("boxA4"))
                    // {
                    //     
                    // }
                    // #endregion

                    orderimportService.DownloadCountPlus(item.id.ToString());
                    if (tp == null || tp.Rows.Count == 0) continue;
                    var zipfilename = System.IO.Path.GetFileNameWithoutExtension(item.zipfilename);
                    string TPpath= MergeFiles(tp, fileUpload, zipfilename+"_套票.pdf");
                    if (!string.IsNullOrEmpty(TPpath)&& tp.Rows.Count>0)
                    {
                        inpdetail2 inp = new inpdetail2();
                        inp.Stacklabel = TPpath;
                        inp.mainId = tp.Rows[0]["mainId"].ToString();
                        inp.filetype ="套票";
                        inpdetail2List.Add(inp);
                    }
                    
                }
                
                if (inpdetail2List.Count == 0)
                {
                    result.success = false;
                    result.message = "无数据可导出";
                    return result;
                }
                //需要确定返回的是压缩包还是pdf ；取套票返回pdf，其他的返回压缩包


                string mess = "";
                // 返回压缩包
                bool isok= returnZip(orderDList, inpdetail2List, out mess);
                if (!isok)
                {
                    result.success = isok;
                    result.message = mess;
                    return result;
                }
                
                result.success = true;
                result.message = "导出成功";
            }
            catch (Exception ex)
            {
                result.success = false;
                result.message = "导出失败：" + ex.Message;
                XErrorLogToolMDM.WriteLog("导出失败，" + ex.Message);

               
            }
            return result;
        }
        public bool returnZip(List<OrderDownload> orderDList ,List<inpdetail2> inpdetail2List, out string message)
        {
            bool isok = true;
            string tempDir = null;
            message = "";
            try
            {
                // 创建临时目录存放子压缩包
                tempDir = Path.Combine(HttpContext.Current.Server.MapPath("~/Uploads"), "Temp_" + Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);
                DirectoryInfo Dtempdir = new DirectoryInfo(tempDir);
                DirectoryInfo dirMerge = new DirectoryInfo(tempDir);

                //foreach (var order in orderDList)
                {
                    //FileInfo f_temp = new FileInfo(order.zipfilename);
                    //string saveTempDir = Path.Combine(tempDir, f_temp.Name.Replace(f_temp.Extension, ""));

                    //Directory.CreateDirectory(saveTempDir);

                    //List<inpdetail2> itemlist = inpdetail2List.Where(t => t.mainId == order.id.ToString()).ToList();

                    //foreach (inpdetail2 row in itemlist)
                    //{
                    //    FileInfo Stacklabel = new FileInfo(row.Stacklabel);
                    //    Stacklabel.CopyTo(Path.Combine(saveTempDir, Stacklabel.Name),true);
                    //}

                    #region 客户要求改成合并

                    //先输出到同一目录
                    // f_temp = new FileInfo(order.zipfilename);

                    // List<inpdetail2> itemlist = inpdetail2List.Where(t => t.mainId == order.id.ToString()).ToList();

                    //把所有文件按照目录分组存放
                    //foreach (inpdetail2 row in inpdetail2List)
                    //{
                    //    string saveTempDir = Path.Combine(tempDir, row.filetype);
                    //    //string saveTempDir = tempDir;
                    //    //if (Directory.Exists(saveTempDir))
                    //    //{
                    //        Directory.CreateDirectory(saveTempDir);
                    //    // }
                    //    if (string.IsNullOrEmpty(row.Stacklabel)) continue;
                    //    FileInfo Stacklabel = new FileInfo(row.Stacklabel);
                    //    Stacklabel.CopyTo(Path.Combine(saveTempDir, Stacklabel.Name), true);                        
                    //}
                    foreach (inpdetail2 row in inpdetail2List)
                    {
                        string saveTempDir = Path.Combine(tempDir, row.filetype);
                        Directory.CreateDirectory(saveTempDir);

                        if (string.IsNullOrEmpty(row.Stacklabel))
                            continue;

                        FileInfo Stacklabel = new FileInfo(row.Stacklabel);
                        string destFileName = Stacklabel.Name;
                        string destFullPath = Path.Combine(saveTempDir, destFileName);

                        // 如果文件已存在，添加数字后缀
                        if (File.Exists(destFullPath))
                        {
                            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(destFileName);
                            string extension = Path.GetExtension(destFileName);
                            int counter = 1;

                            do
                            {
                                destFileName = $"{fileNameWithoutExt}_{counter}{extension}";
                                destFullPath = Path.Combine(saveTempDir, destFileName);
                                counter++;
                            } while (File.Exists(destFullPath));
                        }

                        Stacklabel.CopyTo(destFullPath, true);
                    }

                    DirectoryInfo[] dirs = Dtempdir.GetDirectories();

                    dirMerge = Dtempdir.CreateSubdirectory("Merge");

                    foreach (var item in dirs)
                    {
                        FileInfo[] files = item.GetFiles();
                        string MergePath = Path.Combine(dirMerge.FullName, files[0].Name);
                        SpireHelper.MergeFiles(files.Select(file => file.FullName).ToList(), MergePath);

                    }

                    #endregion
                }



                FileInfo [] finalFiles = dirMerge.GetFiles("*.*", SearchOption.AllDirectories);

                if(finalFiles.Length>1)
                {
                    //zip打包
                    string ZipPath = Path.Combine(HttpContext.Current.Server.MapPath("~/Uploads"), $"{DateTime.Now:yyyyMMddHHmmss}.zip");

                    System.IO.Compression.ZipFile.CreateFromDirectory(dirMerge.FullName, ZipPath);

                    DownloadZip(ZipPath, Path.GetFileNameWithoutExtension(ZipPath));

                    File.Delete(ZipPath);
                }
                else
                {
                    //直接返回文件
                    DownloadPdf(finalFiles[0].FullName, Path.GetFileNameWithoutExtension(finalFiles[0].FullName));                    
                }

                Directory.Delete(tempDir, true);

                #region 海阳哥的老代码不要了
                /*
                 // 生成总压缩包路径，确保文件名唯一
                string totalZipName = $"{orderDList[0].zipfilename}_{DateTime.Now:yyyyMMddHHmmss}.zip";
                string totalZipPath = Path.Combine(HttpContext.Current.Server.MapPath("~/Uploads"), totalZipName);

                using (ZipOutputStream totalZipStream = new ZipOutputStream(File.Create(totalZipPath)))
                {
                    totalZipStream.SetLevel(9); // 设置压缩级别

                    foreach (OrderDownload order in orderDList)
                    {
                        // 筛选当前订单对应的文件行，假设OrderRowId为关联字段
                        //DataRow[] rows = detailDT.Select($"mainid = '{order.id}'");
                        List<inpdetail2> itmelist = inpdetail2List.Where(t => t.mainId == order.id.ToString()).ToList();
                        if (itmelist.Count == 0) continue;

                        // 生成子压缩包临时路径
                        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(order.zipfilename);
                        string subZipName = $"{fileNameWithoutExt}_{order.id}.zip";
                        string subZipPath = Path.Combine(tempDir, subZipName);

                        // 创建子压缩包
                        using (ZipOutputStream subZipStream = new ZipOutputStream(File.Create(subZipPath)))
                        {
                            subZipStream.SetLevel(9);
                            foreach (inpdetail2 row in itmelist)
                            {
                                //string filePath = row["Stacklabel"].ToString();
                                string filePath = row.Stacklabel.ToString();
                                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) continue;

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
                        }

                        // 将子压缩包添加到总压缩包
                        ZipEntry totalEntry = new ZipEntry(subZipName);
                        totalEntry.DateTime = DateTime.Now;
                        totalZipStream.PutNextEntry(totalEntry);
                        using (FileStream subFs = File.OpenRead(subZipPath))
                        {
                            byte[] buffer = new byte[4096];
                            int bytesRead;
                            do
                            {
                                bytesRead = subFs.Read(buffer, 0, buffer.Length);
                                totalZipStream.Write(buffer, 0, bytesRead);
                            } while (bytesRead > 0);
                        }

                        // 删除临时子压缩包
                        File.Delete(subZipPath);
                    }
                }

                // 清理临时目录
                Directory.Delete(tempDir, true);

                // 下载总压缩包
                DownloadZip(totalZipPath, totalZipName);

                // 删除总压缩包临时文件
                File.Delete(totalZipPath);
                 */
                #endregion

            }
            catch (Exception e)
            {
                message = e.Message;
                // 确保临时目录清理
                if (tempDir != null && Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
                isok = false;
            }
            return isok;
        }
        
        /// <summary>
        /// 根据检索条件
        /// </summary>
        /// <param name="orderDList"></param>
        /// <returns></returns>
        [JwtAuthorize]
        [HttpPost]
        public baseMessage DownloadFileZipJS(OrderDownload main)
        {
            baseMessage result = new baseMessage();
            string tempDir = null;
            List<OrderDownload> orderDList = new List<OrderDownload>();
            try
            {
                DataTable MainDT = orderService.getNotPageTB(main.query);
                for (var i=0;i< MainDT.Rows.Count;i++)
                {
                    OrderDownload od = new OrderDownload();
                    od.id = Convert.ToInt32( MainDT.Rows[i]["id"]);
                    od.zipfilename = MainDT.Rows[i]["redundancy3"].ToString();
                    od.type = main.type;
                    orderDList.Add(od);
                }

                DataTable detailDT = orderService.getinp_detailForList(orderDList);
                if (detailDT.Rows.Count == 0)
                {
                    result.success = false;
                    result.message = "无数据可导出";
                    return result;
                }

                // 创建临时目录存放子压缩包
                tempDir = Path.Combine(HttpContext.Current.Server.MapPath("~/Uploads"), "Temp_" + Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);

                // 生成总压缩包路径，确保文件名唯一
                string totalZipName = $"检索导出_{DateTime.Now:yyyyMMddHHmmss}.zip";
                string totalZipPath = Path.Combine(HttpContext.Current.Server.MapPath("~/Uploads"), totalZipName);

                using (ZipOutputStream totalZipStream = new ZipOutputStream(File.Create(totalZipPath)))
                {
                    totalZipStream.SetLevel(9); // 设置压缩级别

                    foreach (OrderDownload order in orderDList)
                    {
                        // 筛选当前订单对应的文件行，假设OrderRowId为关联字段
                        DataRow[] rows = detailDT.Select($"mainid = '{order.id}'");
                        if (rows.Length == 0) continue;

                        // 生成子压缩包临时路径
                        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(order.zipfilename);
                        string subZipName = $"{fileNameWithoutExt}_{order.id}.zip";
                        string subZipPath = Path.Combine(tempDir, subZipName);

                        // 创建子压缩包
                        using (ZipOutputStream subZipStream = new ZipOutputStream(File.Create(subZipPath)))
                        {
                            subZipStream.SetLevel(9);
                            foreach (DataRow row in rows)
                            {
                                string filePath = row["Stacklabel"].ToString();
                                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) continue;

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
                        }

                        // 将子压缩包添加到总压缩包
                        ZipEntry totalEntry = new ZipEntry(subZipName);
                        totalEntry.DateTime = DateTime.Now;
                        totalZipStream.PutNextEntry(totalEntry);
                        using (FileStream subFs = File.OpenRead(subZipPath))
                        {
                            byte[] buffer = new byte[4096];
                            int bytesRead;
                            do
                            {
                                bytesRead = subFs.Read(buffer, 0, buffer.Length);
                                totalZipStream.Write(buffer, 0, bytesRead);
                            } while (bytesRead > 0);
                        }

                        // 删除临时子压缩包
                        File.Delete(subZipPath);
                    }
                }

                // 清理临时目录
                Directory.Delete(tempDir, true);

                // 下载总压缩包
                DownloadZip(totalZipPath, totalZipName);

                // 删除总压缩包临时文件
                File.Delete(totalZipPath);

                result.success = true;
                result.message = "导出成功";
            }
            catch (Exception ex)
            {
                result.success = false;
                result.message = "导出失败：" + ex.Message;
                XErrorLogToolMDM.WriteLog("导出失败，" + ex.Message);

                // 确保临时目录清理
                if (tempDir != null && Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
            return result;
        }
        

        public string MergeFiles(DataTable dt, string outputPath, string filename)
        {
            string outurl = "";
            try
            {
                // 创建最终合并的PDF文档
                Spire.Pdf.PdfDocument mergedPdf = new Spire.Pdf.PdfDocument();
                var row = 0;
                foreach (DataRow item in dt.Rows)
                {
                    string filePath = item["Stacklabel"].ToString();
                    if (filePath.EndsWith(".pdf", System.StringComparison.OrdinalIgnoreCase))
                    {
                        // 直接合并PDF文件
                        Spire.Pdf.PdfDocument pdf = new Spire.Pdf.PdfDocument();
                        pdf.LoadFromFile(filePath);
                        mergedPdf.AppendPage(pdf);
                    }
                    else if (filePath.EndsWith(".xls", System.StringComparison.OrdinalIgnoreCase) ||
                              filePath.EndsWith(".xlsx", System.StringComparison.OrdinalIgnoreCase))
                    {
                        //using (var ms = new MemoryStream())
                        //{
                        //    //Spire.Xls.Workbook workbook = new Spire.Xls.Workbook();
                        //    //workbook.LoadFromFile(filePath);
                        //    //workbook.SaveToStream(ms, Spire.Xls.FileFormat.PDF);

                        //    //Spire.Pdf.PdfDocument pdf = new Spire.Pdf.PdfDocument(ms);
                        //    //mergedPdf.AppendPage(pdf);
                        //    ////////
                        //    Spire.Xls.Workbook workbook = new Spire.Xls.Workbook();
                        //    workbook.LoadFromFile(filePath);

                        //    // 新增代码：强制Excel内容缩放到一页
                        //    foreach (Spire.Xls.Worksheet sheet in workbook.Worksheets)
                        //    {
                        //        // 设置页面缩放选项
                        //        sheet.PageSetup.FitToPagesTall = 1;  // 所有行缩放到1页
                        //        sheet.PageSetup.FitToPagesWide = 1;  // 所有列缩放到1页

                        //        // 可选：设置打印方向（根据内容自动调整方向）
                        //        //if (sheet.PageSetup.PageOrientation == Spire.Xls.PageOrientationType.Landscape)
                        //        //    sheet.PageSetup.PageOrientation = Spire.Xls.PageOrientationType.Portrait;
                        //    }

                        //    workbook.SaveToStream(ms, Spire.Xls.FileFormat.PDF);

                        //    Spire.Pdf.PdfDocument pdf = new Spire.Pdf.PdfDocument(ms);
                        //    mergedPdf.AppendPage(pdf);
                        //}
                    }
                    row++;
                }
                string newFileName = outputPath+$"\\{filename}";//filename 需要带扩展名传入
                // 保存最终PDF
                mergedPdf.SaveToFile(newFileName, Spire.Pdf.FileFormat.PDF);
                mergedPdf.Close();
                outurl = newFileName;
            }
            catch (Exception e)
            {
                outurl = null;
            }
            return outurl;
        }

    }
}