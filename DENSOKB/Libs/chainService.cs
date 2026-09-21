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
    public class chainService
    {
        /// <summary>
        ///  分页查询 
        /// </summary>
        /// <param name="paginationobj"></param>
        /// <param name="queryJson"></param>
        /// <returns></returns>
        public static DataTable getftet_chainPage(Pagination paginationobj, string queryJson)
        {
            var strSql = new StringBuilder();
            strSql.Append("Select *,DATE_FORMAT(labeldate, '%Y-%m-%d') newdate from ftet_chain where 1=1");
            try
            {
                var queryParam = DataHelper.ToJObject(queryJson);
                if (!orderimportService.IsEmpty(queryParam["ordercode"]))// 单号
                {
                    strSql.Append(string.Format(" and ordercode = '{0}'", queryParam["ordercode"].ToString()));
                }
                if (!orderimportService.IsEmpty(queryParam["labeldate"]))// 日期现在不准 先不用
                {
                    strSql.Append(string.Format(" and labeldate = '{0}'", queryParam["labeldate"].ToString()));
                }
                if (!orderimportService.IsEmpty(queryParam["bei"]))
                {
                    strSql.Append(string.Format(" and bei = '{0}'", queryParam["bei"].ToString()));
                }
                string quyersql = DataBaseQuery.OutPageSql(strSql.ToString(), paginationobj);
                return DataHelper.Query(quyersql);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// 获取FTET链信息
        /// </summary>
        /// <returns></returns>
        public static DataTable getftet_chain(string queryJson)
        {
            var strSql = new StringBuilder();
            strSql.Append("Select * from ftet_chain where 1=1");
            try
            {
                var queryParam = DataHelper.ToJObject(queryJson);
                if (!orderimportService.IsEmpty(queryParam["ordercode"]))// 单号
                {
                    strSql.Append(string.Format(" and ordercode = '{0}'", queryParam["ordercode"].ToString()));
                }
                if (!orderimportService.IsEmpty(queryParam["labeldate"]))// 日期现在不准 先不用
                {
                    strSql.Append(string.Format(" and labeldate = '{0}'", queryParam["labeldate"].ToString()));
                }
                if (!orderimportService.IsEmpty(queryParam["bei"]))
                {
                    strSql.Append(string.Format(" and bei = '{0}'", queryParam["bei"].ToString()));
                }
                return DataHelper.Query(strSql.ToString());
            }
            catch (Exception ex)
            {
                return null;
            }
        }



        public static DataTable getftet_chainSum(string queryJson)
        {
            var strSql = new StringBuilder();
            strSql.Append(@"select labeldate,
       sum(l1)  l1,
       sum(l2)  l2,
       sum(l3)  l3,
       sum(l4)  l4,
       sum(l5)  l5,
       sum(l6)  l6,
       sum(l7)  l7,
       sum(l8)  l8,
       sum(l9)  l9,
       sum(l10) l10,
       sum(l11) l11,
       sum(l12) l12,
       sum(l13) l13,
       sum(l14) l14
from ftet_chain where 1=1 ");
            try
            {
                var queryParam = DataHelper.ToJObject(queryJson);
                if (!orderimportService.IsEmpty(queryParam["ordercode"]))// 单号
                {
                    strSql.Append(string.Format(" and ordercode = '{0}'", queryParam["ordercode"].ToString()));
                }
                if (!orderimportService.IsEmpty(queryParam["labeldate"]))// 日期现在不准 先不用
                {
                    strSql.Append(string.Format(" and labeldate = '{0}'", queryParam["labeldate"].ToString()));
                }
                if (!orderimportService.IsEmpty(queryParam["bei"]))
                {
                    strSql.Append(string.Format(" and bei = '{0}'", queryParam["bei"].ToString()));
                }
                strSql.Append(" group by ordercode,labeldate");
                return DataHelper.Query(strSql.ToString());
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static int setftet_chain(List<ftet_chain> chainList,string path)
        {
            int setRow = 0;
            try
            {
                //var delSql = new StringBuilder();
                //foreach (var item in chainList)
                //{
                //    // 通过单号和背番删除历史数据
                //    delSql.Append(string.Format(" delete from ftet_chain where ordercode='{0}' and bei='{1}';", item.ordercode, item.bei));
                //}
                //DataHelper.ExecuteSql(delSql.ToString());
                var setstrSql = new StringBuilder();
                foreach (var item in chainList)
                {

                    // 新增 形成假覆盖
                    setstrSql.Append(string.Format(@" insert into ftet_chain(
                                no,ordercode,labeldate,supname,bei,loction,l1,l2,l3,l4,l5,l6,l7,l8,l9,l10,l11,l12,l13,l14
                                ,remark
                                )values(
                                ifnull({0},0),'{1}',{2},'{3}','{4}','{5}',{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},'{20}'
                                );", item.no, item.ordercode, string.IsNullOrEmpty(item.labeldate) ? "null" : "'" + item.labeldate + "'", item.supname, item.bei, item.loction,
                                string.IsNullOrEmpty(item.l1) ? "null" : item.l1, string.IsNullOrEmpty(item.l2) ? "null" : item.l2,
                                string.IsNullOrEmpty(item.l3) ? "null" : item.l3, string.IsNullOrEmpty(item.l4) ? "null" : item.l4,
                                string.IsNullOrEmpty(item.l5) ? "null" : item.l5, string.IsNullOrEmpty(item.l6) ? "null" : item.l6,
                                string.IsNullOrEmpty(item.l7) ? "null" : item.l7, string.IsNullOrEmpty(item.l8) ? "null" : item.l8,
                                string.IsNullOrEmpty(item.l9) ? "null" : item.l9, string.IsNullOrEmpty(item.l10) ? "null" : item.l10,
                                string.IsNullOrEmpty(item.l11) ? "null" : item.l11, string.IsNullOrEmpty(item.l12) ? "null" : item.l12,
                                string.IsNullOrEmpty(item.l13) ? "null" : item.l13, string.IsNullOrEmpty(item.l14) ? "null" : item.l14, path)
                        );
                }
                setRow = DataHelper.ExecuteSql(setstrSql.ToString());
            }
            catch (Exception e)
            {
                XErrorLogToolMDM.WriteLog("直链配货写入数据库错误：" + e.Message);

            }
            return setRow;
        }

        /// <summary>
        /// 获取原单位表 分页
        /// </summary>
        /// <param name="paginationobj"></param>
        /// <param name="queryJson"></param>
        /// <returns></returns>
        public static DataTable getUnitPage(Pagination paginationobj, string queryJson)
        {
            var strSql = new StringBuilder();
            strSql.Append("Select * from originalunit where 1=1");
            try
            {
                var queryParam = DataHelper.ToJObject(queryJson);
                if (!orderimportService.IsEmpty(queryParam["cust_name"]))// 客户简称
                {
                    strSql.Append(string.Format(" and cust_name like '%{0}%'", queryParam["cust_name"].ToString()));
                }
                if (!orderimportService.IsEmpty(queryParam["cust_code"]))// 客户代码
                {
                    strSql.Append(string.Format(" and cust_code  like '%{0}%'", queryParam["cust_code"].ToString()));
                }
                if (!orderimportService.IsEmpty(queryParam["recv_port"]))// 收入口
                {
                    strSql.Append(string.Format(" and recv_port like '%{0}%'", queryParam["recv_port"].ToString()));
                }
                if (!orderimportService.IsEmpty(queryParam["veh_model"]))// 车型
                {
                    strSql.Append(string.Format(" and veh_model like '%{0}%'", queryParam["veh_model"].ToString()));
                }
                if (!orderimportService.IsEmpty(queryParam["denso_num"]))// 电装品番
                {
                    strSql.Append(string.Format(" and denso_num like '%{0}%'", queryParam["denso_num"].ToString()));
                }
                if (!orderimportService.IsEmpty(queryParam["cust_num"]))// 客户品番
                {
                    strSql.Append(string.Format(" and cust_num like '%{0}%'", queryParam["cust_num"].ToString()));
                }
                if (!orderimportService.IsEmpty(queryParam["back_num"]))// 背番
                {
                    strSql.Append(string.Format(" and back_num like '%{0}%'", queryParam["back_num"].ToString()));
                }
                string quyersql = DataBaseQuery.OutPageSql(strSql.ToString(), paginationobj);
                return DataHelper.Query(quyersql);
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}