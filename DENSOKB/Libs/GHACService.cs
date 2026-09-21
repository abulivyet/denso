using DENSOKB.Helper;
using DENSOKB.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Web;

namespace DENSOKB.Libs
{
    public class GHACService
    {
        public static DataTable getPageTB(Pagination paginationobj, string queryJson)
        {
            var strSql = new StringBuilder();
            strSql.Append(@"select  *,DATE_FORMAT(c_date, '%Y-%m-%d') as c_datenew,DATE_FORMAT(r_date, '%Y-%m-%d') as r_datenew from  ord_weekorder
                        where 1=1 and r_location like 'G%' 
                        ");
            try
            {
                var queryParam = DataHelper.ToJObject(queryJson);
                if (!IsEmpty(queryParam["r_location"]))  // 
                {
                    strSql.Append(string.Format(" and r_location like '%{0}%'", queryParam["r_location"].ToString()));
                }
                if (!IsEmpty(queryParam["part_code"]))  // 
                {
                    strSql.Append(string.Format(" and part_code like '%{0}%'", queryParam["part_code"].ToString()));
                }
                if (!IsEmpty(queryParam["p_code"])) // 
                {
                    strSql.Append(string.Format(" and p_code like '%{0}%'", queryParam["p_code"].ToString()));
                }
                if (!IsEmpty(queryParam["o_version"])) // 
                {
                    strSql.Append(string.Format(" and o_version like '%{0}%'", queryParam["o_version"].ToString()));
                }
                if (!IsEmpty(queryParam["sendStart"])) // 
                {
                    strSql.Append(string.Format(" and c_date >= '{0}'", queryParam["sendStart"]));
                }
                if (!IsEmpty(queryParam["sendEnd"])) // 
                {
                    strSql.Append(string.Format(" and c_date <= '{0}'", queryParam["sendEnd"]));
                }

           
                string quyersql = DataBaseQuery.OutPageSql(strSql.ToString(), paginationobj);
                return DataHelper.Query(quyersql);
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public static DataTable getPagePlanTB(Pagination paginationobj, string queryJson)
        {
            paginationobj.sidx = "create_date";
            paginationobj.sord = "desc";
            var strSql = new StringBuilder();
            strSql.Append(@"SELECT *
FROM (
    SELECT no,
           LEFT(version,2) version,
           carno,
           line,
           supplier,
           suppler_sub,
           factory,
           plancode,
           transcode,
           DATE_FORMAT(pickdate, '%Y-%m-%d ') pickdate,
           picktime,
           downloadcount,
           DATE_FORMAT(create_date, '%Y-%m-%d %H:%i:%s') create_date,
           ROW_NUMBER() OVER (
               PARTITION BY no, LEFT(version, 2), carno, line, supplier, suppler_sub, factory, plancode, transcode, DATE(pickdate) -- , picktime, downloadcount
               ORDER BY create_date DESC
           ) AS rn
    FROM pickupplan
) t
WHERE rn = 1

                        ");
            try
            {
                var queryParam = DataHelper.ToJObject(queryJson);
                if (!IsEmpty(queryParam["ctype"]))
                {
                    switch (queryParam["ctype"].ToString())
                    {
                        case "SLP": // 量产
                            strSql.Append(string.Format(" and no not like '%八千代%' and no not like '%sp%' and no not like '%阶段品%'"));
                            break;
                        case "EIGHT": // 八千代
                            strSql.Append(string.Format(" and no like '%八千代%'"));
                            break;
                        case "SP":  // sp
                            strSql.Append(string.Format(" and no like '%sp%'"));
                            break;
                        case "JD": // 阶段品
                            strSql.Append(string.Format(" and no like '%阶段品%'"));
                            break;
                    }

                }
                if (!IsEmpty(queryParam["no"]))  // 
                {
                    strSql.Append(string.Format(" and no like '%{0}%'", queryParam["no"].ToString()));
                }
                if (!IsEmpty(queryParam["version"]))  // 
                {
                    strSql.Append(string.Format(" and version like '%{0}%'", queryParam["version"].ToString()));
                }
                if (!IsEmpty(queryParam["carno"])) // 
                {
                    strSql.Append(string.Format(" and carno like '%{0}%'", queryParam["carno"].ToString()));
                }
                if (!IsEmpty(queryParam["plancode"])) // 
                {
                    strSql.Append(string.Format(" and plancode like '%{0}%'", queryParam["plancode"].ToString()));
                }

                if (!IsEmpty(queryParam["importStart"]))
                {
                    strSql.Append(string.Format(" and DATE(pickdate) >= DATE('{0}')", queryParam["importStart"].ToString()));
                }
                if (!IsEmpty(queryParam["importEnd"]))
                {
                    strSql.Append(string.Format(" and DATE(pickdate)<= DATE('{0}')", queryParam["importEnd"].ToString()));
                }
                if (!IsEmpty(queryParam["downloadcount"]))
                {
                    strSql.Append(string.Format(" and ifnull(downloadcount,0) = '{0}'", queryParam["downloadcount"].ToString()));
                }


                string quyersql = DataBaseQuery.OutPageSql(strSql.ToString(), paginationobj);
                return DataHelper.Query(quyersql);
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        public static void DeleteWeekProject(List<int> ids)
        {
            string paramNames = string.Join(",", ids.Select((_, index) => $"@id{index}"));

            // 先查出关联的 p_code
            string idList = string.Join(",", ids);
            string querySql = $"SELECT p_code FROM ord_weekorder WHERE id IN ({idList})";
            DataTable dt = DataHelper.Query(querySql);

            List<string> pCodes = dt.AsEnumerable()
                .Select(row => row["p_code"]?.ToString())
                .Where(code => !string.IsNullOrEmpty(code))
                .Distinct()
                .ToList();

            //  删除 pickupplan 关联数据
            if (pCodes.Count > 0)
            {
                string pCodeParamNames = string.Join(",", pCodes.Select((_, index) => $"@pcode{index}"));
                string deletePickupSql = $"DELETE FROM pickupplan WHERE p_code IN ({pCodeParamNames})";
                var pCodeParameters = pCodes.Select((code, index) => new MySqlParameter($"@pcode{index}", code)).ToArray();
                DataHelper.ExecuteSql(deletePickupSql, pCodeParameters);
            }

            // 删除 ord_weekorder 数据
            string deleteSql = $"DELETE FROM ord_weekorder WHERE id IN ({paramNames})";
            var deleteParameters = ids.Select((id, index) => new MySqlParameter($"@id{index}", id)).ToArray();
            DataHelper.ExecuteSql(deleteSql, deleteParameters);
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
    }
}