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
    public class orderOutGoodsService
    {
        public DataTable getPageTB(Pagination paginationobj, string queryJson)
        {
            var strSql = new StringBuilder();
            strSql.Append("Select *,DATE_FORMAT(c_date, '%Y-%m-%d') as c_datenew,DATE_FORMAT(r_date, '%Y-%m-%d') as r_datenew from ord_weekorder where 1=1 and r_location like 'D%' ");
            try
            {
                var queryParam = DataHelper.ToJObject(queryJson);
                if (!IsEmpty(queryParam["sup_code"])) //供应商编号
                {
                    strSql.Append(string.Format(" and sup_code like '%{0}%'", queryParam["sup_code"].ToString()));
                }
                if (!IsEmpty(queryParam["p_code"]))// 传票号
                {
                    strSql.Append(string.Format(" and p_code like '%{0}%'", queryParam["p_code"].ToString()));
                }
                if (!IsEmpty(queryParam["o_version"]))// 版本
                {
                    strSql.Append(string.Format(" and o_version like '%{0}%'", queryParam["o_version"].ToString()));
                }
                if (!IsEmpty(queryParam["order_code"]))// 订单编号
                {
                    strSql.Append(string.Format(" and order_code like '%{0}%'", queryParam["order_code"].ToString()));
                }
                if (!IsEmpty(queryParam["r_location"]))
                {
                    strSql.Append(string.Format(" and r_location like '%{0}%'", queryParam["r_location"].ToString()));
                }
                if (!IsEmpty(queryParam["part_code"]))
                {
                    strSql.Append(string.Format(" and part_code like '%{0}%'", queryParam["part_code"].ToString()));
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