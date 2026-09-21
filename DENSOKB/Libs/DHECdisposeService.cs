using DENSOKB.Helper;
using DENSOKB.Models;
using HelpLog.CommomMethod;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Web;
using MySql.Data.MySqlClient;

namespace DENSOKB.Libs
{
    public class DHECdisposeService
    {
        public static void insertDayPlan(JArray rows)
        {
            //dhec_dayplan  重复上传 数据送货传票号唯一


            var strSql = new StringBuilder();
            try
            {
                var escapedCodes = rows.Select(code => $"'{code["送货传票号"].ToString().Replace("'", "''")}'");
                string inClause = string.Join(", ", escapedCodes);
                strSql.Append($" delete from dhec_dayplan where s_code in ({inClause})");
                int delrow = DataHelper.ExecuteSql(strSql.ToString());
                if (rows.Count > 0)
                {
                    strSql = new StringBuilder(@"insert into dhec_dayplan(line,carno,rec_location,planreachdate,planreachtime
                                            , planleavedate, planleavetime, reach_location, p_code, successtime
                                            , s_code, supplier, pn, reach_num, p_num, plancode
                                            , plandate, plantime, treachdate, treachtime, drops,fileName)
                                            values ");
                }
                foreach (var item in rows)
                {
                    var line           = item["路线组别"].ToString();
                    var carno          = item["取货车组"].ToString();
                    var rec_location   = item["目的地"].ToString();
                    var planreachdate  = item["计划到达日期"].ToString();
                    var planreachtime  = item["计划到达时间"].ToString();
                    var planleavedate  = item["计划离开日期"].ToString();
                    var planleavetime  = item["计划离开时间"].ToString();
                    var reach_location = item["目的地到达时间"].ToString();
                    var p_code         = item["取货传票号"].ToString();
                    var successtime    = item["作成时间"].ToString();
                    //Console.Write(item["作成时间"].ToString());
                    string cstime = item["作成时间"].ToString();
                    var s_code         = item["送货传票号"].ToString();
                    var supplier       = item["供应商"].ToString();
                    var pn             = item["零件号"].ToString();
                    var reach_num      = item["订单数量"].ToString();
                    var p_num          = item["取货数量"].ToString();
                    var plancode       = item["计划代码"].ToString();
                    var plandate       = item["计划到货日期"].ToString();
                    var plantime       = item["计划到货时间"].ToString();
                    var treachdate     = item["调整到货日期"].ToString();
                    var treachtime     = item["调整到货时间"].ToString();
                    var drop           = item["落点"].ToString();
                    var fileName       = item["fileName"].ToString();
                    if (string.IsNullOrEmpty(line))  // 空行
                    {
                        continue;
                    }
                    strSql.Append(string.Format(@" ('{0}','{1}','{2}','{3}','{4}',
                                            '{5}','{6}','{7}','{8}','{9}',
                                            '{10}','{11}','{12}','{13}','{14}','{15}',
                                            '{16}','{17}','{18}','{19}','{20}','{21}'),",
                                                line, carno, rec_location, planreachdate, planreachtime,
                                                planleavedate, planleavetime, reach_location, p_code, successtime,
                                                s_code, supplier, pn, reach_num, p_num, plancode,
                                                plandate, plantime, treachdate, treachtime, drop,fileName));
                }
                string Sql = strSql.ToString().TrimEnd(',')+";";
                int row=DataHelper.ExecuteSql(Sql);
            }
            catch (Exception e)
            {
                XErrorLogToolMDM.WriteLog("上传日计划错误"+e.Message);
                throw e;
            }
        }

