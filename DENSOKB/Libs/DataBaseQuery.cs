using DENSOKB.Helper;
using DENSOKB.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Web;

namespace DENSOKB.Libs
{
    public class DataBaseQuery
    {
        public static string OutPageSql(string sql, Pagination pagination)
        {

            int total = pagination.records;
            pagination.sidx = DataHelper.SqlFilters(pagination.sidx);
            if (string.IsNullOrEmpty(pagination.sidx))
            {
                pagination.sidx = "";
                pagination.sord = "asc";
            }

            StringBuilder sb = new StringBuilder();
            // orderField 排序字段  isAsc 排序类型  pageSize 每页数据条数  pageIndex 页码
            sb.Append(DataHelper.SqlPageSql(sql, pagination.sidx, pagination.sord.ToLower() == "asc" ? true : false, pagination.limit, pagination.page));
            DataTable rowDt = DataHelper.Query(
                    string.Format("Select Count(1) as collnum From ({0}) As t", sql)
                    );
            pagination.records = Convert.ToInt32(rowDt.Rows.Count>=0? rowDt.Rows[0][0].ToString():"-1" ); // 总行数
            return sb.ToString();

        }

      }
}