using DENSOKB.Helper;
using DENSOKB.Libs;
using DENSOKB.Models;
using iText.Layout.Element;
using iText.StyledXmlParser.Jsoup.Select;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace DENSOKB.Controllers
{
    public class UnitController : ApiController
    {
        UnitService _userService = new UnitService();

        [HttpPost]
        public async Task<HttpResponseMessage> UploadFile()
        {
            // 确保请求包含 multipart/form-data
            if (!Request.Content.IsMimeMultipartContent())
            {
                return Request.CreateResponse(HttpStatusCode.UnsupportedMediaType,
                    "Content Type must be multipart/form-data");
            }

            try
            {
                var provider = new MultipartMemoryStreamProvider();
                await Request.Content.ReadAsMultipartAsync(provider);
                
                var fileContent = provider.Contents
                    .FirstOrDefault(c => c.Headers.ContentDisposition.Name?.Trim('"') == "files");

                // 读取 typeCode 参数
                var typeContent = provider.Contents
                    .FirstOrDefault(c => c.Headers.ContentDisposition.Name?.Trim('"') == "typeCode");

                string typeCode = typeContent != null
                    ? await typeContent.ReadAsStringAsync()
                    : null;

                if (fileContent != null)
                {
                    var       filename  = fileContent.Headers.ContentDisposition.FileName.Trim('"');
                    var       fileBytes = await fileContent.ReadAsByteArrayAsync();
                    
                    var    saveDirectory = ConfigurationManager.AppSettings["OriUnit"].ToString();
                    string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    string savePath = System.IO.Path.Combine(baseDirectory, saveDirectory);
                    if (!Directory.Exists(savePath))
                    {
                        Directory.CreateDirectory(savePath);
                    }
                    
                    var fileExt     = Path.GetExtension(filename);                // ".xlsx"
                    var newFileName = $"{typeCode}原单位表{fileExt}";
                    var fullPath    = Path.Combine(savePath, newFileName);

                    // 保存文件
                    File.WriteAllBytes(fullPath, fileBytes);
                    
                    IWorkbook workbook;
                    IFormulaEvaluator evaluator;
                    using (var stream = new MemoryStream(fileBytes))
                    {
                        if (filename.EndsWith(".xlsx"))
                        {
                            workbook = new XSSFWorkbook(stream); // .xlsx
                            evaluator = new XSSFFormulaEvaluator(workbook);

                        }
                        else if (filename.EndsWith(".xls"))
                        {
                            workbook = new HSSFWorkbook(stream); // .xls
                            evaluator = new HSSFFormulaEvaluator(workbook);

                        }
                        else
                        {
                            return Request.CreateResponse(HttpStatusCode.BadRequest, "只支持上传 .xls 或 .xlsx 格式的文件");
                        }

                        var sheet  = workbook.GetSheetAt(0); // 读取第一个工作表
                        var result = new List<OriginalUnit>();
                        for (int i = sheet.FirstRowNum+1; i <= sheet.LastRowNum; i++)
                        {
                            var row = sheet.GetRow(i);
                            if (row == null || row.Cells.All(c => c == null || string.IsNullOrWhiteSpace(c.ToString())))
                                continue; // 跳过空行
                            var entity = new OriginalUnit();
                            if (typeCode == "toyota")
                            {
                                entity.CustName = row.GetCell(0)?.ToString().Trim();
                                entity.CustCode = row.GetCell(1)?.ToString().Trim();
                                entity.RecvPort = row.GetCell(2)?.ToString().Trim();
                                entity.VehModel = row.GetCell(3)?.ToString().Trim();
                                entity.BackNum = row.GetCell(4)?.ToString().Trim();
                                entity.RecvArea = row.GetCell(5)?.ToString().Trim();
                                entity.DelLoc = row.GetCell(6)?.ToString().Trim();
                                entity.DensoNum = row.GetCell(7)?.ToString().Trim();
                                entity.CustNum = row.GetCell(8)?.ToString().Trim();
                                entity.CustNum2 = row.GetCell(8)?.ToString().Trim().Replace("-", "").PadRight(12, '0');
                                entity.PartName = row.GetCell(9)?.ToString().Trim();
                                entity.BoxCap           = GetIntCellValue(row.GetCell(10));
                                entity.PalletBox        = GetIntCellValue(row.GetCell(11));
                                entity.BoxType = row.GetCell(12)?.ToString().Trim();
                                entity.LocNum = row.GetCell(13)?.ToString().Trim();
                                entity.CombPallet = row.GetCell(14)?.ToString().Trim();
                                entity.UseFlag = row.GetCell(15)?.ToString().Trim();
                                   if (entity.PalletBox.HasValue && entity.PalletBox.Value != 0)
                                {
                                    entity.PalletPct = 1m / entity.PalletBox.Value;
                                }
                                else
                                {
                                    entity.PalletPct = 0m; 
                                }

                                //entity.sort = row.GetCell(16)?.ToString().Trim() == "" ? (int?)null : Convert.ToInt32(row.GetCell(16).ToString().Trim());
                            }else if (typeCode == "honda")
                            {
                                entity.CustName          = row.GetCell(0)?.ToString().Trim();
                                entity.factory           = row.GetCell(1)?.ToString().Trim();
                                entity.CustCode          = row.GetCell(2)?.ToString().Trim();
                                entity.RecvPort          = row.GetCell(3)?.ToString().Trim();
                                entity.traceabilityLabel = row.GetCell(4)?.ToString().Trim();
                                entity.VehModel          = row.GetCell(5)?.ToString().Trim();
                                entity.CustNum          = row.GetCell(6)?.ToString().Trim();
                                entity.CustNum2         = row.GetCell(6)?.ToString().Trim().Replace("-", "").PadRight(12, '0');
                                entity.DensoNum          = row.GetCell(7)?.ToString().Trim();
                                entity.productNameCn = PulicHelper.GetCellString(row.GetCell(8), evaluator);

                                entity.PartName         = row.GetCell(9)?.ToString().Trim();
                                entity.BoxCap           = GetIntCellValue(row.GetCell(10));
                                entity.PalletBox        = GetIntCellValue(row.GetCell(11));
                                entity.BoxType          = row.GetCell(12)?.ToString().Trim();
                                entity.LocNum           = row.GetCell(13)?.ToString().Trim();
                                entity.CombPallet       = row.GetCell(14)?.ToString().Trim();
                                entity.UseFlag        = row.GetCell(15)?.ToString().Trim();
                                if (entity.PalletBox.HasValue && entity.PalletBox.Value != 0)
                                {
                                    entity.PalletPct = 1m / entity.PalletBox.Value;
                                }
                                else
                                {
                                    entity.PalletPct = 0m; 
                                }
                                // entity.sort = row
                            }


                            result.Add(entity);
                        }

                        if (result.Count>0) {
                            _userService.saveUnits(result, typeCode);
                        }

                        return Request.CreateResponse(HttpStatusCode.OK, new
                        {
                            success = true,
                            message = "Excel 文件上传并读取成功",
                            data = result
                        });
                    }
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "未找到上传文件");
                }
            }
            catch (Exception e)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, e.Message);
            }


        }
        
        [HttpGet]
        public HttpResponseMessage DownloadFile(string type)
        {
            
            var    saveDirectory = ConfigurationManager.AppSettings["OriUnit"].ToString();
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string fileName      = "";
            if (type == "toyota")
            {
                fileName = "toyota原单位表.xlsx";
            }
            else if (type == "honda")
            {
                fileName = "honda原单位表.xlsx";
            }
            else
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "不支持的类型");
            }

            string filePath = Path.Combine(baseDirectory, saveDirectory, fileName);

            if (!File.Exists(filePath))
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "文件未找到");
            }

            var result = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new FileStream(filePath, FileMode.Open, FileAccess.Read))
            };

            result.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
            {
                FileName = fileName
            };
            result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

            return result;
        }
        
        
        private int? GetIntCellValue(ICell cell)
        {
            if (cell == null)
                return null;

            try
            {
                switch (cell.CellType)
                {
                    case CellType.Numeric:
                        return (int)cell.NumericCellValue;
                    case CellType.String:
                        var str = cell.StringCellValue.Trim();
                        return string.IsNullOrWhiteSpace(str) ? (int?)null : int.Parse(str);
                    case CellType.Formula:
                        // 尝试读取公式结果
                        if (cell.CachedFormulaResultType == CellType.Numeric)
                            return (int)cell.NumericCellValue;
                        else if (cell.CachedFormulaResultType == CellType.String)
                        {
                            var val = cell.StringCellValue.Trim();
                            return string.IsNullOrWhiteSpace(val) ? (int?)null : int.Parse(val);
                        }
                        break;
                    case CellType.Blank:
                        return null;
                }
            }
            catch
            {
                // 可根据需要记录日志或忽略
            }

            return null;
        }

    }
}