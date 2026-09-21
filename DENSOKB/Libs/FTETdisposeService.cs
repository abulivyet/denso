using DENSOKB.Helper;
using DENSOKB.Models;
using HelpLog.CommomMethod;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Web;

namespace DENSOKB.Libs
{
    public class FTETdisposeService
    {
        
        /// <summary>
        /// 查询客户量表
        /// </summary>
        /// <param name="queryJson"></param>
        /// <returns></returns>
        public static DataTable getoriginalunit(string queryJson)
        {
            var strSql = new StringBuilder();
            strSql.Append("Select * from originalunit where 1=1");
            try
            {
                var queryParam = DataHelper.ToJObject(queryJson);
                if (!orderimportService.IsEmpty(queryParam["cust_name"]))
                {
                    strSql.Append(string.Format(" and cust_name = '{0}'", queryParam["cust_name"].ToString()));
                }
                if (!orderimportService.IsEmpty(queryParam["cust_num"]))
                {
                    strSql.Append(string.Format(" and cust_num = '{0}'", queryParam["cust_num"].ToString()));
                }
                if (!orderimportService.IsEmpty(queryParam["back_num"]))
                {
                    strSql.Append(string.Format(" and back_num = '{0}'", queryParam["back_num"].ToString()));
                }
                if (!orderimportService.IsEmpty(queryParam["veh_model"]))//veh_model
                {
                    strSql.Append(string.Format(" and veh_model = '{0}'", queryParam["veh_model"].ToString()));
                }
                return DataHelper.Query(strSql.ToString());
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static DataTable GetPLdetail(string orderCode) {
            try
            {
                string strSql = @"select distinct remark from ftet_chain where ordercode = '{0}'";
                string sql = string.Format(strSql, orderCode);
                DataTable dataTable = DataHelper.Query(sql);
                if (dataTable.Rows.Count == 0) { return null; }
                //return dataTable.Rows[0]["remark"].ToString();
                return dataTable;
                
            }
            catch (Exception)
            {

                throw;
            }
        }

    }
}