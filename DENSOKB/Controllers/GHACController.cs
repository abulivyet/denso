using DENSOKB.Helper;
using DENSOKB.Libs;
using DENSOKB.Models;
using HelpLog.CommomMethod;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Org.BouncyCastle.Bcpg;
using Spire.Pdf;
using Spire.Pdf.Graphics;
using Spire.Pdf.Texts;
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
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Http;
using System.Web.Script.Serialization;
using System.Web.SessionState;
using System.Web.WebPages;
using static System.Net.WebRequestMethods;

namespace DENSOKB.Controllers
{
    public class GHACController : ApiController
    {
        new orderimportService orderimport = new orderimportService();
        /// <summary>
        /// 列表获取周订单数据（分页）
        /// </summary>
        /// <param name="paginationobj"></param>
        /// <returns></returns>
        [JwtAuthorize]
        [HttpPost]
        public object GetGHACPage_weekDT(Pagination paginationobj)
        {
            DataTable data = GHACService.getPageTB(paginationobj, paginationobj.queryJson);
            Dictionary<string, object> jo = new Dictionary<string, object>();
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            jo.Add("success", true);
            jo.Add("total", paginationobj.records);
            jo.Add("data", PulicHelper.ConvertDataTableToListOfDictionaries(data));
            return jo;
        }
        /// <summary>
        /// 获取取货计划列表（分页）
        /// </summary>
        /// <param name="paginationobj"></param>
        /// <returns></returns>
        [JwtAuthorize]
        [HttpPost]
        public object GetGHACPage_planDT(Pagination paginationobj)
        {
            DataTable data = GHACService.getPagePlanTB(paginationobj, paginationobj.queryJson);
            Dictionary<string, object> jo = new Dictionary<string, object>();
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            jo.Add("success", true);
            jo.Add("total", paginationobj.records);
            jo.Add("data", PulicHelper.ConvertDataTableToListOfDictionaries(data));
            return jo;
        }

