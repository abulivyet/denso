using DENSOKB.Helper;
using HelpLog.CommomMethod;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Data;
using System.IO;
using System.Web;
using System.Web.Http;

namespace DENSOKB.Controllers
{
    public class ExportRptController : ApiController
    {
        [HttpGet]
        public void getRpt([FromUri] string cuscode, [FromUri] string startdate, [FromUri] string enddate)
        {
            EnumCustom custom = (EnumCustom)Enum.Parse(typeof(EnumCustom), cuscode);

            startdate = startdate == null ? string.Empty : startdate;

            enddate = enddate == null ? string.Empty : enddate;

            string sql = string.Empty;

            switch (custom)
            {
                case EnumCustom.FTCE:
                    sql = @"select t.shipmentDate, arrivalduedate, orderCode, inclusionNumber, customerPartNumber, dnPartNumber, backNumber, capacity, boardcount, inclusionquantity, category
from (SELECT DISTINCT DATE_FORMAT(shipmentDate, '%Y/%m/%d')          AS shipmentDate,
                      DATE_FORMAT(arrivalduedate, '%Y/%m/%d')        AS arrivalduedate,
                      ordercode,
                      inclusionNumber,
                      customerPartNumber,
                      ori.denso_num as dnPartNumber,
                      IFNULL(NULLIF(o.backNumber, ''), ori.back_num) AS backNumber,
                      CAST(capacity AS SIGNED)                       AS capacity,
                      CAST(boardcount AS SIGNED)                     AS boardcount,
                      CAST(inclusionquantity AS SIGNED)              AS inclusionquantity,
                      category,
                      shipmentDate as date
      FROM orderinfo o
               left join originalunit ori on o.customerPartNumber = ori.cust_num
          and ori.cust_name = 'FTEC'
 AND ori.cust_code = '10001600'
      WHERE 1 = 1
        and ifnull(category, '') not in ('FTET','CHAM','补给品')

      union all

      SELECT DISTINCT DATE_FORMAT(shipmentDate, '%Y/%m/%d')          AS shipmentDate,
                      DATE_FORMAT(arrivalduedate, '%Y/%m/%d')        AS arrivalduedate,
                      ordercode,
                      inclusionNumber,
                      customerPartNumber,
                      ori.denso_num as dnPartNumber,
                      IFNULL(NULLIF(o.backNumber, ''), ori.back_num) AS backNumber,
                      CAST(capacity AS SIGNED)                       AS capacity,
                      CAST(boardcount AS SIGNED)                     AS boardcount,
                      CAST(inclusionquantity AS SIGNED)              AS inclusionquantity,
                      category,
                      shipmentDate as date
      FROM orderinfo o
               left join originalunit ori on o.customerPartNumber = ori.cust_num
          and ori.cust_name = 'FTEC'
          and ori.cust_code = '10001601'
      WHERE 1 = 1
        and ifnull(category, '') not in ('FTET','CHAM')
        and category = '补给品'
      ) t where 1=1
                         ";
                    if (startdate != string.Empty)
                        sql += string.Format("and date >= '{0}' ", startdate);
                    if (enddate != string.Empty)
                        sql += string.Format("and date <= '{0}' ", enddate);
                    sql += "order by DATE_FORMAT(t.shipmentDate, '%Y/%m/%d')";
                    break;
                case EnumCustom.FTET:
                    sql = @"select DISTINCT DATE_FORMAT(SHIPMENTDATE, '%Y/%m/%d')   AS ShipmentDate,
                            DATE_FORMAT(ARRIVALDUEDATE, '%Y/%m/%d') AS ARRIVALDUEDATE,
                            ORDERCODE,
                            INCLUSIONNUMBER,
                            CUSTOMERPARTNUMBER,
                            DNPARTNUMBER,
                            BACKNUMBER,
                            CAPACITY,
                            BOARDCOUNT,
                            INCLUSIONQUANTITY,
                            CATEGORY
            from orderinfo
            where category = 'FTET' AND 1=1 ";
                    if (startdate != string.Empty)
                        sql += string.Format("and shipmentDate >= '{0}' ", startdate);
                    if (enddate != string.Empty)
                        sql += string.Format("and shipmentDate <= '{0}' ", enddate);
                    sql += "order by DATE_FORMAT(SHIPMENTDATE, '%Y/%m/%d')";
                    break;
                case EnumCustom.CHAM:
                    sql = @"select DATE_FORMAT(t.shipmentDate, '%Y/%m/%d')    AS ShipmentDate,
       DATE_FORMAT(t.demanded_date, '%Y/%m/%d')   as demanded_date,
       receiving_time,
       movingIn_code,
       project_code,
       court_summons,
       material_code,
       denso_num,
       box_cap,
       boardCount,
       quantity
from (select distinct info.shipmentDate   AS ShipmentDate,
                      week.demanded_date  as demanded_date,
                      receiving_time,
                      movingIn_code,
                      project_code,
                      court_summons,
                      material_code,
                      ori.denso_num,
                      ori.box_cap,
                      (week.quantity / ori.box_cap)                 as boardCount,
                      ifnull(info.inclusionQuantity, week.quantity) as quantity
      from cham_ord_week week
               left join originalunit ori on week.material_code = ori.cust_num AND ORI.cust_name = 'CHAM'
               left join orderinfo info on info.orderCode = week.court_summons and info.category = 'CHAM'
      union all
      -- 补给品
      select shipmentDate AS ShipmentDate,
             arrivalDueDate as demanded_date,
             NULL as receiving_time,
             ReceiptPlace as movingIn_code,
             planCode as project_code,
             orderCode as court_summons,
             customerPartNumber as material_code,
             dnPartNumber as denso_num,
             capacity as box_cap,
             boardCount as boardCount,
             inclusionQuantity as quantity
      from orderinfo
      where category = 'CHAM'
        and orderCode not in (select court_summons from cham_ord_week)) t
where  1=1  ";
                    if (startdate != string.Empty)
                        sql += string.Format("and demanded_date >= '{0}' ", startdate);
                    if (enddate != string.Empty)
                        sql += string.Format("and demanded_date <= '{0}' ", enddate);
                    sql += " order by demanded_date";
                    break;
                case EnumCustom.DHEC:
                    sql = @"
                            select  DISTINCT
                            -- planreachdate,planreachtime,
                            DATE_FORMAT(planleavedate, '%Y-%m-%d') planleavedate,planleavetime,
                            carno, p_code,
                            plancode,drops,
                            CONVERT(DATE_FORMAT(plandate, '%Y%m%d'), SIGNED ) plandate,
                            CONVERT(plantime , SIGNED ) plantime,
                            s_code,pn,denso_num densopn, reach_num,'' remark
                            from  
                                dhec_dayplan P
                            left join originalunit ORI on P.pn = ORI.cust_num
                            AND ORI.factory = P.plancode
                            where 1 = 1 ";
                    if (startdate != string.Empty)
                        sql += string.Format("and planreachdate >= '{0}' ", startdate);
                    if (enddate != string.Empty)
                        sql += string.Format("and planreachdate <= '{0}' ", enddate);
                    sql += "order by DATE_FORMAT(planleavedate, '%Y-%m-%d')";
                    break;
                case EnumCustom.GHAC:
                    sql = @"select sup_code, r_date, c_date, c_time, 车次, p_code, r_location, part_code, dn, box_cap, sum(order_num) as order_num
from (
-- 号口 量产
         select pick.supplier as sup_code,
                DATE_FORMAT(PICK.pickdate, '%Y/%m/%d') as r_date,
                DATE_FORMAT(week.c_date, '%Y/%m/%d')   as c_date,
                week.c_time,
                PICK.carno                             as 车次,
                PICK.p_code,
                week.r_location,
                pick.pn as part_code,
                ori.denso_num                          as dn,
                ori.box_cap,
                FORMAT(pick.planqty, 0)                as order_num,
                pickdate
         from pickupplan PICK
                  LEFT JOIN ord_weekorder week ON PICK.p_code = WEEK.p_code
                  left join originalunit ori
                            on week.part_code = ori.cust_num and PICK.supplier = ori.recv_port AND ORI.factory NOT LIKE '%SP%'
                                AND ori.factory LIKE CONCAT('%', CAST(CONV(RIGHT(PICK.plancode, 2), 10, 10) AS UNSIGNED), '厂%')
         where ifnull(PICK.factory, '') not like '%SP%'
         and pick.p_code not in (select p_code from pickupplan where no like '%八千代%')
         and pick.no not like '%八千代%'
         union all
         select pick.supplier as sup_code,
                DATE_FORMAT(PICK.pickdate, '%Y/%m/%d') as r_date,
                DATE_FORMAT(week.c_date, '%Y/%m/%d')   as c_date,
                week.c_time,
                PICK.carno                             as 车次,
                PICK.p_code,
                week.r_location,
                pick.pn as part_code,
                ori.denso_num                          as dn,
                ori.box_cap,
                FORMAT(pick.planqty, 0)                as order_num,
                pickdate
         from pickupplan PICK
                  LEFT JOIN ord_weekorder week ON PICK.p_code = WEEK.p_code
                  left join originalunit ori
                            on week.part_code = ori.cust_num and PICK.supplier = ori.recv_port AND ORI.factory NOT LIKE '%SP%'
                                AND ori.factory LIKE CONCAT('%', CAST(CONV(RIGHT(PICK.plancode, 2), 10, 10) AS UNSIGNED), '厂%')
         where ifnull(PICK.factory, '') not like '%SP%'
                and pick.no like '%八千代%'


union all

select  pick.supplier,
                DATE_FORMAT(PICK.pickdate, '%Y/%m/%d') as r_date,
                DATE_FORMAT(pick.recdate, '%Y/%m/%d')   as c_date,
                pick.rectime,
                PICK.carno                             as 车次,
                PICK.p_code,
                week.r_location,
                pick.pn,
                ori.denso_num                          as dn,
                ori.box_cap,
                FORMAT(week.order_num, 0)              as order_num,
                pickdate
from ord_weekorder week
         LEFT JOIN pickupplan PICK ON PICK.p_code = WEEK.p_code
         left join originalunit ori on week.part_code = ori.cust_num
    AND ori.recv_port = sup_code
    AND ORI.factory = 'SP'
where
        PICK.factory  like '%SP%'

         ) t where 1=1 ";
                    if (startdate != string.Empty)
                        sql += string.Format("and pickdate >= '{0}' ", startdate);
                    if (enddate != string.Empty)
                        sql += string.Format("and pickdate <= '{0}' ", enddate);
                    sql += "group by t.sup_code,r_date,pickdate,c_date,c_time,车次,p_code,r_location,part_code,dn,box_cap";
                    sql += " order by pickdate,c_time";
                    break;
                default:
                    break;
            }

            DataTable data = DataHelper.Query(sql);
            XErrorLogToolMDM.WriteLog(sql);

            exportExcel(custom, data);
        }

