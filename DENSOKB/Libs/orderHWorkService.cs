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
    public class orderHWorkService
    {
        // 与ordeImport 分开 防止后期需求变更查询有变化
        public DataTable getPageTB(Pagination paginationobj, string queryJson)
        {
            var strSql = new StringBuilder();
            strSql.Append("Select * from orderimport where 1=1");
            try
            {
                var queryParam = DataHelper.ToJObject(queryJson);
                if (!IsEmpty(queryParam["cusname"]))
                {
                    strSql.Append(string.Format(" and cusname like '%{0}%'", queryParam["cusname"].ToString()));
                }
                if (!IsEmpty(queryParam["cuscode"]))
                {
                    strSql.Append(string.Format(" and cuscode like '%{0}%'", queryParam["cuscode"].ToString()));
                }
                if (!IsEmpty(queryParam["otype"]))
                {
                    strSql.Append(string.Format(" and redundancy2 like '%{0}%'", queryParam["otype"].ToString()));
                }
                if (!IsEmpty(queryParam["orderbin"]))
                {
                    strSql.Append(string.Format(" and orderbin like '%{0}%'", queryParam["orderbin"].ToString()));
                }
                if (!IsEmpty(queryParam["createperson"]))
                {
                    strSql.Append(string.Format(" and createperson like '%{0}%'", queryParam["createperson"].ToString()));
                }
                if (!IsEmpty(queryParam["importstart"]))
                {
                    strSql.Append(string.Format(" and DATE(inportdate) >= DATE('{0}')", queryParam["importstart"].ToString()));
                }
                if (!IsEmpty(queryParam["importend"]))
                {
                    strSql.Append(string.Format(" and DATE(inportdate)<= DATE('{0}')", queryParam["importend"].ToString()));
                }
                // 发货时间  podate
                if (!IsEmpty(queryParam["sendstart"]))
                {
                    strSql.Append(string.Format(" and DATE(podate) >= DATE('{0}')", queryParam["sendstart"].ToString()));
                }
                if (!IsEmpty(queryParam["sendend"]))
                {
                    strSql.Append(string.Format(" and DATE(podate)<= DATE('{0}')", queryParam["sendend"].ToString()));
                }
                strSql.Append(" and (cuscode='CHAM' or cuscode='DHEC' or cuscode='GHAC')");
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