        [HttpPost]
        public void GHACUpload(dynamic obj)
        {
            string filePath;
            enumFileType fileType;
            //string username = obj["username"].ToString();

            string username = obj?.username?.ToString() ?? string.Empty;
            //var dict = obj as IDictionary<string, object>;
            //if (dict != null && dict.ContainsKey("username"))
            //{
            //    username = dict["username"]?.ToString() ?? "default";
            //}

            if (obj["filePath"] == null || obj["fileType"] == null)
            {
                return;
            }

            filePath = obj["filePath"].ToString();
            fileType = (enumFileType)Enum.Parse(typeof(enumFileType), obj["fileType"].ToString());
                        
            switch (fileType)
            {
                case enumFileType.EXCEL:
                    GHAC_EXCEL(filePath,username);
                    break;
                case enumFileType.ORDER:
                    GHAC_ORDER(filePath);
                    break;
                case enumFileType.SUMMONS:
                    GHAC_SUMMONS(filePath);
                    break;
                case enumFileType.PALLET:
                    GHAC_PALLET(filePath);
                    break;
                case enumFileType.PLS:
                    GHAC_PLS(filePath);
                    break;
                case enumFileType.PLAN:
                    GHAC_PLAN(filePath);
                    break;
                case enumFileType.PLAN2://SP/八千代/阶段品的取货计划导入
                    GHAC_PLAN2(filePath,username);
                    break;
                case enumFileType.INSTRUCTIONS:
                    GHAC_INSTRUCTIONS(filePath);
                    break;
                case enumFileType.TRACE:
                    GHAC_TRACE(filePath);
                    break;
                case enumFileType.GHAC01:
                    GHAC_01(filePath);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 输出成套文件
        /// </summary>
        /// <param name="obj"></param>
        [HttpPost]
        public HttpResponseMessage GHACoutputByCode(dynamic obj)
        {
            try
            {

            XErrorLogToolMDM.WriteLog("GHACoutputByCode");
            if (obj["InstructionCode"] == null)
                    return Request.CreateResponse(HttpStatusCode.InternalServerError, "导出失败，InstructionCode不能为空");
                //取货指示号
            string supplierCodes = "";//obj["supplierCode"].ToString();
            if (obj["supplierCode"] != null) {
                supplierCodes = obj["supplierCode"].ToString();
                    //return Request.CreateResponse(HttpStatusCode.InternalServerError, "导出失败，supplierCode不能为空");
                }
            string InstructionCodes = obj["InstructionCode"].ToString();
            
            string[] InstructionCodeList = InstructionCodes.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToArray();
            string[] supplierCodeList = supplierCodes.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToArray();

            string basePath = HttpContext.Current.Server.MapPath("~/UploadFiles/GHAC");
            string output = Path.Combine(basePath, "output\\" + Guid.NewGuid().ToString());

            if (!Directory.Exists(output))
                Directory.CreateDirectory(output);

                //foreach (var InstructionCode in InstructionCodeList)
                //{
                //    XErrorLogToolMDM.WriteLog("GHACoutputByCode");
                //    string outfilepath = GHACoutputByCode(InstructionCode,"");


                //    FileInfo f = new FileInfo(outfilepath);

                //    f.CopyTo(Path.Combine(output, f.Name));
                //}
                var codePairs = InstructionCodeList
    .Select((instructionCode, index) => new
    {
        InstructionCode = instructionCode,
        SupplierCode = supplierCodeList[index]
    })
    .Distinct()
    .ToList();

                foreach (var pair in codePairs)
                {
                    XErrorLogToolMDM.WriteLog(
                        $"GHACoutputByCode instructionCode={pair.InstructionCode}, supplierCode={pair.SupplierCode}"
                    );

                    string outfilepath = GHACoutputByCode(pair.InstructionCode, pair.SupplierCode);

                    FileInfo f = new FileInfo(outfilepath);

                    f.CopyTo(Path.Combine(output, f.Name), true);
                }

                DirectoryInfo dir = new DirectoryInfo(output);

            FileInfo[] fileout = dir.GetFiles("*.pdf", SearchOption.AllDirectories);

            if (fileout.Length > 1)
            {
                //把tempDownLoadPath压缩成zip，保存到basePath            
                string zipFilepath = Path.Combine(basePath, $"{DateTime.Now:yyyyMMddHHmmss}.zip");

                System.IO.Compression.ZipFile.CreateFromDirectory(output, zipFilepath);

                DownloadZip(zipFilepath, $"GHAC{DateTime.Now:yyyyMMddHHmmss}.zip");

                System.IO.File.Delete(zipFilepath);
            }
            else
            {
                DownloadPdf(fileout[0].FullName, $"GHAC{DateTime.Now:yyyyMMddHHmmss}.pdf");
            }

            string strSql = "update pickupplan set downloadcount = ifnull(downloadcount,0) + 1 where no = LEFT('{0}', CHAR_LENGTH('{0}') - 2)";
            foreach (var item in InstructionCodeList)
            {
                strSql = string.Format(strSql, item);
                DataHelper.ExecuteSql(strSql);
            }

            Directory.Delete(output, true);
                var response = Request.CreateResponse(HttpStatusCode.OK);
                return response;

            }
            catch (Exception e)
            {

                return Request.CreateResponse(HttpStatusCode.InternalServerError, "导出失败，" + e.Message); ;
            }

        }

        public string GHACoutputByCode(string InstructionCode,string supplierCode)
        {
            try
            {
                XErrorLogToolMDM.WriteLog("日志");
                //基础输出路径
                string basePath = HttpContext.Current.Server.MapPath("~/UploadFiles/GHAC");
                //临时输出路径
                string outPath = Path.Combine(basePath, string.Format("{0}\\{1}\\{2}", "output", InstructionCode, Guid.NewGuid().ToString()));
                //输出成套文件
                string outFile = string.Empty;

                if (!Directory.Exists(outPath))
                {
                    Directory.CreateDirectory(outPath);
                }
                
                if (InstructionCode.IndexOf("八千代") > -1)
                {
                    XErrorLogToolMDM.WriteLog("GHACoutputByCode  八千代");
                    List<string> File_All = new List<string>();
                    ///确认表
                    List<string> list = new List<string>();
                    list.Add(InstructionCode);
                    XErrorLogToolMDM.WriteLog("GHACoutputByCode GHAC_CONFIRM 八千代");
                    string File_Confirm = GHAC_CONFIRM(list,null,"八千代");
                    ///传票

                    string File_Sum = GHAC_SUMMONS_OUT(InstructionCode, enumOutType.Other,supplierCode);
                    string File_T = GHAC8KDT(InstructionCode);
                    string File_KB = GHAC8KDKB(InstructionCode);
                    //八千代追溯标签
                    string File_TRACE = GHAC_TRACE_OUT(InstructionCode);
                    //GHAC_CONFIRM

                    outFile = Path.Combine(outPath, "GHAC成套文件" + InstructionCode + ".pdf");
                    File_All.Add(File_Confirm);
                    File_All.Add(File_Sum);
                    File_All.Add(File_T);
                    File_All.Add(File_KB);
                    File_All.Add(File_TRACE);


                    SpireHelper.MergeFiles(File_All, outFile);
                }
                else if (InstructionCode.IndexOf("SP") > -1 || InstructionCode.IndexOf("阶段品") > -1)
                {
                    XErrorLogToolMDM.WriteLog("GHACoutputByCode  补给品 阶段品");
                    //查询数据库
                    //                    string sql = string.Format(
                    //                        @"
                    //select DISTINCT  version,rec_location,palletno,pn, concat(recdate,' ' ,rectime),p_code,p_version,planqty from pickupplan
                    //                        where concat(no,'00') = '{0}'
                    //                        order by version", InstructionCode);

                    string sql = string.Format(
                        @"
                    SELECT version, rec_location, palletno, pn,
                           CONCAT(recdate, ' ', rectime) AS rec_datetime,
                           p_code, p_version, planqty
                    FROM (
                        SELECT *
                        FROM pickupplan
                        WHERE CONCAT(no, '00') = '{0}'
                        
                        ORDER BY id
                    ) t
                    GROUP BY version, rec_location, palletno, pn,
                             recdate, rectime, p_code, p_version, planqty;
                    ", InstructionCode);

                    DataTable dt = DataHelper.Query(sql);

                    XErrorLogToolMDM.WriteLog(sql);
                    //最终输出的文件（顺序）
                    List<FileInfo> F_out = new List<FileInfo>();
                    List<FileInfo> F_summons = new List<FileInfo>();
                    List<FileInfo> F_PLS = new List<FileInfo>();
                    List<string> list = new List<string>();
                    list.Add(InstructionCode);
                    string File_Confirm = GHAC_CONFIRM(list,null,"补给品");

                    // 目录提前创建，避免每次循环都 new DirectoryInfo
                    DirectoryInfo DIR_PLS = new DirectoryInfo(Path.Combine(basePath, "PLS"));

                    // 用于判断传票号是否已经处理过
                    HashSet<string> processedCodes = new HashSet<string>();

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        string pCode = dt.Rows[i]["p_code"].ToString();
                        string pVersion = dt.Rows[i]["p_version"].ToString();

                        // 输出 SUMMONS
                        string file_summons = Path.Combine(basePath, "SUMMONS", pCode + pVersion + ".pdf");
                        if (!System.IO.File.Exists(file_summons))
                        {
                            XErrorLogToolMDM.WriteLog("未找到对应传票" + file_summons);
                            throw new Exception("下载失败,传票未上传"+pCode);
                        }
                            F_summons.Add(new FileInfo(file_summons));

                        //  先判断 p_code 是否已经处理过
                        if (!processedCodes.Add(pCode))
                        {
                            // 如果这个 p_code 已经处理过，则跳过，不重复取文件
                            continue;
                        }

                        // ③ 获取箱看板文件（只取一次）
                        var plsFiles = DIR_PLS.GetFiles(pCode + "*.pdf")
                                              .OrderBy(f => f.Name);

                        F_PLS.AddRange(plsFiles);
                    }

                    string F_summons_outpath = string.Empty;
                    string F_PLS_outpath = string.Empty;

                    F_summons_outpath = Path.Combine(outPath, "summons.pdf");
                    F_PLS_outpath = Path.Combine(outPath, "PLS.pdf");
                    SpireHelper.Merge(2, 1, F_summons.ToArray(), F_summons_outpath, true, 0, 0, true);
                    //SpireHelper.Merge(2, 1, F_summons.ToArray(), F_summons_outpath, true);
                    if (F_PLS.Count > 0)
                    {
                        SpireHelper.Merge(3, 2, F_PLS.ToArray(), F_PLS_outpath, true);

                    }
                    else { throw new Exception("下载失败,传票未上传"); }

                        List<string> F_out_path = new List<string>();
                    //F_out_path.Add(File_Confirm);
                    //F_out_path.Add(F_summons_outpath);
                    //F_out_path.Add(F_PLS_outpath);
                    if (System.IO.File.Exists(File_Confirm)) { F_out_path.Add(File_Confirm); }
                    if (System.IO.File.Exists(F_summons_outpath)) { F_out_path.Add(F_summons_outpath); }
                    if (System.IO.File.Exists(F_PLS_outpath)) { F_out_path.Add(F_PLS_outpath); }


                    outFile = Path.Combine(outPath, "GHAC成套文件" + InstructionCode + ".pdf");
                    SpireHelper.MergeFiles(F_out_path, outFile);

                }
                else//量产
                {
                    XErrorLogToolMDM.WriteLog("GHACoutputByCode  量产");
                    //最终输出的文件（顺序）
                    List<FileInfo> F_out = new List<FileInfo>();
                    //取货指示书pdf
                    string F_INSTRUCTION;
                    //托标签
                    List<FileInfo> F_PALLET = new List<FileInfo>();

                    string File_Sum = GHAC_SUMMONS_OUT(InstructionCode, enumOutType.HAOKOU,supplierCode);  //传票

                    //记录当前操作的托号
                    string pallet_no = string.Empty;
                    //记录需要合并的箱看板
                    List<FileInfo> F_PLS_TEMP_MERGE = new List<FileInfo>();
                    //取货指示书
                    F_INSTRUCTION = GHAC_INSTRUCTION_OUT(InstructionCode, supplierCode);

                    //查询数据库
                    string sql = string.Format(
                    @"SELECT
            p.id,
            p.carno,
            w.r_location                      AS rec_location,
            p.palletno,
            p.pn,
            CONCAT(p.recdate, ' ', p.rectime) AS rec_datetime,
            p.recdate,
            p.rectime,
            p.p_code,
            p.p_version,
            p.planqty,
            p.packunit,
            ou.denso_num,
            ou.traceability_label,
            p.carno
        FROM pickupplan p
                 LEFT JOIN ord_weekorder w
                           ON w.p_code = p.p_code
                 LEFT JOIN originalunit ou
                           ON ou.cust_name = 'GHAC'
                              -- AND ou.factory = CONCAT('号口', CAST(RIGHT(p.plancode, 2) AS UNSIGNED), '厂')
                               AND p.supplier = ou.recv_port
AND ou.factory LIKE CONCAT('%',
                                                  CAST(CAST(RIGHT(p.plancode, 2) AS UNSIGNED) AS CHAR),
                                                  '%')
                               AND p.pn = ou.cust_num
        WHERE CONCAT(p.no, p.version) = '{0}' 
        and p.supplier = '{1}' 
        ORDER BY
            CAST(REGEXP_SUBSTR(p.palletno, '[0-9]+') AS UNSIGNED),
            p.pn,
            rec_location,
            p.recdate,
            STR_TO_DATE(p.rectime, '%H:%i')
    
", InstructionCode,supplierCode);

                    DataTable dt = DataHelper.Query(sql);
                    XErrorLogToolMDM.WriteLog("初始sql" + sql);
                    //var dt = dataTable.AsEnumerable()
                    //.OrderBy(r => r.Field<string>("pn"))                      
                    //.ThenBy(r =>
                    //{
                    //    var pallet = r.Field<string>("palletno");
                    //    // 提取托号里的数字
                    //    var num = System.Text.RegularExpressions.Regex.Match(pallet, @"\d+");
                    //    return num.Success ? int.Parse(num.Value) : int.MaxValue;
                    //})                                                         //  同 PN 内按托号
                    //.ThenBy(r => r.Field<DateTime>("recdate"))                //  日期
                    //.ThenBy(r => TimeSpan.Parse(r.Field<string>("rectime")))  //  时间
                    //.CopyToDataTable();
                    //生成确认表
                    string confirmPath = GHAC_CONFIRM(new List<string>() { InstructionCode }, new List<string>() { supplierCode } ,"量产");
                    F_out.Add(new FileInfo(confirmPath));

                    //取货指示书
                    
                    if (F_INSTRUCTION != string.Empty)
                    {
                        for (int i = 0; i < 2; i++)//添加2遍
                        {
                            F_out.Add(new FileInfo(F_INSTRUCTION));
                        }
                    }

                    if (!string.IsNullOrEmpty(File_Sum))
                    {
                        F_out.Add(new FileInfo(File_Sum));
                    }

                    if (dt == null || dt.Rows.Count == 0) return string.Empty;

                    //托标签
                    List<string> palletNos = dt.AsEnumerable()
                    .Select(r => r.Field<string>("palletno"))
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Distinct()
                    .OrderBy(x =>
                    {
                        // 托号 排序
                        var numPart = x.Replace("托", "");
                        return int.TryParse(numPart, out var n) ? n : int.MaxValue;
                    })
                    .ToList();

                    for (int i = 0; i < palletNos.Count; i++)
                    {
                        pallet_no = palletNos[i].ToString();
                        DirectoryInfo dir_PALLET = new DirectoryInfo(Path.Combine(basePath, @"PALLET\"));
                        FileInfo[] F_pallet_temp = TakeUniqueSplitFiles(dir_PALLET.GetFiles(pallet_no + InstructionCode + "*.pdf"));
                        for (int j = 0; j < F_pallet_temp.Length; j++)
                        {
                            F_out.Add(F_pallet_temp[j]);
                        }
                    }

                    //箱看板
                    string F_PLS_outpath = Path.Combine(outPath, "PLS.pdf");

                    List<FileInfo> F_PLS = new List<FileInfo>();
                    DirectoryInfo DIR_PLS = new DirectoryInfo(Path.Combine(basePath, "PLS"));

                    // 按取货计划托号顺序：同一托排完再下一托。文件按传票+车号一次取出，再用文件名末尾托号精确过滤（不用 *3.pdf，避免配到 13）。
                    HashSet<string> processedCodes = new HashSet<string>();
                    HashSet<string> addedPls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    Dictionary<string, FileInfo[]> plsFilesByKey = new Dictionary<string, FileInfo[]>(StringComparer.OrdinalIgnoreCase);

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        string pCode = dt.Rows[i]["p_code"].ToString();
                        string carno = dt.Rows[i]["carno"].ToString();
                        string palletno = NormalizePalletNo(dt.Rows[i]["palletno"].ToString());

                        string groupKey = string.IsNullOrWhiteSpace(carno) ? pCode : $"{pCode}_{carno}";
                        string processedKey = string.IsNullOrEmpty(palletno) ? groupKey : $"{groupKey}_{palletno}";
                        if (!processedCodes.Add(processedKey))
                        {
                            continue;
                        }

                        FileInfo[] groupFiles;
                        if (!plsFilesByKey.TryGetValue(groupKey, out groupFiles))
                        {
                            string searchPattern = string.IsNullOrWhiteSpace(carno)
                                ? $"{pCode}*.pdf"
                                : $"{pCode}*{carno}*.pdf";
                            groupFiles = TakeUniqueSplitFiles(DIR_PLS.GetFiles(searchPattern)
                                                       .OrderBy(f => f.Name));
                            plsFilesByKey[groupKey] = groupFiles;
                        }

                        IEnumerable<FileInfo> files = groupFiles;
                        if (!string.IsNullOrEmpty(palletno))
                        {
                            files = groupFiles.Where(f => FileNameMatchesPallet(f.Name, palletno)).OrderBy(f => f.Name);
                        }

                        foreach (var file in files)
                        {
                            if (addedPls.Add(file.FullName))
                            {
                                F_PLS.Add(file);
                            }
                        }
                    }

                    if (F_PLS.Count == 0 ) {
                        throw new Exception("未找到对应看板");
                    }

                    SpireHelper.Merge(3, 2, F_PLS.ToArray(), F_PLS_outpath, true);
                    F_out.Add(new FileInfo(F_PLS_outpath));

                    XErrorLogToolMDM.WriteLog("开始处理追溯标签");
                    //量产  追溯标签
                    string File_TRACE = GHAC_TRACE_OUT(dt);
                    if (!string.IsNullOrEmpty(File_TRACE))
                    {
                        F_out.Add(new FileInfo(File_TRACE));
                    }

                    List<string> F_out_path = new List<string>();
                    for (int i = 0; i < F_out.Count; i++)
                    {
                        F_out_path.Add(F_out[i].FullName);
                    }

                    outFile = Path.Combine(outPath, "GHAC成套文件" + InstructionCode + ".pdf");
                    SpireHelper.MergeFiles(F_out_path, outFile);

                }

                return outFile;
            }
            catch (Exception )
            {

                throw;
            }
            
        }

        /// <summary>
        /// 输出单个文件
        /// </summary>
        /// <param name="obj"></param>
        ///
        [HttpPost]
        public HttpResponseMessage GHACoutputSingleByCode(dynamic obj)
        {
            if (obj["InstructionCode"] == null || obj["fileType"] == null)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "取货指示号或文件类别为空");
            }
            string outPath = string.Empty;
            string strInstructionCode = obj["InstructionCode"].ToString();
            string[] InstructionCodes = strInstructionCode.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

            if (InstructionCodes.Length == 0) return Request.CreateResponse(HttpStatusCode.InternalServerError, "取货指示号为空"); ;


            string strFileType = obj["fileType"].ToString();
            string[] arrayFileType = strFileType.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

            List<enumFileType> fileTypes = new List<enumFileType>();
            for (int i = 0; i < arrayFileType.Length; i++)
            {
                fileTypes.Add((enumFileType)Enum.Parse(typeof(enumFileType), arrayFileType[i]));
            }

            enumOutType outType;
            if (InstructionCodes[0].IndexOf("SP") > -1 || InstructionCodes[0].IndexOf("八千代") > -1 || InstructionCodes[0].IndexOf("阶段品") > -1)
            {
                outType = enumOutType.Other;
            }
            else
            {
                outType = enumOutType.HAOKOU;
            }

            string basePath = HttpContext.Current.Server.MapPath("~/UploadFiles/GHAC/download");

            string tempDownLoadPath = Path.Combine(basePath, $"{Guid.NewGuid().ToString()}");
            if (!Directory.Exists(tempDownLoadPath))
                Directory.CreateDirectory(tempDownLoadPath);


            //创建主输出目录
            foreach (var InstructionCode in InstructionCodes)
            {
                //创建文件目录
                string tempPath = Path.Combine(tempDownLoadPath, InstructionCode);

                if (!Directory.Exists(tempPath))
                    Directory.CreateDirectory(tempPath);

                string perFileName = string.Empty;//存文件前缀
                
                foreach (var fileType in fileTypes)
                {
                    switch (fileType)
                    {
                        case enumFileType.CONFIRM:
                            outPath = GHAC_CONFIRM(new List<string>() { InstructionCode },null,"");
                            //perFileName = "确认表";
                            break;
                        case enumFileType.SUMMONS:
                            outPath = GHAC_SUMMONS_OUT(InstructionCode, outType,"");
                            perFileName = "传票";
                            break;
                        case enumFileType.PALLET:
                            if (InstructionCode.IndexOf("八千代") > -1)
                                outPath = GHAC8KDT(InstructionCode);
                            else 
                                outPath = GHAC_PALLET_OUT(InstructionCode, outType);
                            perFileName = "托标签";
                            break;
                        case enumFileType.PLS:
                            if (InstructionCode.IndexOf("八千代") > -1)
                                outPath = GHAC8KDKB(InstructionCode);
                            else 
                                outPath = GHAC_PLS_OUT(InstructionCode, outType);
                            perFileName = "看板";
                            break;
                        case enumFileType.INSTRUCTIONS:
                            outPath = GHAC_INSTRUCTION_OUT(InstructionCode,"");
                            perFileName = "取货指示书";
                            break;
                        case enumFileType.TRACE:
                            outPath= GHAC_TRACE_OUT(InstructionCode);
                            perFileName = "追溯标签";
                            break;
                        default:
                            break;
                    }

                    if (string.IsNullOrEmpty(outPath)) {
                        break;
                    }
                    FileInfo outfile = new FileInfo(outPath);
                    outfile.CopyTo(Path.Combine(tempPath, perFileName + outfile.Name), true);
                }
                
                string strSql = "update pickupplan set downloadcount = ifnull(downloadcount,0) + 1 where no = LEFT('{0}', CHAR_LENGTH('{0}') - 2)";
                strSql = string.Format(strSql, InstructionCode);
                DataHelper.ExecuteSql(strSql);
                
            }

            DirectoryInfo dirout = new DirectoryInfo(tempDownLoadPath);

            FileInfo[] fileout = dirout.GetFiles("*.pdf", SearchOption.AllDirectories);

            if (fileout.Length == 0) {
                throw new Exception("文件不存在");
            }
            List<string> pdfFiles = dirout.GetFiles("*.pdf", SearchOption.AllDirectories)
                .Select(f => f.FullName)
                .ToList();

            if (fileTypes.Count == 1 && fileTypes[0] == enumFileType.CONFIRM && fileout.Length > 1)
            {

                //多个确认表合成一个pdf
                string timeStr = DateTime.Now.ToString("yyyyMMddHHmmss");

                string confirmOutFile = Path.Combine(basePath, $"GHAC{timeStr}.pdf");

                SpireHelper.MergeFiles(pdfFiles, confirmOutFile);

                DownloadPdf(confirmOutFile, $"GHAC{timeStr}.pdf");
            }
            else if (fileTypes.Count > 1 && fileout.Length > 1)
            {
                //把tempDownLoadPath压缩成zip，保存到basePath            
                string zipFilepath = Path.Combine(basePath, $"{DateTime.Now:yyyyMMddHHmmss}.zip");

                System.IO.Compression.ZipFile.CreateFromDirectory(tempDownLoadPath, zipFilepath);

                DownloadZip(zipFilepath, $"GHAC{DateTime.Now:yyyyMMddHHmmss}.zip");

                System.IO.File.Delete(zipFilepath);
            }
            else
            {
                DownloadPdf(fileout[0].FullName, $"GHAC{DateTime.Now:yyyyMMddHHmmss}.pdf");
            }


            Directory.Delete(tempDownLoadPath, true);
            return Request.CreateResponse(HttpStatusCode.OK);


        }





        /// <summary>
        /// 弃用的
        /// </summary>
        /// <param name="obj"></param>
        public void GHACoutputByMuti(dynamic obj)
        {
            //基础输出路径
            string basePath = HttpContext.Current.Server.MapPath("~/UploadFiles/GHAC");
            //临时输出路径
            string outPath = Path.Combine(basePath, string.Format("{0}\\{1}\\{2}", "output", DateTime.Now.ToString("yyyyMMddHHmmss"), Guid.NewGuid().ToString()));
            //输出成套文件
            string outFile = Path.Combine(outPath, "八千代成套文件.pdf");

            if (!Directory.Exists(outPath))
            {
                Directory.CreateDirectory(outPath);
            }

            if (obj["ids"] == null)
            {
                return;
            }

            JArray ids = (JArray)obj["ids"];
            string wherein = string.Empty;

            for (int i = 0; i < ids.Count; i++)
            {
                if (i > 0) wherein += ",";
                wherein += ids[i].ToString();
            }

            string sql = string.Format("select * from ord_weekorder where id in ({0})", wherein);

            DataTable dt = DataHelper.Query(sql);

            List<FileInfo> f_summons = new List<FileInfo>();
            List<FileInfo> f_pls = new List<FileInfo>();
            List<string> f_out = new List<string>();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string p_code = dt.Rows[i]["p_code"].ToString() + dt.Rows[i]["o_version"].ToString();
                f_summons.Add(new FileInfo(Path.Combine(basePath, "SUMMONS", p_code + ".pdf")));
            }

            string summonsFile = Path.Combine(outPath, "SUMMONS.pdf");
            SpireHelper.Merge(2, 1, f_summons.ToArray(), Path.Combine(outPath, "SUMMONS.pdf"), true);

            f_out.Add(summonsFile);

            SpireHelper.MergeFiles(f_out, outFile);
        }

        
        [HttpGet]
        public string GHAC8KDKB(string no)
        {
            if (no.Length > 2)
            {
                no = no.Substring(0, no.Length - 2);
            }
            //string ddate = "2023-12-12";
            string sql = string.Format(@"select b.veh_model,
                '' lv,
                b.part_name,
                b.product_name_cn,
                b.cust_num,
                b.denso_num,
                planqty order_num,
                b.box_cap,
                a.rec_location r_location,
                DATE_FORMAT(recdate, '%Y/%m/%d') c_date,
                rectime c_time
            from pickupplan a
                left join originalunit b on 
                  a.pn = b.cust_num 
                  and recv_port = a.supplier
                  and (    ( right(a.rec_location,1) = '1' and b.factory = '八千代1厂') 
                        or ( right(a.rec_location,1) = '2' and b.factory = '八千代2厂') 
                        or ( right(a.rec_location,1) = '3' and b.factory = '八千代3厂'))
            where 1 = 1
            and supplier = '81106'
            and no = '{0}' order by pickdate , picktime", no);

            DataTable dt = DataHelper.Query(sql);

            //生成看板list对象
            List<BaQianDaiKB> ListKB = new List<BaQianDaiKB>();
            int sn = 1;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                int count = Convert.ToInt32(dt.Rows[i]["order_num"]) / Convert.ToInt32(dt.Rows[i]["box_cap"]);
                int remainder = Convert.ToInt32(dt.Rows[i]["order_num"]) % Convert.ToInt32(dt.Rows[i]["box_cap"]);
                if (remainder > 0) count++;
                for (int j = 0; j < count; j++)
                {
                    BaQianDaiKB kb = new BaQianDaiKB();
                    kb.veh_model = dt.Rows[i]["veh_model"].ToString();
                    kb.lv = dt.Rows[i]["lv"].ToString();
                    kb.part_name = dt.Rows[i]["part_name"].ToString();
                    kb.product_name_cn = dt.Rows[i]["product_name_cn"].ToString();
                    kb.part_code = dt.Rows[i]["cust_num"].ToString();
                    
                    if(j == count - 1 && remainder > 0)//最后一个看板 取余数
                    {
                        kb.qty = remainder.ToString();
                    }
                    else
                    {
                        kb.qty = Convert.ToInt32(dt.Rows[i]["box_cap"]).ToString();
                    }


                    kb.box_cap = Convert.ToInt32(dt.Rows[i]["box_cap"]).ToString();
                    kb.r_location = dt.Rows[i]["r_location"].ToString();
                    //根据落点获取厂别
                    string lastTwo = kb.r_location.Length >= 2 ? kb.r_location.Substring(kb.r_location.Length - 2) : kb.r_location;
                    string factory = GetFactoryByLocation(lastTwo);
                    kb.factory = factory;
                    kb.c_date = dt.Rows[i]["c_date"].ToString();
                    kb.c_time = dt.Rows[i]["c_time"].ToString();
                    kb.user = "王若云";
                    kb.qrcode =
                        string.Format("{0}{1} {2}{3}",
                        dt.Rows[i]["cust_num"].ToString(),
                        dt.Rows[i]["denso_num"].ToString(),
                        kb.box_cap.ToString().PadLeft(7, '0'),
                        sn.ToString().PadLeft(3,'0')
                        );
                    sn++;

                    ListKB.Add(kb);
                }
            }

            string kbFilePath = SpireHelper.CreateBaQianDaiKB(ListKB);

            return kbFilePath;
        }


        public string GetFactoryByLocation(string localtion) {
            if (localtion == "01")
            {
                return "GHAC1";
            }
            else if (localtion == "02")
            {
                return "GHAC2";
            }
            else if (localtion == "03")
            {
                return "GHAC3";
            }
            else if (localtion == "04")
            {
                return "GHAC4";
            }
            else if (localtion == "09")
            {
                return "GHAC9";
            }
            else if (localtion == "SP")
            {
                return "GHAC SP";
            }
            else {
                return "GHAC";
            }
        }

        [HttpGet]
        public string GHAC8KDT(string no)
        {
            if (no.Length > 2) {
                no = no.Substring(0, no.Length - 2);
            }

            string sql = string.Format(@"
            select LPAD(right(a.rec_location,1), 2, '0') code,
                a.rec_location r_location,
                a.plancode,
                b.cust_num,
                '-' color,
                DATE_FORMAT(recdate, '%m月%d日') c_date,
                rectime c_time,
                -- DATE_FORMAT(pickdate, '%m月%d日') c_date,                
                -- picktime c_time,
                planqty order_num,                
                b.box_cap,
                CEILING(planqty /b.box_cap) box_qty,
                b.pallet_box
            from pickupplan a
                left join originalunit b on 
                  a.pn = b.cust_num 
                  and recv_port = a.supplier
                  and (    ( right(a.rec_location,1) = '1' and b.factory = '八千代1厂') 
                        or ( right(a.rec_location,1) = '2' and b.factory = '八千代2厂') 
                        or ( right(a.rec_location,1) = '3' and b.factory = '八千代3厂'))
            where 1 = 1
            and supplier = '81106'
            and no = '{0}'
             order by pickdate , picktime ", no);

            DataTable dt = DataHelper.Query(sql);

            List<BaQianDaiT> ListT = new List<BaQianDaiT>();

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                // 基础字段
                string code = dt.Rows[i]["code"].ToString();
                string r_location = dt.Rows[i]["r_location"].ToString();
                string part_code = dt.Rows[i]["cust_num"].ToString();
                string color = dt.Rows[i]["color"].ToString();
                string c_date = dt.Rows[i]["c_date"].ToString();
                string c_time = dt.Rows[i]["c_time"].ToString();
                string plancode = dt.Rows[i]["plancode"].ToString();
                int order_num = Convert.ToInt32(dt.Rows[i]["order_num"]);   //总个数
                int box_qty = Convert.ToInt32(dt.Rows[i]["box_qty"]);  // 总箱数
                int box_cap = Convert.ToInt32(dt.Rows[i]["box_cap"]);  // 每箱容量
                int pallet_box = Convert.ToInt32(dt.Rows[i]["pallet_box"]); // 每托盘装多少箱
                                                                            // 总托盘数
                int pallet_count = (int)Math.Ceiling((double)box_qty / pallet_box);

                // 已经分配的总个数
                int used_qty = 0;

                for (int j = 0; j < pallet_count; j++)
                {
                    // 本托盘实际箱数
                    int current_box_qty = (j == pallet_count - 1)
                        ? box_qty - j * pallet_box     // 最后一托
                        : pallet_box;                  // 满托盘

                    // 最大可容纳（理论满箱）
                    int max_full_pallet_qty = current_box_qty * box_cap;

                    // 计算剩余可分配数量
                    int remain = order_num - used_qty;

                    // 本托盘的实际数量（不能超过理论满托，也不能超过剩余）
                    int qty = Math.Min(max_full_pallet_qty, remain);

                    // 更新已使用
                    used_qty += qty;

                    BaQianDaiT T = new BaQianDaiT()
                    {
                        no = $"T{(ListT.Count + 1).ToString().PadLeft(2, '0')}",
                        code = code,
                        plancode = plancode,
                        r_location = r_location,
                        part_code = part_code,
                        color = color,
                        c_date = c_date,
                        c_time = c_time,
                        qty = qty,                   // 每托盘实际个数
                        box_qty = current_box_qty    // 当前托盘箱数
                    };

                    if (T.part_code == "17708TLA A020M1")
                        T.Is2VG = true;

                    ListT.Add(T);
                }
            }



            string TFilePath = SpireHelper.CreateBaQianDaiT(ListT);

            return TFilePath;
        }

        #region GHAC处理方法

        /// <summary>
        ///  如果有sheet货物流运输清单，只处理取货计划
        ///  如果没有，就处理日明细作为订单
        /// </summary>
        /// <param name="filePath"></param>
        public void GHAC_EXCEL(string filePath,string username)
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook;
                ISheet sheet;

                StringBuilder sql = new StringBuilder();

                // 判断是 .xls 还是 .xlsx 文件
                if (filePath.EndsWith(".xlsx"))
                {
                    workbook = new XSSFWorkbook(fileStream);
                }
                else if (filePath.EndsWith(".xls"))
                {
                    workbook = new HSSFWorkbook(fileStream);
                }
                else
                {
                    throw new ArgumentException("文件格式不支持，仅支持 .xls 或 .xlsx");
                }

                #region 判断八千代

                sheet = workbook.GetSheetAt(0);
                if (sheet != null)
                {
                    IRow row = sheet.GetRow(0);

                    if (row != null)
                    {
                        if (row.GetCell(2).ToString() == "八千代" || row.GetCell(2).ToString() == "阶段品" || row.GetCell(1).ToString() == "取货物流申请单")
                        {
                            object obj = new
                            {
                                filePath = filePath,
                                fileType = "PLAN2",
                                username = username
                            };

                            GHACUpload(JObject.Parse(JsonConvert.SerializeObject(obj)));

                            return;
                        }
                    }
                }

                #endregion


                #region 处理出货计划
                sheet = null;

                // 遍历所有 Sheet 获取日明细
                for (int i = 0; i < workbook.NumberOfSheets; i++)
                {

                    if (workbook.GetSheetName(i) == "取货物流运输清单")
                    {
                        sheet = workbook.GetSheetAt(i);
                        break;
                    }
                }

                if (sheet != null)
                {
                    Dictionary<string, string> RemoveNo = new Dictionary<string, string>();
                    Dictionary<string, string> SuppliersNo = new Dictionary<string, string>();

                    sql.Append("insert into pickupplan (");
                    sql.Append("no,");
                    sql.Append("version,");
                    sql.Append("carno,");
                    sql.Append("line,");
                    sql.Append("supplier,");
                    sql.Append("suppler_sub,");
                    sql.Append("factory,");
                    sql.Append("plancode,");
                    sql.Append("transcode,");
                    sql.Append("pickdate,");
                    sql.Append("picktime,");
                    sql.Append("p_code,");
                    sql.Append("p_version,");
                    sql.Append("rec_location,");
                    sql.Append("squre,");
                    sql.Append("vertical,");
                    sql.Append("pn,");
                    sql.Append("color,");
                    sql.Append("recdate,");
                    sql.Append("rectime,");
                    sql.Append("planqty,");
                    sql.Append("palletno,");
                    sql.Append("packcode,");
                    sql.Append("packunit,");
                    sql.Append("packqty,");
                    sql.Append("containers,");
                    sql.Append("createPerson,");
                    sql.Append("create_date)");
                    sql.Append("       values");

                    for (int i = 0; i < sheet.LastRowNum; i++)
                    {
                        if (i > 0)
                            sql.Append(",");
                        IRow row = sheet.GetRow(i + 1);
                        sql.Append(string.Format(" ('{0}',	'{1}',	'{2}',	'{3}',	'{4}',	'{5}',	'{6}',	'{7}',	'{8}',	'{9}',	'{10}',	'{11}',	'{12}',	'{13}',	'{14}',	'{15}',	'{16}',	'{17}','{18}','{19}',	'{20}','{21}','{22}','{23}','{24}','{25}','{26}','{27}')",
                            //row.GetCell(0).StringCellValue.Trim(),
                            //row.GetCell(1).StringCellValue.Trim(),
                            ////row.GetCell(2).StringCellValue.Trim(),

                            //row.GetCell(3).StringCellValue.Trim(),
                            //row.GetCell(4).StringCellValue.Trim(),
                            //row.GetCell(5).StringCellValue.Trim(),
                            //row.GetCell(6).StringCellValue.Trim(),
                            //row.GetCell(7).StringCellValue.Trim(),
                            //row.GetCell(8).ToString().Trim(),
                            ////DateTime.ParseExact(row.GetCell(9).StringCellValue, "yyyyMMdd", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd"),
                            //GetDateStr(row.GetCell(9)),
                            //row.GetCell(10).ToString().Trim(),
                            //row.GetCell(11).ToString().Trim(),
                            //row.GetCell(12).ToString().Trim(),
                            //row.GetCell(13).ToString().Trim(),
                            //row.GetCell(14).ToString().Trim(),
                            //row.GetCell(15).ToString().Trim(),
                            //row.GetCell(16).ToString().Trim(),
                            //row.GetCell(17).ToString().Trim(),
                            ////DateTime.ParseExact(row.GetCell(18).StringCellValue, "yyyyMMdd", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd"),
                            //GetDateStr(row.GetCell(18)),
                            //row.GetCell(19).ToString().Trim(),
                            //row.GetCell(20).ToString().Trim(),
                            //row.GetCell(21).ToString().Trim(),
                            //row.GetCell(22).ToString().Trim(),
                            //row.GetCell(23).ToString().Trim(),
                            //(double.Parse(row.GetCell(20).ToString().Trim()) / double.Parse(row.GetCell(23).ToString().Trim())),

                            ////row.GetCell(24).ToString().Trim(),
                            //row.GetCell(25).ToString().Trim(),
                            //username,
                            //DateTime.Now
                            GetCellStr(row.GetCell(0)),
        GetCellStr(row.GetCell(1)),
        GetCellStr(row.GetCell(2)),   // ← 之前直接用 .StringCellValue 在你这份文件里就是这里崩的
        GetCellStr(row.GetCell(3)),
        GetCellStr(row.GetCell(4)),
        GetCellStr(row.GetCell(5)),
        GetCellStr(row.GetCell(6)),
        GetCellStr(row.GetCell(7)),
        GetCellStr(row.GetCell(8)),
        GetDateStr(row.GetCell(9)),
        GetCellStr(row.GetCell(10)),
        GetCellStr(row.GetCell(11)),
        GetCellStr(row.GetCell(12)),
        GetCellStr(row.GetCell(13)),
        GetCellStr(row.GetCell(14)),
        GetCellStr(row.GetCell(15)),
        GetCellStr(row.GetCell(16)),
        GetCellStr(row.GetCell(17)),
        GetDateStr(row.GetCell(18)),
        GetCellStr(row.GetCell(19)),
        GetCellStr(row.GetCell(20)),
        GetCellStr(row.GetCell(21)),
        GetCellStr(row.GetCell(22)),
        GetCellStr(row.GetCell(23)),
        (double.Parse(GetCellStr(row.GetCell(20))) / double.Parse(GetCellStr(row.GetCell(23)))),

        //GetCellStr(row.GetCell(24)),
        GetCellStr(row.GetCell(25)),
        username,
        DateTime.Now

                            ));
                        if (!RemoveNo.ContainsKey(row.GetCell(0).StringCellValue.Trim()))
                        {
                            RemoveNo.Add(row.GetCell(0).StringCellValue.Trim(), string.Empty);
                        }
                        if (!SuppliersNo.ContainsKey(row.GetCell(4).StringCellValue.Trim())) {

                            SuppliersNo.Add(row.GetCell(4).StringCellValue.Trim(), string.Empty);
                        }
                    }
                    string deleteNo = string.Empty;
                    string supplyNo = string.Empty;
                    foreach (var item in RemoveNo)
                    {
                        deleteNo += string.Format("'{0}',", item.Key);
                    }
                    foreach (var item in SuppliersNo)
                    {
                        supplyNo += string.Format("'{0}',", item.Key);
                    }


                    deleteNo = deleteNo.Substring(0, deleteNo.Length - 1);
                    supplyNo = supplyNo.Substring(0, supplyNo.Length - 1);

                    string sql_del = string.Format("delete from pickupplan where no in ({0}) and supplier in ({1})", deleteNo,supplyNo);

                    DataHelper.ExecuteSql(sql_del);
                    int n = DataHelper.ExecuteSql(sql.ToString());

                    //return;
                }
                #endregion

                #region 处理日明细
                sheet = null;

                // 遍历所有 Sheet 获取日明细
                for (int i = 0; i < workbook.NumberOfSheets; i++)
                {

                    if (workbook.GetSheetName(i) == "日明细")
                    {
                        sheet = workbook.GetSheetAt(i);
                        break;
                    }
                }

                if (sheet != null)
                {

                    Dictionary<string, string> RemoveNo = new Dictionary<string, string>();
                    Dictionary<string, string> SuppliersNo = new Dictionary<string, string>();
                    int lastDataRow = 0;

                    for (int i = sheet.LastRowNum; i >= 0; i--)
                    {
                        IRow row = sheet.GetRow(i);
                        if (row == null) continue;

                        var cell = row.GetCell(10);
                        if (cell != null && !string.IsNullOrWhiteSpace(cell.ToString()))
                        {
                            lastDataRow = i;
                            break;
                        }
                    }
                    StringBuilder sqldetail = new StringBuilder();

                    sqldetail.Append("insert into ord_weekorder	(	");
                    sqldetail.Append("o_number	,");    //序号
                    sqldetail.Append("sup_code	,");    //供应商编号
                    sqldetail.Append("r_location	,");    //收货地点
                    sqldetail.Append("part_code	,");    //零件号
                    sqldetail.Append("part_color	,");    //零件颜色
                    sqldetail.Append("part_name	,");    //零件名称
                    sqldetail.Append("plan_code	,");    //计划代码
                    sqldetail.Append("c_date	,");        //收货日期
                    sqldetail.Append("c_time	,");        //收货时间
                    sqldetail.Append("order_num	,");    //	订货数量
                    sqldetail.Append("p_code	,");        //传票号码
                    sqldetail.Append("o_version	,");    //	版本
                    sqldetail.Append("o_week	,");        // 周次
                    sqldetail.Append("o_class	,");        // 分类
                    sqldetail.Append("rule	,");        // 交货规则
                    sqldetail.Append("remarks	,");        // 备注
                    sqldetail.Append("order_code	,");    //订单编号
                    sqldetail.Append("carmodel");         //车型
                    sqldetail.Append(")");
                    sqldetail.Append("       values");

                    for (int i = 0; i < lastDataRow; i++)
                    {
                        if (i > 0)
                            sqldetail.Append(",");
                        IRow row = sheet.GetRow(i + 1);
                        sqldetail.Append(string.Format(" ('{0}',	'{1}',	'{2}',	'{3}',	'{4}',	'{5}',	'{6}',	'{7}',	'{8}',	'{9}',	'{10}',	'{11}',	'{12}',	'{13}',	'{14}',	'{15}',	'{16}',	'{17}')",
                            row.GetCell(0).StringCellValue,
                            row.GetCell(1).StringCellValue,
                            row.GetCell(2).StringCellValue,
                            row.GetCell(3).StringCellValue,
                            row.GetCell(4).StringCellValue,
                            row.GetCell(5).StringCellValue,
                            row.GetCell(6).StringCellValue,
                            row.GetCell(7).DateCellValue.ToString("yyyy-MM-dd"),
                            row.GetCell(8).ToString(),
                            row.GetCell(9).ToString(),
                            row.GetCell(10).ToString(),
                            row.GetCell(11).ToString(),
                            row.GetCell(12).ToString(),
                            row.GetCell(13).ToString(),
                            row.GetCell(14).ToString(),
                            row.GetCell(15).ToString(),
                            "",//row.GetCell(16).ToString(),
                            ""//row.GetCell(17).ToString()
                            ));

                        string p_code = row.GetCell(10).ToString();
                        if (!RemoveNo.ContainsKey(p_code))
                            RemoveNo.Add(p_code, string.Empty);
                    }

                    string deleteNo = string.Empty;

                    foreach (var item in RemoveNo)
                    {
                        deleteNo += string.Format("'{0}',", item.Key);
                    }
                    deleteNo = deleteNo.Substring(0, deleteNo.Length - 1);
                    string sql_del = string.Format("delete from ord_weekorder where p_code in ({0})", deleteNo);

                    DataHelper.ExecuteSql(sql_del);

                    int n = DataHelper.ExecuteSql(sqldetail.ToString());
                }
                #endregion


            }
        }