        private void exportExcel(EnumCustom custom, DataTable data)
        {
            if (data.Rows.Count == 0)
            {
                return ;
            }

            string basePath = HttpContext.Current.Server.MapPath("~/Report");

            string tempFilePath = Path.Combine(basePath, "template", getTemplateFileName(custom));

            string outputPath = Path.Combine(basePath, "output");

            string outputFile = Path.Combine(outputPath, getOutFileName(custom));

            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            using (FileStream fs = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook;
                ISheet    sheet;


                if (Path.GetExtension(tempFilePath).ToLower() == ".xlsx")
                {
                    workbook = new XSSFWorkbook(fs);
                }
                else
                {
                    workbook = new HSSFWorkbook(fs);
                }

                // 获取或创建工作表
                sheet = workbook.GetSheetAt(0);

                ICellStyle style = workbook.CreateCellStyle();
                style.VerticalAlignment = VerticalAlignment.Center;
                IFont font = workbook.CreateFont();
                font.FontHeightInPoints = 10; // 设置字体大小为10号
                font.FontName           = "宋体";
                style.SetFont(font);

                ICellStyle numberStyle = workbook.CreateCellStyle();
                // 设置右对齐
                numberStyle.Alignment = HorizontalAlignment.Right;
                // 设置数值格式（保留2位小数）
                IDataFormat format = workbook.CreateDataFormat();
                numberStyle.DataFormat = format.GetFormat("#,##0.00");
                numberStyle.SetFont(font);

                //第一行为模板行，每个模板保留一行数据，用来取样式，最后吧这一行删掉
                IRow rowbase = sheet.GetRow(1);

                for (int i = 0; i < data.Rows.Count; i++)
                {
                    IRow row = sheet.CreateRow(2 + i);

                    for (int j = 0; j < data.Columns.Count; j++)
                    {
                        ICell cell = row.CreateCell(j);
                        if (rowbase.GetCell(j) != null)
                            cell.CellStyle = rowbase.GetCell(j).CellStyle;
                        else
                            cell.CellStyle = style;
                        if (data.Columns[j].DataType == typeof(Int64) || data.Columns[j].DataType == typeof(Int32))
                        {
                            if (data.Rows[i][j] == DBNull.Value)
                            {
                                cell.SetCellValue(string.Empty); // 或者 0，看你业务
                            }
                            else
                            {
                                cell.SetCellValue(Convert.ToInt32(data.Rows[i][j]));
                            }
                            //cell.SetCellValue(Convert.ToInt32(data.Rows[i][j]));
                        }
                        else
                        {
                            cell.SetCellValue(data.Rows[i][j].ToString());
                        }
                    }
                }

                // 删除第2行（索引从0开始）
                int rowToDelete = 1; // 第3行
                sheet.RemoveRow(sheet.GetRow(rowToDelete));

                // 移动后续行上移
                sheet.ShiftRows(rowToDelete + 1, sheet.LastRowNum, -1);

                using (FileStream fsout = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                {
                    workbook.Write(fsout);
                }

                DownloadXls(outputFile);
                //删除临时文件
                // File.Delete(outputFile);

            }


        }


        private static void DownloadXls(string outputFile)
        {
            string       fileName = Path.GetFileName(outputFile);
            HttpResponse response = HttpContext.Current.Response;

            try
            {
                // 清空响应
                response.Clear();
                response.ClearHeaders();
                response.ClearContent();

                // 设置响应头
                response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                response.AddHeader("Content-Disposition", $"attachment; filename={HttpUtility.UrlPathEncode(fileName)}");

                // 添加缓存控制头
                response.Cache.SetCacheability(HttpCacheability.NoCache);

                // 读取文件内容并写入响应流（使用 FileStream）
                using (FileStream fs = new FileStream(outputFile, FileMode.Open, FileAccess.Read))
                {
                    byte[] buffer = new byte[4096]; // 4 KB buffer
                    int    bytesRead;
                    while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        response.OutputStream.Write(buffer, 0, bytesRead);
                    }
                }

                // 刷新并结束响应
                response.Flush();
                response.End();
            }
            catch (Exception ex)
            {
                // 记录错误日志
                System.Diagnostics.Trace.WriteLine($"文件下载错误: {ex.Message}");
                response.Clear();
                response.Write("文件下载失败，请稍后重试");
                response.End();
                
            }
            finally
            {
                // 确保资源释放
                response.Close();
            }
        }

