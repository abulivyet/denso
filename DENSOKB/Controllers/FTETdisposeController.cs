using DENSOKB.Helper;
using DENSOKB.Libs;
using DENSOKB.Models;
using HelpLog.CommomMethod;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Newtonsoft.Json;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using SharpCompress.Archives;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Http;
using static iTextSharp.text.pdf.AcroFields;


namespace DENSOKB.Controllers
{
    public class FTETdisposeController: ApiController
    {
        private string BJtype = "BJ";
        private string POORDTType = "POORDT";
        private string KB = "KB";//看板标识
        private string T = "T";//码垛单标识
        private string TB = "TB";//TB 开头的才有链标签
        FTETnew _FTETnew = new FTETnew();
        LiberOfficeHelper loHelper = new LiberOfficeHelper();
        orderimportService orderService = new orderimportService();
        [HttpPost]
        public ResParameter CompressedFileFTET(FtceDto ftceDto)
        {
            ResParameter rp = new ResParameter();
            List<string> tempFiles = new List<string>();
            string extractPath = string.Empty;
            string outputDirectory = string.Empty;
            List<inpdetail2> detail = new List<inpdetail2>();
            string message = "";
            string retruninfo = "";
            string ArrivalDueDate = "";
            //string plDetailPath = "";
            List<inpdetail2> PLList = new List<inpdetail2>();
            List<inpdetail2> QList = new List<inpdetail2>();
            List<inpdetail2> DList = new List<inpdetail2>();
            List<inpdetail2> LList = new List<inpdetail2>();
            List<inpdetail2> A4List = new List<inpdetail2>();
            List<inpdetail2> minList = new List<inpdetail2>();
            List<inpdetail2> SList = new List<inpdetail2>();
            detail = new List<inpdetail2>();
            try
            {
                //集货计划直接返回
                if (ftceDto.zipPath.Contains("集货")) {
                    rp.code = "200";
                    rp.info = string.IsNullOrEmpty(rp.info) ? "成功" : rp.info;
                    return rp;
                }

                // 检查 zipPath 是否有效
                if (string.IsNullOrEmpty(ftceDto.zipPath) || !File.Exists(ftceDto.zipPath))
                {
                    rp.code = "400";
                    rp.info = "未找到文件";
                    return rp;
                }
                using (var fileStream = new FileStream(ftceDto.zipPath, FileMode.Open, FileAccess.Read))
                {
                    
                    extractPath = System.IO.Path.Combine(ftceDto.basePath)+"\\FTET";
                    // 创建目录
                    Directory.CreateDirectory(extractPath);
                    //解压文件                    
                    using (var archive = SharpCompress.Archives.Zip.ZipArchive.Open(fileStream))
                    {
                        foreach (var entry in archive.Entries.Where(x => !x.IsDirectory))
                        {
                            var filePath = System.IO.Path.Combine(extractPath, entry.Key);
                            entry.WriteToFile(filePath);
                            tempFiles.Add(filePath);
                        }
                    }
                    string zipfilename = System.IO.Path.GetFileName(ftceDto.zipPath);
                    string filename = System.IO.Path.GetFileNameWithoutExtension(ftceDto.zipPath);
                    outputDirectory = ftceDto.basePath + "\\FTET\\"+ filename+"\\";// 一个文件一个文件夹存储
                    // 创建输出目录（如果不存在）
                    if (!Directory.Exists(outputDirectory))
                    {
                        Directory.CreateDirectory(outputDirectory);
                    }
                    FTETEntity ftet = new FTETEntity();
                    FTETEntity ftet_BJ = new FTETEntity(); // 补给品
                    

                    foreach (var tempFile in tempFiles)
                    {
                       string file= System.IO.Path.GetFileName(tempFile);
                        
                        try
                        {
                            if (file.IndexOf(BJtype) > -1 || file.IndexOf(POORDTType) > -1)
                            {

                                //entity.mainId = ftceDto.mainId;
                                string pathName = Path.GetFileNameWithoutExtension(file);
                                //if (file.Substring(2).IndexOf(T)<0 && file.IndexOf(KB)<0)
                                if (!pathName.EndsWith("T") && file.IndexOf(KB) < 0)
                                {
                                    // 受领书
                                    ArrivalDueDate = SpireHelper.GetText(tempFile, new System.Drawing.Rectangle(655, 115, 140, 20), 0);   //到货纳期 
                                    string allpath =  BJ_FromPdf(tempFile, outputDirectory, out ftet_BJ);
                                    string[] patharr = allpath.Split(';');
                                    foreach (var item in patharr)
                                    {
                                        if (string.IsNullOrEmpty(item)) continue;
                                        inpdetail2 entity = new inpdetail2();
                                        entity.mainId = ftceDto.mainId;
                                        entity.filetype = "受领书";
                                        entity.remarks += message;
                                        entity.Stacklabel = item;
                                        entity.ordercode = ftet_BJ.FZcode;
                                        entity.rowcode = 99;
                                        SList.Add(entity);
                                    }
                                    rp.ordercode = ftet_BJ.FZcode;
                                    rp.bin = "";
                                    rp.fdate = ftet_BJ.outdate;
                                    rp.ddate = ftet_BJ.arriveDate;
                                }

                                #region   新增0订单判断 对于数量都为0的订单 下载套票只需要包含受领书
                                bool allZero = ftet_BJ.FTETdetailList.All(item => item.number == 0);
                                //allZero = true;
                                if (allZero)
                                {
                                    int index = 1;
                                    foreach (var item in SList)
                                    {
                                        index++;
                                        item.rowcode = index;
                                        detail.Add(item);
                                    }
                                    if (zipfilename.IndexOf(TB) > -1)
                                    {
                                        orderService.InsertDetail2_rowcode(detail);
                                    }
                                    else
                                    {
                                        orderService.InsertDetail2(detail);
                                    }
                                    rp.code = "200";
                                    rp.info = string.IsNullOrEmpty(rp.info) ? "成功" : rp.info;
                                    //if (!string.IsNullOrEmpty(retruninfo))
                                    //{
                                        //rp.code = "400";
                                        rp.ordercode = ftet.FZcode;
                                        rp.info = retruninfo;
                                        rp.fdate = ftet.outdate;
                                        rp.ddate = ftet.arriveDate;
                                    //}
                                    return rp;
                                }
                                #endregion

                                //entity.orderCode = ftet_BJ.FZcode;
                                // 有数据后再处理码垛单 看板等
                                if (ftet_BJ.FTETdetailList.Count>0)
                                {
                                    // 补给码垛单
                                    if(Path.GetFileNameWithoutExtension(file).EndsWith("T"))
                                    //if (file.Substring(2).IndexOf(T) > -1)
                                    {
                                        string allpath = BJ_ExtractFromPdfT(tempFile, outputDirectory, ftet_BJ);
                                        string[] mdarr = allpath.Split(';');
                                        //for (int i = 0; i < 2; i++) // 2份
                                        //{
                                            foreach (var item in mdarr)
                                            {
                                                if (string.IsNullOrEmpty(item)) continue;
                                                inpdetail2 entity = new inpdetail2();
                                                entity.mainId = ftceDto.mainId;
                                                entity.filetype = "码垛单";
                                                entity.remarks += message;
                                                entity.Stacklabel = item;
                                                entity.ordercode = ftet_BJ.FZcode;
                                                entity.rowcode = 2;
                                                DList.Add(entity);
                                            }
                                        //}
                                    }
                                    else if (file.IndexOf(KB) > -1)
                                    {
                                        // A4 和小纸张
                                        string pahtA4 = outputDirectory + filename + "_A4.pdf";
                                        string pahtMin = outputDirectory + filename + "_小纸张.pdf";
                                        //bool isok= _FTETnew.ProcessPdf_BJ(tempFile, pahtA4, pahtMin, ftet_BJ);
                                        string outA4 = "";
                                        string outmin = "";
                                        A4andMin(tempFile, outputDirectory, pahtA4, pahtMin, out outA4,out outmin, ftet_BJ);
                                        //entity.Board = pahtMin;
                                        string[] A4arr = outA4.Split(';');
                                        foreach (var item in A4arr) {
                                            if (string.IsNullOrEmpty(item)) continue;
                                            inpdetail2 entity = new inpdetail2();
                                            entity.mainId = ftceDto.mainId;
                                            entity.filetype = "A4";
                                            entity.remarks += message;
                                            entity.Stacklabel = item;
                                            entity.ordercode = ftet_BJ.FZcode;
                                            entity.rowcode = 4;
                                            A4List.Add(entity);
                                        }
                                        string[] mainarr = outmin.Split(';');
                                        // entity.A4Board = pahtA4;
                                        foreach (var item in mainarr)
                                        {
                                            if (string.IsNullOrEmpty(item)) continue;
                                            inpdetail2 entitya4 = new inpdetail2();
                                            entitya4.mainId = ftceDto.mainId;
                                            entitya4.filetype = "小纸张";
                                            entitya4.remarks += message;
                                            entitya4.Stacklabel = item;
                                            entitya4.ordercode = ftet_BJ.FZcode;
                                            entitya4.rowcode = 5;
                                            minList.Add(entitya4);
                                        }
                                        
                                        //// ------------------------ 确认表处理      补给品确认表
                                        string qrpaht = REBExcelToPdf(outputDirectory, ftet_BJ, filename, ftceDto,ArrivalDueDate);
                                        string[] ararr = qrpaht.Split(';');
                                        foreach (var item in ararr)
                                        {
                                            if (string.IsNullOrEmpty(item)) continue;
                                            inpdetail2 entityqeb = new inpdetail2();
                                            entityqeb.mainId = ftceDto.mainId;
                                            entityqeb.filetype = "确认表";
                                            entityqeb.remarks += message;
                                            entityqeb.Stacklabel = item;
                                            entityqeb.ordercode = ftet_BJ.FZcode;
                                            entityqeb.rowcode = 1;
                                            QList.Add(entityqeb);
                                        }
                                    }
                                   

                                }
                                //detail.Add(entity);
                            }
                            else
                            {
                                if (Path.GetFileNameWithoutExtension(file).EndsWith("T"))
                                {

                                    // 码垛单
                                    string allpath = ExtractFromPdfT(tempFile, outputDirectory, ftet,out message);
                                    string[] patharr = allpath.Split(';');
                                    //for (int i = 0; i < 2; i++) // 2份
                                    //{
                                        foreach (var item in patharr)
                                        {
                                            if (string.IsNullOrEmpty(item)) continue;
                                            inpdetail2 entity = new inpdetail2();
                                            entity.mainId = ftceDto.mainId;
                                            entity.filetype = "码垛单";
                                            entity.remarks += message;
                                            entity.Stacklabel = item;
                                            entity.ordercode = ftet.FZcode;
                                            entity.rowcode = 2;
                                            DList.Add(entity);
                                        }
                                    //}
                                    if (string.IsNullOrEmpty(allpath))
                                    {
                                        retruninfo = message;
                                    }
                                }
                                else if (file.IndexOf(KB) > -1)
                                {
                                    // 看板
                                    int box = (int)ftet.FTETdetailList.Sum(t => t.box);
                                    string pahtA4 = outputDirectory + filename + "_A4.pdf";
                                    string pahtMin = outputDirectory + filename + "_小纸张.pdf";
                                    string outA4 = "";
                                    string outMin = "";
                                    A4andMin(tempFile, outputDirectory, pahtA4, pahtMin,out outA4,out outMin);
                                    string[] A4arr = outA4.Split(';');
                                    foreach (var item in A4arr)
                                    {
                                        if (string.IsNullOrEmpty(item)) continue;
                                        inpdetail2 entity = new inpdetail2();
                                        //entity.Board = pahtMin;
                                        entity.mainId = ftceDto.mainId;
                                        entity.filetype = "A4";
                                        entity.remarks += message;
                                        entity.Stacklabel = item;
                                        entity.ordercode = ftet.FZcode;
                                        entity.rowcode = 4;
                                        A4List.Add(entity);
                                    }
                                    string[] Minarr = outMin.Split(';');
                                    foreach (var item in Minarr)
                                    {
                                        if (string.IsNullOrEmpty(item)) continue;
                                        inpdetail2 entity2 = new inpdetail2();
                                        //entity.A4Board = pahtA4;
                                        entity2.mainId = ftceDto.mainId;
                                        entity2.filetype = "小纸张";
                                        entity2.remarks += message;
                                        entity2.Stacklabel = item;
                                        entity2.ordercode = ftet.FZcode;
                                        entity2.rowcode = 5;
                                        minList.Add(entity2);
                                    }

                                }
                                else
                                {


                                    /// 受领书
                                    //FindOrderInfos(tempFile);
                                    ArrivalDueDate = SpireHelper.GetText(tempFile, new System.Drawing.Rectangle(655, 115, 140, 20), 0);   //到货纳期 
                                    string allpath = LC_FromPdf(tempFile, outputDirectory, out ftet);
                                    string[] patharr = allpath.Split(';');
                                    XErrorLogToolMDM.WriteLog("首领书开始");
                                    foreach (var item in patharr)
                                    {
                                        if (string.IsNullOrEmpty(item)) continue;
                                        inpdetail2 entity = new inpdetail2();
                                        entity.mainId = ftceDto.mainId;
                                        entity.filetype = "受领书";
                                        entity.remarks += message;
                                        entity.Stacklabel = item;
                                        entity.ordercode = ftet.FZcode;
                                        entity.rowcode = 99;
                                        SList.Add(entity);
                                    }
                                    if (string.IsNullOrEmpty(allpath))
                                    {
                                        retruninfo = message;
                                    }
                                    XErrorLogToolMDM.WriteLog("FTETdetailList不为空");

                                    #region   新增0订单判断 对于数量都为0的订单 下载套票只需要包含受领书
                                    bool allZero = ftet.FTETdetailList.All(item => item.number == 0);
                                    //allZero = true;
                                    if (allZero) {
                                        int index = 1;
                                        foreach (var item in SList)
                                        {
                                            index++;
                                            item.rowcode = index;
                                            detail.Add(item);
                                        }
                                        if (zipfilename.IndexOf(TB) > -1)
                                        {
                                            orderService.InsertDetail2_rowcode(detail);
                                        }
                                        else
                                        {
                                            orderService.InsertDetail2(detail);
                                        }
                                        rp.code = "200";
                                        rp.info = string.IsNullOrEmpty(rp.info) ? "成功" : rp.info;
                                        //if (!string.IsNullOrEmpty(retruninfo))
                                        //{
                                            //rp.code = "400";
                                            rp.ordercode = ftet.FZcode;
                                            rp.info = retruninfo;
                                            rp.fdate = ftet.outdate;
                                            rp.ddate = ftet.arriveDate;
                                        //}
                                        XErrorLogToolMDM.WriteLog(rp.ordercode);
                                        XErrorLogToolMDM.WriteLog(rp.fdate);
                                        return rp;
                                    }
                                    #endregion


                                    if (ftet != null && ftet.FTETdetailList != null && ftet.FTETdetailList.Count > 0)
                                    {
                                        rp.ordercode = ftet.FZcode;
                                        rp.bin = "";
                                        rp.fdate = ftet.outdate;
                                        rp.ddate = ftet.arriveDate;
                                        //////  ---------------------链标签处理
                                        if (file.IndexOf(TB)>-1) {
                                            //string lianAll = LianExcelToFdf(outputDirectory, ftet, filename, out message);
                                            //string[] lianArr = lianAll.Split(';');

                                            //    foreach (var item in lianArr)
                                            //    {
                                            //        if (string.IsNullOrEmpty(item)) continue;
                                            //        inpdetail2 entity = new inpdetail2();
                                            //        entity.mainId = ftceDto.mainId;
                                            //        entity.filetype = "链标签";
                                            //        entity.remarks += message;
                                            //        entity.Stacklabel = item;
                                            //        entity.ordercode = ftet.FZcode;
                                            //        entity.rowcode = 3;
                                            //        LList.Add(entity);
                                            //    }
                                            //DataTable dtChain = FTETdisposeService.GetPLdetail(ftet.FZcode);

                                            //if (string.IsNullOrEmpty(lianAll))
                                            //{
                                            //    retruninfo = message;
                                            //}
                                        }
                                        //// ------------------------ 确认表处理   量产确认表
                                        XErrorLogToolMDM.WriteLog("确认表开始处理");
                                        string qrAll = REBExcelToPdf(outputDirectory, ftet, filename, ftceDto,ArrivalDueDate);
                                        string[] arArr = qrAll.Split(';');
                                        foreach (var item in arArr)
                                        {
                                            if (string.IsNullOrEmpty(item)) continue;
                                            inpdetail2 entity = new inpdetail2();
                                            entity.mainId = ftceDto.mainId;
                                            entity.filetype = "确认表";
                                            entity.remarks += message;
                                            entity.Stacklabel = item;
                                            entity.ordercode = ftet.FZcode;
                                            entity.rowcode = 1;
                                            QList.Add(entity);
                                        }
                                        if (string.IsNullOrEmpty(qrAll))
                                        {
                                            retruninfo = message;
                                        }
                                    }
                                    else
                                    {
                                        XErrorLogToolMDM.WriteLog("链标签处理失败，FTET处理现调品单未获取到明细数据");
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            //entity.remarks = e.Message;
                            rp.code = "400";
                            rp.info = e.Message;
                            return rp;
                        }
                        finally
                        {
                            // 确保处理完成后立即释放文件
                            //File.SetAttributes(tempFile, FileAttributes.Normal);
                            //File.Delete(tempFile);
                        }
                        
                    }

                    //////////---------------- 数据写入数据库
                    /// detail排序
                    /// 
                    //detail = new List<inpdetail2>();
                    foreach (var item in QList)
                    {
                        item.rowcode = 1;
                        detail.Add(item);
                    }
                    int row = 2;
                    if (filename.Contains("TB") && PLList.Count != 0) {
                        for (int i = 0; i < PLList.Count; i++)
                        {
                            PLList[i].rowcode = row;
                            detail.Add(PLList[i]);
                        }
                    }
                    for (int i=0;i< minList.Count;i++)
                    {
                        if (filename.IndexOf(TB) > 0)
                        {
                            if (DList.Count > 0)
                            {
                                DList[i].rowcode = row;
                                detail.Add(DList[i]);
                                detail.Add(DList[i]);
                            }
                            row++;
                            
                        }
                        else
                        {
                            foreach (var item2 in DList.OrderBy(t => t.Stacklabel))
                            {
                                item2.rowcode = row;
                                detail.Add(item2);
                                detail.Add(item2);
                            }
                        }


                        if (LList.Count > 0)
                        {
                            for (int j = 0; j < LList.Count; j++)
                            {
                                LList[j].rowcode = row + 1;
                                detail.Add(LList[j]);
                                detail.Add(LList[j]);
                            }
                            //LList[i].rowcode = row + 1;
                            //detail.Add(LList[i]);
                            //detail.Add(LList[i]);
                        }
                        //foreach (var item2 in LList.OrderBy(t => t.Stacklabel))
                        //{
                        //    item2.rowcode = row + 1;
                        //    detail.Add(item2);
                        //}
                        if (minList.Count > 0)
                        {
                            minList[i].rowcode = row + 2;
                            detail.Add(minList[i]);
                        }
                        if (A4List.Count > 0)
                        {
                            A4List[i].rowcode = row + 3;
                            detail.Add(A4List[i]);
                        }
                       
                        row=row+4;
                    }

                    foreach (var item in SList)
                    {
                        row++;
                        item.rowcode = row;
                        detail.Add(item);
                    }


                    if (zipfilename.IndexOf(TB) > -1)
                    {
                        orderService.InsertDetail2_rowcode(detail);
                    }
                    else
                    {
                        orderService.InsertDetail2(detail);
                    }

                    rp.code = "200";
                    rp.info = string.IsNullOrEmpty(rp.info)?"成功": rp.info;
                    if (!string.IsNullOrEmpty(retruninfo))
                    {
                        rp.code = "400";
                        rp.info = retruninfo;
                    }
                    return rp;
                }
            }
            catch (Exception ex)
            {

                rp.code = "400";
                rp.info = ex.Message;
                return rp;
            }

        }



        public void FindOrderInfos(string pdfPath)
        {
            try
            {
                List<OrderInfo>    infos      = new List<OrderInfo>();
                string shipmentDate    = SpireHelper.GetText(pdfPath,new System.Drawing.Rectangle(515,115,140,20),0);
                string ArrivalDueDate  = SpireHelper.GetText(pdfPath,new System.Drawing.Rectangle(655,115,140,20),0);
                string orderCode       = SpireHelper.GetText(pdfPath,new System.Drawing.Rectangle(654, 3, 140, 20),0);
                string InclusionNumber = SpireHelper.GetText(pdfPath,new System.Drawing.Rectangle(654, 30, 140, 20),0);
                // string InclusionNumber      = SpireHelper.GetText(pdfPath,new System.Drawing.Rectangle(654, 30, 140, 20),0);
                string detail = SpireHelper.GetText(pdfPath,new System.Drawing.Rectangle(23, 150, 750, 500),0);
                
                // 创建 DataTable
                DataTable dataTable = new DataTable("PartsTable");
                // 定义列
                dataTable.Columns.Add("序号", typeof(int));
                dataTable.Columns.Add("品番", typeof(string));
                dataTable.Columns.Add("背番", typeof(string));
                dataTable.Columns.Add("品名", typeof(string));
                dataTable.Columns.Add("收容数", typeof(int));
                dataTable.Columns.Add("订货箱", typeof(int));
                dataTable.Columns.Add("订货个", typeof(int));
                dataTable.Columns.Add("受入区代码", typeof(string));
                dataTable.Columns.Add("交货地点", typeof(string));
                
                // 按行分割字符串
                var lines = detail.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

                // 只处理零件数据行（直到遇到“合计”）
                foreach (var line in lines)
                {
                    // 跳过空行或“合计”及其后面的行
                    if (string.IsNullOrWhiteSpace(line) || line.Trim().StartsWith("合计"))
                        break;

                    // 按空格分割
                    var parts = line.Trim().Split(' ');
                    if (parts.Length < 8) continue; // 确保行有足够的数据

                    // 提取字段
                    int index;
                    if (!int.TryParse(parts[0], out index)) continue; // 确保序号是数字
                    string partNumber = parts[1];
                    string code       = parts[2];
                    // 描述字段：从第4个字段到倒数第6个字段
                    string description = string.Join(" ", parts.Skip(3).Take(parts.Length - 8));
                    int    quantity1;
                    if (!int.TryParse(parts[parts.Length - 5], out quantity1)) continue; // 数量1
                    int quantity2;
                    if (!int.TryParse(parts[parts.Length - 4], out quantity2)) continue; // 数量2
                    int total;
                    if (!int.TryParse(parts[parts.Length - 3], out total)) continue; // 总数
                    string date     = parts[parts.Length - 2];
                    string location = parts[parts.Length - 1];

                    // 添加到 DataTable
                    dataTable.Rows.Add(index, partNumber, code, description, quantity1, quantity2, total, date, location);
                }
                
                foreach (DataRow item in dataTable.Rows)
                {
                    OrderInfo info = new OrderInfo();
                    info.ShipmentDate       = shipmentDate;
                    info.ArrivalDueDate     = ArrivalDueDate;
                    info.OrderCode          = orderCode;
                    info.InclusionNumber    = InclusionNumber;
                    info.CustomerPartNumber = item["品番"].ToString();
                    info.DnPartNumber = PulicHelper.GetDnPartNumber(item["品番"].ToString()); //item["背番"].ToString();
                    info.BackNumber = item["背番"].ToString();
                    info.Capacity = Convert.ToInt32(item["收容数"]);
                    info.BoardCount = Convert.ToInt32(item["订货箱"]);
                    info.InclusionQuantity = Convert.ToInt32(item["订货个"]);
                    info.Category = "FTET";
                    infos.Add(info);

                }
                
                PulicHelper.SaveOrderInfos(infos);
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
                
            }
        }


        public void A4andMin(string tempFile,string outputDirectory, string A4path, string minPath, out string A4outPath, out string minOutPaht)
        {
            A4outPath = "";
            minOutPaht = "";
            #region  测试楠哥方法 
            //  拆分  
            string outPath = System.IO.Path.Combine(outputDirectory, "MainList");//outputDirectory + "\\MainList";
            DirectoryInfo outdir = new DirectoryInfo(outPath);

            if (outdir.Exists)
            {
                // 删除所有文件
                foreach (FileInfo file in outdir.GetFiles())
                {
                    file.Delete();
                }

                // 删除所有子文件夹及其内容
                foreach (DirectoryInfo dir in outdir.GetDirectories())
                {
                    dir.Delete(true);
                }
            }

            SpireHelper.PdfSplit(
               tempFile, // 源文件路径
               outPath, // 导出文件路径
               0,// 拆分起始页 
               3,// 垂直分几页
               1,// 水平分几页
               SpireSplit.EnumNameRule.Customize,  // 命名规则 Auto 系统自动给文件命名，Customize 下边参数需要传命名的位置 左上角是0坐标
               new System.Drawing.Rectangle[] {
                    new System.Drawing.Rectangle(410, 170, 150, 75),
                    new System.Drawing.Rectangle(130, 24, 150, 20)
               },
               // 如果命名规则 是 Auto new一个空对象就行，如果命名规则是 Customize 传桌标位置 Rectangle(130, 24, 150, 20)
               new System.Drawing.Rectangle(0, 10, 596, 777),
               false,
               true
               );  // 原始pdf有效范围，如果PDF底部有页码，页面部分就不应该算在有效范围内
                   // ---------- 合并 


            FileInfo tempFileInfo = new FileInfo(tempFile);

            if (tempFileInfo.Name.IndexOf("TB") > -1)
            {
                //string text = ExtractTextFromPdf(tempFile);
                //string orcercode = text.Substring(text.IndexOf("TB"), 12);
                //string sql = string.Format(
                //    @"select * from ftet_chain where ordercode = '{0}'", orcercode);

                //DataTable dt = DataHelper.Query(sql);
                //if (dt == null || dt.Rows.Count == 0)
                //{
                //    XErrorLogToolMDM.WriteLog(string.Format("链数据未获取到【{0}】", orcercode));
                //    return;
                //}
                /////保存背番当前下标
                //Dictionary<string, int> dict_idx = new Dictionary<string, int>();

                //for (int i = 0; i < 14; i++)//遍历1链到14链
                //{
                //    if (dt.Rows[0][$"l{i + 1}"] != DBNull.Value && dt.Rows[0][$"l{i + 1}"].ToString() != string.Empty)
                //    {
                //        List<FileInfo> fileMerge = new List<FileInfo>();

                //        for (int j = 0; j < dt.Rows.Count; j++)
                //        {
                //            string pncode = dt.Rows[j]["bei"].ToString();
                //            int count = Convert.ToInt32(dt.Rows[j][$"l{i + 1}"]);

                //            if (!dict_idx.ContainsKey(pncode))
                //                dict_idx.Add(pncode, 0);

                //            FileInfo[] fileinfos = outdir.GetFiles(pncode + "*.pdf");

                //            fileinfos = fileinfos.OrderBy(f => f.Name).ToArray();

                //            fileMerge.Add(fileinfos[dict_idx[pncode]++]);
                //        }

                //        SpireHelper.Merge(3, 1, fileMerge.ToArray(), A4path.Replace(".pdf", $"{i + 1}.pdf"), true);  // A4文件
                //        A4outPath += A4path.Replace(".pdf", $"{i + 1}.pdf") + ";";
                //        SpireHelper.MergeFiles(fileMerge.Select(f => f.FullName).ToList(), minPath.Replace(".pdf", $"{i + 1}.pdf"));
                //        minOutPaht += minPath.Replace(".pdf", $"{i + 1}.pdf") + ";";

                //    }
                //}

                List<FileInfo> files = new List<FileInfo>();
                DirectoryInfo dir = new DirectoryInfo(outPath);
                //补给品需要获取看板张数 
                FileInfo[] fileInfos = dir.GetFiles("*", SearchOption.AllDirectories).OrderBy(f => f.CreationTime).ToArray();
                foreach (FileInfo mainfile in fileInfos)
                {
                    files.Add(mainfile);
                }
                SpireHelper.SetBJboard(fileInfos);

                SpireHelper.Merge(3, 1, files.ToArray(), A4path, true);  // A4文件
                SpireHelper.Merge(1, 1, files.ToArray(), minPath, false, 20f, 0f);
                A4outPath = A4path;
                minOutPaht = minPath;
            }
            else if (tempFileInfo.Name.IndexOf("BJ") > -1)
            {
                List<FileInfo> files = new List<FileInfo>();
                DirectoryInfo dir = new DirectoryInfo(outPath);
                //补给品需要获取看板张数 
                FileInfo[] fileInfos = dir.GetFiles("*", SearchOption.AllDirectories).OrderBy(f => f.CreationTime).ToArray();
                foreach (FileInfo mainfile in fileInfos)
                {
                    files.Add(mainfile);
                }
                SpireHelper.SetBJboard(fileInfos);

                SpireHelper.Merge(3, 1, files.ToArray(), A4path, true);  // A4文件
                SpireHelper.Merge(1, 1, files.ToArray(), minPath, false, 20f, 0f);
                A4outPath = A4path;
                minOutPaht = minPath;
            }
            else
            {
                List<FileInfo> files = new List<FileInfo>();
                DirectoryInfo dir = new DirectoryInfo(outPath);
                bool found = false;

                FileInfo[] fileInfos = dir.GetFiles("*", SearchOption.AllDirectories).OrderBy(f => f.CreationTime).ToArray();
                foreach (FileInfo mainfile in fileInfos)
                {
                    files.Add(mainfile);
                }
                SpireHelper.SetBJboard(fileInfos);
                //FileInfo[] fileinfo = SpireHelper.SortA4(files.ToArray(), 1, 3);//排序

                SpireHelper.Merge(3, 1, files.ToArray(), A4path, true);  // A4文件
                //SpireHelper.MergeFiles(files.Select(f => f.FullName).ToList(), minPath);     
                SpireHelper.Merge(1, 1, files.ToArray(), minPath, false, 20f, 0f);
                A4outPath = A4path;
                minOutPaht = minPath;
            }


            #endregion
        }
        public void A4andMin(string tempFile,string outputDirectory, string A4path,string minPath,out string A4outPath,out string minOutPaht, FTETEntity ftetla)
        {
            A4outPath = "";
            minOutPaht = "";
            #region  测试楠哥方法 
            //  拆分  
            //string outPath = outputDirectory + "\\MainList";
            string outPath = System.IO.Path.Combine(outputDirectory, "MainList");//outputDirectory + "\\MainList";
            DirectoryInfo outdir = new DirectoryInfo(outPath);

            if (outdir.Exists)
            {
                // 删除所有文件
                foreach (FileInfo file in outdir.GetFiles())
                {
                    file.Delete();
                }

                // 删除所有子文件夹及其内容
                foreach (DirectoryInfo dir in outdir.GetDirectories())
                {
                    dir.Delete(true);
                }
            }
            //DirectoryInfo outdir = new DirectoryInfo(outPath);
             SpireHelper.PdfSplit(
                tempFile, // 源文件路径
                outPath, // 导出文件路径
                0,// 拆分起始页 
                3,// 垂直分几页
                1,// 水平分几页
                SpireSplit.EnumNameRule.Customize,  // 命名规则 Auto 系统自动给文件命名，Customize 下边参数需要传命名的位置 左上角是0坐标
                new System.Drawing.Rectangle[] {
                    new System.Drawing.Rectangle(410, 170, 150, 75),
                    new System.Drawing.Rectangle(130, 24, 150, 20)
                }, 
                  // 如果命名规则 是 Auto new一个空对象就行，如果命名规则是 Customize 传桌标位置 Rectangle(130, 24, 150, 20)
                new System.Drawing.Rectangle(0, 10, 596, 777),
                false,
                true
                );  // 原始pdf有效范围，如果PDF底部有页码，页面部分就不应该算在有效范围内
                                                                 // ---------- 合并 
            

            FileInfo tempFileInfo = new FileInfo(tempFile);

            if (tempFileInfo.Name.IndexOf("TB") > -1)
            {
                string text = ExtractTextFromPdf(tempFile);
                string orcercode = text.Substring(text.IndexOf("TB"), 12);
                string sql = string.Format(
                    @"select * from ftet_chain where ordercode = '{0}'", orcercode);

                DataTable dt = DataHelper.Query(sql);
                if (dt == null || dt.Rows.Count == 0)
                {
                    XErrorLogToolMDM.WriteLog(string.Format("链数据未获取到【{0}】", orcercode));
                    return;
                }
                ///保存背番当前下标
                Dictionary<string, int> dict_idx = new Dictionary<string, int>();

                for (int i = 0; i < 14; i++)//遍历1链到14链
                {
                    if (dt.Rows[0][$"l{i + 1}"] != DBNull.Value && dt.Rows[0][$"l{i + 1}"].ToString() != string.Empty)
                    {
                        List<FileInfo> fileMerge = new List<FileInfo>();

                        for (int j = 0; j < dt.Rows.Count; j++)
                        {
                            string pncode = dt.Rows[j]["bei"].ToString();
                            int count = Convert.ToInt32(dt.Rows[j][$"l{i + 1}"]);

                            if (!dict_idx.ContainsKey(pncode))
                                dict_idx.Add(pncode, 0);

                            FileInfo[] fileinfos = outdir.GetFiles(pncode + "*.pdf");

                            fileinfos = fileinfos.OrderBy(f => f.Name).ToArray();

                            fileMerge.Add(fileinfos[dict_idx[pncode]++]);
                        }

                        SpireHelper.Merge(3, 1, fileMerge.ToArray(), A4path.Replace(".pdf", $"{i + 1}.pdf"), true);  // A4文件
                        A4outPath += A4path.Replace(".pdf", $"{i + 1}.pdf") + ";";
                        SpireHelper.MergeFiles(fileMerge.Select(f => f.FullName).ToList(), minPath.Replace(".pdf", $"{i + 1}.pdf"));
                        minOutPaht += minPath.Replace(".pdf", $"{i + 1}.pdf") + ";";

                    }
                }
            }
            else if (tempFileInfo.Name.IndexOf("BJ") > -1 || tempFileInfo.Name.IndexOf("POORDT") > -1) {
                //List<FileInfo> files = new List<FileInfo>();
                DirectoryInfo dir = new DirectoryInfo(outPath);
                //补给品需要获取看板张数 
                // 获取原始文件并排序
                FileInfo[] fileInfos = dir.GetFiles("*", SearchOption.AllDirectories)
                                          .OrderBy(f => f.CreationTime)
                                          .ToArray();




                List<FileInfo> files = SpireHelper.SetBJboard(fileInfos, ftetla.FTETdetailList);

                SpireHelper.Merge(3, 1, files.ToArray(), A4path, true);  // A4文件
                SpireHelper.Merge(1, 1, files.ToArray(), minPath, false, 20f, 0f);
                A4outPath = A4path;
                minOutPaht = minPath;
            }
            else
            {
                List<FileInfo> files = new List<FileInfo>();
                DirectoryInfo dir = new DirectoryInfo(outPath);
                bool found = false;

                FileInfo[] fileInfos = dir.GetFiles("*", SearchOption.AllDirectories).OrderBy(f => f.CreationTime).ToArray();
                foreach (FileInfo mainfile in fileInfos)
                {
                    files.Add(mainfile);
                }
                SpireHelper.SetBJboard(fileInfos);
                //FileInfo[] fileinfo = SpireHelper.SortA4(files.ToArray(), 1, 3);//排序

                SpireHelper.Merge(3, 1, files.ToArray(), A4path, true);  // A4文件
                //SpireHelper.MergeFiles(files.Select(f => f.FullName).ToList(), minPath);     
                SpireHelper.Merge(1, 1, files.ToArray(), minPath, false, 20f, 0f);
                A4outPath = A4path;
                minOutPaht = minPath;
            }

            
            #endregion
        }

        // 量产处理
        public  string  LC_FromPdf(string inputPdfPath, string outputTextPath,out FTETEntity ftetla)
        {
            string outPath = "";
            try
            {
                // 检查输入文件是否存在
                if (!File.Exists(inputPdfPath))
                {
                    throw new FileNotFoundException("输入的PDF文件不存在。", inputPdfPath);
                }
                //  获取最后接个文字 判断kb(看板），T（码垛单）和订单  防止前边其他文本带这几个文字
                string fileEnd = "";
                if (inputPdfPath.Contains("POORDT"))
                {
                    fileEnd = Regex.Match(inputPdfPath, @"POORDT(.+)").Groups[1].Value;
                }
                else {
                    fileEnd = inputPdfPath.Substring((inputPdfPath.Length - 7), 5);
                }
                    
                FTETEntity ftet = new FTETEntity();
                List<FTETdetailEntity> ftetList = new List<FTETdetailEntity>();              
                    // 使用iTextSharp提取文本
                using (PdfReader reader = new PdfReader(inputPdfPath))
                {
                    for (int i = 1; i <= reader.NumberOfPages; i++)
                    {
                        //string text = PdfTextExtractor.GetTextFromPage(reader, i);
                        string text=ExtractTextFromPdf(inputPdfPath);
                        string[] textNjson = text.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                        int row = 1;
                        int detailbegin = 0;
                        int detailend = 200;
                            
                        foreach (var item in textNjson)
                        {
                            int fzcode = item.IndexOf("发注订单号");
                            if (fzcode > -1)
                            {
                                int gbcode = item.IndexOf("GB");
                                string vals = item.Trim().Replace(" ", "");

                                if (vals.Contains("小批试制"))
                                {
                                    ftet.FZcode = "小批试制";
                                }
                                else {
                                    ftet.FZcode = item.Trim().Replace(" ", "").Substring(5, 12).Trim();
                                }
                                    
                            }
                            if (item.IndexOf("K3订单号")>-1)
                            {
                                int gbcode = item.IndexOf("GB");

                                string cleaned = item.Trim().Replace(" ", "");  // "K3订单号POORDT2998028"
                                int index = cleaned.IndexOf("POORDT");
                                if (index != -1)
                                {
                                    ftet.k3code = cleaned.Substring(index);

                                }
                                else {
                                    ftet.k3code = item.Trim().Replace(" ", "").Substring(5, 14).Trim();
                                }
                                    
                            }
                            if (item.IndexOf("出货时间")>-1)
                            {
                                string datastring = textNjson[row];
                                string[] dataArr= Regex.Replace(datastring, @"\s+", " ").Trim().Split(' ');
                                if (dataArr.Length > 2) ftet.outdate = dataArr[1].ToString();
                                if (dataArr.Length > 2) ftet.arriveDate = dataArr[2].ToString();

                            }
                            // 寻找明细区域
                            int nocode = item.IndexOf("NO.");
                            if (nocode > -1)
                            {
                                detailbegin = row;
                            }
                            if (item.IndexOf("合计") > -1) detailend = row;
                            if (row> detailbegin && row < detailend)
                            {
                                // 正则表达式：匹配行首为数字 + 空格+数，
                                var pattern = @"^\d\s\d";
                                var detailvalue = Regex.Replace(item, @"\s+", " ").Trim();
                                if (Regex.IsMatch(detailvalue, pattern))
                                {
                                    string[] detaioRow = detailvalue.Replace("VVT 总成","VVT总成").Split(' ');   //Replace("VVT ","VVT").
                                    FTETdetailEntity detail = new FTETdetailEntity();
                                    detail.pin = detaioRow[1].ToString();
                                    detail.bei= detaioRow[2].ToString();
                                    try
                                    {
                                        detail.box = Convert.ToInt32(detaioRow[5].ToString());
                                        detail.number = string.IsNullOrEmpty(detaioRow[6].ToString()) ? 0 : Convert.ToInt32(detaioRow[6].ToString());
                                    }
                                    catch (Exception)
                                    {
                                        detail.box = 0;
                                    }
                                    ftetList.Add(detail);
                                }



                                //try
                                //{
                                //    DataRow[] dr = FTETDT.Select(string.Format("cust_num='{0}' and back_num='{1}'", detail.pin, detail.bei));
                                //    if (dr.Length <= 0)
                                //    {
                                //        XErrorLogToolMDM.WriteLog(string.Format("通过品番【{0}】背番【{1}】未在量表获取到数据，请先上传量表数据", detail.pin, detail.bei));
                                //        throw new InvalidOperationException(string.Format("通过品番【{0}】背番【{1}】未在量表获取到数据，请先上传量表数据", detail.pin, detail.bei));
                                //    }
                                //    else
                                //    {
                                //        //detail.box = Convert.ToInt32(detaioRow[5].ToString());
                                //        detail.shou = string.IsNullOrEmpty(dr[0]["box_cap"].ToString()) ? 0 : Convert.ToInt32(dr[0]["box_cap"].ToString());
                                //        int inBox = 0;
                                //        if (((int)detail.shou) > 0)
                                //        {
                                //            inBox = (int)Math.Ceiling((decimal)detail.number / (decimal)detail.shou);

                                //        }
                                //        detail.box = inBox;
                                //        detail.boxTou = string.IsNullOrEmpty(dr[0]["pallet_pct"].ToString()) ? 0 : Convert.ToDecimal(dr[0]["pallet_pct"].ToString());
                                //    }

                                //}
                                }
                            row++;
                        }
                            
                    }

                }
                ftet.FTETdetailList = ftetList;
                ftetla = ftet;
                // 没有发生错误调用另存 输出PDF
               // string newPdfPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(inputPdfPath),
                string file= System.IO.Path.GetFileNameWithoutExtension(inputPdfPath);
                
                string path = file+"_受领书";
                for (var i=0;i<2;i++) { // 需求要三个
                    string newpath = outputTextPath + path + "_" + (i + 1) + ".pdf";
                    //using (FileStream fs = new FileStream(newpath, FileMode.Create))
                    //{
                    //    //PdfStamper stamper = new PdfStamper(new PdfReader(inputPdfPath), fs);
                    //    //stamper.Close();
                    //    using (PdfReader tempReader = new PdfReader(inputPdfPath))
                    //    using (PdfStamper stamper = new PdfStamper(tempReader, fs))
                    //    {
                    //        // 在此添加 PDF 编辑操作（如添加文本、表单）
                    //        // 示例：添加空白页（仅作占位）
                    //        //stamper.InsertPage(1, PageSize.A4);
                    //    }
                    //}
                    SpireHelper.ResizeToA4Landscape(inputPdfPath, newpath);

                    Console.WriteLine($"PDF文本已成功提取并保存到: {inputPdfPath}");
                    XErrorLogToolMDM.WriteLog($"PDF文本已成功提取并保存到: {inputPdfPath}");
                    outPath += newpath + ";";
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理PDF时发生错误: {ex.Message}");
                XErrorLogToolMDM.WriteLog($"处理PDF时发生错误: {ex.Message}");
                throw; // 可以选择重新抛出异常或处理它
            }
            return outPath;
        }
        //  旧版本 
        //public static string ExtractTextFromPdf(string filePath)
        //{
        //    try
        //    {
        //        using (Spire.Pdf.PdfDocument pdf = new Spire.Pdf.PdfDocument())
        //        {
        //            pdf.LoadFromFile(filePath);
        //            StringBuilder sb = new StringBuilder();
        //            for (int i = 0; i < pdf.Pages.Count; i++)
        //            {
        //                string pageText = pdf.Pages[i].ExtractText();
        //                //pageText = Regex.Replace(pageText, @"\s+", " "); // 清理多余空格
        //                sb.AppendLine(pdf.Pages[i].ExtractText()); // 分页追加换行符
        //            }
        //            return sb.ToString();
        //        }
        //    }
        //    catch (FileNotFoundException ex)
        //    {
        //        return $"错误: 文件未找到 - {ex.Message}";
        //    }
        //    catch (Exception ex)
        //    {
        //        return $"错误: {ex.GetType().Name} - {ex.Message}";
        //    }
        //}
        public static string ExtractTextFromPdf(string filePath)
        {
            try
            {
                using (Spire.Pdf.PdfDocument doc = new Spire.Pdf.PdfDocument())
                {
                    doc.LoadFromFile(filePath);
                    StringBuilder result = new StringBuilder();

                    // 创建通用提取参数
                    Spire.Pdf.Texts.PdfTextExtractOptions options = new Spire.Pdf.Texts.PdfTextExtractOptions
                    {
                        // IsKeepTextSameLine = true,
                        //Encoding = Encoding.UTF8
                    };

                    foreach (Spire.Pdf.PdfPageBase page in doc.Pages)
                    {
                        // 新版调用方式
                        Spire.Pdf.Texts.PdfTextExtractor extractor = new Spire.Pdf.Texts.PdfTextExtractor(page);
                        string pageText = extractor.ExtractText(options);

                        result.AppendLine(pageText);
                    }
                    return result.ToString().Replace("Evaluation Warning : The document was created with Spire.PDF for .NET. ","");
                }
            }
            catch (FileNotFoundException ex)
            {
                return $"错误: 文件未找到 - {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"错误: {ex.GetType().Name} - {ex.Message}";
            }
        }

        // 补给品梳理

        public string  BJ_FromPdf(string inputPdfPath, string outputTextPath, out FTETEntity ftetla)
        {
            string paht = "";
            try
            {

                // 检查输入文件是否存在
                if (!File.Exists(inputPdfPath))
                {
                    throw new FileNotFoundException("输入的PDF文件不存在。", inputPdfPath);
                }
                DataTable FTETDT = new DataTable();
                
                //  获取最后接个文字 判断kb(看板），T（码垛单）和订单  防止前边其他文本带这几个文字
                string fileEnd = inputPdfPath.Substring((inputPdfPath.Length - 7), 5);
                FTETEntity ftet = new FTETEntity();
                List<FTETdetailEntity> ftetList = new List<FTETdetailEntity>();
                using (iText.Kernel.Pdf.PdfReader reader = new iText.Kernel.Pdf.PdfReader(inputPdfPath))
                {
                    iText.Kernel.Pdf.PdfDocument pdfDoc = new iText.Kernel.Pdf.PdfDocument(reader);
                    for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
                    {
                        // 获取页面尺寸
                        var page = pdfDoc.GetPage(i);
                        var pageSize = page.GetPageSize();

                        // 设置文本区域过滤器（全页面） 获取订单号啥的
                        var filterAll = new iText.Kernel.Pdf.Canvas.Parser.Filter.TextRegionEventFilter(
                        new iText.Kernel.Geom.Rectangle(
                            0, // 左下角 X 坐标（Left Lower X）
                            0,  // 左下角 Y 坐标（Left Lower Y）
                            pageSize.GetWidth(), // 右上角 X 坐标（Upper Right X）
                            pageSize.GetHeight()) // 右上角 Y 坐标（Upper Right Y）
                            ); ;
                        // 创建NO列接收器
                        var strategyAll= new iText.Kernel.Pdf.Canvas.Parser.Listener.FilteredTextEventListener(
                            new iText.Kernel.Pdf.Canvas.Parser.Listener.LocationTextExtractionStrategy(),
                            filterAll);
                        string textAll = iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor.GetTextFromPage(page, strategyAll);
                        string[] textNjsonAll = textAll.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                        for (int allrow = 0; allrow < textNjsonAll.Length; allrow++)
                        {
                            int fzcode = textNjsonAll[allrow].IndexOf("发注订单号");
                            if (fzcode > -1)
                            {
                                if (textNjsonAll[allrow].Contains("小批试制"))
                                {
                                    ftet.FZcode = "小批试制";
                                }
                                else {
                                    ftet.FZcode = textNjsonAll[allrow].Substring(6, 12).Trim();
                                }

                                string veh_model = "";
                                if (ftet.FZcode.Contains("小批试制"))
                                {
                                    veh_model = "POORDT";
                                }
                                else
                                {
                                    veh_model = ftet.FZcode.Substring(0, 2);
                                }
                                var queryJson = new
                                {
                                    cust_name = "FTET",
                                    veh_model = veh_model

                                };
                                FTETDT = FTETdisposeService.getoriginalunit(JsonConvert.SerializeObject(queryJson, Formatting.Indented));
                                if (FTETDT == null || FTETDT.Rows.Count == 0)
                                {
                                    XErrorLogToolMDM.WriteLog("通过FTET和" + ftet.FZcode.Substring(0, 2) + "，未找到上传的量表数据");
                                    throw new InvalidOperationException("通过FTET和" + ftet.FZcode.Substring(0, 2) + "，未找到上传的量表数据");
                                }
                            }
                            if (textNjsonAll[allrow].IndexOf("K3订单号") > -1)
                            {

                                string cleaned = textNjsonAll[allrow].Trim().Replace(" ", "");  // "K3订单号POORDT2998028"
                                int index = cleaned.IndexOf("POORDT");
                                if (index != -1)
                                {
                                    ftet.k3code = cleaned.Substring(index);

                                }
                                else
                                {
                                    ftet.k3code = textNjsonAll[allrow].Trim().Replace(" ", "").Substring(5, 14).Trim();
                                }
                                //ftet.k3code = textNjsonAll[allrow].Substring(6, 14).Trim();
                            }
                            if (textNjsonAll[allrow].IndexOf("出货时间") > -1)
                            {
                                string datastring = textNjsonAll[allrow+1];
                                string[] dataArr = datastring.Split(' ');
                                if (dataArr.Length > 2) ftet.outdate = dataArr[1].ToString();  // 出货时间
                                if(dataArr.Length==3)ftet.arriveDate = dataArr[2].ToString();//到货时间

                            }
                        }
                            // 设置文本区域过滤器 品番
                        //    var filterPin = new iText.Kernel.Pdf.Canvas.Parser.Filter.TextRegionEventFilter(
                        //new iText.Kernel.Geom.Rectangle(
                        //    80, // 左下角 X 坐标（Left Lower X）
                        //    230,  // 左下角 Y 坐标（Left Lower Y）
                        //    100, // 右上角 X 坐标（Upper Right X）
                        //    300) // 右上角 Y 坐标（Upper Right Y）
                        //    );
                        //// 创建带过滤的监听器  品番
                        //var strategyPin = new iText.Kernel.Pdf.Canvas.Parser.Listener.FilteredTextEventListener(
                        //    new iText.Kernel.Pdf.Canvas.Parser.Listener.LocationTextExtractionStrategy(),
                        //    filterPin);

                        // 提取文本（含坐标信息）
                        //string textPin = iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor.GetTextFromPage(page, strategyPin);
                        string textPin = SpireHelper.GetText(inputPdfPath,new System.Drawing.Rectangle(75,120,100,240),i-1);
                        string[] textNjsonPin = textPin.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

                        // 设置文本区域过滤器 背番+品番 下标1取背番
                        //var filterBei = new iText.Kernel.Pdf.Canvas.Parser.Filter.TextRegionEventFilter(
                        //new iText.Kernel.Geom.Rectangle(
                        //    110, // 左下角 X 坐标（Left Lower X）
                        //    230,  // 左下角 Y 坐标（Left Lower Y）
                        //    120, // 右上角 X 坐标（Upper Right X）
                        //    300) // 右上角 Y 坐标（Upper Right Y）
                        //    );
                        //// 创建带过滤的监听器  品番
                        //var strategyBei = new iText.Kernel.Pdf.Canvas.Parser.Listener.FilteredTextEventListener(
                        //    new iText.Kernel.Pdf.Canvas.Parser.Listener.LocationTextExtractionStrategy(),
                        //    filterBei);

                        // 提取文本（含坐标信息）
                        //string textBei = iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor.GetTextFromPage(page, strategyBei);
                        string textBei = SpireHelper.GetText(inputPdfPath, new System.Drawing.Rectangle(194, 137, 50, 240), i - 1);
                        string[] textNjsonBei = textBei.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);


                        // 设置文本区域过滤器 下标1是订货个
                        var filterDHG = new iText.Kernel.Pdf.Canvas.Parser.Filter.TextRegionEventFilter(
                        new iText.Kernel.Geom.Rectangle(
                            400f,  // 左下角 X 坐标（Left Lower X）
                            220f,// 左下角 Y 坐标（Left Lower Y）
                            400.1f, // 右上角 X 坐标（Upper Right X）
                            260f) // 右上角 Y 坐标（Upper Right Y）
                            );
                        // 创建带过滤的监听器  品番
                        var strategyDHG = new iText.Kernel.Pdf.Canvas.Parser.Listener.FilteredTextEventListener(
                            new iText.Kernel.Pdf.Canvas.Parser.Listener.LocationTextExtractionStrategy(),
                            filterDHG);

                        // 提取文本（含坐标信息）
                        //string textDHG = iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor.GetTextFromPage(page, strategyDHG);
                        string textDHG = SpireHelper.GetText(inputPdfPath, new System.Drawing.Rectangle(415, 161, 379, 218), i-1);
                        string[] textNjsonDHG = textDHG.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);


                        for (int row=0; row < textNjsonPin.Length-1; row++)
                        {
                            
                            FTETdetailEntity detail = new FTETdetailEntity();
                            detail.pin = textNjsonPin[row + 1].ToString().Trim();
                            detail.bei = textNjsonBei[row + 1].Split(' ')[1].ToString();
                            try
                            {
                                detail.number = Convert.ToInt32(textNjsonDHG[row].Trim().Split(' ')[1].ToString());
                            }
                            catch (Exception e)
                            {
                                detail.number = 0;
                            }
                            // 计算 
                            try
                            {
                                DataRow[] dr = FTETDT.Select(string.Format("cust_num='{0}' and back_num='{1}'", detail.pin, detail.bei));
                                //DataRow[] dr = FTETDT.Select(string.Format("cust_num='{0}' and back_num='{1}'", item.pin, item.bei));


                                if (dr.Length <= 0)
                                {
                                    XErrorLogToolMDM.WriteLog(string.Format("通过品番【{0}】背番【{1}】未在量表获取到数据，请先上传量表数据", detail.pin, detail.bei));
                                    throw new InvalidOperationException(string.Format("通过品番【{0}】背番【{1}】未在量表获取到数据，请先上传量表数据", detail.pin, detail.bei));
                                }
                                else
                                {
                                    //detail.box = Convert.ToInt32(detaioRow[5].ToString());
                                    detail.shou = string.IsNullOrEmpty(dr[0]["box_cap"].ToString()) ? 0 : Convert.ToInt32(dr[0]["box_cap"].ToString());
                                    int inBox = 0;
                                    if (((int)detail.shou) > 0)
                                    {
                                        inBox = (int)Math.Ceiling((decimal)detail.number / (decimal)detail.shou);

                                    }
                                    detail.box = inBox;
                                    detail.boxTou= string.IsNullOrEmpty(dr[0]["pallet_pct"].ToString()) ? 0 : Convert.ToDecimal(dr[0]["pallet_pct"].ToString());
                                }

                            }
                            catch (Exception)
                            {
                                detail.box = 0;
                                detail.boxTou = 0;
                            }
                            ftetList.Add(detail);
                        }
                    }
                }
 
                ftet.FTETdetailList = ftetList;
                ftetla = ftet;
                // 没有发生错误调用另存 输出PDF
                // string newPdfPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(inputPdfPath),
                string file = System.IO.Path.GetFileNameWithoutExtension(inputPdfPath);

                string path = file + "_受领书";
                for (var i = 0; i < 2; i++)
                {
                    string newoutPath = outputTextPath + path + "_" + (i + 1) + ".pdf";
                    using (var reader = new PdfReader(inputPdfPath))
                    using (var fs = new FileStream(newoutPath, FileMode.Create))
                    {
                        using (var stamper = new PdfStamper(reader, fs))
                        {
                            // 获取第一页（页码从1开始）
                            var pdfContent = stamper.GetOverContent(1);

                            // 创建字体（中文字体需指定具体字体文件路径）
                            var baseFont = BaseFont.CreateFont(
                                "C:/Windows/Fonts/simsun.ttc,1",
                                BaseFont.IDENTITY_H,
                                BaseFont.EMBEDDED
                            );
                           
                            var i_Y = 480;// 上线宽度
                            float textHeight = 12; // 字体大小
                            int textX = 393;// 左右移动
                            int textXBox = 443;// 左右移动
                            int HJBOX = 0;
                            foreach (var item in ftet.FTETdetailList)
                            {
                                pdfContent.SetRGBColorFill(255, 255, 255); // RGB范围0.0~1.0
                                // 设置文本位置（坐标系原点在左下角）
                                float textWidth = baseFont.GetWidthPoint(item.shou.ToString(), 15); // 获取文本宽度
                                float centerX = textX - ((textWidth + 2) / 2); // 居中位置
                                // 绘制矩形参数：左下角X, 左下角Y, 宽度, 高度
                                pdfContent.Rectangle(
                                    centerX - 2,         // X坐标（向左微调2点，避免文字贴边）
                                    i_Y - 2,         // Y坐标（向下微调2点）
                                    textWidth + 4,   // 宽度（左右各留2点边距）
                                    textHeight + 2    // 高度（上下各留2点边距）
                                );
                                pdfContent.Fill();    // 填充颜色
                                pdfContent.SaveState(); //  保存当前颜色、路径等状态
                                // 开始写入文本
                                pdfContent.BeginText();
                                pdfContent.SetFontAndSize(baseFont, textHeight);
                                pdfContent.SetRGBColorFill(0, 0, 0); // 黑色
                                
                                // 参数：x位置, y位置, 旋转角度（单位：点）
                                pdfContent.ShowTextAligned(
                                    Element.ALIGN_LEFT,
                                    item.shou.ToString(),
                                    centerX,  // X 坐标
                                    i_Y,  // Y 坐标（A4纸高约842点）
                                    0     // 旋转角度
                                );
                                pdfContent.EndText();
                                //////////-----------------------------------箱数列
                                pdfContent.SetRGBColorFill(255, 255, 255); // RGB范围0.0~1.0
                                float textWidthBox = baseFont.GetWidthPoint(item.box.ToString(), 15); // 获取文本宽度
                                float centerXBox = textXBox - ((textWidthBox + 2) / 2); // 居中位置
                                pdfContent.Rectangle(
                                    centerXBox - 2,         // X坐标（向左微调2点，避免文字贴边）
                                    i_Y - 2,         // Y坐标（向下微调2点）
                                    textWidthBox + 4,   // 宽度（左右各留2点边距）
                                    textHeight + 2    // 高度（上下各留2点边距）
                                );
                                pdfContent.Fill();    // 填充颜色
                                pdfContent.SaveState(); //  保存当前颜色、路径等状态
                                pdfContent.BeginText();
                                pdfContent.SetFontAndSize(baseFont, textHeight);
                                pdfContent.SetRGBColorFill(0, 0, 0); // 黑色

                                // 参数：x位置, y位置, 旋转角度（单位：点）
                                pdfContent.ShowTextAligned(
                                    Element.ALIGN_LEFT,
                                    item.box.ToString(),
                                    centerXBox,  // X 坐标
                                    i_Y,  // Y 坐标（A4纸高约842点）
                                    0     // 旋转角度
                                );
                                pdfContent.EndText();
                                pdfContent.RestoreState(); //  恢复之前的颜色、路径状态

                                // 最后一个合计行怎么办

                                i_Y = i_Y-20;
                                HJBOX += (int)item.box;
                            }
                            ////--------------------- 合计箱数
                            pdfContent.SetRGBColorFill(255, 255, 255); // RGB范围0.0~1.0
                            float textWidthHJ = baseFont.GetWidthPoint(HJBOX.ToString(), 15); // 获取文本宽度
                            float centerXHJ = textXBox - ((textWidthHJ + 2) / 2); // 居中位置


                            pdfContent.Rectangle(
                                centerXHJ - 2,         // X坐标（向左微调2点，避免文字贴边）
                                258,         // Y坐标（向下微调2点）
                                textWidthHJ + 4,   // 宽度（左右各留2点边距）
                                textHeight + 2    // 高度（上下各留2点边距）
                            );
                            pdfContent.Fill();    // 填充颜色
                            pdfContent.SaveState(); //  保存当前颜色、路径等状态
                            pdfContent.BeginText();
                            pdfContent.SetFontAndSize(baseFont, textHeight);
                            pdfContent.SetRGBColorFill(0, 0, 0); // 黑色


                            // 参数：x位置, y位置, 旋转角度（单位：点）
                            pdfContent.ShowTextAligned(
                                Element.ALIGN_LEFT,
                                HJBOX.ToString(),
                                centerXHJ,  // X 坐标
                                260,  // Y 坐标（A4纸高约842点）
                                0     // 旋转角度
                            );
                            pdfContent.EndText();
                            pdfContent.RestoreState(); //  恢复之前的颜色、路径状态


                        }
                    }

                    string newoutPathA4 = System.IO.Path.Combine(outputTextPath, path + "_" + Guid.NewGuid().ToString("N") + ".pdf");
                    FileInfo fileInfo = new FileInfo(newoutPath);
                    FileInfo[] files = new FileInfo[1];
                    files[0] = fileInfo;
                    SpireHelper.Merge(1, 1, files, newoutPathA4,true,0,0,true);

                    Console.WriteLine($"PDF文本已成功提取并保存到: {newoutPathA4}");
                    XErrorLogToolMDM.WriteLog($"PDF文本已成功提取并保存到: {newoutPathA4}");




                    paht += newoutPathA4 + ";";
                }
                return paht;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理PDF时发生错误: {ex.Message}");
                throw; // 可以选择重新抛出异常或处理它
            }
        }
        // 码垛单
        public static string ExtractFromPdfT(string inputPdfPath, string outputTextPath, FTETEntity ftet,out string message)
        {
            string outPath = "";
            message = "";
            try
            {
                if (ftet==null|| ftet.FTETdetailList.Count==0)
                {
                    string mes = "FTET处理码垛未获取到订单数据，请查看排列顺序";
                    XErrorLogToolMDM.WriteLog(mes);
                    message = mes;
                    throw new InvalidOperationException(mes);
                }
                // ----------- 通过ftet去库里查链信息 通过品番，背番，车型查pallet_pct字段，箱数* pallet_pct 汇总，向上取整计算得出托盘数 ------做上传链信息
                string veh_model = "";
                if (ftet.FZcode.Contains("小批试制"))
                {
                    veh_model = "POORDT";
                }
                else
                {
                    veh_model = ftet.FZcode.Substring(0, 2);
                }
                var queryJson = new
                {
                    cust_name = "FTET",
                    veh_model = veh_model

                };
                DataTable FTETDT= FTETdisposeService.getoriginalunit(JsonConvert.SerializeObject(queryJson, Formatting.Indented));
                if (FTETDT==null|| FTETDT.Rows.Count==0)
                {
                    string mes = "通过FTET和" + ftet.FZcode.Substring(0, 2) + "，未找到上传的量表数据";
                    XErrorLogToolMDM.WriteLog(mes);
                    message = mes;
                    throw new InvalidOperationException(mes);
                }
                decimal SUMpallet = 0;
                foreach(var item in ftet.FTETdetailList)
                {
                   DataRow[] dr= FTETDT.Select(string.Format("cust_num='{0}' and back_num='{1}'",item.pin,item.bei));
                    if (dr.Length>0)
                    {
                        decimal pallet_pct = 0;
                        decimal pallet = 0;
                        //if (dr[0]["UseFlag"].ToString() == "混托")
                        //{
                        //    pallet_pct = dr[0].IsNull("pallet_pct") ? 0 : Convert.ToDecimal(dr[0]["pallet_pct"]);
                        //    decimal pallet = pallet_pct * (decimal)item.box;
                        //}
                        //else 
                        if (dr[0]["UseFlag"].ToString() == "单托")
                        {
                            pallet_pct = dr[0].IsNull("pallet_pct") ? 0 : Convert.ToDecimal(dr[0]["pallet_pct"]);
                            pallet = Math.Ceiling(pallet_pct * (decimal)item.box);
                        }
                        else {
                            pallet_pct = dr[0].IsNull("pallet_pct") ? 0 : Convert.ToDecimal(dr[0]["pallet_pct"]);
                            pallet = pallet_pct * (decimal)item.box;
                        }


                        SUMpallet += pallet; //pallet_pct * (decimal)item.box;
                    }

                }
                int allPate = (int)Math.Ceiling(SUMpallet);
                for (int i=0;i < allPate; i++)
                {
                    //outputTextPath 带文件名
                    int fileno = i+1;
                    string filenumber = fileno.ToString("D3");

                    string newoutPath= outputTextPath  + filenumber + "_"+System.IO.Path.GetFileName(inputPdfPath);
                    using (var reader = new PdfReader(inputPdfPath))
                    using (var fs = new FileStream(newoutPath, FileMode.Create))
                    {
                        using (var stamper = new PdfStamper(reader, fs))
                        {
                            // 获取第一页（页码从1开始）
                            var pdfContent = stamper.GetOverContent(1);

                            // 创建字体（中文字体需指定具体字体文件路径）
                            var baseFont = BaseFont.CreateFont(
                                BaseFont.HELVETICA_BOLD,
                                BaseFont.CP1252,
                                BaseFont.NOT_EMBEDDED
                            );
                            // 开始写入文本
                            pdfContent.BeginText();
                            pdfContent.SetFontAndSize(baseFont, 30);
                            pdfContent.SetRGBColorFill(0, 0, 0); // 黑色
                            // 设置文本位置（坐标系原点在左下角）

                            // 参数：x位置, y位置, 旋转角度（单位：点）
                            pdfContent.ShowTextAligned(
                                Element.ALIGN_LEFT,
                                allPate.ToString(),
                                460,  // X 坐标
                                50,  // Y 坐标（A4纸高约842点）
                                0     // 旋转角度
                            );
                            pdfContent.EndText();
                            // ----------- 第二个文本
                            pdfContent.BeginText();
                            pdfContent.SetFontAndSize(baseFont, 30);
                            pdfContent.SetRGBColorFill(0, 0, 0); // 黑色
                            pdfContent.ShowTextAligned(
                                Element.ALIGN_LEFT,
                                (i+1).ToString(),
                                265,  // X 坐标
                                50,  // Y 坐标（A4纸高约842点）
                                0     // 旋转角度
                            );
                            pdfContent.EndText();
                        }
                    }
                    outPath += newoutPath + ";";// 垛单可能有10几个20几个，路径放不下 只存一个路径 其他的下载是批量查询
                }
                
            }
            catch (Exception e)
            {
                string mes = "FTET处理码垛单错误" + e.Message;
                XErrorLogToolMDM.WriteLog(mes);
                message = mes;
                throw new InvalidOperationException(mes); 

            }
            return outPath;
        }

        /// <summary>
        /// 补给品托标签 码垛单
        /// </summary>
        /// <param name="inputPdfPath"></param>
        /// <param name="outputTextPath"></param>
        /// <param name="ftet"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static string BJ_ExtractFromPdfT(string inputPdfPath, string outputTextPath, FTETEntity ftet)
        {
            string outPath = "";
            try
            {
                decimal SUMpallet = 0;
                foreach (var item in ftet.FTETdetailList)
                {
                    SUMpallet += (decimal)item.boxTou * (decimal)item.box;
                }
                int allPate = (int)Math.Ceiling(SUMpallet);  // 计算托数
                for (int i = 0; i < allPate; i++)
                {
                    string newoutPath = outputTextPath + "\\" + (i + 1) + "_" + System.IO.Path.GetFileName(inputPdfPath);
                    using (var reader = new PdfReader(inputPdfPath))
                    using (var fs = new FileStream(newoutPath, FileMode.Create))
                    {
                        using (var stamper = new PdfStamper(reader, fs))
                        {
                            // 获取第一页（页码从1开始）
                            var pdfContent = stamper.GetOverContent(1);

                            // 创建字体（中文字体需指定具体字体文件路径）
                            var baseFont = BaseFont.CreateFont(
                                 "C:/Windows/Fonts/simsun.ttc,1",
                                BaseFont.IDENTITY_H,
                                BaseFont.EMBEDDED
                            );
                            ////////////////// 规定到货时间计算出星期几 填在工场纳入星期--便次
                            //string[] weekDays = { "日", "一", "二", "三", "四", "五", "六" };
                            //int weekNumber = (int) Convert.ToDateTime(ftet.arriveDate).DayOfWeek;
                            //string NRday=weekDays[weekNumber]+ "    1";// 占位符 “　　”
                            //pdfContent.BeginText();
                            //pdfContent.SetFontAndSize(baseFont, 60);
                            //pdfContent.SetRGBColorFill(0, 0, 0); // 黑色
                            //pdfContent.ShowTextAligned(
                            //    Element.ALIGN_LEFT,
                            //    NRday,
                            //    335,  // X 坐标
                            //    522,  // Y 坐标（A4纸高约842点）
                            //    0     // 旋转角度
                            //);
                            //pdfContent.EndText();
                            // 供应商出发时间写规定出货时间的年月  日 时间 17:54
                            //string outYearMonth=ftet.outdate.Substring(0, 7); // 红色
                            //string outDay= ftet.outdate.Substring(8, 2); // 红色大号字体
                            //string outTime = "17:54";//  黑色
                            //pdfContent.BeginText();
                            //pdfContent.SetFontAndSize(baseFont, 18);
                            //pdfContent.SetRGBColorFill(255, 0, 0); // 黑色
                            //pdfContent.ShowTextAligned(
                            //    Element.ALIGN_LEFT,
                            //    outYearMonth,
                            //    320,  // X 坐标
                            //    370,  // Y 坐标（A4纸高约842点）
                            //    0     // 旋转角度
                            //);
                            //pdfContent.EndText();
                            //pdfContent.BeginText();
                            //pdfContent.SetFontAndSize(baseFont, 48);
                            //pdfContent.SetRGBColorFill(255, 0, 0); // 红色日期 大号
                            //pdfContent.ShowTextAligned(
                            //    Element.ALIGN_LEFT,
                            //    outDay,
                            //    408,  // X 坐标
                            //    335,  // Y 坐标（A4纸高约842点）
                            //    0     // 旋转角度
                            //);
                            //pdfContent.EndText();
                            //pdfContent.SaveState(); //  保存当前颜色、路径等状态

                            ////  --------背景色
                            //pdfContent.SetRGBColorFill(255, 255, 255); // RGB范围0.0~1.0
                            //float textWidthBox = baseFont.GetWidthPoint(outTime.ToString(), 20); // 获取文本宽度
                            //float centerXBox = 360 - ((textWidthBox + 2) / 2); // 居中位置
                            //int i_Y = 337;
                            //int textCode = 18;  // 字号大小
                            //pdfContent.Rectangle(
                            //    centerXBox - 2,         // X坐标（向左微调2点，避免文字贴边）
                            //    i_Y - 2,         // Y坐标（向下微调2点）
                            //    textWidthBox + 4,   // 宽度（左右各留2点边距）
                            //    textCode + 2    // 高度（上下各留2点边距）
                            //);
                            //pdfContent.Fill();    // 填充颜色
                            //pdfContent.SaveState(); //  保存当前颜色、路径等状态

                            //pdfContent.BeginText();
                            //pdfContent.SetFontAndSize(baseFont, textCode);
                            //pdfContent.SetRGBColorFill(0, 0, 0); // 黑色
                            //pdfContent.ShowTextAligned(
                            //    Element.ALIGN_LEFT,
                            //    outTime,
                            //    centerXBox,  // X 坐标
                            //    i_Y,  // Y 坐标（A4纸高约842点）
                            //    0     // 旋转角度
                            //);
                            //pdfContent.EndText();
                            // 塘沽CD出发时间 是规定到货时间 ；

                            //FTET#2到达时间也是规定到货时间

                            // 开始写入文本  -----------写入页码
                            pdfContent.BeginText();
                            pdfContent.SetFontAndSize(baseFont, 30);
                            pdfContent.SetRGBColorFill(0, 0, 0); // 黑色
                            // 设置文本位置（坐标系原点在左下角）

                            // 参数：x位置, y位置, 旋转角度（单位：点）
                            pdfContent.ShowTextAligned(
                                Element.ALIGN_LEFT,
                                allPate.ToString(),
                                460,  // X 坐标
                                50,  // Y 坐标（A4纸高约842点）
                                0     // 旋转角度
                            );
                            pdfContent.EndText();
                            // ----------- 第二个文本
                            pdfContent.BeginText();
                            pdfContent.SetFontAndSize(baseFont, 30);
                            pdfContent.SetRGBColorFill(0, 0, 0); // 黑色
                            pdfContent.ShowTextAligned(
                                Element.ALIGN_LEFT,
                                (i + 1).ToString(),
                                265,  // X 坐标
                                50,  // Y 坐标（A4纸高约842点）
                                0     // 旋转角度
                            );
                            pdfContent.EndText();
                        }
                    }
                    outPath +=newoutPath+ ";";
                }
            }
            catch (Exception e)
            {
                XErrorLogToolMDM.WriteLog("FTET补给_处理码垛单错误" + e.Message);
                throw new InvalidOperationException("FTET补给_处理码垛单错误" + e.Message); ;
            }
            return outPath;
        }
            /// <summary>
            /// 直链模板
            /// </summary>
            /// <param name="ftet"></param>
        public string LianExcelToFdf(string outputTextPath,  FTETEntity ftet,string filename,out string message)
        {
            
            string projectPath = AppDomain.CurrentDomain.BaseDirectory;
            string TFTELian = ConfigurationManager.AppSettings["TFTELian"].ToString();// 模板地址
            List<string> excepahList = new List<string>();
            outputTextPath = outputTextPath + "直链\\";
            message = "";
            string outPath = "";
            Directory.CreateDirectory(outputTextPath);
            try
            {
                // int 
                var queryJson = new
                {
                    ordercode = ftet.FZcode

                };
                DataTable FTETLian = chainService.getftet_chainSum(JsonConvert.SerializeObject(queryJson, Formatting.Indented));
                if (FTETLian == null || FTETLian.Rows.Count == 0)
                {
                    XErrorLogToolMDM.WriteLog(string.Format("FTET直链模板单号【{0}】链数据未上传", ftet.FZcode));
                    message += string.Format("FTET直链模板处理通过单号【{0}】未找到链数据,请上传", ftet.FZcode);
                    return "";
                }
                else
                {
                    // byte[] templateBytes = File.ReadAllBytes(projectPath+ TFTELian);
                    //foreach (var FTETdetai in ftet.FTETdetailList)
                    //{
                    //DataRow[] beiRow = FTETLian.Select(string.Format("bei='{0}' ", FTETdetai.bei));
                    //if (beiRow.Length > 0)
                    //{
                    for (int index = 0; index < FTETLian.Rows.Count; index++)
                    {

                        for (int i = 1; i <= 14; i++)
                        {
                            string cellname = "l" + i;
                            string lianname = FTETLian.Rows[index][cellname].ToString();
                            string date = FTETLian.Rows[index]["labeldate"].ToString();
                            if (!string.IsNullOrEmpty(lianname))// 有数的话生成对应的链pdf
                            {
                                // Excel编辑修改，返回路径
                                using (FileStream templateStream = new FileStream(projectPath + TFTELian, FileMode.Open, FileAccess.Read))
                                {
                                    IWorkbook workbook = new XSSFWorkbook(templateStream); // 使用 XSSFWorkbook 处理 .xlsx
                                    ISheet sheet = workbook.GetSheetAt(0);

                                    if (sheet == null)
                                    {
                                        throw new InvalidOperationException("Sheet1 不存在");
                                    }

                                    // 修改 A37 单元格（行号从0开始，第37行对应索引36）
                                    IRow row = sheet.GetRow(36) ?? sheet.CreateRow(36);
                                    ICell cell = row.GetCell(0) ?? row.CreateCell(0);
                                    cell.SetCellValue(i + "链");
                                    IRow row2 = sheet.GetRow(17) ?? sheet.CreateRow(17);
                                    ICell cel2 = row2.GetCell(0) ?? row2.CreateCell(0);
                                    //cel2.SetCellValue(string.IsNullOrEmpty(date)?"": date.Substring(0,10));
                                    if (DateTime.TryParse(date, out DateTime dt))
                                    {
                                        cel2.SetCellValue(dt.ToString("yyyy/M/d")); // 输出 2025/6/5
                                    }
                                    else
                                    {
                                        cel2.SetCellValue(string.Empty);
                                    }

                                    // 生成新文件名
                                    string newFileName = $"直链put_{filename}_{i}.xlsx";
                                    string newFilePath = System.IO.Path.Combine(outputTextPath, newFileName);
                                    // 保存文件
                                    using (FileStream fs = new FileStream(newFilePath, FileMode.Create))
                                    {
                                        workbook.Write(fs);
                                    }
                                    Thread.Sleep(800);
                                    excepahList.Add(newFilePath);
                                }
                            }
                        }
                    }
                    
                        //}
                        //else
                        //{
                        //    XErrorLogToolMDM.WriteLog(string.Format("FTET直链模板处理通过单号【{0}】背番【{1}】未找到链数据,请上传", ftet.FZcode, FTETdetai.bei));
                        //    message += string.Format("FTET直链模板处理通过单号【{0}】背番【{1}】未找到链数据,请上传", ftet.FZcode, FTETdetai.bei);
                        //}
                    //}
                    if (excepahList.Count>0)
                    {
                        foreach (var item in excepahList)
                        {
                            string pdffilename = System.IO.Path.GetFileNameWithoutExtension(item);
                           string paht =loHelper.ExcelOutPdf(item, pdffilename);
                            outPath += paht + ";";
                        }
                    }

                }
            }
            catch (Exception e)
            {
                XErrorLogToolMDM.WriteLog("FTET直链模板处理错误" + e.Message);
            }
            return outPath;
        }
        /*主表信息 发注单号 f4
 * k3订单号 f5
 * 出荷日期 f6
 * 传票作成者 B7
 * 传票做成时刻 F7
 * 受入区代码，品番从量表里拿
 */
        public string REBExcelToPdf(string outputTextPath, FTETEntity ftet, string filename, FtceDto ftceDto,string ArrivalDueDate)
        {
            // 注意点 Excel行13~18是明细行，如果超出，需要插入行
            List<OrderInfo> orderInfos = new List<OrderInfo>();
            string projectPath = AppDomain.CurrentDomain.BaseDirectory;
            string TFTELian = ConfigurationManager.AppSettings["FTETQRB"].ToString();// 模板地址
            string outPath = "";
            string extension = System.IO.Path.GetExtension(TFTELian).ToLower();
            try
            {
                using (FileStream templateStream = new FileStream(projectPath + TFTELian, FileMode.Open, FileAccess.Read))
                {
                    IWorkbook workbook;

                    if (extension == ".xls")
                    {
                        workbook = new HSSFWorkbook(templateStream); // Excel 2003 及以下版本
                    }
                    else if (extension == ".xlsx")
                    {
                        workbook = new XSSFWorkbook(templateStream); // Excel 2007 及以上版本
                    }
                    else
                    {
                        throw new NotSupportedException("Unsupported file format. Only .xls and .xlsx are supported.");
                    }

                    ISheet sheet = workbook.GetSheetAt(0);
                    string car = filename.Substring(2);// 车型
                    // 通过客户+车型查量表 能查到背番的唯一行
                    string veh_model = "";
                    if (ftet.FZcode.Contains("小批试制"))
                    {
                        veh_model = "POORDT";
                    }
                    else {
                        veh_model = ftet.FZcode.Substring(0, 2);
                    }
                        var queryJson = new
                        {
                            cust_name = "FTET",
                            veh_model = veh_model
                            
                        };
                    DataTable LTB = FTETdisposeService.getoriginalunit(JsonConvert.SerializeObject(queryJson, Formatting.Indented));
                    if (LTB==null|| LTB.Rows.Count==0)
                    {
                        throw new InvalidOperationException("FTET导出确认表，量表未获取到数据");
                    }
                    if (sheet == null)
                    {
                        throw new InvalidOperationException("FTET确认表Sheet1 不存在");
                    }
                    else
                    {
                        IRow row = sheet.GetRow(3) ?? sheet.CreateRow(3);
                        ICell cel = row.GetCell(5) ?? row.CreateCell(5);
                        cel.SetCellValue(ftet.FZcode);
                        IRow row2 = sheet.GetRow(4) ?? sheet.CreateRow(4);
                        ICell cel2 = row2.GetCell(5) ?? row2.CreateCell(5);
                        cel2.SetCellValue(ftet.k3code);

                        IRow row3 = sheet.GetRow(5) ?? sheet.CreateRow(5);
                        ICell cel3 = row3.GetCell(5) ?? row3.CreateCell(5);
                        cel3.SetCellValue(ftet.outdate);
                        //orderInfo

                        IRow row4 = sheet.GetRow(6) ?? sheet.CreateRow(6);
                        ICell cel4 = row4.GetCell(1) ?? row4.CreateCell(1);
                        cel4.SetCellValue(ftceDto.createPerson);

                        IRow row5 = sheet.GetRow(6) ?? sheet.CreateRow(6);
                        ICell cel5 = row5.GetCell(5) ?? row5.CreateCell(5);

                        ICell userCode =  row3.GetCell(1) ?? row3.CreateCell(1);
                        if (filename.Contains("BJ")) {
                            userCode.SetCellValue("10001311");
                        }

                        cel5.SetCellValue(DateTime.Now.ToString("yyyy-MM-dd"));

                        if (ftet.FTETdetailList.Count > 6)
                        {

                            int templateRowIndex = 12;  // 复制样式行
                            int currentRowIndex = templateRowIndex+1;  // 在那一行增加
                            var templateRow = sheet.GetRow(templateRowIndex);

                            // 复制模板行的单元格
                            for (int j = 0; j < ftet.FTETdetailList.Count - 6; j++)
                            {

                                sheet.ShiftRows(currentRowIndex , sheet.LastRowNum, 1, true, false);

                                // 创建新行
                                IRow newRow = sheet.CreateRow(currentRowIndex);
                                newRow.Height = templateRow.Height; // 保留行高
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
                                for (int i = 0; i < sheet.NumMergedRegions; i++)
                                {
                                    var mergedRegion = sheet.GetMergedRegion(i);
                                    if (mergedRegion != null && mergedRegion.FirstRow == templateRowIndex)
                                    {
                                        var newMergedRegion = new NPOI.SS.Util.CellRangeAddress(
                                            currentRowIndex,
                                            currentRowIndex,
                                            mergedRegion.FirstColumn,
                                            mergedRegion.LastColumn);
                                        sheet.AddMergedRegion(newMergedRegion);
                                    }
                                }
                            }

                            if (sheet is XSSFSheet xssfSheet)
                            {
                                var drawing = xssfSheet.CreateDrawingPatriarch() as XSSFDrawing;
                                if (drawing != null)
                                {
                                    foreach (var shape in drawing.GetShapes())
                                    {
                                        if (shape is XSSFSimpleShape simpleShape)
                                        {
                                            var anchor = simpleShape.GetAnchor() as XSSFClientAnchor;
                                            if (anchor != null )
                                            {
                                                int insertedRowCount = currentRowIndex - 6;
                                                anchor.Row1 += insertedRowCount;
                                                anchor.Row2 += insertedRowCount;
                                            }
                                        }
                                    }
                                }
                            }
                            else if (sheet is HSSFSheet hssfSheet)
                            {
                                var drawing = hssfSheet.CreateDrawingPatriarch() as HSSFPatriarch;
                                if (drawing != null)
                                {
                                    foreach (var shape in drawing.Children)
                                    {
                                        if (shape is HSSFSimpleShape simpleShape)
                                        {
                                            var anchor = simpleShape.Anchor as HSSFClientAnchor;
                                            if (anchor != null )
                                            {
                                                int insertedRowCount = currentRowIndex - 6;
                                                anchor.Row1 += insertedRowCount;
                                                anchor.Row2 += insertedRowCount;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        int SetBegin = 12;
                        foreach (var item in ftet.FTETdetailList)
                        {
                            DataRow[] dr = LTB.Select(string.Format("cust_num='{0}' and back_num='{1}'", item.pin, item.bei));
                            
                            IRow detailRow = sheet.GetRow(SetBegin) ;
                            // 列0 车型，      ；
                            ICell detCel0 =detailRow.GetCell(0)?? detailRow.CreateCell(0);

                            if (ftet.FZcode.Contains("小批试制"))
                            {
                                detCel0.SetCellValue("小批试制");
                            }
                            else {
                                detCel0.SetCellValue(ftet.FZcode.Substring(0, 2));
                            }
                                
                            

                            OrderInfo orderInfo = new OrderInfo();
                            orderInfo.ShipmentDate = ftet.outdate;
                            orderInfo.ArrivalDueDate = ArrivalDueDate;
                            orderInfo.OrderCode = ftet.FZcode;
                            orderInfo.InclusionNumber = ftet.k3code;
                            orderInfo.Category = "FTET";

                            //orderInfo.k3
                            
                            if (dr.Length>0)
                            {
                                // 列1 受入区代码
                                ICell detCel1 = detailRow.GetCell(1) ?? detailRow.CreateCell(1);
                                detCel1.SetCellValue(dr[0]["recv_area"].ToString());
                                //列3DN品番
                                ICell detCel3 = detailRow.GetCell(3) ?? detailRow.CreateCell(3);
                                detCel3.SetCellValue(dr[0]["denso_num"].ToString());
                                orderInfo.DnPartNumber = dr[0]["denso_num"].ToString();
                                //列5收容数
                                ICell detCel6 = detailRow.GetCell(6) ?? detailRow.CreateCell(6);
                                detCel6.SetCellValue(dr[0]["box_cap"].ToString());
                                orderInfo.Capacity = int.Parse(dr[0]["box_cap"].ToString());
                                //列7纳入数量
                                ICell detCel8 = detailRow.GetCell(8) ?? detailRow.CreateCell(8);
                                //int sr = string.IsNullOrEmpty(dr[0]["box_cap"].ToString()) ? 0 : Convert.ToInt32(dr[0]["box_cap"].ToString());
                                //int box = string.IsNullOrEmpty(item.box.ToString()) ? 0 : (int)item.box;
                                detCel8.SetCellValue(item.number.ToString());
                                orderInfo.InclusionQuantity = int.Parse(item.number.ToString());
                                //；列8拖码
                                ICell detCel9 = detailRow.GetCell(9) ?? detailRow.CreateCell(9);
                                detCel9.SetCellValue(dr[0]["useflag"].ToString());
                                //列10库位号
                                ICell detCel10 = detailRow.GetCell(10) ?? detailRow.CreateCell(10);
                                detCel10.SetCellValue(dr[0]["loc_num"].ToString());

                            }
                            //列2客户品番
                            ICell detCel2 = detailRow.GetCell(2) ?? detailRow.CreateCell(2);
                            detCel2.SetCellValue(item.pin);
                            orderInfo.CustomerPartNumber = item.pin;
                            //列5背番
                            ICell detCel5 = detailRow.GetCell(5) ?? detailRow.CreateCell(5);
                            detCel5.SetCellValue(item.bei);
                            orderInfo.BackNumber = item.bei;
                            //列6 看板张数
                            ICell detCel7 = detailRow.GetCell(7) ?? detailRow.CreateCell(7);
                            detCel7.SetCellValue(item.box.ToString());
                            orderInfo.BoardCount = int.Parse(item.box.ToString());
                            SetBegin++;
                            orderInfos.Add(orderInfo);
                        }
                        int HJRowNum = 18;
                        if (ftet.FTETdetailList.Count>6)
                        {
                            HJRowNum = SetBegin;
                        }
                        //  合计
                        IRow HJRow = sheet.GetRow(HJRowNum);
                        ICell HJCel7 = HJRow.GetCell(7) ?? HJRow.CreateCell(7);
                        int hj=(int)ftet.FTETdetailList.Sum(t => t.box);
                        HJCel7.SetCellValue(hj.ToString());
                        // 周几
                        string[] weekDays = { "周日", "周一", "周二", "周三", "周四", "周五", "周六" };
                        int weekNumber = (int)Convert.ToDateTime(ftet.outdate).DayOfWeek;
                        string NRday = weekDays[weekNumber];// 占位符 “　　”
                        IRow workRow = sheet.GetRow(3);
                        ICell workCell = workRow.GetCell(8) ?? workRow.CreateCell(8);
                        workCell.SetCellValue(NRday);
                    }
                    
                    // 生成新文件名
                    string newFileName = $"{filename}_确认表{extension}";
                    string newFilePath = System.IO.Path.Combine(outputTextPath, newFileName);
                    // 保存文件
                    using (FileStream fs = new FileStream(newFilePath, FileMode.Create))
                    {
                        workbook.Write(fs);
                    }
                    XErrorLogToolMDM.WriteLog("生成确认表excel");
                    LiberOfficeHelper loh = new LiberOfficeHelper();
                    string newPdfPath = outputTextPath + filename + "_确认表.pdf";
                    loh.ExcelOutOfPdf_H(newFilePath, newPdfPath);
                    PulicHelper.saveConfirm(ftceDto.mainId,newFilePath,newPdfPath);
                    orderService.EditConfirmRemark(ftceDto.mainId.ToString(), "FTET", filename.Substring(0,2));

                    PulicHelper.SaveOrderInfos(orderInfos);
                    // outPath = newFilePath;  不要Excel路径
                    outPath = newPdfPath;


                }
            }
            catch (Exception ex)
            {
                XErrorLogToolMDM.WriteLog("FTET确认表处理错误" + ex.Message);
                throw new InvalidOperationException("FTET确认表处理错误。"+ ex.Message);
            }
            return outPath;

         }

        /// <summary>
        /// FTET 导入出货计划表，写入数据库ftet_plan
        /// </summary>
        /// <param name="inputFile"></param>
        public static void ExecPlanExcel(string inputFile)
        {
            try {
                //inputFile = @"D:\01work\24大宇宙\01南沙电装OMS\05客户提供\FTET 2025年4月物流集货计划--广州地区2.xls";

                Dictionary<DateTime, List<string>> data = new Dictionary<DateTime, List<string>>();

                //解析excel
                using (FileStream fs = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
                {
                    IWorkbook workbook;
                    ISheet sheet;
                    IRow row = null;
                    IRow rowMaru = null;
                    IRow rowOrder = null;
                    ICell cell;

                    if (System.IO.Path.GetExtension(inputFile).ToLower() == ".xlsx")
                    {
                        workbook = new XSSFWorkbook(fs);
                    }
                    else
                    {
                        workbook = new HSSFWorkbook(fs);
                    }

                    sheet = workbook.GetSheetAt(0);

                    int time = 0;
                    for (int K = 0; K < sheet.LastRowNum; K++)
                    {
                        IRow rowi = sheet.GetRow(K);
                        if (rowi != null && rowi.GetCell(2) != null && rowi.GetCell(2).ToString() == "第一便") time++;
                        if (time == 2)
                        {
                            row = sheet.GetRow(K);
                            rowMaru = sheet.GetRow(K + 2);
                            rowOrder = sheet.GetRow(K - 4);
                            break;
                        }
                    }

                    //row = sheet.GetRow(33);
                    //rowMaru = sheet.GetRow(35);
                    //rowOrder = sheet.GetRow(29);

                    int cellCount = row.LastCellNum - 10;
                    int dateCount = cellCount / 5; //每个日期占5列

                    int begin = 10;
                    for (int q = 0; q < row.LastCellNum; q++) {
                        var cellq = row.GetCell(q);
                        if (cellq == null || cellq.CellType == CellType.Blank)
                            continue;

                        // 检查是否为日期格式
                        if (cellq.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(cellq))
                        {
                            begin = q;
                            break; 
                        }
                    }

                    for (int i = begin; i < dateCount * 5 + begin; i += 5)
                    {
                        if (sheet.IsColumnHidden(i)) continue; // 跳过隐藏列
                        ICell celli = row.GetCell(i);
                        DateTime? date = GetCellDate(row.GetCell(i));
                        string Maru = rowMaru.GetCell(i).StringCellValue;

                        //有○的时候，写入字典，并把上边的五个单元格的单据号写入
                        if (Maru == "○" || Maru == "①")
                        //if (Maru != null)
                        {

                            // 添加到字典
                            if (!data.ContainsKey(date.Value))
                            {
                                data.Add(date.Value, new List<string>());
                            }
                            for (int j = 0; j < 5; j++)
                            {
                                //string rowOrderStr = rowOrder.GetCell(i + j).StringCellValue;
                                //if (rowOrder.GetCell(i + j) != null && rowOrder.GetCell(i + j).StringCellValue != string.Empty)
                                //{

                                //    data[date].Add(rowOrderStr);
                                //}
                                ICell orderCell = rowOrder.GetCell(i + j);
                                if (orderCell != null)
                                {
                                    string rowOrderStr = orderCell.ToString().Trim();
                                    rowOrderStr = System.Text.RegularExpressions.Regex.Replace(rowOrderStr, @"\s+", "");
                                    if (!string.IsNullOrEmpty(rowOrderStr))
                                    {
                                        data[date.Value].Add(rowOrderStr);
                                    }
                                }
                            }
                        }
                        else if (data.Count > 0)//月份对，但是没有○，写入到上一日
                        {
                            //取最大的出货日期，上边如果有订单，把订单号写入到最大的出货日期里
                            DateTime maxDate = data.Keys.Max();

                            //for (int j = 0; j < 5; j++)

                            //{
                            //    if (rowOrder.GetCell(i + j) != null && rowOrder.GetCell(i + j).StringCellValue != string.Empty)
                            //    {
                            //        data[maxDate].Add(rowOrder.GetCell(i + j).StringCellValue);
                            //    }
                            //}
                            for (int j = 0; j < 5; j++)
                            {
                                var cellData = rowOrder.GetCell(i + j);
                                if (cellData != null)
                                {
                                    // 获取单元格字符串值
                                    string value = cellData.StringCellValue ?? "";

                                    // 去掉两侧空格：
                                    //value = value.Trim();

                                    // 去掉内部所有空格
                                     value = System.Text.RegularExpressions.Regex.Replace(value, @"\s+", "");

                                    // 非空才加入
                                    if (!string.IsNullOrEmpty(value))
                                    {
                                        data[maxDate].Add(value);
                                    }
                                }
                            }
                        }
                    }
                }
                //清除数据
                string sql_delete = string.Format("delete from ftet_plan where ddate>='{0}' and ddate <= '{1}'",
                    data.Keys.Min().ToString("yyyy-MM-dd"),
                    data.Keys.Max().ToString("yyyy-MM-dd"));
                DataHelper.ExecuteSql(sql_delete);
                StringBuilder sql = new StringBuilder();

                sql.Append("insert into ftet_plan(ddate,orderno) values ");

                foreach (var item in data)
                {
                    for (int i = 0; i < item.Value.Count; i++)
                    {
                        sql.Append(string.Format("('{0}','{1}'),", item.Key.ToString("yyyy-MM-dd"), item.Value[i]));
                    }
                }

                if (sql[sql.Length - 1] == ',')
                {
                    sql.Length--; 
                }


                int n = DataHelper.ExecuteSql(sql.ToString());
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
            }
            
        }



        public static DateTime? GetCellDate(ICell cell)
        {
            if (cell == null) return null;

            try
            {
                // 1. 数值型日期
                if (cell.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(cell))
                {
                    return cell.DateCellValue;
                }

                // 2. 字符串类型，或者日期类型异常
                string str = cell.ToString().Trim();

                // 如果字符串里没有年份，默认加上今年年份
                if (!str.Contains("年"))
                {
                    str = DateTime.Now.Year + "年" + str;
                }

                // 支持格式：yyyy年M月d日 或 yyyy年M月d
                string[] formats = { "yyyy年M月d日", "yyyy年M月d" };

                if (DateTime.TryParseExact(str, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                {
                    return date;
                }
                else
                {
                    return null; // 无法解析
                }
            }
            catch
            {
                return null;
            }
        }


        /// <summary>
        /// 导出FTET核对表
        /// </summary>
        /// <param name="orderno">出货日任意一个订单号</param>
        /// <param name="excelTemplate">模板文件路径</param>
        /// <param name="outputFile">输出文件路径</param>
        public static  string Export(string orderno, string excelTemplate, string outputFile)
        {
            //传入任意一个单号，获取规定出货时间
            //orderno = "BJ2024102801";
            //excelTemplate = @"D:\01work\24大宇宙\01南沙电装OMS\05客户提供\核对记录表.xlsx";
            //outputFile = @"D:\01work\24大宇宙\01南沙电装OMS\05客户提供\核对记录表test.xlsx";

            DateTime podate = new DateTime();
    
            string sql = string.Format(
                @"select DISTINCT podate,ordercode,redundancy7  from orderimport where podate = 
                    (select DISTINCT podate from orderimport where ordercode  ='{0}')",
                    orderno);

            DataTable dt_order = DataHelper.Query(sql);

            DataTable dt_D = new DataTable();

            if (dt_order.Rows.Count == 0)
                return "";
            else
            {
                //规定出货时间
                podate = Convert.ToDateTime(dt_order.Rows[0]["podate"].ToString());

                //反查出货计划表的单据 D列
                sql = string.Format("select REGEXP_REPLACE(orderno, '\\s+', '') AS orderno from ftet_plan where ddate = '{0}' order by id", podate.ToString("yyyy-MM-dd"));
                dt_D = DataHelper.Query(sql);
            }

            //输出
            using (FileStream fs = new FileStream(excelTemplate, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook;
                ISheet sheet;
                IRow row;
                ICell cell;


                if (System.IO.Path.GetExtension(excelTemplate).ToLower() == ".xlsx")
                {
                    workbook = new XSSFWorkbook(fs);
                }
                else
                {
                    workbook = new HSSFWorkbook(fs);
                }


                sheet = workbook.GetSheetAt(0);


                ICellStyle wrapStyle = sheet.GetRow(2).GetCell(1).CellStyle;
                wrapStyle.WrapText = true; // 关键设置：启用文本换行

                row = sheet.GetRow(2);

                cell = row.GetCell(1) == null ? row.CreateCell(1) : row.GetCell(1);
                cell.SetCellValue(podate.ToString("yyyy/MM/dd"));
                sheet.GetRow(0).GetCell(1).SetCellValue(podate.ToString("yyyy/MM/dd"));

                cell = row.GetCell(2) == null ? row.CreateCell(2) : row.GetCell(2);
                Dictionary<string, string> dict_date = new Dictionary<string, string>();
                string sdate = string.Empty;
                for (int i = 0; i < dt_order.Rows.Count; i++)
                {
                    if (dt_order.Rows[i]["redundancy7"] == DBNull.Value || dt_order.Rows[i]["redundancy7"].ToString() == string.Empty) continue;
                    DateTime tempdate = Convert.ToDateTime(dt_order.Rows[i]["redundancy7"].ToString());
                    if (!dict_date.ContainsKey(tempdate.ToString("yyyy/MM/dd")))
                    {
                        dict_date.Add(tempdate.ToString("yyyy/MM/dd"), string.Empty);
                    }
                }
                foreach (var item in dict_date)
                {
                    sdate += item.Key + "\n";
                }
                cell.SetCellValue(sdate);
                cell.CellStyle = wrapStyle;

                cell = row.GetCell(3) == null ? row.CreateCell(3) : row.GetCell(3);
                string orders = string.Empty;
                for (int i = 0; i < dt_D.Rows.Count; i++)
                {
                    orders += dt_D.Rows[i][0].ToString() + "\n";
                }
                cell.SetCellValue(orders);
                cell.CellStyle = wrapStyle;

                int GBIdx = 2;
                int NBIdx = 6;
                int ZBIdx = 10;
                int TBIdx = 14;
                int BJIdx = 18;
                for (int i = 0; i < dt_order.Rows.Count; i++)
                {
                    string ordercode = dt_order.Rows[i]["ordercode"].ToString();
                    string carno = ordercode.Substring(0, 2);
                    int rowIdx = 0;
                    switch (carno)
                    {
                        case "GB":
                            rowIdx = GBIdx;
                            GBIdx++;
                            break;
                        case "NB":
                            rowIdx = NBIdx;
                            NBIdx++;
                            break;
                        case "ZB":
                            rowIdx = ZBIdx;
                            ZBIdx++;
                            break;
                        case "TB":
                            rowIdx = TBIdx;
                            TBIdx++;
                            break;
                        case "BJ":
                            rowIdx = BJIdx;
                            BJIdx++;
                            break;
                        default:
                            break;
                    }

                    row = sheet.GetRow(rowIdx);
                    cell = row.GetCell(5) == null ? row.CreateCell(5) : row.GetCell(5);
                    cell.SetCellValue(ordercode);
                }


                //输出
                using (FileStream fsout = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                {
                    workbook.Write(fsout);
                }
                LiberOfficeHelper loh = new LiberOfficeHelper();
                string newPdfPath = System.IO.Path.GetDirectoryName(outputFile)  + "\\00套票含核对记录表.pdf";
                loh.ExcelOutOfPdf_H(outputFile, newPdfPath);
                return newPdfPath;
            }
        }

        public class rowcode
        {
            public int beginnum { get; set; }
            public int endnum { get; set; }
            public string code { get; set; }

            public int rownumber { get; set; }
        }
        // 直链配货数据处理
        public static async void setZL(string filePath)
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
                    List<ftet_chain> chainList = new List<ftet_chain>();
                    List<int> clist = new List<int>();
                    List<rowcode> ordercodeList = new List<rowcode>();
                    int numMerged = sheet.NumMergedRegions;
                    // 遍历当前 Sheet 的所有合并区域

                    for (int i = 0; i < sheet.NumMergedRegions; i++)
                    {
                        CellRangeAddress mergedRegion = sheet.GetMergedRegion(i);
                        int startRow = mergedRegion.FirstRow;
                        int endRow = mergedRegion.LastRow;
                        int startCol = mergedRegion.FirstColumn;
                        int endCol = mergedRegion.LastColumn;
                        var row = sheet.GetRow(startRow);
                        var cell = row?.GetCell(startCol);
                        var content = cell?.ToString() ?? string.Empty;
                        // 获取合并区域的起始行列
                        rowcode rcode = new rowcode();
                        rcode.code = content;
                        rcode.beginnum = startCol;
                        rcode.endnum = endCol;
                        rcode.rownumber = startRow;
                        ordercodeList.Add(rcode);
                    }
                    // 拿到了所有的合并格 ordercodeList 然后....
                    List<rowcode> ordercode = ordercodeList.Where(t => t.code == "订单号").ToList();


                    foreach (var item in ordercode)// 所有订单号所在的行
                    {
                        foreach (var hbcell in ordercodeList)// 所有的合并单元格
                        {

                            if (item.rownumber == hbcell.rownumber && hbcell.code != "订单号")
                            {
                                IRow ordercoderow = sheet.GetRow(hbcell.rownumber+1);
                                var cellDate = ordercoderow?.GetCell(8);
                                var orderdate = getcell(cellDate, workbook);
                                // 3行以后才是数据
                                for (int rowIdx = hbcell.rownumber + 3; rowIdx < sheet.LastRowNum; rowIdx++)
                                {
                                    IRow row = sheet.GetRow(rowIdx);
                                    var cell1 = row?.GetCell(0);
                                    var xuhao = cell1?.ToString() ?? string.Empty;
                                    var cell5 = row?.GetCell(5);
                                    var gys = cell5?.ToString() ?? string.Empty;
                                    var cell6 = row?.GetCell(6);
                                    var beifan = cell6?.ToString() ?? string.Empty;
                                    if (string.IsNullOrEmpty(beifan))
                                    {
                                        break;
                                    }
                                    else
                                    {
                                        ftet_chain chain = new ftet_chain();
                                        chain.no = xuhao;
                                        chain.ordercode = hbcell.code;
                                        chain.supname = gys;
                                        chain.bei = beifan;
                                        chain.labeldate = orderdate;
                                        for (var i = hbcell.beginnum; i <= hbcell.endnum; i++)
                                        {
                                            string celvalue = (row?.GetCell(i))?.ToString() ?? string.Empty;
                                            if (!string.IsNullOrEmpty(celvalue))
                                            {
                                                switch (i - 7)
                                                {
                                                    case 1:
                                                        chain.l1 = celvalue;
                                                        break;
                                                    case 2:
                                                        chain.l2 = celvalue;
                                                        break;
                                                    case 3:
                                                        chain.l3 = celvalue;
                                                        break;
                                                    case 4:
                                                        chain.l4 = celvalue;
                                                        break;
                                                    case 5:
                                                        chain.l5 = celvalue;
                                                        break;
                                                    case 6:
                                                        chain.l6 = celvalue;
                                                        break;
                                                    case 7:
                                                        chain.l7 = celvalue;
                                                        break;
                                                    case 8:
                                                        chain.l8 = celvalue;
                                                        break;
                                                    case 9:
                                                        chain.l9 = celvalue;
                                                        break;
                                                    case 10:
                                                        chain.l10 = celvalue;
                                                        break;
                                                    case 11:
                                                        chain.l11 = celvalue;
                                                        break;
                                                    case 12:
                                                        chain.l12 = celvalue;
                                                        break;
                                                    case 13:
                                                        chain.l13 = celvalue;
                                                        break;
                                                    case 14:
                                                        chain.l14 = celvalue;
                                                        break;
                                                }
                                            }
                                        }

                                        chainList.Add(chain);
                                    }
                                }
                            }
                        }
                    }

                    LiberOfficeHelper liberOfficeHelper = new LiberOfficeHelper();
                    string tempPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(filePath), "temp.pdf");
                    string pdfPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(filePath), $"{Guid.NewGuid()}.pdf");
                    string safePath = pdfPath.Replace(@"\", @"\\").Replace("'", "''");

                    liberOfficeHelper.ExcelOutOfPdf1(filePath, tempPath);
                    SpireHelper.SaveFirstPage(tempPath, pdfPath);
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                    chainService.setftet_chain(chainList, safePath);
                }
                


                //for (int i = 0; i < clist.Count; i++)
                //{
                //    int end;  // 小表格的末尾行
                //    if (i == clist.Count)
                //    {
                //        end = sheet.LastRowNum;
                //    }
                //    else
                //    {
                //        end = clist[i + 1];
                //    }

                //    for (int rowIndex = clist[i]; rowIndex <= end; rowIndex++) // 
                //    {
                //        for (var colIdx = 0; colIdx < sheet.GetRow(rowIndex).LastCellNum; colIdx++)
                //        {
                //        }
                //    }
                //}
            }
            catch (Exception ex)
            {
                XErrorLogToolMDM.WriteLog("直链配货excel拆解错误："+ex.Message);
            }
            }
        private static string getcell(ICell cell, IWorkbook workbook)
        {
            string resultText = "";
            if (cell != null && cell.CellType == CellType.Formula)
            {
                IFormulaEvaluator evaluator = workbook.GetCreationHelper().CreateFormulaEvaluator();
                CellValue evaluatedValue = evaluator.Evaluate(cell);

                
                switch (evaluatedValue.CellType)
                {
                    case CellType.Numeric:
                        if (DateUtil.IsCellDateFormatted(cell))
                        {
                            DateTime date = DateTime.FromOADate(evaluatedValue.NumberValue);
                            resultText = date.ToString("yyyy-MM-dd");
                        }
                        else
                        {
                            resultText = evaluatedValue.NumberValue.ToString();
                        }
                        break;
                    case CellType.String:
                        resultText = evaluatedValue.StringValue;
                        break;
                    default:
                        resultText = cell.ToString(); // 默认处理
                        break;
                }
               
            }
            return resultText;
        }
        private static object GetCellValue(ICell cell)
        {
            if (cell == null) return null;
            // 手动判断日期格式............
            if (IsDateCell(cell))
            {
                return cell.DateCellValue;
            }
            switch (cell.CellType)
            {
                case CellType.Numeric:
                    return cell.NumericCellValue;
                case CellType.String:
                    return cell.StringCellValue.Trim();
                case CellType.Boolean:
                    return cell.BooleanCellValue;
                case CellType.Formula:
                    return cell.ToString(); // 按需处理公式结果
                default:
                    return null;
            }
        }
        private static bool IsDateCell(ICell cell)
        {
            if (cell.CellType != CellType.Numeric) return false;

            // 通过格式字符串判断是否为日期
            string format = cell.CellStyle.GetDataFormatString().ToLower();
            return format.Contains("yy") || format.Contains("mm") || format.Contains("dd");
        }
      


    }
}