        private string GetCellStr(ICell cell)
        {
            if (cell == null) return string.Empty;
            switch (cell.CellType)
            {
                case CellType.String:
                    return cell.StringCellValue.Trim();
                case CellType.Numeric:
                    if (DateUtil.IsCellDateFormatted(cell))
                        return cell.DateCellValue.ToString("yyyy-MM-dd"); // 注意：这里已经是日期了
                    return cell.NumericCellValue.ToString();
                case CellType.Blank:
                    return string.Empty;
                case CellType.Formula:
                    return cell.CachedFormulaResultType == CellType.String
                        ? cell.StringCellValue.Trim()
                        : cell.NumericCellValue.ToString();
                default:
                    return cell.ToString().Trim();
            }
        }


        private string GetDateStr(ICell cell)
        {
            if (cell == null) throw new Exception("日期列为空");
            if (cell.CellType == CellType.Numeric)
            {
                if (DateUtil.IsCellDateFormatted(cell))
                    return cell.DateCellValue.ToString("yyyy-MM-dd");
                // 数值但不是日期格式，比如存的是 20231005 这种数字
                return DateTime.ParseExact(((long)cell.NumericCellValue).ToString(),
                    "yyyyMMdd", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd");
            }
            return DateTime.ParseExact(cell.StringCellValue.Trim(),
                "yyyyMMdd", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd");
        }
        public void GHAC_ORDER(string filePath)
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook;
                ISheet sheet;

                StringBuilder sql = new StringBuilder();

                // 判断是 .xls 还是 .xlsx 文件
                if (filePath.EndsWith(".xlsx"))
                {
                    workbook = new XSSFWorkbook(fileStream);
                }
                else if (filePath.EndsWith(".xls"))
                {
                    workbook = new HSSFWorkbook(fileStream);
                }
                else
                {
                    throw new ArgumentException("文件格式不支持，仅支持 .xls 或 .xlsx");
                }

                sheet = workbook.GetSheetAt(0);

                // 遍历所有 Sheet 获取日明细
                for (int i = 0; i < workbook.NumberOfSheets; i++)
                {

                    if (workbook.GetSheetName(i) == "日明细")
                    {
                        sheet = workbook.GetSheetAt(i);
                        break;
                    }
                }

                sql.Append("insert into ord_weekorder	(	");
                sql.Append("o_number	,");    //序号
                sql.Append("sup_code	,");    //供应商编号
                sql.Append("r_location	,");    //收货地点
                sql.Append("part_code	,");    //零件号
                sql.Append("part_color	,");    //零件颜色
                sql.Append("part_name	,");    //零件名称
                sql.Append("plan_code	,");    //计划代码
                sql.Append("c_date	,");        //收货日期
                sql.Append("c_time	,");        //收货时间
                sql.Append("order_num	,");    //	订货数量
                sql.Append("p_code	,");        //传票号码
                sql.Append("o_version	,");    //	版本
                sql.Append("o_week	,");        // 周次
                sql.Append("o_class	,");        // 分类
                sql.Append("rule	,");        // 交货规则
                sql.Append("remarks	,");        // 备注
                sql.Append("order_code	,");    //订单编号
                sql.Append("carmodel");         //车型
                sql.Append(")");
                sql.Append("       values");

                for (int i = 0; i < sheet.LastRowNum; i++)
                {
                    if (i > 0)
                        sql.Append(",");
                    IRow row = sheet.GetRow(i + 1);
                    sql.Append(string.Format(" ('{0}',	'{1}',	'{2}',	'{3}',	'{4}',	'{5}',	'{6}',	'{7}',	'{8}',	'{9}',	'{10}',	'{11}',	'{12}',	'{13}',	'{14}',	'{15}',	'{16}',	'{17}')",
                        row.GetCell(0).StringCellValue,
                        row.GetCell(1).StringCellValue,
                        row.GetCell(2).StringCellValue,
                        row.GetCell(3).StringCellValue,
                        row.GetCell(4).StringCellValue,
                        row.GetCell(5).StringCellValue,
                        row.GetCell(6).StringCellValue,
                        row.GetCell(7).DateCellValue.ToString("yyyy-MM-dd"),
                        row.GetCell(8).ToString(),
                        row.GetCell(9).ToString(),
                        row.GetCell(10).ToString(),
                        row.GetCell(11).ToString(),
                        row.GetCell(12).ToString(),
                        row.GetCell(13).ToString(),
                        row.GetCell(14).ToString(),
                        row.GetCell(15).ToString(),
                        "",//row.GetCell(16).ToString(),
                        ""//row.GetCell(17).ToString()
                        ));
                }

                int n = DataHelper.ExecuteSql(sql.ToString());
            }
        }
        public void GHAC_SUMMONS(string filePath)
        {
            int startPage = SpireHelper.GetStartPage(filePath, "送货传票");
            string output = HttpContext.Current.Server.MapPath("~/");
            output += "UploadFiles\\GHAC\\SUMMONS";
            SpireHelper.PdfSplit(
                filePath, output, startPage, 2, 1,
                SpireSplit.EnumNameRule.Customize,
                new Rectangle(50, 210, 162, 45),
                new Rectangle());
        }
        public void GHAC_PALLET(string filePath)
        {
            string output = HttpContext.Current.Server.MapPath("~/");
            output += "UploadFiles\\GHAC\\PALLET";
            SpireHelper.PdfSplit(
                filePath, output, 0, 1, 1,
                SpireSplit.EnumNameRule.CustomizeAuto,
                new Rectangle(0, 0, 450, 75),
                new Rectangle());
        }
        public void GHAC_PLS(string filePath)
        {
            string output = HttpContext.Current.Server.MapPath("~/");
            output += "UploadFiles\\GHAC\\PLS";
            Rectangle[] rectangles = new Rectangle[3];
            rectangles[0] = new Rectangle(90, 48, 100, 8);
            rectangles[1] = new Rectangle(140, 170, 70, 14);
            rectangles[2] = new Rectangle(252, 28, 16, 20);

            //暂时注掉，不知道这里有什么用  
            //if (SpireHelper.GetStartPage(filePath, "81106") > -1)
            //{
            //    //八千代看板
            //    SpireHelper.PdfSplit(
            //    filePath, output, 0, 3, 2,
            //    SpireSplit.EnumNameRule.Customize,
            //    new List<Rectangle>() { new Rectangle(90, 150, 150, 20), new Rectangle(90, 200, 100, 20) }.ToArray(),
            //    new Rectangle(11, 35, 562, 740), true);
            //}
            //else
            //{

            SpireHelper.PdfSplit(
                filePath, output, 0, 3, 2,
                SpireSplit.EnumNameRule.CustomizeAuto,
                rectangles,
                new Rectangle());
            //}



        }
        public void GHAC_PLAN(string filePath)
        {
            List<string> sheetNames = new List<string>();

            Dictionary<string, string> RemoveNo = new Dictionary<string, string>();

            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook;
                ISheet sheet;

                StringBuilder sql = new StringBuilder();

                // 判断是 .xls 还是 .xlsx 文件
                if (filePath.EndsWith(".xlsx"))
                {
                    workbook = new XSSFWorkbook(fileStream);
                }
                else if (filePath.EndsWith(".xls"))
                {
                    workbook = new HSSFWorkbook(fileStream);
                }
                else
                {
                    throw new ArgumentException("文件格式不支持，仅支持 .xls 或 .xlsx");
                }

                sheet = workbook.GetSheetAt(0);

                // 遍历所有 Sheet 获取日明细
                for (int i = 0; i < workbook.NumberOfSheets; i++)
                {

                    if (workbook.GetSheetName(i) == "取货物流运输清单")
                    {
                        sheet = workbook.GetSheetAt(i);
                        break;
                    }
                }

                sql.Append("insert into pickupplan (");
                sql.Append("no,");
                sql.Append("version,");
                sql.Append("carno,");
                sql.Append("line,");
                sql.Append("supplier,");
                sql.Append("suppler_sub,");
                sql.Append("factory,");
                sql.Append("plancode,");
                sql.Append("transcode,");
                sql.Append("pickdate,");
                sql.Append("picktime,");
                sql.Append("p_code,");
                sql.Append("p_version,");
                sql.Append("rec_location,");
                sql.Append("squre,");
                sql.Append("vertical,");
                sql.Append("pn,");
                sql.Append("color,");
                sql.Append("recdate,");
                sql.Append("rectime,");
                sql.Append("planqty,");
                sql.Append("palletno,");
                sql.Append("packcode,");
                sql.Append("packunit,");
                sql.Append("packqty,");
                sql.Append("containers,");
                sql.Append("create_date)");
                sql.Append("       values");

                for (int i = 0; i < sheet.LastRowNum; i++)
                {
                    if (i > 0)
                        sql.Append(",");
                    IRow row = sheet.GetRow(i + 1);
                    sql.Append(string.Format(" ('{0}',	'{1}',	'{2}',	'{3}',	'{4}',	'{5}',	'{6}',	'{7}',	'{8}',	'{9}',	'{10}',	'{11}',	'{12}',	'{13}',	'{14}',	'{15}',	'{16}',	'{17}','{18}','{19}','{20}',	'{20}','{21}','{22}','{23}','{24}','{25}')",
                        row.GetCell(0).StringCellValue.Trim(),
                        row.GetCell(1).StringCellValue.Trim(),
                        row.GetCell(2).StringCellValue.Trim(),
                        row.GetCell(3).StringCellValue.Trim(),
                        row.GetCell(4).StringCellValue.Trim(),
                        row.GetCell(5).StringCellValue.Trim(),
                        row.GetCell(6).StringCellValue.Trim(),
                        row.GetCell(7).StringCellValue.Trim(),
                        row.GetCell(8).ToString().Trim(),
                        DateTime.ParseExact(row.GetCell(9).StringCellValue, "yyyyMMdd", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd"),
                        row.GetCell(10).ToString().Trim(),
                        row.GetCell(11).ToString().Trim(),
                        row.GetCell(12).ToString().Trim(),
                        row.GetCell(13).ToString().Trim(),
                        row.GetCell(14).ToString().Trim(),
                        row.GetCell(15).ToString().Trim(),
                        row.GetCell(16).ToString().Trim(),
                        row.GetCell(17).ToString().Trim(),
                        DateTime.ParseExact(row.GetCell(18).StringCellValue, "yyyyMMdd", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd"),
                        row.GetCell(19).ToString().Trim(),
                        row.GetCell(20).ToString().Trim(),
                        row.GetCell(21).ToString().Trim(),
                        row.GetCell(22).ToString().Trim(),
                        row.GetCell(23).ToString().Trim(),
                        row.GetCell(24).ToString().Trim(),
                        row.GetCell(25).ToString().Trim(),
                        DateTime.Now

                        ));
                    if (!RemoveNo.ContainsKey(row.GetCell(0).StringCellValue.Trim()))
                    {
                        RemoveNo.Add(row.GetCell(0).StringCellValue.Trim(), string.Empty);
                    }
                }
                string deleteNo = string.Empty;

                foreach (var item in RemoveNo)
                {
                    deleteNo += string.Format("'{0}',", item.Key);
                }
                deleteNo = deleteNo.Substring(0, deleteNo.Length - 1);
                string sql_del = string.Format("delete from pickupplan where no in ({0})", deleteNo);

                DataHelper.ExecuteSql(sql_del);
                int n = DataHelper.ExecuteSql(sql.ToString());


            }
        }
        public void GHAC_PLAN2(string filePath,string username)
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook;
                ISheet sheet;


                // 判断是 .xls 还是 .xlsx 文件
                if (filePath.EndsWith(".xlsx"))
                {
                    workbook = new XSSFWorkbook(fileStream);
                }
                else if (filePath.EndsWith(".xls"))
                {
                    workbook = new HSSFWorkbook(fileStream);
                }
                else
                {
                    throw new ArgumentException("文件格式不支持，仅支持 .xls 或 .xlsx");
                }
                sheet = workbook.GetSheetAt(0);


                IRow row;
                string planType = "";
                //判断受入口
                row = sheet.GetRow(0);
                if (row == null || row.GetCell(2) == null || row.GetCell(2).ToString() == "")
                {
                    planType = "SP";
                }
                else
                {
                    planType = row.GetCell(2).ToString();
                }

                if (planType == "SP")
                {

                    StringBuilder sql = new StringBuilder();

                    sql.Append(@"insert into pickupplan (version,no,line,supplier,pickdate,picktime,p_code,p_version,pn,recdate,rectime,factory,create_date,createPerson) values ");

                    string sql_delete = "delete from pickupplan where p_code in ({0})";

                    Dictionary<string, string> deletePcode = new Dictionary<string, string>();

                    //取表头信息
                    row = sheet.GetRow(2);
                    DateTime pickdate = row.GetCell(3).DateCellValue;
                    string picktime = row.GetCell(5).DateCellValue.ToString("HH:mm"); ;
                    row = sheet.GetRow(4);
                    string line = row.GetCell(3).ToString();


                    //取列表 
                    int startIndex = 11;

                    for (int i = 0; i <= sheet.LastRowNum; i++)  // 注意 <= 遍历到最后一行
                    {
                        IRow rowi = sheet.GetRow(i);
                        if (rowi == null) continue;   // 跳过空行

                        ICell cell = rowi.GetCell(1);
                        if (cell == null) continue;  // 跳过空单元格

                        string cellValue = cell.ToString().Trim(); // 转成字符串
                        if (cellValue == "1")
                        {
                            startIndex = i;
                            break; // 如果只要第一个匹配，加个 break
                        }
                    }

                    int indexi = startIndex ;
                    string factory = "SP" + sheet.GetRow(3).GetCell(11).ToString();
                    while (sheet.GetRow(startIndex) != null
                        && sheet.GetRow(startIndex).GetCell(2) != null
                        && sheet.GetRow(startIndex).GetCell(2).ToString() != string.Empty)
                    {
                        if (startIndex > indexi) sql.Append(",");
                        row = sheet.GetRow(startIndex);
                        string version = "0000" + row.GetCell(1).ToString();
                        version = version.Substring(version.Length - 4);
                        string supplier = row.GetCell(2).ToString();
                        string p_code = row.GetCell(3).ToString();
                        string pn = row.GetCell(5).ToString();
                        string recdate = row.GetCell(7).DateCellValue.ToString("yyyy-MM-dd");
                        string rectime = row.GetCell(8).ToString();
                        string no = pickdate.ToString("yyyyMMdd") + "SP" + supplier;

                        sql.Append(string.Format(@" ('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}') ",
                            version, no, line, supplier, pickdate.ToString("yyyy-MM-dd"), picktime, p_code, "00", pn, recdate, rectime,factory,DateTime.Now,username
                            ));

                        if (!deletePcode.ContainsKey(p_code))
                            deletePcode.Add(p_code, string.Empty);

                        startIndex++;
                    }

                    string deletePcodes = string.Empty;
                    foreach (var item in deletePcode)
                    {
                        deletePcodes += string.Format("'{0}',", item.Key);
                    }
                    deletePcodes = deletePcodes.Substring(0, deletePcodes.Length - 1);

                    sql_delete = string.Format(sql_delete, deletePcodes);

                    DataHelper.ExecuteSql(sql_delete);
                    XErrorLogToolMDM.WriteLog(sql_delete);

                    DataHelper.ExecuteSql(sql.ToString());
                    XErrorLogToolMDM.WriteLog(sql.ToString());
                }
                else
                {
                    StringBuilder sql = new StringBuilder();

                    sql.Append("insert into pickupplan (no,version,supplier,rec_location,pn,color,plancode,recdate,rectime,planqty,p_code,p_version,weeks,category,roles,remark,createPerson,create_date,pickdate)  values ");

                    string sql_delete = "delete from pickupplan where p_code in ({0})";

                    Dictionary<string, string> deletePcode = new Dictionary<string, string>();

                    //取表头信息
                    row = sheet.GetRow(0);
                    DateTime pickdate = row.GetCell(5).DateCellValue;

                    //取列表 
                    int startIndex = 3;
                    string no = $"{pickdate:yyyyMMdd}{planType}{DateTime.Now:HHmmssfff}";
                    while (sheet.GetRow(startIndex) != null
                        && sheet.GetRow(startIndex).GetCell(1) != null
                        && sheet.GetRow(startIndex).GetCell(1).ToString() != string.Empty)
                    {
                        if (startIndex > 3) sql.Append(",");
                        row = sheet.GetRow(startIndex);
                        string p_code = row.GetCell(10).ToString();
                        //string no = pickdate.ToString("yyyyMMdd") + planType ;
                        

                        sql.Append(string.Format(@" ('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}','{14}','{15}','{16}','{17}','{18}') ",
                            no,
                            row.GetCell(0).ToString(),
                            row.GetCell(1).ToString(),
                            row.GetCell(2).ToString(),
                            row.GetCell(3).ToString(),
                            row.GetCell(4).ToString(),
                            row.GetCell(6).ToString(),
                            row.GetCell(7).DateCellValue.ToString("yyyy-MM-dd"),
                            row.GetCell(8).ToString(),
                            row.GetCell(9).ToString(),
                            row.GetCell(10).ToString(),
                            row.GetCell(11).ToString(),
                            row.GetCell(12).ToString(),
                            row.GetCell(13).ToString(),
                            row.GetCell(14).ToString(),
                            row.GetCell(15).ToString(),
                            username,
                            DateTime.Now,
                            pickdate.ToString("yyyy-MM-dd")
                            ));

                        if (!deletePcode.ContainsKey(p_code))
                            deletePcode.Add(p_code, string.Empty);

                        startIndex++;
                    }

                    string deletePcodes = string.Empty;
                    foreach (var item in deletePcode)
                    {
                        deletePcodes += string.Format("'{0}',", item.Key);
                    }
                    deletePcodes = deletePcodes.Substring(0, deletePcodes.Length - 1);

                    sql_delete = string.Format(sql_delete, deletePcodes);

                    DataHelper.ExecuteSql(sql_delete);

                    DataHelper.ExecuteSql(sql.ToString());
                }
            }
        }
        public void GHAC_INSTRUCTIONS(string filePath)
        {
            string tempID = Guid.NewGuid().ToString();
            string output = HttpContext.Current.Server.MapPath("~/");
            output += "UploadFiles\\GHAC\\INSTRUCTIONS\\" + tempID;
            SpireHelper.PdfSplit(
                filePath, output, SpireHelper.GetStartPage(filePath, "取货指示书"), 1, 1,
                SpireSplit.EnumNameRule.Customize,
               new List<Rectangle>() { new Rectangle(40, 28, 100, 20) }.ToArray(),
                new Rectangle(), true);

            DirectoryInfo dir = new DirectoryInfo(output);

            List<FileInfo> fileAll = dir.GetFiles().ToList();

            Dictionary<string, List<FileInfo>> dictFiles = new Dictionary<string, List<FileInfo>>();

            for (int i = 0; i < fileAll.Count; i++)
            {
                //25719101315000_0000
                string billno = Path.GetFileNameWithoutExtension(fileAll[i].Name);   
                if (billno.IndexOf("_") > -1)
                    billno = billno.Substring(0, billno.IndexOf("_"));
                if (!dictFiles.ContainsKey(billno))   //25719101315000
                    dictFiles.Add(billno, new List<FileInfo>());
                dictFiles[billno].Add(fileAll[i]);
            }

            foreach (var item in dictFiles)
            {
                string output2 = HttpContext.Current.Server.MapPath("~/");
                output2 += "UploadFiles\\GHAC\\INSTRUCTIONS\\" + item.Key + ".pdf";

                SpireHelper.Merge(1, 1, item.Value.ToArray(), output2);
            }

            Directory.Delete(output, true);



        }

        public void GHAC_01(string filePath)
        {
            int startPageSUMMONS = SpireHelper.GetStartPage(filePath, "送货传票");
            int startPageINSTRUCTIONS = SpireHelper.GetStartPage(filePath, "取货指示书");
            string output = HttpContext.Current.Server.MapPath("~/");
            string outputSUMMONS  = output + "UploadFiles\\GHAC\\SUMMONS";
            string outputINSTRUCTIONS = output + "UploadFiles\\GHAC\\INSTRUCTIONS";
            //有新版传票
            if (startPageSUMMONS != -1) {
                //判断是否有新版指示书 如果有 结束页为 指示书开始页-1
                if (startPageINSTRUCTIONS != -1)
                {
                    SpireHelper.PdfSplitEndPage(
                    filePath, outputSUMMONS, startPageSUMMONS, startPageINSTRUCTIONS-1,2, 1,
                    SpireSplit.EnumNameRule.Customize,
                    new Rectangle(50, 210, 162, 45),
                    new Rectangle());

                }
                else {
                    SpireHelper.PdfSplit(
                    filePath, outputSUMMONS, startPageSUMMONS, 2, 1,
                    SpireSplit.EnumNameRule.Customize,
                    new Rectangle(50, 210, 162, 45),
                    new Rectangle());
                } 
            }
            if (startPageINSTRUCTIONS != -1) {
                string tempID = Guid.NewGuid().ToString();
                string tempInstructions = Path.Combine(outputINSTRUCTIONS, tempID);
                SpireHelper.PdfSplit(
                    filePath, tempInstructions, startPageINSTRUCTIONS, 1, 1,
                    SpireSplit.EnumNameRule.Customize,
                   new List<Rectangle>() { new Rectangle(40, 28, 100, 20) }.ToArray(),
                    new Rectangle(), true);

                DirectoryInfo dir = new DirectoryInfo(tempInstructions);

                List<FileInfo> fileAll = dir.GetFiles().ToList();

                Dictionary<string, List<FileInfo>> dictFiles = new Dictionary<string, List<FileInfo>>();

                for (int i = 0; i < fileAll.Count; i++)
                {
                    //25719101315000_0000
                    string billno = Path.GetFileNameWithoutExtension(fileAll[i].Name);
                    if (billno.IndexOf("_") > -1)
                        billno = billno.Substring(0, billno.IndexOf("_"));
                    if (!dictFiles.ContainsKey(billno))   //25719101315000
                        dictFiles.Add(billno, new List<FileInfo>());
                    dictFiles[billno].Add(fileAll[i]);
                }

                foreach (var item in dictFiles)
                {
                    string output2 = Path.Combine(outputINSTRUCTIONS, item.Key + ".pdf");
                    SpireHelper.Merge(1, 1, item.Value.ToArray(), output2);
                }

                if (Directory.Exists(tempInstructions))
                {
                    Directory.Delete(tempInstructions, true);
                }

            }


        }


        public void GHAC_TRACE(string filePath)
        {
            string output = HttpContext.Current.Server.MapPath("~/");
            output += "UploadFiles\\GHAC\\TRACE";
            Rectangle[] rectangles = new Rectangle[4];
            rectangles[0] = new Rectangle(78, 59, 220, 22);    //零件号
            rectangles[1] = new Rectangle(78, 176, 120, 18);   //计划到货日期
            rectangles[2] = new Rectangle(284, 258, 40, 20);   //落点

            rectangles[3] = new Rectangle(163,130,40,20);      // 顺序号
            PdfSplit(
                filePath, output, 0, 2, 2,
                SpireSplit.EnumNameRule.Customize,
                rectangles,
                new Rectangle(),
                false,
                false,
                true);

        }

        /// <summary>
        /// 输出传票
        /// </summary>
        /// <param name="InstructionCode"></param>
        /// <returns></returns>
        public string GHAC_SUMMONS_OUT(string InstructionCode, enumOutType outType,string supplyCode)
        {
            //查询数据库
            string sql = string.Empty;

            if (outType == enumOutType.HAOKOU)
            {
                sql = string.Format(
                    @"select DISTINCT  carno,rec_location,pn, concat(recdate,' ' ,rectime),p_code,p_version from pickupplan
                        where concat(no,version) = '{0}'
                        and supplier = '{1}'
                        and pn in ('17708T5R A020M1','17708T51 H012M1')
                        order by p_code", InstructionCode,supplyCode);
                XErrorLogToolMDM.WriteLog("号口查找传票" + sql);
            }
            else if (outType == enumOutType.Other)
            {
                sql = string.Format(
                @"
                SELECT version, rec_location, pn,
       CONCAT(recdate, ' ', rectime) AS rec_datetime,
       p_code, p_version, planqty
FROM (
    SELECT *
    FROM pickupplan
    WHERE CONCAT(no, '00') = '{0}'
    ORDER BY id
) t
GROUP BY version, rec_location, pn,
         recdate, rectime, p_code, p_version, planqty;", InstructionCode);
            }

            DataTable dt = DataHelper.Query(sql);

            if (dt.Rows.Count == 0) { return ""; }

            List<FileInfo> F_summons = new List<FileInfo>();

            //基础输出路径
            string basePath = HttpContext.Current.Server.MapPath("~/UploadFiles/GHAC");
            //临时输出路径
            string outPath = Path.Combine(basePath, string.Format("{0}\\{1}\\{2}", "output", InstructionCode, Guid.NewGuid().ToString()));
            //最终输出路径
            string F_summons_outpath = Path.Combine(outPath, "summons.pdf");

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                //输出传票
                string file_summons = Path.Combine(basePath, "SUMMONS", dt.Rows[i]["p_code"].ToString() + dt.Rows[i]["p_version"].ToString() + ".pdf");
                F_summons.Add(new FileInfo(file_summons));
            }

            SpireHelper.Merge(2, 1, F_summons.ToArray(), F_summons_outpath, true,0,0,true);

            return F_summons_outpath;
        }
        /// <summary>
        /// 输出托标签
        /// </summary>
        /// <param name="InstructionCode"></param>
        /// <param name="outType"></param>
        /// <returns></returns>
        public string GHAC_PALLET_OUT(string InstructionCode, enumOutType outType)
        {
            //基础输出路径
            string basePath = HttpContext.Current.Server.MapPath("~/UploadFiles/GHAC");
            //临时输出路径
            string outPath = Path.Combine(basePath, string.Format("{0}\\{1}\\{2}", "output", InstructionCode, Guid.NewGuid().ToString()));
            //最终输出路径
            string F_summons_outpath = Path.Combine(outPath, "pallet.pdf");

            List<FileInfo> files = new List<FileInfo>();
            //查询数据库
            string sql = string.Format(
                @"
                        select DISTINCT  palletno from pickupplan
                        where concat(no,LEFT(version,2)) = '{0}'", InstructionCode);

            DataTable dt = DataHelper.Query(sql);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(Path.Combine(basePath, "PALLET"));

                string fileNameKeywords = string.Format("{0}{1}*.pdf", dt.Rows[i]["palletno"].ToString(), InstructionCode);

                FileInfo[] filess = TakeUniqueSplitFiles(directoryInfo.GetFiles(fileNameKeywords));

                foreach (var item in filess)
                {
                    files.Add(item);
                }
            }


            SpireHelper.Merge(1, 1, files.ToArray(), F_summons_outpath, false);

            return F_summons_outpath;

        }

        /// <summary>
        /// 输出PLS看板
        /// </summary>
        /// <param name="InstructionCode"></param>
        /// <returns></returns>
        public string GHAC_PLS_OUT(string InstructionCode, enumOutType outType)
        {
            //查询数据库
            string sql = string.Empty;

            if (outType == enumOutType.HAOKOU)
            {
                sql = string.Format(
                    @"select DISTINCT  carno,rec_location,palletno,pn, concat(recdate,' ' ,rectime),p_code,p_version,planqty from pickupplan
                        where concat(no,version) = '{0}'
                        order by carno,rec_location,palletno,pn, concat(recdate,' ' ,rectime)", InstructionCode);
            }
            else if (outType == enumOutType.Other)
            {
                sql = string.Format(
                @"select DISTINCT  version,rec_location,palletno,concat(pn,DATE_FORMAT(pickdate, '%Y%c%e')) pn, concat(recdate,' ' ,rectime),p_code,p_version,planqty from pickupplan
                                        where concat(no,'00') = '{0}'
                                        order by version", InstructionCode);
            }

            DataTable dt = DataHelper.Query(sql);

            List<FileInfo> F_PLS = new List<FileInfo>();

            //基础输出路径
            string basePath = HttpContext.Current.Server.MapPath("~/UploadFiles/GHAC");
            //临时输出路径
            string outPath = Path.Combine(basePath, string.Format("{0}\\{1}\\{2}", "output", InstructionCode, Guid.NewGuid().ToString()));
            //最终输出路径
            string F_PLS_outpath = Path.Combine(outPath, "PLS.pdf");

            HashSet<string> addedPls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DirectoryInfo DIR_PLS = new DirectoryInfo(Path.Combine(basePath, "PLS"));
                FileInfo[] F_PLS_TEMP = new FileInfo[] { };
                if (outType == enumOutType.HAOKOU)
                    F_PLS_TEMP = DIR_PLS.GetFiles(dt.Rows[i]["p_code"].ToString() + "*.pdf");
                else if (outType == enumOutType.Other)
                    F_PLS_TEMP = DIR_PLS.GetFiles(dt.Rows[i]["pn"].ToString().Replace("-", "").Replace(" ", "") + "*.pdf");

                F_PLS_TEMP = TakeUniqueSplitFiles(F_PLS_TEMP.OrderBy(f => f.Name));

                for (int j = 0; j < F_PLS_TEMP.Length; j++)
                {
                    if (addedPls.Add(F_PLS_TEMP[j].FullName))
                    {
                        F_PLS.Add(F_PLS_TEMP[j]);
                    }
                }
            }

            SpireHelper.Merge(3, 2, F_PLS.ToArray(), F_PLS_outpath, true);

            return F_PLS_outpath;
        }

        /// <summary>
        /// 输出取货指示书
        /// </summary>
        /// <param name="InstructionCode"></param>
        /// <returns></returns>
        public string GHAC_INSTRUCTION_OUT(string InstructionCode,string supplyCode)
        {
            //基础输出路径
            string basePath = HttpContext.Current.Server.MapPath("~/UploadFiles/GHAC");

            if (InstructionCode == "26689100638800") { 
                string tempPath = Path.Combine(basePath, @"INSTRUCTIONS\") + InstructionCode+supplyCode + ".pdf";
                if (System.IO.File.Exists(tempPath))
                    return tempPath;
                else
                    return string.Empty;
            }

            string F_INSTRUCTION = Path.Combine(basePath, @"INSTRUCTIONS\") + InstructionCode + ".pdf";
            XErrorLogToolMDM.WriteLog("获取取货指示");
            XErrorLogToolMDM.WriteLog(F_INSTRUCTION);

            if (System.IO.File.Exists(F_INSTRUCTION))
            {
                return F_INSTRUCTION;
            }
            else
                return string.Empty;

        }

        public string GHAC_TRACE_OUT(string InstructionCode) {
            try
         {
                string strSql = string.Format(
                                        @"SELECT carno, rec_location, palletno, pn, rec_datetime, recdate,
       rectime, p_code, p_version, planqty, packunit
FROM (
    SELECT *,
           CONCAT(recdate,' ',rectime) AS rec_datetime
    FROM pickupplan
    WHERE CONCAT(no, LEFT(version,2)) = '{0}'
    ORDER BY id  
) t
GROUP BY carno, rec_location, palletno, pn, rec_datetime,
         recdate, rectime, p_code, p_version, planqty, packunit", InstructionCode);
                string sql = string.Format(strSql, InstructionCode);
                DataTable data = DataHelper.Query(sql);
                if (data.Rows.Count == 0) {
                    throw new Exception("未查询到取货信息"); 
                   }

                string basePath = HttpContext.Current.Server.MapPath("~/UploadFiles/GHAC/TRACE");
                List<FileInfo> allMatchingFileInfos = new List<FileInfo>();
                List<FileInfo> latestVersion = new List<FileInfo>();

                #region 可能重复
                //foreach (DataRow row in data.Rows)
                //{
                //    // 安全获取值的方式
                //    string pn = row.Field<string>("pn").Replace("-","").Replace(" ","") ?? string.Empty;
                //    // 推荐：使用 Field<DateTime?> 处理可能的空值
                //    DateTime? pickdateValue = row.Field<DateTime?>("recdate");
                //    string pickdate = pickdateValue?.ToString("yyyyMMdd") ?? string.Empty;
                //    string rectime = row.Field<string>("rectime") ?? "";
                //    int boxQty = row.Field<int?>("packunit")?? 0;
                //    // 尝试解析时间字符串
                //    DateTime timeValue;
                //    string recTimeFormatted = "";

                //    if (DateTime.TryParse(rectime, out timeValue))
                //    {
                //        recTimeFormatted = timeValue.ToString("HHmmss");  // 转为 133000
                //    }
                //    else
                //    {
                //        // 如果不规范，可手动补全
                //        recTimeFormatted = rectime.Replace(":", "").PadRight(6, '0');
                //    }
                //    string rec_location = row.Field<string>("rec_location").Replace("-", "") ?? string.Empty;

                //    if (string.IsNullOrEmpty(pn) || string.IsNullOrEmpty(pickdate) || string.IsNullOrEmpty(rec_location))
                //    {
                //        continue; 
                //    }

                //    string searchPattern = $"*{pn}*{pickdate}*{recTimeFormatted}*{rec_location}*.pdf";

                //    string[] allFiles = Directory.GetFiles(basePath, searchPattern, SearchOption.TopDirectoryOnly);


                //    string[] matchingFiles = (boxQty > 0)
                //    ? allFiles.Take(boxQty).ToArray()
                //    : allFiles;

                //    foreach (string filePath in matchingFiles)
                //    {
                //        allMatchingFileInfos.Add(new FileInfo(filePath));
                //    }
                //}

                //FileInfo[] fils = allMatchingFileInfos.ToArray();
                #endregion

                HashSet<string> processedFiles = new HashSet<string>();

                foreach (DataRow row in data.Rows)
                {
                    // 安全获取值
                    string pn = row.Field<string>("pn")?.Replace("-", "").Replace(" ", "") ?? string.Empty;
                    DateTime? pickdateValue = row.Field<DateTime?>("recdate");
                    string pickdate = pickdateValue?.ToString("yyyyMMdd") ?? string.Empty;
                    string rectime = row.Field<string>("rectime") ?? "";
                    int boxQty = row.Field<int?>("packunit") ?? 0;

                    // 格式化时间
                    DateTime timeValue;
                    string recTimeFormatted = "";
                    if (DateTime.TryParse(rectime, out timeValue))
                        recTimeFormatted = timeValue.ToString("HHmmss");  // 转为 133000
                    else
                        recTimeFormatted = rectime.Replace(":", "").PadRight(6, '0');

                    string rec_location = row.Field<string>("rec_location")?.Replace("-", "") ?? string.Empty;

                    // 跳过无效行
                    if (string.IsNullOrEmpty(pn) || string.IsNullOrEmpty(pickdate) || string.IsNullOrEmpty(rec_location))
                        continue;

                    string searchPattern = $"*{pn}*{pickdate}*{recTimeFormatted}*{rec_location}*.pdf";

                    var files = Directory
                        .GetFiles(basePath, searchPattern, SearchOption.TopDirectoryOnly)
                        .Select(f => new FileInfo(f))
                        .OrderBy(f => f.LastWriteTime)
                        .ToList();

                    const int versionWindowSeconds = 120; // 2 分钟  

                    var versionGroups = new List<List<FileInfo>>();
                    List<FileInfo> currentGroup = new List<FileInfo>();

                    foreach (var file in files)
                    {
                        if (currentGroup.Count == 0)
                        {
                            currentGroup.Add(file);
                            continue;
                        }

                        var last = currentGroup.Last();

                        if ((file.LastWriteTime - last.LastWriteTime).TotalSeconds <= versionWindowSeconds)
                        {
                            // 认为是同一版本
                            currentGroup.Add(file);
                        }
                        else
                        {
                            // 新版本
                            versionGroups.Add(currentGroup);
                            currentGroup = new List<FileInfo> { file };
                        }
                    }

                    if (currentGroup.Count > 0)
                    {
                        versionGroups.Add(currentGroup);
                    }

                    // 取最新版本（时间最大的那一组）
                    latestVersion = versionGroups.LastOrDefault();

                    if (latestVersion != null)
                    {
                        foreach (var file in latestVersion)
                        {
                            allMatchingFileInfos.Add(file);
                            // 处理最新版本文件
                        }
                    }


                }

                FileInfo[] fils = allMatchingFileInfos.ToArray();


                if (fils.Length == 0) { return ""; }

                string outputPath = HttpContext.Current.Server.MapPath($"~/UploadFiles/{Guid.NewGuid().ToString()}");

                if (!Directory.Exists(outputPath))
                    Directory.CreateDirectory(outputPath);

                SpireHelper.Merge(2, 2, fils, outputPath + ".pdf", true,0,0,true);


                return outputPath+".pdf";
            }
            catch (Exception)
            {

                throw;
            }
        }


        public string GHAC_TRACE_OUT(DataTable dt)
        {
            try
            {
                string basePath = HttpContext.Current.Server.MapPath("~/UploadFiles/GHAC/TRACE");
                string usedPath = basePath + "/USED";    //使用过的追溯标签移动到新文件夹 防止重复取    
                //output += "UploadFiles\\GHAC\\TRACE";
                HashSet<string> filePathSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                List<FileInfo> allMatchingFileInfos = new List<FileInfo>();
                foreach (DataRow row in dt.Rows)
                {
                    if (row.Field<string>("traceability_label") != "是") { continue; }
                    // 安全获取值的方式
                    string pn = row.Field<string>("pn").Replace("-", "").Replace(" ", "") ?? string.Empty;
                    DateTime? recdateValue = row.Field<DateTime?>("recdate");
                    string reckdate = recdateValue?.ToString("yyyyMMdd") ?? string.Empty;
                    string rectime = row.Field<string>("rectime") ?? "";
                    DateTime timeValue;
                    string recTimeFormatted = "";

                    if (DateTime.TryParse(rectime, out timeValue))
                    {
                        recTimeFormatted = timeValue.ToString("HHmmss");  // 转为 133000
                    }
                    else
                    {
                        recTimeFormatted = rectime.Replace(":", "").PadRight(6, '0');
                    }
                    string rec_location = row.Field<string>("rec_location").Replace("-", "") ?? string.Empty;
                    int boxQty = GetTraceInt(row, "packunit");
                    int planQty = GetTraceInt(row, "planqty");
                    // packunit 是箱数，标签零件数 = 纳入数 / 箱数（25/1=25，90/3=30）
                    int needLabelQty = GetTraceNeedLabelQty(planQty, boxQty);
                    int takeCount = boxQty > 0 ? boxQty : 0;


                    if (string.IsNullOrEmpty(pn) || string.IsNullOrEmpty(rectime) || string.IsNullOrEmpty(rec_location))
                    {
                        continue;
                    }

                    string searchPattern = $"*{pn}*{reckdate}*{recTimeFormatted}*{rec_location}*.pdf";

                    var allFiles = Directory.GetFiles(basePath, searchPattern);
                    XErrorLogToolMDM.WriteLog(string.Format(
                        "GHAC追溯 pn={0} planqty={1} packunit={2} labelQty={3} take={4} files={5} pattern={6}",
                        pn, planQty, boxQty, needLabelQty, takeCount, allFiles.Length, searchPattern));

                    //  读取当前 offset（从状态文件读，不存在则从0开始）
                    string stateKey = $"{pn}_{reckdate}_{recTimeFormatted}_{rec_location}";
                    string stateFile = Path.Combine(basePath, $"_state_{stateKey}.txt");
                    int offset = System.IO.File.Exists(stateFile) ? int.Parse(System.IO.File.ReadAllText(stateFile)) : 0;

                    //  如果 offset 超出范围，归零（循环）
                    if (offset >= allFiles.Length) offset = 0;

                    int addedThisLine = 0;
                    if (needLabelQty > 0 && takeCount > 0)
                    {
                        for (int i = 0; i < allFiles.Length && addedThisLine < takeCount; i++)
                        {
                            string filePath = allFiles[i];
                            if (filePathSet.Contains(filePath))
                            {
                                continue;
                            }
                            if (!TraceLabelQtyMatches(filePath, needLabelQty))
                            {
                                continue;
                            }
                            if (filePathSet.Add(filePath))
                            {
                                allMatchingFileInfos.Add(new FileInfo(filePath));
                                addedThisLine++;
                                offset = i + 1;
                                XErrorLogToolMDM.WriteLog("GHAC追溯按零件数命中 " + filePath);
                            }
                        }
                    }

                    if (addedThisLine < takeCount)
                    {
                        XErrorLogToolMDM.WriteLog(string.Format(
                            "GHAC追溯零件数未凑齐 pn={0} labelQty={1} got={2} need={3}", pn, needLabelQty, addedThisLine, takeCount));
                        for (int i = offset; i < allFiles.Length; i++)
                        {
                            string filePath = allFiles[i];
                            if (filePathSet.Add(filePath))
                            {
                                allMatchingFileInfos.Add(new FileInfo(filePath));
                                addedThisLine++;
                                if (takeCount > 0 && addedThisLine >= takeCount)
                                {
                                    offset = i + 1;
                                    break;
                                }
                            }
                        }

                        if (addedThisLine < takeCount) offset = allFiles.Length;
                    }
                    // 保存 offset
                    System.IO.File.WriteAllText(stateFile, offset.ToString());

                }
                FileInfo[] fils = allMatchingFileInfos.ToArray();

                if (fils.Length == 0) { return ""; }

                string outputPath = HttpContext.Current.Server.MapPath($"~/UploadFiles/{Guid.NewGuid().ToString()}");

                if (!Directory.Exists(outputPath))
                    Directory.CreateDirectory(outputPath);

                SpireHelper.Merge(2, 2, fils, outputPath + ".pdf", true, 0, 0, true);


                return outputPath + ".pdf";
                //return "";
            }
            catch (Exception)
            {

                throw;
            }
        }

        private static int GetTraceInt(DataRow row, string column)
        {
            if (row == null || !row.Table.Columns.Contains(column) || row[column] == null || row[column] == DBNull.Value)
            {
                return 0;
            }
            try
            {
                return Convert.ToInt32(Convert.ToDecimal(row[column]));
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 标签上的零件数 = 纳入数 / 箱数。pickupplan.packunit 是箱数，不是每箱件数。
        /// </summary>
        private static int GetTraceNeedLabelQty(int planQty, int boxQty)
        {
            if (planQty <= 0)
            {
                return 0;
            }
            if (boxQty <= 0)
            {
                return planQty;
            }
            return planQty / boxQty;
        }

        /// <summary>
        /// 追溯标签 PDF 里的零件数是否等于指定数量（拆开的文件可能仍带四联文本）。
        /// </summary>
        private static bool TraceLabelQtyMatches(string filePath, int qty)
        {
            if (string.IsNullOrEmpty(filePath) || qty <= 0 || !System.IO.File.Exists(filePath))
            {
                return false;
            }

            Spire.Pdf.PdfDocument pdf = null;
            try
            {
                pdf = new Spire.Pdf.PdfDocument();
                pdf.LoadFromFile(filePath);
                if (pdf.Pages.Count == 0)
                {
                    return false;
                }

                PdfTextExtractor extractor = new PdfTextExtractor(pdf.Pages[0]);
                string text = extractor.ExtractText(new PdfTextExtractOptions()) ?? string.Empty;
                return GetTraceLabelQtys(text).Contains(qty);
            }
            catch (Exception ex)
            {
                XErrorLogToolMDM.WriteLog("GHAC追溯读零件数失败 " + filePath + " " + ex.Message);
                return false;
            }
            finally
            {
                if (pdf != null)
                {
                    pdf.Close();
                }
            }
        }

        private static HashSet<int> GetTraceLabelQtys(string text)
        {
            var qtys = new HashSet<int>();
            if (string.IsNullOrEmpty(text))
            {
                return qtys;
            }
            string[] patterns =
            {
                @"PARTS\s*NO\s+(\d{1,3})\s*批次号",
                @"零件数\s*(\d{1,3})",
                @"Q['’]?\s*T\s*Y\s*(\d{1,3})"
            };
            foreach (string pattern in patterns)
            {
                foreach (Match match in Regex.Matches(text, pattern, RegexOptions.IgnoreCase))
                {
                    int labelQty;
                    if (int.TryParse(match.Groups[1].Value, out labelQty) && labelQty > 0)
                    {
                        qtys.Add(labelQty);
                    }
                }
            }
            return qtys;
        }

        private static string NormalizePalletNo(string palletno)
        {
            if (string.IsNullOrEmpty(palletno))
            {
                return string.Empty;
            }
            string s = palletno.Trim();
            if (s.StartsWith("托"))
            {
                s = s.Substring(1);
            }
            return s.Trim();
        }

        /// <summary>
        /// 看板文件名末尾是托号。用数值比较（3 对 03，不对 13）。
        /// </summary>
        private static bool FileNameMatchesPallet(string fileName, string palletno)
        {
            int want;
            if (string.IsNullOrEmpty(fileName) || !int.TryParse(NormalizePalletNo(palletno), out want))
            {
                return false;
            }
            string name = Path.GetFileNameWithoutExtension(fileName);
            name = Regex.Replace(name ?? string.Empty, @"_Page_\d+$", string.Empty);
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }
            int got;
            if (name.Length >= 2 && int.TryParse(name.Substring(name.Length - 2), out got))
            {
                return got == want;
            }
            return int.TryParse(name.Substring(name.Length - 1), out got) && got == want;
        }

        /// <summary>
        /// 拆分时若文件名已存在会另存 _Page_N，下载时只留一份。
        /// </summary>
        private static FileInfo[] TakeUniqueSplitFiles(IEnumerable<FileInfo> files)
        {
            if (files == null)
            {
                return new FileInfo[0];
            }
            return files
                .Where(f => f != null)
                .GroupBy(f => Regex.Replace(f.Name, @"_Page_\d+(?=\.pdf$)", string.Empty, RegexOptions.IgnoreCase), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.FirstOrDefault(x => x.Name.IndexOf("_Page_", StringComparison.OrdinalIgnoreCase) < 0) ?? g.OrderBy(x => x.Name).First())
                .ToArray();
        }

        /// <summary>
        ///  生成确认表
        /// </summary>
        /// <param name="ListID"></param>
        /// <param name="type">1 八千代，2 计划的</param>
        public string GHAC_CONFIRM(List<string> ListID, List<string> supplierCode, string type = "")
        {
            //没看出来哪用了 先注掉
            //string F_INSTRUCTION = GHAC_INSTRUCTION_OUT(ListID[0]);

            DataTable dt =  new DataTable();
            if (type == "量产") {
                dt = GetPlanDBHAOKO(ListID, supplierCode);
            }
            else {
                dt = GetPlanDB(ListID);
            }
            if (dt == null || dt.Rows.Count == 0) return string.Empty;

            string fileUrl = ConfigurationManager.AppSettings["CHAMQRB"].ToString();

            foreach (var No in ListID)
            {
                CHAMconfirm cHAMconfirm = new CHAMconfirm();

                var version = No.Substring(No.Length - 2, 2);
                var number = No.Substring(0, No.Length - 2);
                var noDB = dt.Select($"no='{No}'");

                // 根据 plan_code 分组
                var groupedData = noDB
                        .GroupBy(row =>
                            type == "补给品"
                                ? row.Field<string>("r_location")
                                : row.Field<string>("plan_code")   // 另一套逻辑的分组字段
                        )
                        .ToList();


                // 创建存储结果的列表
                List<DataTable> splitTables = new List<DataTable>();

                foreach (var group in groupedData)
                {
                    // 克隆原表结构创建新表
                    DataTable newTable = dt.Clone();
                    

                    // 将当前分组的所有行导入新表
                    foreach (DataRow row in group)
                    {
                        newTable.ImportRow(row);
                    }
                    var result = MergeRows(newTable, new[] { "part_code", "p_code" }, "p_num");

                    splitTables.Add(result);

                }

                


                List<string> listPath = new List<string>();
                //string[] listPath = new string[splitTables.Count];
                //int index = 0;
                foreach (var item in splitTables)
                {
                    List<CHAMOrdWeekWithOrig> detailList = new List<CHAMOrdWeekWithOrig>();
                    string outPath = Path.Combine(HttpContext.Current.Server.MapPath("~/UploadFiles/GHAC"), string.Format("{0}\\{1}\\{2}", "output", DateTime.Now.ToString("yyyyMMddHHmmss"), Guid.NewGuid().ToString()));
                    if (!Directory.Exists(outPath))
                        Directory.CreateDirectory(outPath);

                    cHAMconfirm.summonsCreatePerson = dt.Rows[0]["createPerson"].ToString(); //创建人
                    cHAMconfirm.title = "GHAC 制品集货・出货异品・欠品照合确认表";

                    cHAMconfirm.carGroup = dt.Rows[0]["carGroup"].ToString(); //carGroup
                    string cusnum = GetCusCode(item.Rows[0]["no"].ToString(), item.Rows[0]["plan_code"].ToString(), item.Rows[0]["supplier"].ToString());
                    cHAMconfirm.cusCode = cusnum;
                    #region  暂时注释
                    DateTime? pickdateValue = dt.Rows[0].Field<DateTime?>("pickdate");
                    cHAMconfirm.shipmentDate = pickdateValue?.ToString("yyyy-M-d") ?? string.Empty;
                    cHAMconfirm.summonsCreateTime = DateTime.Now.ToString("yyyy-MM-dd");
                    #endregion
                    //cHAMconfirm.details = detailList;
                    string confirmRemark = orderimport.GetConfirmBatch("广州本田", "");//GetConfirmRemark(No);
                    cHAMconfirm.confirmRemark = confirmRemark;
                    cHAMconfirm.type = "广州本田";

                    for (int i = 0; i < item.Rows.Count; i++)
                    {

                        var itemRow = item.Rows[i];
                        string cusNum = GetCustNum(item.Rows[i]["plan_code"].ToString());
                        cHAMconfirm.cusName = "GHAC" + cusNum + type ?? "";
                        XErrorLogToolMDM.WriteLog(cHAMconfirm.cusName + dt.Rows[i]["plan_code"].ToString());
                        CHAMOrdWeekWithOrig detail = new CHAMOrdWeekWithOrig();
                        detail.projectCode = item.Rows[i]["plan_code"].ToString();                   //计划代码
                        XErrorLogToolMDM.WriteLog("GHACoutputByCode GHAC_CONFIRM 计划代码 "+ detail.projectCode);
                        detail.movingInCode = item.Rows[i]["r_location"].ToString();                 //落点
                        detail.materialCode = item.Rows[i]["part_code"].ToString();                  //零件号
                        detail.supplier = item.Rows[i]["supplier"].ToString();
                        string factoryCode = "";
                        if (detail.projectCode.Contains("SP")) {
                            factoryCode = "SP";
                        }

                        DataTable oriData = PulicHelper.GetGHACDensoPN(detail.materialCode, type, detail.supplier, cusnum, item.Rows[i]["plan_code"].ToString());

                        // 统一拿一行，后面只用 oriRow
                        DataRow oriRow = (oriData != null && oriData.Rows.Count > 0)
                            ? oriData.Rows[0]
                            : null;

                        detail.traceabilityLabel = oriRow?["traceability_label"]?.ToString() ?? "";

                        // 电装品番 & 箱容
                        detail.DensoNum = oriRow?["denso_num"]?.ToString() ?? "";

                        object boxCapObj = oriRow?["box_cap"];
                        detail.BoxCap = (boxCapObj == null || boxCapObj == DBNull.Value)
                            ? 0
                            : Convert.ToInt32(boxCapObj);

                        // 收容数
                        int palletBoxValue;
                        if (!int.TryParse(item.Rows[i]["pallet_box"]?.ToString(), out palletBoxValue))
                        {
                            if (!int.TryParse(oriRow?["pallet_box"]?.ToString(), out palletBoxValue))
                            {
                                palletBoxValue = 0;
                            }
                        }

                        detail.PalletBox = palletBoxValue;
                        detail.quantity = item.Rows[i]["p_num"].ToString();                         //纳入个数
                        detail.cpdate = item.Rows[i]["c_date"]?.ToString();
                        detail.cptime = item.Rows[i]["c_time"].ToString();                          //传票时间
                        if (type == "量产") {
                            detail.orderCode = "";//item.Rows[i]["p_code"].ToString() + "-00" + item.Rows[i]["p_version"].ToString();                     //订单号
                        }
                        else {
                            XErrorLogToolMDM.WriteLog("非量产");
                            detail.orderCode = item.Rows[i]["p_code"].ToString() + "-" + item.Rows[i]["p_version"].ToString();                     //订单号
                        }
                            
                        detailList.Add(detail);
                    }

                    cHAMconfirm.details = detailList;

                    var pdfPath = PulicHelper.EditConfirmExcel(fileUrl, outPath + $"\\确认表{item.Rows[0]["plan_code"]}.xls", cHAMconfirm);  // 返回PDF路径
                        listPath.Add(pdfPath);

                }

               
                string outPathMain = Path.Combine(HttpContext.Current.Server.MapPath("~/UploadFiles/GHAC"), string.Format("{0}\\{1}\\{2}", "output", DateTime.Now.ToString("yyyyMMddHHmmss"), Guid.NewGuid().ToString()));
                XErrorLogToolMDM.WriteLog("输出路径" + outPathMain);
                if (!Directory.Exists(outPathMain))
                    Directory.CreateDirectory(outPathMain);
                var pdfPathMain = PulicHelper.EditConfirmExcel(fileUrl, outPathMain + $"\\确认表{No}.xls", cHAMconfirm);  // 返回PDF路径

                SpireHelper.MergeFiles(listPath, pdfPathMain);
                XErrorLogToolMDM.WriteLog("确认表完成");
                return pdfPathMain;
            }

            return string.Empty;

        }


        //public Da


        public DataTable GetOri(string cusCode,string custNum) {
            try
            {
                string strSql = @"SELECT * FROM originalunit WHERE cust_NUM = '{0}' and cust_code = '{1}' ";
                string sql = string.Format(strSql, custNum, cusCode);
                DataTable data = DataHelper.Query(sql);
                return data;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public string GetCustNum(string projectCode) {
            try
            {
                if (projectCode.Contains("01")) { return " #1 "; }
                if (projectCode.Contains("02")) { return " #2 "; }
                if (projectCode.Contains("03")) { return " #3 "; }
                if (projectCode.Contains("04")) { return " #4 "; }
                if (projectCode.Contains("09")) { return " #9 "; }
                if (projectCode.Contains("SP")) { return " #SP "; }
                return "";
            }
            catch (Exception)
            {

                throw;
            }
        }

        public string GetCusCode(string no,string planCode,string supplyer) {
            try
            {
                if (no.Contains("八千代")) {
                    if (planCode.Contains("01")) return "13001208";
                    else if (planCode.Contains("02") || planCode.Contains("03") || planCode.Contains("09")) return "13001218";
                    else if (planCode.Contains("04")) return "13001308";
                    else return "";
                }
                else if (no.Contains("SP")) {
                    if (no.Contains("81106")) { return "13001219"; }
                    if (no.Contains("71101")) { return "13001211"; }
                    else { return ""; }
                }
                else {
                    if (supplyer == "71101") {
                        if (planCode.Contains("01")) return "13001200";
                        else if (planCode.Contains("02") || planCode.Contains("03")) return "13001210";
                        else if (planCode.Contains("05")) return "13001300";
                        else if (planCode.Contains("09")) return "13001210";
                        else if (planCode.Contains("04")) return "13001210";
                        else return "";
                    }
                    else if (supplyer == "81106") {
                        if (planCode.Contains("01")) return "13001208";
                        else if (planCode.Contains("02") || planCode.Contains("03")) return "13001218";
                        else if (planCode.Contains("05")) return "13001309";
                        else if (planCode.Contains("09")) return "13001218";
                        else if (planCode.Contains("04")) return "13001308";
                        else return "";
                    }
                    else {
                        return "";
                    }



                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public string GetCusName(string factory)
        {
            try
            {
                if (factory.Contains("八千代"))
                {
                    return "八千代";
                }
                else if (factory.Contains("SP"))
                {
                    return factory;
                }
                else
                {
                    if (factory.Contains("01")) return "号口1厂";
                    else if (factory.Contains("02")) return "号口2厂";
                    else if (factory.Contains("03")) return "号口3厂";
                    else if (factory.Contains("05")) return "号口5厂";
                    else if (factory.Contains("09")) return "号口9厂";
                    else if (factory.Contains("04")) return "号口厂";
                    else return "";

                }
            }
            catch (Exception)
            {

                throw;
            }
        }


        public string GetConfirmRemark(string mainId)
        {
            try
            {
                string strSql = @"select * from pickupplan where no = LEFT('{0}', CHAR_LENGTH('{0}') - 2);";
                strSql = string.Format(strSql, mainId);
                DataTable confirm = DataHelper.Query(strSql.ToString());
                if (confirm.Rows.Count == 0)
                {
                    return "";
                }
                return confirm.Rows[0]["confirmRemark"].ToString();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        // 获取八千代数据 建完service放到service里
        public DataTable GetEnghtDB(List<string> ListID)
        {
            //select * from ord_weekorder where id in ('2381', '2382', '2383')

            string wherein = string.Empty;
            for (int i = 0; i < ListID.Count; i++)
            {
                if (i > 0) wherein += ",";
                wherein += ListID[i];
            }
            string sql = string.Format(@"select plan_code  -- 计划代码
            ,r_location  -- 落点
            ,part_code  -- 零件号
            ,'' denso_num  -- 电装品番
            ,0 box_cap  -- 收容数
            ,0 pallet_box -- 拖台数
            ,sum(order_num) as  p_num   -- 纳入个数
            , c_date   -- 传票日期
            , c_time  -- 传票时间
            ,'' as carGroup  -- 取货车组
             from ord_weekorder where id in ({0})
             group by 
             plan_code  -- 计划代码
            ,r_location  -- 落点
            ,part_code  -- 零件号
             , c_date   -- 传票日期
            , c_time  -- 传票时间
            ", wherein);
            DataTable dt = DataHelper.Query(sql);
            return dt;
        }
        /// <summary>
        /// 号口的确认表单独处理 相同品番合并
        /// </summary>
        /// <param name="ListID"></param>
        /// <returns></returns>
        public DataTable GetPlanDBHAOKO(List<string> ListID,List<string> supplyList)
        {
            string wherein = string.Empty;
            string wheresup = string.Empty;
            for (int i = 0; i < ListID.Count; i++)
            {
                if (i > 0) wherein += ",";
                wherein += "'" + ListID[i] + "'";
            }

            for (int i = 0; i < supplyList.Count; i++)
            {
                if (i > 0) wheresup += ",";
                wheresup += "'" + supplyList[i] + "'";
            }

            string sql = string.Format(@"
select ifnull(plan.plancode, ord.plan_code)      as                                               plan_code  -- 计划代码
     , ifnull(plan.rec_location, ord.r_location)     as                                               r_location -- 落点
     , plan.pn as                                               part_code  -- 零件号
     , '-'                                                      denso_num  -- 电装品番
     , plan.packqty                                             box_cap    -- 收容数
     , '-'                                                      pallet_box-- 拖台数
     , SUM(IFNULL(plan.planqty, CAST(ord.order_num AS SIGNED))) p_num--  纳入个数
     , '-'     as                                               c_date     -- 传票日期
     , '-'     as                                               c_time     -- 传票时间
     , plan.carno     as                                               carGroup   -- 取货车组
     , concat(plan.no, LEFT(plan.version, 2))                   no
     , createPerson
     , '-'     as                                               p_code
     , plan.pickdate     as                                               pickdate
     , plan.picktime     as                                               picktime
     , plan.factory      as                                               factory
     , plan.supplier
from pickupplan plan
         left join ord_weekorder ord on plan.p_code = ord.p_code
            where concat(plan.no,LEFT(plan.version,2) ) in ({0})
            and plan.supplier in ({1})

group by
#     plan.id,
        ifnull(plan.plancode, ord.plan_code)
       ,ifnull(plan.rec_location, ord.r_location)
    ,plan.pn
       , plan.packqty
       -- , plan.recdate
       -- , plan.rectime
        , plan.carno
       , concat(plan.no, LEFT(plan.version, 2))
       , createPerson
        , plan.pickdate
        , plan.picktime
        , plan.factory
        , supplier
       -- , ord.r_location
       -- , ord.order_num
       -- , ord.plan_code
# order by plan.id
", wherein,wheresup);
            DataTable dt = DataHelper.Query(sql);
            //XErrorLogToolMDM.WriteLog("确认表明细DT");
            XErrorLogToolMDM.WriteLog(sql);
            return dt;
        }
        #endregion

        /// <summary>
        /// 其余的确认表不需要合并
        /// </summary>
        /// <param name="ListID"></param>
        /// <returns></returns>
        public DataTable GetPlanDB(List<string> ListID)
        {
            string wherein = string.Empty;
            for (int i = 0; i < ListID.Count; i++)
            {
                if (i > 0) wherein += ",";
                wherein += "'" + ListID[i] + "'";
            }

            string sql = string.Format(@"select
         ifnull(plan.plancode,ord.plan_code) as plan_code  -- 计划代码
     ,ifnull(plan.rec_location,ord.r_location) r_location  -- 落点
         ,plan.pn part_code  -- 零件号
         ,'' denso_num -- 电装品番
         ,plan.packqty box_cap  -- 收容数
         ,'' pallet_box-- 拖台数
         ,SUM(IFNULL(plan.planqty, CAST(ord.order_num AS SIGNED))) p_num--  纳入个数
         ,CONVERT(plan.recdate,CHAR(10)) c_date   -- 传票日期
        , plan.rectime c_time  -- 传票时间
            ,plan.carno as carGroup  -- 取货车组
            ,concat(plan.no,LEFT(plan.version,2) ) no
,createPerson,plan.p_code,plan.p_version,
plan.pickdate,
plan.picktime,
plan.factory,
plan.supplier
            from pickupplan plan

             left join ord_weekorder ord on plan.p_code = ord.p_code

            where concat(plan.no,LEFT(plan.version,2) ) in ({0})
         group by
plan.id,
         plan.plancode
         ,plan.rec_location
         ,plan.pn
         ,plan.packqty
         ,plan.recdate
         , plan.rectime
            ,plan.carno
            ,concat(plan.no,LEFT(plan.version,2) )
,createPerson,plan.p_code,plan.pickdate,plan.picktime,plan.factory,ord.r_location,ord.order_num,ord.plan_code,plan.p_version,plan.supplier
order by plan.id
", wherein);
            DataTable dt = DataHelper.Query(sql);
            XErrorLogToolMDM.WriteLog("确认表明细DT");
            XErrorLogToolMDM.WriteLog(sql);
            return dt;
        }


        private void DownloadZip(string filePath, string zipName)
        {
            HttpResponse response = HttpContext.Current.Response;
            response.Clear();
            response.ContentType = "application/zip";
            response.AppendHeader("Content-Disposition", $"attachment; filename={HttpUtility.UrlEncode(zipName)}");
            response.TransmitFile(filePath);

            response.Flush();
        }

        private void DownloadPdf(string filePath, string zipName)
        {
            HttpResponse response = HttpContext.Current.Response;
            response.Clear();
            response.ContentType = "application/pdf";
            response.AppendHeader("Content-Disposition", $"attachment; filename={HttpUtility.UrlEncode(zipName)}");
            response.TransmitFile(filePath);

            response.Flush();
        }

        /// <summary>
        /// FTET 导入出货计划表，写入数据库ftet_plan
        /// </summary>
        /// <param name="inputFile"></param>
        public void ExecPlanExcel(string inputFile)
        {
            //inputFile = @"D:\01work\24大宇宙\01南沙电装OMS\05客户提供\FTET 2025年4月物流集货计划--广州地区2.xls";

            Dictionary<DateTime, List<string>> data = new Dictionary<DateTime, List<string>>();

            //解析excel
            using (FileStream fs = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook;
                ISheet sheet;
                IRow row;
                IRow rowMaru;
                IRow rowOrder;
                ICell cell;

                if (Path.GetExtension(inputFile).ToLower() == ".xlsx")
                {
                    workbook = new XSSFWorkbook(fs);
                }
                else
                {
                    workbook = new HSSFWorkbook(fs);
                }

                sheet = workbook.GetSheetAt(0);
           

                row = sheet.GetRow(33);
                rowMaru = sheet.GetRow(35);
                rowOrder = sheet.GetRow(29);

                for (int i = 10; i < 41 * 5 + 10; i += 5)
                {
                    DateTime date = row.GetCell(i).DateCellValue;
                    string Maru = rowMaru.GetCell(i).StringCellValue;

                    //有○的时候，写入字典，并把上边的五个单元格的单据号写入
                    if (Maru == "○")
                    {
                        data.Add(date, new List<string>());
                        for (int j = 0; j < 5; j++)
                        {
                            if (rowOrder.GetCell(i + j) != null && rowOrder.GetCell(i + j).StringCellValue != string.Empty)
                            {
                                data[date].Add(rowOrder.GetCell(i + j).StringCellValue);
                            }
                        }
                    }
                    else if (data.Count > 0)//月份对，但是没有○，写入到上一日
                    {
                        //取最大的出货日期，上边如果有订单，把订单号写入到最大的出货日期里
                        DateTime maxDate = data.Keys.Max();

                        for (int j = 0; j < 5; j++)
                        {
                            if (rowOrder.GetCell(i + j) != null && rowOrder.GetCell(i + j).StringCellValue != string.Empty)
                            {
                                data[maxDate].Add(rowOrder.GetCell(i + j).StringCellValue);
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
                    sql.Append(string.Format("('{0}',{1}),", item.Key.ToString("yyyy-MM-dd"), item.Value[i]));
                }
            }

            sql.Length--;

            int n = DataHelper.ExecuteSql(sql.ToString());
        }

        /// <summary>
        /// 导出FTET核对表
        /// </summary>
        /// <param name="orderno">出货日任意一个订单号</param>
        /// <param name="excelTemplate">模板文件路径</param>
        /// <param name="outputFile">输出文件路径</param>
        public void Export(string orderno, string excelTemplate, string outputFile)
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
                return;
            else
            {
                //规定出货时间
                podate = Convert.ToDateTime(dt_order.Rows[0]["podate"].ToString());

                //反查出货计划表的单据 D列
                sql = string.Format("select orderno from ftet_plan where ddate = '{0}' order by id", podate.ToString("yyyy-MM-dd"));
                dt_D = DataHelper.Query(sql);
            }

            //输出
            using (FileStream fs = new FileStream(excelTemplate, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook;
                ISheet sheet;
                IRow row;
                ICell cell;
                

                if (Path.GetExtension(excelTemplate).ToLower() == ".xlsx")
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
            }
        }



        /// <summary>
        /// PDF拆分
        /// </summary>
        /// <param name="inputFile">输入原始PDF文件</param>
        /// <param name="outputFolder">输出路径</param>
        /// <param name="StartPage">起始页，如果不确定，调用起始页查询函数获取</param>
        /// <param name="VerticalCount">垂直分几页</param>
        /// <param name="HorizontalCount">水平分几页</param>
        /// <param name="nameRule">命名规则</param>
        /// <param name="NameRectangle">命名取值区域，如果是自定义命名，必须有值</param>
        /// <param name="DataRectangle">原始PDF有效数据范围，如果全页等分，传入new Rectangle()即可</param>
        public void PdfSplit(
            string inputFile,
            string outputFolder,
            int StartPage,
            int VerticalCount,
            int HorizontalCount,
            SpireSplit.EnumNameRule nameRule,
            Rectangle[] NameRectangle,
            Rectangle DataRectangle,
            bool bsortno = false,
            bool getLimit = false,
            bool rename = false)
        {
            try
            {
                XErrorLogToolMDM.WriteLog("PDF拆分开始");
                //需要重写收容数的话，先去原单位表
                Dictionary<string, int> dict_originalunit = new Dictionary<string, int>();
                if (getLimit)
                {
                    //目前只有FTET需要
                    string sql = "select DENSO_NUM,CUST_NUM,custnum,box_cap from originalunit where cust_name = 'FTET'";
                    DataTable dt = DataHelper.Query(sql);

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        if (!dict_originalunit.ContainsKey(dt.Rows[i]["cust_num"].ToString()))
                        {
                            dict_originalunit.Add(dt.Rows[i]["cust_num"].ToString(),
                                Convert.ToInt32(dt.Rows[i]["box_cap"].ToString()));
                        }
                    }
                }

                //原始文档
                Spire.Pdf.PdfDocument pdf = new Spire.Pdf.PdfDocument();
                pdf.LoadFromFile(inputFile);
                XErrorLogToolMDM.WriteLog("原始文档" + inputFile);
                //通过转向情况获得实际宽高
                bool isLandscape = pdf.Pages[StartPage].Rotation == PdfPageRotateAngle.RotateAngle90 ||
                      pdf.Pages[StartPage].Rotation == PdfPageRotateAngle.RotateAngle270;
                float pdfWidth = isLandscape ? pdf.Pages[StartPage].Size.Height : pdf.Pages[StartPage].Size.Width;
                float pdfHeight = isLandscape ? pdf.Pages[StartPage].Size.Width : pdf.Pages[StartPage].Size.Height;

                //创建新PDF文档
                Spire.Pdf.PdfDocument newPdf = new Spire.Pdf.PdfDocument();

                //移除新文档的页边距
                newPdf.PageSettings.Margins.All = 0;

                //按照拆分横纵数，计算新页面尺寸
                newPdf.PageSettings.Width = pdfWidth / HorizontalCount;
                newPdf.PageSettings.Height = pdfHeight / VerticalCount;

                if (HorizontalCount == 1 && VerticalCount == 3)
                {
                    newPdf.PageSettings.Width = pdfWidth / HorizontalCount;
                    newPdf.PageSettings.Height = 254;
                }

                //原始文档有效范围，传入参数
                if (DataRectangle == new Rectangle())
                    DataRectangle = new Rectangle(0, 0, (int)pdfWidth, (int)pdfHeight);
                //单页有效尺寸，计算出
                SizeF youxiaoSize = new SizeF(DataRectangle.Width / HorizontalCount, DataRectangle.Height / VerticalCount);
                XErrorLogToolMDM.WriteLog("新文档");
                //获取第一页
                for (int i = StartPage; i < pdf.Pages.Count; i++)
                {
                    PdfPageBase page = pdf.Pages[i];

                    PdfTemplate temp = page.CreateTemplate();

                    for (int y = 0; y < VerticalCount; y++)
                    {
                        for (int x = 0; x < HorizontalCount; x++)
                        {
                            //创建一个页面
                            PdfPageBase newPage = newPdf.Pages.Add(
                                newPdf.PageSettings.Size, //size就是拆分后的纸张大小
                                new PdfMargins((newPdf.PageSettings.Width - youxiaoSize.Width) / 2, (newPdf.PageSettings.Height - youxiaoSize.Height) / 2));
                            //边距是纸张大小减去有效范围/2


                            if (HorizontalCount == 1 && VerticalCount == 3)
                            {
                                newPage.Canvas.DrawTemplate(temp,
                                    new PointF(-DataRectangle.X - youxiaoSize.Width * x, -DataRectangle.Y - youxiaoSize.Height * y + 5.4f),
                                    new SizeF(pdfWidth, pdfHeight));
                            }
                            else
                            {
                                newPage.Canvas.DrawTemplate(temp,
                                    new PointF(-DataRectangle.X - youxiaoSize.Width * x, -DataRectangle.Y - youxiaoSize.Height * y),
                                    new SizeF(pdfWidth, pdfHeight));
                            }
                        }
                    }
                }

                if (!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }
                XErrorLogToolMDM.WriteLog("拆分保存");
                //拆分保存
                for (int i = 0; i < newPdf.Pages.Count; i++)
                {
                    string str = string.Empty;

                    for (int j = 0; j < NameRectangle.Length; j++)
                    {
                        RectangleF extractRegion = NameRectangle[j];
                        //new RectangleF(newPdf2.Pages[i].Size.Width-200, newPdf2.Pages[i].Size.Height - 30,200 , 30); // x, y, width, height
                        //extractRegion = new RectangleF(75, 240, 210, 35);
                        PdfTextExtractor extractor = new PdfTextExtractor(newPdf.Pages[i]);
                        PdfTextExtractOptions options = new PdfTextExtractOptions();
                        options.ExtractArea = extractRegion;

                        string str_temp = extractor.ExtractText(options);

                        if (j == 1) {
                            //str_temp = str_temp.Split(' ')[0];
                            str_temp = str_temp.Trim().Replace('/', '-').Replace('.', '-');

                            if (DateTime.TryParse(str_temp, out DateTime dt))
                            {
                                str_temp = dt.ToString("yyyyMMddHHmmss");
                            }
                        }

                        str += str_temp.Replace("\r\n", "").Replace(" ", "");
                    }

                    //检测空白页     
                    if (str == string.Empty && nameRule == SpireSplit.EnumNameRule.Customize)
                    {
                        continue;
                    }

                    Spire.Pdf.PdfDocument splitPdf = new Spire.Pdf.PdfDocument();

                    splitPdf.InsertPage(newPdf, i);

                    string fileName = string.Empty;
                    string outputFile = string.Empty;
                    //命名
                    if (nameRule == SpireSplit.EnumNameRule.Customize)
                    {
                        fileName = str.Replace("\r\n", "").Replace(" ", "").Replace("/", "");
                        if (bsortno)
                        {
                            string nameindex = "0000" + (i).ToString();
                            fileName += $"_{nameindex.Substring(nameindex.Length - 4)}";
                        }
                        outputFile = Path.Combine(outputFolder, $"{fileName}.pdf");
                    }
                    else if (nameRule == SpireSplit.EnumNameRule.Auto)
                    {
                        fileName = Path.GetFileNameWithoutExtension(inputFile);
                        string nameindex = "0000" + (i + 1).ToString();
                        outputFile = Path.Combine(outputFolder, $"{fileName}_Page_{nameindex.Substring(nameindex.Length - 4)}.pdf");
                    }
                    else if (nameRule == SpireSplit.EnumNameRule.CustomizeAuto)
                    {
                        int n = 0;
                        fileName = str.Replace("\r\n", "").Replace(" ", "");
                        if (string.IsNullOrEmpty(fileName))
                        {
                            continue;
                        }

                        do
                        {
                            if (n == 0)
                            {
                                outputFile = Path.Combine(outputFolder, $"{fileName}.pdf");
                                //XErrorLogToolMDM.WriteLog($"PDF拆分文件名：{outputFile}");
                            }
                            else
                            {
                                outputFile = Path.Combine(outputFolder, $"{fileName}_Page_{i + n}.pdf");
                                //XErrorLogToolMDM.WriteLog($"PDF拆分文件名：{outputFile}");
                            }
                            n++;
                        } while (System.IO.File.Exists(outputFile));
                    }

                    //需要重写收容数
                    if (getLimit)
                    {
                        string limit = string.Empty;
                        string custCode = string.Empty;

                        RectangleF extractRegion = new RectangleF(300, 105, 60, 18);
                        PdfTextExtractor extractor = new PdfTextExtractor(splitPdf.Pages[0]);
                        PdfTextExtractOptions options = new PdfTextExtractOptions();
                        options.ExtractArea = extractRegion;

                        string str_temp = extractor.ExtractText(options);

                        limit = str_temp.Replace("\r\n", "").Replace(" ", "");

                        if (limit == "0")
                        {
                            extractRegion = new RectangleF(420, 90, 120, 30);
                            extractor = new PdfTextExtractor(splitPdf.Pages[0]);
                            options = new PdfTextExtractOptions();
                            options.ExtractArea = extractRegion;
                            str_temp = extractor.ExtractText(options);
                            custCode = str_temp.Replace("\r\n", "").Replace(" ", "");

                            if (dict_originalunit.ContainsKey(custCode))
                            {
                                limit = dict_originalunit[custCode].ToString();

                                // 获取特定的页面
                                PdfPageBase pageR = splitPdf.Pages[0];

                                // 根据页面创建 PdfTextReplacer 对象
                                PdfTextReplacer textReplacer = new PdfTextReplacer(pageR);

                                // 创建PdfTextReplaceOptions 对象
                                PdfTextReplaceOptions textReplaceOptions = new PdfTextReplaceOptions();
                                textReplaceOptions.SetReplacementArea(new RectangleF(300, 105, 60, 18));

                                // 指定文本替换的选项
                                textReplaceOptions.ReplaceType = PdfTextReplaceOptions.ReplaceActionType.WholeWord;
                                textReplaceOptions.ReplaceType = PdfTextReplaceOptions.ReplaceActionType.AutofitWidth;
                                textReplacer.Options = textReplaceOptions;

                                // 将所有目标文本的出现替换为新文本
                                textReplacer.ReplaceAllText("0", limit);
                            }
                        }
                    }

                    if (rename)
                    {
                        outputFile = outputFile.Replace("-", "");
                    }

                    // 保存为单独文件
                    splitPdf.SaveToFile(outputFile, FileFormat.PDF);

                    // 关闭当前文档
                    splitPdf.Close();

                }
                newPdf.Close();
                pdf.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }


        /// <summary>
        /// 按多个字段进行分组，将指定字段做求和，其他字段取组内第一条。
        /// </summary>
        /// <param name="source">原 DataTable</param>
        /// <param name="groupFields">要分组的字段名数组，例如 new[] {"part_code", "p_code"}</param>
        /// <param name="sumField">要求和的字段名，例如 "p_num"</param>
        /// <returns>合并后的 DataTable</returns>
        public static DataTable MergeRows(DataTable source, string[] groupFields, string sumField)
        {
            if (source == null || source.Rows.Count == 0) return source;

            DataTable result = source.Clone();

            // 分组
            var grouped = source.AsEnumerable()
                .GroupBy(r => string.Join("|", groupFields.Select(f => r[f]?.ToString() ?? "")));

            foreach (var group in grouped)
            {
                DataRow baseRow = group.First();
                DataRow newRow = result.NewRow();

                foreach (DataColumn col in source.Columns)
                {
                    if (col.ColumnName == sumField)
                    {
                        int total = group.Sum(r =>
                            string.IsNullOrEmpty(r[sumField]?.ToString())
                                ? 0
                                : Convert.ToInt32(r[sumField])
                        );
                        newRow[sumField] = total;
                    }
                    else
                    {
                        newRow[col.ColumnName] = baseRow[col.ColumnName];
                    }
                }

                result.Rows.Add(newRow);
            }

            return result;
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
                GHACService.DeleteWeekProject(ids);
                return ResParameter.Success("操作成功");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return ResParameter.Fail(e.Message);
            }


        }



    }

    public class BaQianDaiKB
    {
        public string factory { get; set; }
        public string veh_model { get; set; }
        public string lv { get; set; }
        public string part_name { get; set; }
        public string product_name_cn { get; set; }
        public string part_code { get; set; }
        public string qty { get; set; }
        public string box_cap { get; set; }
        public string r_location { get; set; }
        public string c_date { get; set; }
        public string c_time { get; set; }
        public string qrcode { get; set; }
        public string user { get; set; }

    }
    public class BaQianDaiT
    {
        public string no { get; set; }
        public string code { get; set; }

        public string plancode { get; set; }
        public string r_location { get; set; }
        public string part_code { get; set; }
        public string color { get; set; }
        public string c_date { get; set; }
        public string c_time { get; set; }
        public int qty { get; set; }
        public int box_qty { get; set; }
        public bool Is2VG { get; set; } = false;
    }
    public enum enumFileType
    {
        /// <summary>
        /// 通用Excel格式导入
        /// </summary>
        EXCEL,
        /// <summary>
        /// 周订单
        /// </summary>
        ORDER,
        /// <summary>
        /// 传票
        /// </summary>
        SUMMONS,
        /// <summary>
        /// 托标签
        /// </summary>
        PALLET,
        /// <summary>
        /// PLS看板
        /// </summary>
        PLS,
        /// <summary>
        /// 取货计划
        /// </summary>
        PLAN,
        /// <summary>
        /// 取货指示书
        /// </summary>
        INSTRUCTIONS,
        /// <summary>
        /// 追溯标签
        /// </summary>
        TRACE,
        /// <summary>
        /// SP 八千代的自定义取货计划
        /// </summary>
        PLAN2,
        /// <summary>
        /// 确认表
        /// </summary>
        CONFIRM,
        GHAC01

    }

    public enum enumOutType
    {
        HAOKOU,
        Other
    }
}