        /// <summary>
        /// 根据客户获取模板文件名
        /// </summary>
        /// <param name="custom"></param>
        /// <returns></returns>
        private string getTemplateFileName(EnumCustom custom)
        {
            string FileName = string.Empty;
            switch (custom)
            {
                case EnumCustom.FTCE:
                    FileName = "FTCE订单数据对账资料.xlsx";
                    break;
                case EnumCustom.FTET:
                    FileName = "FTET订单数据对账资料.xlsx";
                    break;
                case EnumCustom.CHAM:
                    FileName = "CHAM订单数据.xlsx";
                    break;
                case EnumCustom.DHEC:
                    FileName = "DHEC订单数据.xlsx";
                    break;
                case EnumCustom.GHAC:
                    FileName = "GHAC订单数据.xlsx";
                    break;
                default:
                    break;
            }

            return FileName;
        }

        private string getOutFileName(EnumCustom custom)
        {
            string outFileName = string.Empty;

            string TemplateFileName = string.Empty;

            switch (custom)
            {
                case EnumCustom.FTCE:
                    TemplateFileName = "FTCE订单数据对账资料.xlsx";
                    break;
                case EnumCustom.FTET:
                    TemplateFileName = "FTET订单数据对账资料.xlsx";
                    break;
                case EnumCustom.CHAM:
                    TemplateFileName = "CHAM订单数据.xlsx";
                    break;
                case EnumCustom.DHEC:
                    TemplateFileName = "DHEC订单数据.xlsx";
                    break;
                case EnumCustom.GHAC:
                    TemplateFileName = "GHAC订单数据.xlsx";
                    break;
                default:
                    break;
            }

            outFileName = Path.GetFileNameWithoutExtension(TemplateFileName) +
                          $"{DateTime.Now:yyyyMMddHHmmssffff}.xlsx";

            return outFileName;
        }
    }

    public enum EnumCustom
    {
        FTCE,
        FTET,
        CHAM,
        DHEC,
        GHAC
    }
}