        public static void DownloadCountPlus(string carno)
        {
            try
            {
                string strSql = @"update dhec_dayplan set downloadcount = ifnull(downloadcount,0) + 1 where carno = @carno";
                var parameters = new List<MySqlParameter>
                {
                    new MySqlParameter("@carno", carno)
                };
                DataHelper.ExecuteSql(strSql, parameters.ToArray());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
     
        // 获取日计划
        public DataTable getPageTB(Pagination paginationobj, string queryJson)
        {
            var strSql = new StringBuilder();
            strSql.Append(@"select 
                            DISTINCT
                            p_code,
                            carno,
                            downloadcount,
                            DATE_FORMAT(planleavedate, '%Y-%m-%d') planleavedate,
                            planleavetime
                            from dhec_dayplan
                            where 1 = 1 
                        ");
            try
            {
                var queryParam = DataHelper.ToJObject(queryJson);
                if (!IsEmpty(queryParam["carno"]))
                {
                    strSql.Append(string.Format(" and carno like '%{0}%'", queryParam["carno"].ToString()));
                }
                if (!IsEmpty(queryParam["plancode"]))
                {
                    strSql.Append(string.Format(" and plancode like '%{0}%'", queryParam["plancode"].ToString()));
                }
                if (!IsEmpty(queryParam["pn"]))
                {
                    strSql.Append(string.Format(" and pn like '%{0}%'", queryParam["pn"].ToString()));
                }

                // 计划到货日期  plandate
                if (!IsEmpty(queryParam["plandate"]))
                {
                    strSql.Append(string.Format(" and convert(plandate,podate) >= '{0}'", queryParam["plandate"].ToString()));
                }
                if (!IsEmpty(queryParam["plandate"]))
                {
                    strSql.Append(string.Format(" and convert(date,podate)<= '{0}'", queryParam["plandate"].ToString()));
                }
                if (!IsEmpty(queryParam["downloadcount"]))
                {
                    strSql.Append(string.Format(" and ifnull(downloadcount,0) = '{0}'", queryParam["downloadcount"].ToString()));
                }
                strSql.Append(@" GROUP BY p_code,
                            carno,
                            downloadcount,
                            planleavedate,
                            planleavetime ");
                string quyersql = DataBaseQuery.OutPageSql(strSql.ToString(), paginationobj);
                return DataHelper.Query(quyersql);
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        // 无分页
        public DataTable getnotPageTB( string Where)
        {
            var strSql = new StringBuilder();
            strSql.Append(@"SELECT
    plan.carno,                   -- 取货车组
    '-' AS plancode,              -- 计划代码
    plan.pn,                      -- 零件号
    plan.confirmRemark,
    SUM(plan.reach_num) AS reach_num, -- 订单数量
    SUM(plan.p_num) AS p_num,         -- 取货数量
    plan.drops,                   -- 落点
    COALESCE(unit_dsp.denso_num, unit_normal.denso_num) AS denso_num,  -- 电装品番
    COALESCE(unit_dsp.box_cap, unit_normal.box_cap) AS box_cap,         -- 收容数
    COALESCE(unit_dsp.pallet_box, unit_normal.pallet_box) AS pallet_box,-- 拖/箱数
    plan.fileName,
    DATE_FORMAT(plan.planreachdate, '%Y-%m-%d') AS planreachdate,
    '-' AS plandate,
    '-' AS plantime
FROM dhec_dayplan plan

--  drops <> 'DSP02'
         LEFT JOIN (
    SELECT *
    FROM (
             SELECT *,
                    ROW_NUMBER() OVER (PARTITION BY cust_num ORDER BY factory) AS rn
             FROM originalunit
             WHERE cust_name = 'DHEC'
AND factory <> 'A*SP**'
         ) t
    WHERE rn = 1
) unit_normal
                   ON plan.pn = unit_normal.cust_num
                       AND plan.drops <> 'DSP02'
-- DSP02 情况
         LEFT JOIN (
    SELECT *
    FROM originalunit
    WHERE cust_name = 'DHEC'
) unit_dsp
                   ON plan.pn = unit_dsp.cust_num
                       AND plan.drops = 'DSP02'
                       AND unit_dsp.factory = 'A*SP**'

WHERE 1=1
                        ");
            try
            {
                if (!string.IsNullOrEmpty(Where))
                {
                    strSql.Append(Where);
                }
                strSql.Append(@" GROUP BY
    plan.carno,
    plan.pn,
    plan.drops,
    plan.confirmRemark,
    plan.fileName,
    plan.planreachdate,
    COALESCE(unit_dsp.denso_num, unit_normal.denso_num),
    COALESCE(unit_dsp.box_cap, unit_normal.box_cap),
    COALESCE(unit_dsp.pallet_box, unit_normal.pallet_box)
ORDER BY plan.pn, plan.drops
                        ");

                XErrorLogToolMDM.WriteLog("getnotPageTB:" + strSql);
                //string quyersql = DataBaseQuery.OutPageSql(strSql.ToString(), paginationobj);
                return DataHelper.Query(strSql.ToString());
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        public void InsertSummonOrder(Dictionary<int, List<string>> dictionary) {
            try
            {
                if (dictionary == null || dictionary.Count == 0) return;

                // 收集所有需要插入的 summons_code
                HashSet<string> codesToDelete = new HashSet<string>();
                foreach (var kv in dictionary)
                {
                    var values = kv.Value;
                    if (values == null || values.Count < 2) continue;

                    string col1 = values[0].Replace("'", "''").Replace("-", ""); // summons_code
                    codesToDelete.Add(col1);
                }

                if (codesToDelete.Count == 0) return;

                StringBuilder strSql = new StringBuilder();

                // 先删除已有数据
                strSql.Append("DELETE FROM dhec_summons_order WHERE summons_code IN (");
                strSql.Append(string.Join(",", codesToDelete.Select(c => $"'{c}'")));
                strSql.Append("); ");

                // 再批量插入
                strSql.Append("INSERT INTO dhec_summons_order (summons_code, ordercode) VALUES ");

                bool firstRow = true;
                foreach (var kv in dictionary)
                {
                    var values = kv.Value;
                    if (values == null || values.Count < 2) continue;

                    string col1 = values[0].Replace("'", "''").Replace("-", ""); // summons_code
                    string col2 = values[1].Replace("'", "''");                  // ordercode

                    if (!firstRow) strSql.Append(", ");
                    strSql.AppendFormat("('{0}', '{1}')", col1, col2);
                    firstRow = false;
                }

                if (!firstRow) // 有数据才执行
                {
                    DataHelper.Query(strSql.ToString());
                }



            }
            catch (Exception)
            {

                throw;
            }
        
        }


        // 传条件获取数据
        public DataTable getdayplan(string sqlWhere)
        {
            var strSql = new StringBuilder();
            try
            {
                strSql.Append(" select * from dhec_dayplan where 1=1 ");
                if (!string.IsNullOrEmpty(sqlWhere))
                {
                    strSql.Append(sqlWhere);
                }
                // strSql.Append(" order by  carno,drops,plancode,plandate,plantime,s_code");
                strSql.Append(" order by  pn,drops,plandate,plantime+0,s_code+0");
                XErrorLogToolMDM.WriteLog("getdayplan:" + strSql);
                return DataHelper.Query(strSql.ToString());
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public static bool IsEmpty(object value)
        {
            if (value != null && !string.IsNullOrEmpty(value.ToString()))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public string getCreatePerson(string fileName)
        {
            try
            {
                string strSql = @"select distinct createperson from orderimport where redundancy3 = '{0}' ";
                strSql = string.Format(strSql, fileName);
                DataTable dataTable = DataHelper.Query(strSql.ToString());
                if (dataTable.Rows.Count == 0)
                {
                    throw new Exception("获取传票做成者失败");
                }
                return dataTable.Rows[0]["createperson"].ToString();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public static void DeleteWeekProject(List<int> ids)
        {
            if (ids == null || ids.Count == 0) return;

            // 参数占位
            string paramNames = string.Join(",", ids.Select((_, index) => $"@id{index}"));
            string idList = string.Join(",", ids);

            // 先查出关联的 p_code（ord_weekorder 表）
            string querySql = $"SELECT p_code FROM ord_weekorder WHERE id IN ({idList})";
            DataTable dt = DataHelper.Query(querySql);

            List<string> pCodes = dt.AsEnumerable()
                .Select(row => row["p_code"]?.ToString())
                .Where(code => !string.IsNullOrEmpty(code))
                .Distinct()
                .ToList();

            // 删除 dhec_dayplan 里与 p_code 或 s_code 关联的记录
            if (pCodes.Count > 0)
            {
                string pCodeParamNames = string.Join(",", pCodes.Select((_, index) => $"@pcode{index}"));
                string deleteDhecSql = $"DELETE FROM dhec_dayplan WHERE p_code IN ({pCodeParamNames}) OR s_code IN ({pCodeParamNames})";
                var pCodeParameters = pCodes.Select((code, index) => new MySqlParameter($"@pcode{index}", code)).ToArray();
                DataHelper.ExecuteSql(deleteDhecSql, pCodeParameters);
            }

            // 删除 ord_weekorder
            string deleteSql = $"DELETE FROM ord_weekorder WHERE id IN ({paramNames})";
            var deleteParameters = ids.Select((id, index) => new MySqlParameter($"@id{index}", id)).ToArray();
            DataHelper.ExecuteSql(deleteSql, deleteParameters);
        }
    }
}