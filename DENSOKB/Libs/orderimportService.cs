using DENSOKB.Helper;
using DENSOKB.Models;
using HelpLog.CommomMethod;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace DENSOKB.Libs
{
    public class orderimportService
    {
        
        /// <summary>
        /// 列表获取数据（分页）
        /// </summary>
        /// <param name="paginationobj"></param>
        /// <param name="queryJson"></param>
        /// <returns></returns>
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
                if (!IsEmpty(queryParam["ordercode"]))
                {
                    strSql.Append(string.Format(" and ordercode like '%{0}%'", queryParam["ordercode"].ToString()));
                }
                if (!IsEmpty(queryParam["orderbin"]))
                {
                    strSql.Append(string.Format(" and orderbin like '%{0}%'", queryParam["orderbin"].ToString()));
                }
                if (!IsEmpty(queryParam["createperson"]))
                {
                    strSql.Append(string.Format(" and createperson like '%{0}%'", queryParam["createperson"].ToString()));
                }
                if (!IsEmpty(queryParam["redundancy2"]))
                {
                    strSql.Append(string.Format(" and redundancy2 like '%{0}%'", queryParam["redundancy2"].ToString()));
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

                if (!IsEmpty(queryParam["status"]))
                {
                    strSql.Append(string.Format(" and handlestate = '{0}'", queryParam["status"].ToString()));
                }

                if (!IsEmpty(queryParam["downloadcount"]))
                {
                    strSql.Append(string.Format(" and ifnull(downloadcount,0) = '{0}'", queryParam["downloadcount"].ToString()));
                }

                strSql.Append(" and (cuscode='FTET' or cuscode='FTCE')");
                string quyersql = DataBaseQuery.OutPageSql(strSql.ToString(), paginationobj);
                //XErrorLogToolMDM.WriteLog(queryJson);
                return DataHelper.Query(quyersql);
                
            }catch(Exception ex)
            {
                XErrorLogToolMDM.WriteLog(ex.Message);
                return null;
            }
        }

        public static void DeleteDetail2(List<orderImport> reouseList)
        {
            var strSql = new StringBuilder();
            foreach (var item in reouseList)
            {
                strSql.Append(string.Format(" delete from ord_inp_detail2 where mainId={0}", item.id));
            }
            DataHelper.ExecuteSql(strSql.ToString());
        }

        public DataTable getNotPageTB( string queryJson)
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
                if (!IsEmpty(queryParam["ordercode"]))
                {
                    strSql.Append(string.Format(" and ordercode like '%{0}%'", queryParam["ordercode"].ToString()));
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
                    strSql.Append(string.Format(" and convert(date,inportdate) >= '{0}'", queryParam["importstart"].ToString()));
                }
                if (!IsEmpty(queryParam["importend"]))
                {
                    strSql.Append(string.Format(" and convert(date,inportdate)<= '{0}'", queryParam["importend"].ToString()));
                }
                // 发货时间  podate
                if (!IsEmpty(queryParam["sendstart"]))
                {
                    strSql.Append(string.Format(" and convert(date,podate) >= '{0}'", queryParam["sendstart"].ToString()));
                }
                if (!IsEmpty(queryParam["sendend"]))
                {
                    strSql.Append(string.Format(" and convert(date,podate)<= '{0}'", queryParam["sendend"].ToString()));
                }
                //strSql.Append(" and (cuscode='FTET' or cuscode='FTCE')");
                //string quyersql = DataBaseQuery.OutPageSql(strSql.ToString());
                return DataHelper.Query(strSql.ToString());
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        //public DataTable 
        /// <summary>
        /// 获取未处理的数据 并改为处理中  1处理中 2 完成 3异常 4 重新解析 （重新解析为手动处理，不在自动处理中）
        /// </summary>
        /// <returns></returns>
        public static DataTable getNotHead()
        {
            var strSql = new StringBuilder();
            
            strSql.Append(@"Select * from orderimport where ifnull(handlestate,'')!='2' and ifnull(handlestate,'')!='3' and ifnull(handlestate,'')!='4' and ifnull(redundancy1,'')!='' and (cuscode='FTET' or cuscode='FTCE' or redundancy2='送货单' or cuscode='DHEC' or cuscode='CHAM' or redundancy3 like '%pdf%');
            update orderimport set handlestate = 1 where ifnull(handlestate, '') != '2' and ifnull(handlestate, '') != '3' and ifnull(handlestate,'')!='4' and ifnull(redundancy1, '') != '' ");
            DataTable dt= DataHelper.Query(strSql.ToString());

            return dt;
        }
        public static List<orderImport> getOrderDB(List<orderImport> orderlist)
        {
            XErrorLogToolMDM.WriteLog("进入getOrderDB");
            var strSql = new StringBuilder();
            strSql.Append(" select * from orderimport where 1=1 ");
            var escapedCodes = orderlist.Select(code => $"'{code.redundancy4.Replace("'", "''")}'");
            string inClause = string.Join(", ", escapedCodes);
            strSql.Append($" and  redundancy4 in({inClause})");

            //strSql.Append(" AND cuscode = 'FTCE'");

            //strSql.Append(" AND ((cuscode = 'FTCE') OR (redundancy3 NOT LIKE '%xls%' AND redundancy3 NOT LIKE '%xlsx%'))"); // 非解析数据

            DataTable dt = DataHelper.Query(strSql.ToString());

            List<orderImport> orderNewlist = DataHelper.MapToEntities<orderImport>(dt);
            XErrorLogToolMDM.WriteLog("getOrderDB查询结果数量：" + orderNewlist.Count);
            return orderNewlist;
        }
            
            /// <summary>
            ///  修改handlestate状态和消息
            /// </summary>
            /// <param name="reouseList"></param>
            public static void setState(List<orderImport> resourceList)
        {
            if (resourceList == null || resourceList.Count == 0)
            {
                return;
            }

            
            try
            {
                foreach (var item in resourceList)
            {
                    var sqlBuilder = new StringBuilder();
                    var parameters = new List<MySqlParameter>();
                    // 构建参数化SQL
                    sqlBuilder.AppendLine("UPDATE orderimport SET ");
                sqlBuilder.AppendLine("handlestate = @handlestate,");
                sqlBuilder.AppendLine("handlemess = @handlemess,");
                sqlBuilder.AppendLine("handlebegin = @handlebegin,");
                sqlBuilder.AppendLine("handleend = @handleend,");
                sqlBuilder.AppendLine("handletime = @handletime,");
                sqlBuilder.AppendLine("ordercode = @ordercode,");
                sqlBuilder.AppendLine("orderbin = @orderbin,");
                sqlBuilder.AppendLine("podate = @podate, ");
                    sqlBuilder.AppendLine("redundancy7 = @redundancy7 ");
                sqlBuilder.AppendLine("WHERE id = @id;");

                // 添加参数
                parameters.Add(new MySqlParameter("@handlestate", item.handlestate));
                parameters.Add(new MySqlParameter("@handlemess", item.handlemess ?? string.Empty));
                parameters.Add(new MySqlParameter("@handlebegin", item.handlebegin));
                parameters.Add(new MySqlParameter("@handleend", item.handleend));
                parameters.Add(new MySqlParameter("@handletime", item.handletime));
                parameters.Add(new MySqlParameter("@ordercode", item.ordercode ?? string.Empty));
                parameters.Add(new MySqlParameter("@orderbin", item.orderbin ?? string.Empty));
                parameters.Add(new MySqlParameter("@podate", item.podate));
                    parameters.Add(new MySqlParameter("@redundancy7", item.redundancy7));
                parameters.Add(new MySqlParameter("@id", item.id));
                DataHelper.ExecuteSql(sqlBuilder.ToString(), parameters.ToArray());
            }
                
            }
            catch (Exception ex)
            {
                // 记录错误日志
    
                throw; // 根据业务需求决定是否抛出异常
            }
        }

        public static void DownloadCountPlus(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return;
                }
                string strSql = @"update orderimport set downloadcount = ifnull(downloadcount,0) + 1 where id = @id";
                var parameters = new List<MySqlParameter>
                {
                    new MySqlParameter("@id", id)
                };
                DataHelper.ExecuteSql(strSql, parameters.ToArray());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }


        public static void setState(orderImport item)
        {
            try
            {
                if (item == null)
                {
                    return;
                }
                var sqlBuilder = new StringBuilder();
                var parameters = new List<MySqlParameter>();
                // 构建参数化SQL
                sqlBuilder.AppendLine("UPDATE orderimport SET ");
                sqlBuilder.AppendLine("handlestate = @handlestate,");
                sqlBuilder.AppendLine("handlemess = @handlemess,");
                sqlBuilder.AppendLine("handlebegin = @handlebegin,");
                sqlBuilder.AppendLine("handleend = @handleend,");
                sqlBuilder.AppendLine("handletime = @handletime,");
                sqlBuilder.AppendLine("ordercode = @ordercode,");
                sqlBuilder.AppendLine("orderbin = @orderbin,");
                sqlBuilder.AppendLine("podate = @podate, ");
                sqlBuilder.AppendLine("redundancy7 = @redundancy7 ");
                sqlBuilder.AppendLine("WHERE id = @id;");

                // 添加参数
                parameters.Add(new MySqlParameter("@handlestate", item.handlestate));
                parameters.Add(new MySqlParameter("@handlemess", item.handlemess ?? string.Empty));
                parameters.Add(new MySqlParameter("@handlebegin", item.handlebegin));
                parameters.Add(new MySqlParameter("@handleend", item.handleend));
                parameters.Add(new MySqlParameter("@handletime", item.handletime));
                parameters.Add(new MySqlParameter("@ordercode", item.ordercode ?? string.Empty));
                parameters.Add(new MySqlParameter("@orderbin", item.orderbin   ?? string.Empty));
                parameters.Add(new MySqlParameter("@podate", item.podate));
                parameters.Add(new MySqlParameter("@redundancy7", item.redundancy7));
                parameters.Add(new MySqlParameter("@id", item.id));
                DataHelper.ExecuteSql(sqlBuilder.ToString(), parameters.ToArray());
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }


        // 后续优化找个工厂类 用实体操作数据库
        public static int UpdateHandlestate(DataTable dt)
        {
            int isok = -1;
            var strSql = new StringBuilder();
            foreach (DataRow item in dt.Rows)
            {
                strSql.Append(string.Format(@" update orderimport set handlestate='{0}',handlemess='{1}' where id={3} ;"
                , item["handlestate"].ToString()
                , item["handlemess"].ToString()
                , item["id"].ToString()
                )
                    );
            }
            isok = DataHelper.ExecuteSql(strSql.ToString());
            return isok;
        }
        public int Setorderimport(List<orderImport> list)
        {
            int isok = -1;
            var strSql = new StringBuilder();
            try
            {
                XErrorLogToolMDM.WriteLog("数据记录行数" + list.Count);
                // 重复上传  源思路是通过文件名称删除 一般文件都会有单号+日期作为名称，但是有的文件是“传票.pdf"无法通过名称作为为一项删除
                foreach (var item in list)
                {
                    //if (item.redundancy3 == "出货指示.pdf") {
                    //    continue;
                    //}
                    if (item.cuscode == "CHAM" && item.redundancy1.Contains(".pdf"))
                    {
                        string text = SpireHelper.GetText(item.redundancy1, new System.Drawing.Rectangle(231, 22, 150, 40), 0);
                        if (text.Contains("出货指示单")) {
                            continue;
                        }
                    }

                    strSql.Append(string.Format("delete from ord_inp_detail2 where mainid=(select id from orderimport where redundancy3='{0}')  ;", item.redundancy3));
                    strSql.Append(string.Format("delete from orderimport where redundancy3='{0}'  ;", item.redundancy3));
                }
                DataHelper.ExecuteSql(strSql.ToString());
                strSql = new StringBuilder();
                foreach (var item in list)
                {
                    strSql.Append(@"insert into orderimport(
                            inportdate
                            , cusname
                            , cuscode
                            , ordercode
                            , orderbin
                            , podate
                            , createperson
                            , createdate
                            , redundancy1
                            , redundancy2
                            , redundancy3
                            , redundancy4
                            , redundancy5
                            , redundancy6
                            , redundancy7
                            , redundancy8
                            , handlestate
                            , filehash
                            )
                            VALUES(");
                    strSql.Append("'" + item.inportdate + "'");
                    strSql.Append(",'" + item.cusname + "'");
                    strSql.Append(",'" + item.cuscode + "'");
                    strSql.Append(",'" + item.ordercode + "'");
                    strSql.Append(",'" + item.orderbin + "'");
                    if (string.IsNullOrEmpty(item.podate.ToString()))
                    {
                        strSql.Append(",null");
                    }
                    else
                    {
                        strSql.Append(",'" + item.podate + "'");
                    }

                    strSql.Append(",'" + item.createperson + "'");
                    strSql.Append(",'" + item.createdate + "'");
                    strSql.Append(",'" + item.redundancy1.Replace(@"\", @"\\") + "'");
                    strSql.Append(",'" + item.redundancy2 + "'");
                    strSql.Append(",'" + item.redundancy3 + "'");
                    strSql.Append(",'" + item.redundancy4 + "'");
                    if (string.IsNullOrEmpty(item.redundancy5.ToString()))
                    {
                        strSql.Append(",null");
                    }
                    else
                    {
                        strSql.Append(",'" + item.redundancy5 + "'");
                    }
                    if (string.IsNullOrEmpty(item.redundancy6.ToString()))
                    {
                        strSql.Append(",null");
                    }
                    else
                    {
                        strSql.Append(",'" + item.redundancy6 + "'");
                    }
                    if (string.IsNullOrEmpty(item.redundancy7.ToString()))
                    {
                        strSql.Append(",null");
                    }
                    else
                    {
                        strSql.Append(",'" + item.redundancy7 + "'");
                    }
                    if (string.IsNullOrEmpty(item.redundancy8.ToString()))
                    {
                        strSql.Append(",null");
                    }
                    else
                    {
                        strSql.Append(",'" + item.redundancy8 + "'");
                    }
                    strSql.Append(",'" + item.handlestate + "'");
                    strSql.Append(",'" + item.filehash + "'");
                    strSql.Append("       );");

                }
                isok = DataHelper.ExecuteSql(strSql.ToString());
            }catch(Exception e)
            {
                XErrorLogToolMDM.WriteLog("写入数据失败"+e.Message);
            }
            return isok;
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

        public void InsertDetail(List<FtceDetail> detail)
        {
            List<inpdetail2> inpdetail2s = new List<inpdetail2>();
            int              index       = 2;
            
            // if (!string.IsNullOrEmpty(detail[0].comfirm))
            // {
            //     inpdetail2 inpdetail2 = new inpdetail2();
            //     // inpdetail2.ordercode  = orderCode;
            //     inpdetail2.filetype   = "确认表";
            //     inpdetail2.Stacklabel = detail[0].comfirm;
            //     inpdetail2.mainId     = detail[0].mainId;
            //     inpdetail2.rowcode    = 1;
            //     inpdetail2s.Add(inpdetail2);
            // }
            foreach (FtceDetail item in detail)
            {
                string orderCode = "";
                if (!string.IsNullOrEmpty(item.orderCode))
                {
                    orderCode = item.orderCode;
                }
                
                if (!string.IsNullOrEmpty(item.comfirm))
                {
                    inpdetail2 inpdetail2 = new inpdetail2();
                    inpdetail2.ordercode  = orderCode;
                    inpdetail2.filetype   = "确认表";
                    inpdetail2.Stacklabel = item.comfirm;
                    inpdetail2.mainId     = item.mainId;
                    inpdetail2.rowcode    = index;
                    inpdetail2s.Add(inpdetail2);
                    // inpdetail2s.Add(inpdetail2);
                    index++;
                }

                if (!string.IsNullOrEmpty(item.Stacklabel))
                {
                    inpdetail2 inpdetail2 = new inpdetail2();
                    inpdetail2.ordercode  = orderCode;
                    inpdetail2.filetype   = "码垛单";
                    inpdetail2.Stacklabel = item.Stacklabel;
                    inpdetail2.mainId     = item.mainId;
                    inpdetail2.rowcode    = index;
                    inpdetail2s.Add(inpdetail2);
                    inpdetail2s.Add(inpdetail2);
                    index++;
                }
                
                if (!string.IsNullOrEmpty(item.Board))
                {
                    inpdetail2 inpdetail2 = new inpdetail2();
                    inpdetail2.ordercode  = orderCode;
                    inpdetail2.filetype   = "小纸张";
                    inpdetail2.Stacklabel = item.Board;
                    inpdetail2.mainId     = item.mainId;
                    inpdetail2.rowcode    = index;
                    inpdetail2s.Add(inpdetail2);
                    index++;
                }
                
                if (!string.IsNullOrEmpty(item.A4Board))
                {
                    inpdetail2 inpdetail2 = new inpdetail2();
                    inpdetail2.ordercode  = orderCode;
                    inpdetail2.filetype   = "A4";
                    inpdetail2.Stacklabel = item.A4Board;
                    inpdetail2.mainId     = item.mainId;
                    inpdetail2.rowcode    = index;
                    inpdetail2s.Add(inpdetail2);
                    index++;
                }
                
                
            }
            if (!string.IsNullOrEmpty(detail[0].bale))
            {
                inpdetail2 inpdetail2 = new inpdetail2();
                // inpdetail2.ordercode  = orderCode;
                inpdetail2.filetype   = "捆包";
                inpdetail2.Stacklabel = detail[0].bale;
                inpdetail2.mainId     = detail[0].mainId;
                inpdetail2.rowcode    = index;
                inpdetail2s.Add(inpdetail2);
                index++;
            }
            if (!string.IsNullOrEmpty(detail[0].Receipt))
            {
                inpdetail2 inpdetail2 = new inpdetail2();
                // inpdetail2.ordercode  = orderCode;
                inpdetail2.filetype   = "受领书";
                inpdetail2.Stacklabel = detail[0].Receipt;
                inpdetail2.mainId     = detail[0].mainId;
                inpdetail2.rowcode    = index;
                inpdetail2s.Add(inpdetail2);
                inpdetail2s.Add(inpdetail2);
                inpdetail2s.Add(inpdetail2);
                index++;
            }
            XErrorLogToolMDM.WriteLog("detail");
            InsertDetail2(inpdetail2s);
            
        }




        //public static void insertweekorder(List<ord_weekorder> weekList) { }

        //public static void insertweekorder(List<ord_weekorder> weekList)
        public void InsertDetail2(List<inpdetail2> detail)
        {
            try {
                if (detail == null || detail.Count == 0)
                    return;

                XErrorLogToolMDM.WriteLog("开始插入数据");

                var sql = new StringBuilder("INSERT INTO ord_inp_detail2 (ordercode, mainId, Stacklabel, filetype, remarks, r1,rowcode) VALUES ");
                var parameters = new List<MySqlParameter>();

                for (int i = 0; i < detail.Count; i++)
                {
                    var item = detail[i];
                    sql.Append($"(@ordercode{i}, @mainId{i}, @Stacklabel{i}, @filetype{i}, @remarks{i}, @r1_{i},@rowcode_{i})");

                    if (i < detail.Count - 1)
                        sql.Append(", ");

                    parameters.Add(new MySqlParameter($"@ordercode{i}", item.ordercode ?? ""));
                    parameters.Add(new MySqlParameter($"@mainId{i}", item.mainId ?? ""));
                    parameters.Add(new MySqlParameter($"@Stacklabel{i}", item.Stacklabel ?? ""));
                    parameters.Add(new MySqlParameter($"@filetype{i}", item.filetype ?? ""));
                    parameters.Add(new MySqlParameter($"@remarks{i}", item.remarks ?? ""));
                    parameters.Add(new MySqlParameter($"@r1_{i}", i));
                    parameters.Add(new MySqlParameter($"@rowcode_{i}", item.rowcode ?? null));
                }

                DataHelper.ExecuteSql(sql.ToString(), parameters.ToArray());
            }
            catch (Exception ex) {
                XErrorLogToolMDM.WriteLog("插入ord_inp_detail2数据失败：" + ex.Message);
            }
            

        }
        // 排号顺序的插入
        public void InsertDetail2_rowcode(List<inpdetail2> detail)
        {
            if (detail == null || detail.Count == 0)
                return;



            var sql = new StringBuilder("INSERT INTO ord_inp_detail2 (ordercode, mainId, Stacklabel, filetype, remarks, r1,rowcode) VALUES ");
            var parameters = new List<MySqlParameter>();

            for (int i = 0; i < detail.Count; i++)
            {
                var item = detail[i];
                sql.Append($"(@ordercode{i}, @mainId{i}, @Stacklabel{i}, @filetype{i}, @remarks{i}, @r1_{i},@rowcode_{i})");

                if (i < detail.Count - 1)
                    sql.Append(", ");

                parameters.Add(new MySqlParameter($"@ordercode{i}", item.ordercode ?? ""));
                parameters.Add(new MySqlParameter($"@mainId{i}", item.mainId ?? ""));
                parameters.Add(new MySqlParameter($"@Stacklabel{i}", item.Stacklabel ?? ""));
                parameters.Add(new MySqlParameter($"@filetype{i}", item.filetype ?? ""));
                parameters.Add(new MySqlParameter($"@remarks{i}", item.remarks ?? ""));
                parameters.Add(new MySqlParameter($"@r1_{i}", item.r1 ?? ""));
                parameters.Add(new MySqlParameter($"@rowcode_{i}", i+1));
            }

            DataHelper.ExecuteSql(sql.ToString(), parameters.ToArray());
        }
        public DataTable getinp_detail(string mainid)
        {
            try
            {
                var strSql = new StringBuilder();
                strSql.Append("Select * from ord_inp_detail where 1=1");
                if (!string.IsNullOrEmpty(mainid))
                {
                    strSql.Append($" and mainid={mainid}");
                }
                return DataHelper.Query(strSql.ToString());
            }catch(Exception e)
            {
                return null;
            }
        }
       
        public DataTable getinp_detail2(string mainid)
        {
            try
            {
                var strSql = new StringBuilder();
                strSql.Append("Select * from ord_inp_detail2 where 1=1");
                if (!string.IsNullOrEmpty(mainid))
                {
                    strSql.Append($" and mainid={mainid}");
                }
                strSql.Append(" order by rowcode,ordercode,Stacklabel");
                return DataHelper.Query(strSql.ToString());
            }
            catch (Exception e)
            {
                return null;
            }
        }
        public DataTable getinp_detailForList(List<OrderDownload> mainid)
        {
            try
            {
                var strSql = new StringBuilder();
                strSql.Append("Select * from ord_inp_detail2 where 1=1");
                strSql.Append(" and mainid in(");
                int row = 0;

                if (mainid.Count > 0)
                {
                    foreach (var item in mainid)
                    {
                        if (!string.IsNullOrEmpty(item.id.ToString()))
                        {
                            if (row == mainid.Count-1)
                            {
                                strSql.Append(item.id);
                            }
                            else
                            {
                                strSql.Append(item.id + ",");
                            }

                        }
                        row++;
                    }
                    strSql.Append(" )");
                    /*   类型解析
                     *   box   箱标签
                     *   boxMin  小纸打印
                     *   boxA4  A4穿透打印
                     *   check2  拖标签
                     *   check3  拖标签合并成一个文件
                     *   check4  纳品书
                     *   check5  取货指示书
                     *   check6  取货指示书单位组合文件
                     *   check7  取套票
                     *  check8 确认表
                     *  如果勾选了取套票 就是成套下载 确认表+垛标签+受领书+链标签+捆包+(A4或者小纸张）
                     */
                    string[] typeArr = mainid[0].type.ToString().Replace("check7;","").Replace("check3;", "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
          
                    if (mainid[0].type.ToString().IndexOf("check7")>-1&& mainid[0].type.ToString().IndexOf("box;")<0)
                    {
                        typeArr = mainid[0].type.ToString().Replace("check7;", "").Replace("boxA4;", "").Replace("boxMin","").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    }


                    if (typeArr.Length > 0)
                    {
                        // 判断是不是只包含 取套票+小纸打印 或者取套票+A4穿透打印 如果是则不查询数据
                       // bool isTP = false;
                        //string[] components = mainid[0].type.ToString().Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                        //var componentSet = new HashSet<string>(components);
                        //var validCombination1 = new HashSet<string> { "boxMin", "check7" };
                        //var validCombination2 = new HashSet<string> { "boxA4", "check7" };
                        //isTP= CheckComponents(componentSet);
                        //if (isTP) return new DataTable();

                       // strSql.Append("  and (  ");
                            var i = 0;
                        var conditions = new List<string>();
                        foreach (var item in typeArr)
                            {
                                if (string.IsNullOrEmpty(item)) continue;
                                //if (i > 0 && i< typeArr.Length) strSql.Append(" or ");
                                switch (item)
                                {
                                    case "boxMin":
                                    case "boxA4":
                                    if (mainid[0].type.IndexOf("box")>-1)
                                        conditions.Add("  filetype='小纸张'");
                                        break;
                                    //case "boxA4":
                                    //if (mainid[0].type.IndexOf("box") > -1)
                                    //    conditions.Add("  filetype='A4' ");
                                    //    break;
                                    case "check2":
                                    conditions.Add("   filetype='码垛单' ");
                                        break;
                                    case "check4":
                                    conditions.Add("   filetype='受领书' ");
                                        break;
                                    //case "check7":
                                    //    strSql.Append("  filetype='套票' ");
                                    //    break;
                                    case "check8":
                                    conditions.Add("   filetype='确认表' ");
                                        break;
                                }
                                i++;
                            }
                        if (conditions.Count > 0)
                        {
                            strSql.Append(" and (");
                            strSql.Append(string.Join(" or ", conditions));
                            strSql.Append(")");
                        }
                        // strSql.Append(" )");

                        // }
                    }
                    else
                    {
                        return new DataTable();
                    }
                    strSql.Append(" order by ordercode,rowcode");
                }
                else
                {
                     return new DataTable();
                }


                return DataHelper.Query(strSql.ToString());
            }
            catch (Exception e)
            {
                return null;
            }
        }
        public static bool CheckComponents(HashSet<string> components)
        {
            bool hasBox = components.Contains("box");
            bool hasBoxMin = components.Contains("boxMin");
            bool hasBoxA4 = components.Contains("boxA4");
            bool hasCheck7 = components.Contains("check7");

            // 包含"box"且包含boxMin或boxA4
            if (hasBox && (hasBoxMin || hasBoxA4))
                return false;

            // 包含boxMin和check7或boxA4和check7
            if ((hasBoxMin && hasCheck7) || (hasBoxA4 && hasCheck7))
                return true;

            // 全部包含
            if (hasBox && hasBoxMin && hasBoxA4 && hasCheck7)
                return false;

            return false; // 其他情况
        }
        public DataTable getdetailForMainid(List<OrderDownload> mainid)

        {
            try
            {
                var strSql = new StringBuilder();
                strSql.Append("Select * from ord_inp_detail2 where 1=1");
                string condition = string.Join(" or ", mainid.Select(t => $" mainId='{t.id}' "));
                strSql.Append($" and ({condition})");
                strSql.Append(" order by ordercode,rowcode");
                return DataHelper.Query(strSql.ToString());
            }
            catch (Exception e)
            {
                throw e;
            }
            }
        
        // public 

            //  套票所需数据源
            public DataTable getinp_detaiTP(OrderDownload mainid)
        {
            try
            {
                var strSql = new StringBuilder();
                strSql.Append("Select * from ord_inp_detail2 where 1=1");
                strSql.Append(string.Format(" and mainid = {0}", mainid.id));
                int row = 0;
                    /*   类型解析
                     *   boxMin  小纸打印
                     *   boxA4  A4穿透打印
                     *   check2  拖标签
                     *   check3  拖标签合并成一个文件
                     *   check4  纳品书
                     *   check5  取货指示书
                     *   check6  取货指示书单位组合文件
                     *   check7  取套票
                     *  check8 确认表
                     *  如果勾选了取套票 就是成套下载 确认表+垛标签+受领书+链标签+捆包+(A4或者小纸张）
                     */

                //  2025-06-04 客户现场需求，套票不包含小纸张和A4
                if (mainid.type.ToString().IndexOf("check7") > -1)
                {
                    strSql.Append("  and (");
                    strSql.Append("  filetype='确认表' or filetype = 'PL标签' or filetype='码垛单' or filetype='受领书' or filetype='受领书' or filetype='链标签' or filetype='捆包'");
                    if (mainid.type.ToString().IndexOf("boxMin") > -1)
                    {
                        strSql.Append(" or filetype='小纸张'");
                    }
                    //else
                    //{
                    //    strSql.Append(" or filetype='A4'");
                    //}
                    strSql.Append("  )");
                    strSql.Append(" order by rowcode,Stacklabel");
                }
                else
                {
                    return null;
                }


                return DataHelper.Query(strSql.ToString());
            }
            catch (Exception e)
            {
                return null;
            }
        }
        public static void insertweekorder(List<OrdWeekOrder> weekList)
        {
            //ord_weekorder  传票+版本唯一
            var strSql = new StringBuilder();
            try
            {
                foreach (var item in weekList)
                {
                    strSql.Append(string.Format("delete from ord_weekorder where p_code='{0}' and o_version='{1}' ;", item.p_code, item.o_version));
                }
                int delrow=DataHelper.ExecuteSql(strSql.ToString());
                strSql = new StringBuilder(@"insert into ord_weekorder(o_number,sup_code,r_location,online_v,part_code
                                            , part_color, part_name, plan_code, r_date, r_time
                                            , c_date, c_time, order_num, p_code, o_version, o_week
                                            , o_class, rule, remarks, order_code, carmodel)
                                            values ");
                foreach (var item in weekList)
                {
                    string rDate = item.r_date.HasValue
                                   ? $"'{item.r_date.Value:yyyy-MM-dd}'"
                                   : "NULL";
                    string cDate = item.c_date.HasValue
                                   ? $"'{item.c_date.Value:yyyy-MM-dd}'"
                                   : "NULL";
                    strSql.Append(string.Format(@" ('{0}','{1}','{2}','{3}','{4}',
                                            '{5}','{6}','{7}',{8},'{9}',
                                            {10},'{11}','{12}','{13}','{14}','{15}',
                                            '{16}','{17}','{18}','{19}','{20}'),",
                                                item.o_number, item.sup_code, item.r_location, item.online_v, item.part_code,
                                                item.part_color, item.part_name, item.plan_code, rDate, item.r_time,
                                                cDate, item.c_time, item.order_num, item.p_code, item.o_version, item.o_week,
                                                item.o_class, item.rule, item.remarks, item.order_code, item.carmodel));


                }
                int insrow = DataHelper.ExecuteSql(strSql.ToString().TrimEnd(',') + ";");
                if (insrow>=1)
                {
                    XErrorLogToolMDM.WriteLog("周订单写入库成功");
                }
            }
            catch (Exception e)
            {
                XErrorLogToolMDM.WriteLog("周订单写入库错误" + e.Message);
            }
        }
        
        
        
        /// <summary>
        /// 根据品番查询原单位表信息
        /// </summary>
        /// <param name="densoNum"></param>
        /// <returns></returns>
        public OriginalUnit GetOriginalUnit(string densoNum)
        {
            var strSql = @"select * from omsdb.originalunit where denso_num= '{0}'";
            strSql = string.Format(strSql, densoNum);
            DataTable table       = DataHelper.Query(strSql.ToString());
            OriginalUnit       originalUnit = DataHelper.MapToObject<OriginalUnit>(table);
            return originalUnit;

        }

        /// <summary>
        /// 保存确认表备注字段信息   此方法作废，确认表备注改为批量设置
        /// </summary>
        /// <param name="confirmDto"></param>
        // public void SaveConformRemark(ConfirmDto confirmDto)
        // {
        //     try
        //     {
        //         if (confirmDto.type == "丰田" || confirmDto.type == "佛山本田")
        //         {
        //             string strSql = @"update orderimport set confirmRemark = '{0}' where id = '{1}'";
        //             strSql = string.Format(strSql, confirmDto.confirmRemark, confirmDto.id);
        //             DataHelper.ExecuteSql(strSql);
        //             Confirm confirm = findConfirmPath(confirmDto);
        //             EditConfirmRemark(confirm, confirmDto);
        //         }
        //
        //         else if (confirmDto.type == "东风本田")
        //         {
        //             string strSql = @"update dhec_dayplan set confirmRemark = '{0}' where carno = '{1}' and p_code = '{2}'";
        //             strSql = string.Format(strSql, confirmDto.confirmRemark, confirmDto.carno, confirmDto.p_code);
        //             DataHelper.ExecuteSql(strSql);
        //         }else if (confirmDto.type == "广州本田")
        //         {
        //             string strSql = @"update pickupplan set confirmRemark = '{0}' where no = '{1}' ";
        //             strSql = string.Format(strSql, confirmDto.confirmRemark, confirmDto.p_code);
        //             DataHelper.ExecuteSql(strSql);
        //         }
        //
        //     }
        //     catch (Exception e)
        //     {
        //         Console.WriteLine(e);
        //         throw;
        //         
        //     }
        // }
        
        
        /// <summary>
        /// 批量更新备注
        /// </summary>
        /// <param name="confirmDto"></param>
        public void SaveConformRemark(ConfirmDto confirmDto)
        {
            try
            {
                if (confirmDto.type == "FTCE" || confirmDto.type == "佛山本田" || confirmDto.type == "东风本田" || confirmDto.type == "广州本田")
                {
                    //先判断confirmBatch表中是否有该客户名称 类型的数据
                    string strSql = @"select * from confirmbatch where cusname = '{0}'";
                    strSql = string.Format(strSql, confirmDto.type);
                    DataTable table = DataHelper.Query(strSql.ToString());
                    //如果没有   新增
                    if (table.Rows.Count == 0)
                    {
                        string addSql = @"insert into confirmbatch (cusname,remark) values ('{0}','{1}')";
                        addSql = string.Format(addSql, confirmDto.type, confirmDto.confirmRemark);
                        DataHelper.ExecuteSql(addSql);
                    }
                    //如果有   更新
                    else
                    {
                        string updateSql = "update confirmbatch set remark = '{0}' where cusname = '{1}'";
                        updateSql = string.Format(updateSql, confirmDto.confirmRemark, confirmDto.type);
                        DataHelper.ExecuteSql(updateSql);
                    }

                } else if (confirmDto.type == "FTET") {
                    //先判断confirmBatch表中是否有该客户名称 类型的数据
                    string strSql = @"select * from confirmbatch where cusname = '{0}' and type = '{1}'";
                    string typeCode = confirmDto.ordercode.Substring(0, 2); // 获取类型前两位
                    strSql = string.Format(strSql, confirmDto.type, typeCode);
                    DataTable table = DataHelper.Query(strSql.ToString());
                    //如果没有   新增
                    if (table.Rows.Count == 0)
                    {
                        string addSql = @"insert into confirmbatch (cusname,remark,type) values ('{0}','{1}','{2}')";
                        addSql = string.Format(addSql, confirmDto.type, confirmDto.confirmRemark, typeCode);
                        DataHelper.ExecuteSql(addSql);
                    }
                    //如果有   更新
                    else
                    {
                        string updateSql = "update confirmbatch set remark = '{0}' where cusname = '{1}' and type = '{2}'";
                        updateSql = string.Format(updateSql, confirmDto.confirmRemark, confirmDto.type,typeCode);
                        DataHelper.ExecuteSql(updateSql);
                    }


                }


            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
                
            }
        }

        public string GetConfirmBatch(string cusname,string type) {
            try
            {
                StringBuilder strSql = new StringBuilder();
                strSql.Append("select * from confirmBatch where cusname = '{0}'");
                if (string.IsNullOrEmpty(type)) {
                    DataTable table = DataHelper.Query(string.Format(strSql.ToString(), cusname));
                    if (table.Rows.Count > 0)
                    {
                        return table.Rows[0]["remark"].ToString();
                    }
                    else {
                        return "";
                    }
                }
                else
                {
                    strSql.Append(" and type = '{1}'");
                    DataTable table = DataHelper.Query(string.Format(strSql.ToString(), cusname, type));
                    if (table.Rows.Count > 0)
                    {
                        return table.Rows[0]["remark"].ToString();
                    }
                    else
                    {
                        return "";
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public void EditConfirmRemark(string id,string cuscode,string ordercode)
        {
            try
            {
                string confirm;
                ConfirmDto confirmDto = new ConfirmDto();

                var strSql  = new StringBuilder();
                strSql.Append("select * from confirmbatch where cusname= '{0}'");
                if (cuscode == "FTET")
                {
                    string type = ordercode.Substring(0, 2);
                    strSql.Append(" and type = '{1}'");
                    string sql       = string.Format(strSql.ToString(), cuscode, type);
                    DataTable    table = DataHelper.Query(sql);
                    if (table.Rows.Count == 0)
                    {
                        confirm = "";
                    }
                    else { confirm = table.Rows[0]["remark"].ToString(); }
                    
                    confirmDto.type = "丰田";
                }
                else
                {
                    if (cuscode == "FTCE") { confirmDto.type = "丰田"; }
                    if (cuscode == "佛山本田") { confirmDto.type = "佛山本田"; }
                    string sql = string.Format(strSql.ToString(), cuscode);
                    DataTable table = DataHelper.Query(sql);
                    if (table.Rows.Count == 0)
                    {
                        confirm = "";
                    }
                    else { confirm = table.Rows[0]["remark"].ToString(); }
                    
                }

                confirmDto.id = id;
                confirmDto.confirmRemark = confirm;
                List<Confirm> confirmObj    = findConfirmPath(confirmDto);
                EditConfirmRemark(confirmObj, confirmDto);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public List<Confirm> findConfirmPath(ConfirmDto confirmDto)
        {
            try
            {
                string strSql = @"select * from confirm where mainId = '{0}'";
                strSql = string.Format(strSql, confirmDto.id);
                DataTable confirm     = DataHelper.Query(strSql.ToString());
                List<Confirm>       confirmObj = DataHelper.MapToEntities<Confirm>(confirm);
                return confirmObj;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }

        /// <summary>
        /// 查询orderimpport备注
        /// </summary>
        /// <param name="mainId"></param>
        /// <returns></returns>
        public string GetConfirmRemark(string mainId)
        {
            try
            {
                string strSql = @"select * from orderimport where id = '{0}'";
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

        /// <summary>
        /// 按备注文本估算所需行数（考虑换行与自动折行），最少 1 行，最多 maxLines 行
        /// </summary>
        private static int CalcRemarkLineCount(string remark, int charsPerLine, int maxLines = 4)
        {
            if (string.IsNullOrWhiteSpace(remark))
            {
                return 1;
            }

            if (charsPerLine <= 0)
            {
                charsPerLine = 82;
            }

            string normalized = remark.Replace("\r\n", "\n").Replace("\r", "\n");
            string[] parts = normalized.Split('\n');
            int total = 0;
            foreach (string part in parts)
            {
                if (string.IsNullOrEmpty(part))
                {
                    total += 1;
                }
                else
                {
                    total += Math.Max(1, (int)Math.Ceiling((double)part.Length / charsPerLine));
                }
            }

            return Math.Min(Math.Max(1, total), maxLines);
        }

        /// <summary>
        /// 填写确认表excel备注字段
        /// </summary>
        /// <param name="confirm"></param>
        /// <param name="confirmDto"></param>
        public void EditConfirmRemark(Confirm confirm,ConfirmDto confirmDto)
        {
            try
            {
                string excelPath = confirm.excelPath;
                string remark    = confirmDto.confirmRemark;
                // 读取 Excel 文件
                IWorkbook workbook;
                using (FileStream fs = new FileStream(excelPath, FileMode.Open, FileAccess.Read))
                {
                    if (Path.GetExtension(excelPath).Equals(".xls", StringComparison.OrdinalIgnoreCase))
                    {
                        workbook = new HSSFWorkbook(fs); // Excel 97-2003
                    }
                    else
                    {
                        workbook = new XSSFWorkbook(fs); // Excel 2007+
                    }
                }

                // 默认操作第一个 sheet
                ISheet sheet = workbook.GetSheetAt(0);


                for (int i = 0; i <= sheet.LastRowNum; i++)
                {
                    IRow row = sheet.GetRow(i);
                    if (row == null) continue;

                    ICell cell = row.GetCell(0); // 第一列
                    if (cell != null && cell.ToString().Trim().Contains("备注"))
                    {
                        // 设置文本内容
                        ICell remarkCell = row.GetCell(1) ?? row.CreateCell(1);
                        remarkCell.SetCellValue(remark);

                        if (confirmDto.type == "丰田")
                        {
                            ICellStyle baseStyle = remarkCell.CellStyle;
                            ICellStyle style     = workbook.CreateCellStyle();
                            style.CloneStyleFrom(baseStyle); // 复制已有样式，包括边框

                            style.WrapText = true;
                            // IFont font = workbook.CreateFont();
                            // font.FontHeightInPoints = 36;
                            // style.SetFont(font);

                            style.BorderTop      = BorderStyle.Thin;
                            style.BorderBottom   = BorderStyle.Thin;
                            remarkCell.CellStyle = style;
                            // 设置垂直对齐方式为顶部对齐
                            style.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Top;
                            // 备注不长时保持最小行数，避免固定撑成 4 行
                            int lineCount = CalcRemarkLineCount(remark, 82, 4);
                            row.HeightInPoints = lineCount * 44f;

                            break;
                        }
                        else if (confirmDto.type == "佛山本田")
                        {
                            ICellStyle style = workbook.CreateCellStyle();
                            style.WrapText = true;
                            
                            IFont font = workbook.CreateFont();
                            font.FontHeightInPoints = 18;  
                            style.SetFont(font);
                            
                            // 添加边框
                            style.BorderTop    = BorderStyle.Thin;
                            style.BorderBottom = BorderStyle.Thin;
                            // style.BorderLeft   = BorderStyle.Thin;
                            // style.BorderRight  = BorderStyle.Thin;
                            
                            remarkCell.CellStyle = style;

                            style.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Top;
                            int lineCount = CalcRemarkLineCount(remark, 188, 4);
                            row.HeightInPoints = lineCount * 35f;

                            break;
                        }
                        else
                        {
                        }


                    }
                }

                // 保存修改，覆盖原文件
                using (FileStream fs = new FileStream(excelPath, FileMode.Create, FileAccess.Write))
                {
                    workbook.Write(fs);
                }
                LiberOfficeHelper.ExcelOutOfPdf(confirm.excelPath, confirm.pdfPath);
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }




        public void EditConfirmRemark(List<Confirm> confirm, ConfirmDto confirmDto)
        {
            try
            {
                foreach (var item in confirm)
                {
                    string excelPath = item.excelPath;
                    string remark = confirmDto.confirmRemark;
                    // 读取 Excel 文件
                    IWorkbook workbook;
                    using (FileStream fs = new FileStream(excelPath, FileMode.Open, FileAccess.Read))
                    {
                        if (Path.GetExtension(excelPath).Equals(".xls", StringComparison.OrdinalIgnoreCase))
                        {
                            workbook = new HSSFWorkbook(fs); // Excel 97-2003
                        }
                        else
                        {
                            workbook = new XSSFWorkbook(fs); // Excel 2007+
                        }
                    }

                    // 默认操作第一个 sheet
                    ISheet sheet = workbook.GetSheetAt(0);


                    for (int i = 0; i <= sheet.LastRowNum; i++)
                    {
                        IRow row = sheet.GetRow(i);
                        if (row == null) continue;

                        ICell cell = row.GetCell(0); // 第一列
                        if (cell != null && cell.ToString().Trim().Contains("备注"))
                        {
                            // 设置文本内容
                            ICell remarkCell = row.GetCell(1) ?? row.CreateCell(1);
                            remarkCell.SetCellValue(remark);

                            if (confirmDto.type == "丰田")
                            {
                                ICellStyle baseStyle = remarkCell.CellStyle;
                                ICellStyle style = workbook.CreateCellStyle();
                                style.CloneStyleFrom(baseStyle); // 复制已有样式，包括边框

                                style.WrapText = true;
                                // IFont font = workbook.CreateFont();
                                // font.FontHeightInPoints = 36;
                                // style.SetFont(font);

                                style.BorderTop = BorderStyle.Thin;
                                style.BorderBottom = BorderStyle.Thin;
                                remarkCell.CellStyle = style;
                                // 设置垂直对齐方式为顶部对齐
                                style.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Top;
                                // 备注不长时保持最小行数，避免固定撑成 4 行
                                int lineCount = CalcRemarkLineCount(remark, 82, 4);
                                row.HeightInPoints = lineCount * 44f;

                                break;
                            }
                            else if (confirmDto.type == "佛山本田")
                            {
                                ICellStyle style = workbook.CreateCellStyle();
                                style.WrapText = true;

                                IFont font = workbook.CreateFont();
                                font.FontHeightInPoints = 18;
                                style.SetFont(font);

                                // 添加边框
                                style.BorderTop = BorderStyle.Thin;
                                style.BorderBottom = BorderStyle.Thin;
                                // style.BorderLeft   = BorderStyle.Thin;
                                // style.BorderRight  = BorderStyle.Thin;

                                remarkCell.CellStyle = style;

                                style.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Top;
                                int lineCount = CalcRemarkLineCount(remark, 188, 4);
                                row.HeightInPoints = lineCount * 35f;

                                break;
                            }
                            else
                            {
                            }


                        }
                    }

                    // 保存修改，覆盖原文件
                    using (FileStream fs = new FileStream(excelPath, FileMode.Create, FileAccess.Write))
                    {
                        workbook.Write(fs);
                    }
                    LiberOfficeHelper.ExcelOutOfPdf(item.excelPath, item.pdfPath);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }


    }
}