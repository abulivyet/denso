using DENSOKB.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using DENSOKB.Helper;
using static iTextSharp.text.pdf.AcroFields;
using HelpLog.CommomMethod;

namespace DENSOKB.Libs
{
    public class CHAMService
    {
        /// <summary>
        ///  周订单分页查询
        /// </summary>
        /// <param name="paginationobj"></param>
        /// <param name="queryJson"></param>
        /// <returns></returns>
        public DataTable getPageTB(Pagination paginationobj, string queryJson)
        {
            var strSql = new StringBuilder();
            strSql.Append(@"select  * from  cham_ord_week
                        where 1=1
                        ");
            try
            {
                var queryParam = DataHelper.ToJObject(queryJson);
                if (!IsEmpty(queryParam["orderCode"]))  // 订单好
                {
                    strSql.Append(string.Format(" and orderCode like '%{0}%'", queryParam["orderCode"].ToString()));
                }
                if (!IsEmpty(queryParam["court_summons"]))  // 传票号
                {
                    strSql.Append(string.Format(" and court_summons like '%{0}%'", queryParam["court_summons"].ToString()));
                }
                if (!IsEmpty(queryParam["material_code"])) // 物料编码
                {
                    strSql.Append(string.Format(" and material_code like '%{0}%'", queryParam["material_code"].ToString()));
                }
                if (!IsEmpty(queryParam["ord_status"]))
                {
                    strSql.Append(string.Format(" and ord_status like '%{0}%'", queryParam["ord_status"].ToString()));
                }
                if (!IsEmpty(queryParam["project_code"]))
                {
                    strSql.Append(string.Format(" and project_code like '%{0}%'", queryParam["project_code"].ToString()));
                }
                if (!IsEmpty(queryParam["sendStart"])) // 
                {
                    strSql.Append(string.Format(" and demanded_date >= '{0}'", queryParam["sendStart"]));
                }
                if (!IsEmpty(queryParam["sendEnd"])) // 
                {
                    strSql.Append(string.Format(" and demanded_date <= '{0}'", queryParam["sendEnd"]));
                }
                string quyersql = DataBaseQuery.OutPageSql(strSql.ToString(), paginationobj);
                return DataHelper.Query(quyersql);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public void DeleteWeekProject(List<int> ids) {
            // 拼接参数占位符
            string paramNames = string.Join(",", ids.Select((_, index) => $"@id{index}"));

            string sql = $"DELETE FROM cham_ord_week WHERE id IN ({paramNames})";

            // 构建参数数组
            var parameters = ids.Select((id, index) => new MySqlParameter($"@id{index}", id)).ToArray();

            // 执行 SQL
            DataHelper.ExecuteSql(sql, parameters);
        }


        /// <summary>
        /// 出货指示分页查询
        /// </summary>
        /// <param name="paginationobj"></param>
        /// <param name="queryJson"></param>
        /// <returns></returns>
        public DataTable getPageTBout(Pagination paginationobj, string queryJson)
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
                if (!IsEmpty(queryParam["status"]))
                {
                    strSql.Append(string.Format(" and handlestate = '{0}'", queryParam["status"].ToString()));
                }

                if (!IsEmpty(queryParam["downloadcount"]))
                {
                    strSql.Append(string.Format(" and ifnull(downloadcount,0) = '{0}'", queryParam["downloadcount"].ToString()));
                }
                
                strSql.Append(" and cuscode='CHAM' and redundancy2='送货单' ");
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
        /// <summary>
        /// 插入周订单
        /// </summary>
        /// <param name="ordWeekOrder"></param>
        /// <returns></returns>
        public int InsertOrdWeek(CHAMOrdWeek ordWeekOrder)
        {
            var strSql =
                    @"insert into cham_ord_week(ord_status, orderCode,publishDate, cusname, court_summons, material_code, material_name, project_code,
                          demanded_date, receiving_time, quantity, unit, order_type, receiving_address, movingIn_code,
                          demanded_date_new)
values (@ord_status,@orderCode, @publishDate, @cusname, @court_summons, @material_code, @material_name, @project_code,
        @demanded_date, @receiving_time, @quantity, @unit, @order_type, @receiving_address, @movingIn_code,
        @demanded_date_new)"
                ;
            var parameters = new MySqlParameter[]
            {
                new MySqlParameter("@ord_status", ordWeekOrder.ordStatus ?? ""),
                new MySqlParameter("@orderCode", ordWeekOrder.orderCode),
                new MySqlParameter("@publishDate", ordWeekOrder.publishDate),
                new MySqlParameter("@cusname", ordWeekOrder.cusName            ?? ""),
                new MySqlParameter("@court_summons", ordWeekOrder.courtSummons ?? ""),
                new MySqlParameter("@material_code", ordWeekOrder.materialCode ?? ""),
                new MySqlParameter("@material_name", ordWeekOrder.materialName ?? ""),
                new MySqlParameter("@project_code", ordWeekOrder.projectCode   ?? ""),
                new MySqlParameter("@demanded_date", ordWeekOrder.demandedDate),
                new MySqlParameter("@receiving_time", ordWeekOrder.receivingTime),
                new MySqlParameter("@quantity", ordWeekOrder.quantity),
                new MySqlParameter("@unit", ordWeekOrder.unit),
                new MySqlParameter("@order_type", ordWeekOrder.orderType ?? ""),
                new MySqlParameter("@receiving_address", ordWeekOrder.receivingAddress),
                new MySqlParameter("@movingIn_code", ordWeekOrder.movingInCode        ?? ""),
                new MySqlParameter("@demanded_date_new", null)
            };
            return DataHelper.ExecuteSql(strSql, parameters);
            
        }


        /// <summary>
        /// 覆盖之前查询旧的周订单
        /// </summary>
        /// <param name="orderCode"></param>
        /// <param name="courtSummons"></param>
        /// <returns></returns>
        public CHAMOrdWeek GetOldWeekOrder(string orderCode,string courtSummons)
        {
            var strSql = @"select * from cham_ord_week where orderCode = '{0}' and court_summons = '{1}'";
            strSql = string.Format(strSql, orderCode, courtSummons);
            DataTable listOrdWeek = DataHelper.Query(strSql);
            CHAMOrdWeek ordWeeks = DataHelper.MapToObject<CHAMOrdWeek>(listOrdWeek);
            return ordWeeks;
        }

        public int UpdateWeekOrder(CHAMOrdWeek ordWeekOrder)
        {
            var strSql = @"update cham_ord_week set demanded_date_new = '{0}',quantity = '{1}' where id = '{2}'";
            strSql = string.Format(strSql,ordWeekOrder.demandeDatedNew, ordWeekOrder.quantity, ordWeekOrder.id);
            int executeSql = DataHelper.ExecuteSql(strSql);
            return executeSql;
        }

        /// <summary>
        /// 根据部品番号、线别、搬入日期以及部品数量确定传票号
        /// </summary>
        /// <param name="shipmentItem"></param>
        /// <returns></returns>
        public CHAMOrdWeek FindSummons(ShipmentItem shipmentItem)
        {
            var strSql = @"select * from cham_ord_week where material_code = '{0}' and project_code = '{1}' and demanded_date = '{2}' and quantity = '{3}'";
            strSql =  string.Format(strSql,shipmentItem.PartNumber,shipmentItem.Line,shipmentItem.InDate,shipmentItem.Quantity);
            DataTable listOrdWeek = DataHelper.Query(strSql);
            CHAMOrdWeek ordWeek = DataHelper.MapToObject<CHAMOrdWeek>(listOrdWeek);
            return ordWeek;
        }
        
        public List<CHAMOrdWeek> FindSummons(List<ShipmentItem> shipmentItems)
        {
            if (shipmentItems == null || shipmentItems.Count == 0) return new List<CHAMOrdWeek>();

            var conditions = new List<string>();

            foreach (var item in shipmentItems)
            {
                string condition = string.Format(
                    "(material_code = '{0}' AND project_code = '{1}' AND demanded_date = '{2}' )",
                    item.PartNumber.Replace("'", "''"),
                    item.Line.Replace("'", "''"),
                    item.InDate.ToString("yyyy-MM-dd")
                );

                conditions.Add(condition);
            }

            //var strSql = "SELECT * FROM cham_ord_week WHERE " + string.Join(" OR ", conditions);
            StringBuilder strSql = new StringBuilder();
            strSql.Append("SELECT * FROM cham_ord_week WHERE " + string.Join(" OR ", conditions));
            strSql.Append("and order_type like '%量产%' ");

            DataTable         resultTable = DataHelper.Query(strSql.ToString());
            List<CHAMOrdWeek> result      = DataHelper.MapToEntities<CHAMOrdWeek>(resultTable);

            return result;
        }

        /// <summary>
        /// 拆分船票后保存路径至数据库
        /// </summary>
        /// <param name="foldPath"></param>
        public void saveCourtSummons(string foldPath)
        {
            try
            {
                if (Directory.Exists(foldPath))
                {
                    
                    string[] pdfFiles = Directory.GetFiles(foldPath, "*.pdf", SearchOption.TopDirectoryOnly);

                    StringBuilder strSql = new StringBuilder();
                    strSql.Append("INSERT INTO CHAM_COURT_SUMMONS (code, path) VALUES ");

                    for (int i = 0; i < pdfFiles.Length; i++)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(pdfFiles[i]);
                        string fullPath = pdfFiles[i].Replace("\\", "\\\\");

                        strSql.AppendFormat("('{0}', '{1}')", fileName, fullPath);

                        if (i < pdfFiles.Length - 1)
                            strSql.Append(","); 
                    }

                    string sql    = strSql.ToString();
                    int    result = DataHelper.ExecuteSql(sql, null);

                    Console.WriteLine(result > 0 ? "批量插入成功！" : "批量插入失败！");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }


        public DataTable getCourtSummons(List<string> codes)
        {
            try
            {
                
                var escapedCodes = codes.Select(code => $"'{code.Replace("'", "''")}'");

                string inClause = string.Join(", ", escapedCodes);

                string strSql = $"SELECT distinct code,path FROM cham_court_summons WHERE code IN ({inClause})";
                XErrorLogToolMDM.WriteLog("获取传票号: " + strSql);
                DataTable dt = DataHelper.Query(strSql);

                return dt;

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }


        public void saveBoard(CHAMBoard chamBoard)
        {
            //上传之前先判断一下看板是否上传过，如果上传过先把之前的删掉
            var strSql1 = "select * from cham_board where boardCode = '{0}'";
            strSql1 = string.Format(strSql1, chamBoard.boardCode);
            DataTable dataTable1 = DataHelper.Query(strSql1);
            if (dataTable1 != null && dataTable1.Rows.Count > 0)
            {
                var strSql2 = "delete from cham_board where boardCode = '{0}'";
                strSql2 = string.Format(strSql2, chamBoard.boardCode);
                DataHelper.ExecuteSql(strSql2);
            }

            var strSql = "insert into cham_board (boardName, boardPath, boardDictory, boardCode) values (@boardName, @boardPath, @boardDictory, @boardCode)";
            var parameters = new MySqlParameter[]
            {
                new MySqlParameter("@boardName", chamBoard.boardName ?? ""),
                new MySqlParameter("@boardPath", chamBoard.boardPath ?? ""),
                new MySqlParameter("@boardDictory", chamBoard.boardDictory ?? ""),
                new MySqlParameter("@boardCode", chamBoard.boardCode ?? ""),
            };
            DataHelper.ExecuteSql(strSql, parameters);
        }

        /// <summary>
        /// 根据传票号找对应看板
        /// </summary>
        /// <param name="summonsCode"></param>
        /// <returns></returns>
        public string FindBoardBySummons(string summonsCode)
        {
            try
            {
                
                var strSql = "select * from cham_board where boardCode like '{0}%'";
                strSql =  string.Format(strSql, summonsCode);
                DataTable dataTable   = DataHelper.Query(strSql);
                if (dataTable == null|| dataTable.Rows.Count == 0)
                {
                    throw new Exception($"看板未上传,对应传票号:{summonsCode}");
                }

                return dataTable.Rows[0]["boardDictory"].ToString();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }

        /// <summary>
        /// 保存cham托标签
        /// </summary>
        /// <param name="stackLabel"></param>
        public void InsertStackLabel(List<CHAMStackLabel> stackLabels)
        {
            if (stackLabels == null || stackLabels.Count == 0)
                return;

            try
            {
                var deleteSqlBuilder = new StringBuilder();
                var insertSqlBuilder = new StringBuilder();
                var parameters = new List<MySqlParameter>();

                // 删除语句（每条一条）
                deleteSqlBuilder.Append("DELETE FROM chamStackLabel WHERE shippingCode = @shippingCode{0} AND stackLabelCode = @stackLabelCode{0};");

                // 插入语句头
                insertSqlBuilder.Append("INSERT INTO chamStackLabel (shippingCode, stackLabelCode, stackLabelName, inputPath) VALUES ");

                for (int i = 0; i < stackLabels.Count; i++)
                {
                    var label = stackLabels[i];

                    // 删除部分
                    parameters.Add(new MySqlParameter($"@shippingCode{i}", label.shippingCode ?? ""));
                    parameters.Add(new MySqlParameter($"@stackLabelCode{i}", label.stackLabelCode ?? ""));

                    // 插入部分
                    insertSqlBuilder.Append($"(@ins_shippingCode{i}, @ins_stackLabelCode{i}, @ins_stackLabelName{i}, @ins_inputPath{i})");
                    if (i < stackLabels.Count - 1)
                        insertSqlBuilder.Append(", ");

                    parameters.Add(new MySqlParameter($"@ins_shippingCode{i}", label.shippingCode ?? ""));
                    parameters.Add(new MySqlParameter($"@ins_stackLabelCode{i}", label.stackLabelCode ?? ""));
                    parameters.Add(new MySqlParameter($"@ins_stackLabelName{i}", label.stackLabelName ?? ""));
                    parameters.Add(new MySqlParameter($"@ins_inputPath{i}", label.inputPath ?? ""));
                }

                var fullSql = new StringBuilder();

                // 先执行批量删除
                for (int i = 0; i < stackLabels.Count; i++)
                {
                    fullSql.AppendFormat(deleteSqlBuilder.ToString(), i);
                }

                // 再执行批量插入
                fullSql.AppendLine();
                fullSql.Append(insertSqlBuilder.ToString());
                //XErrorLogToolMDM.WriteLog("托标签插入语句" + fullSql);
                // 执行
                int v = DataHelper.ExecuteSql(fullSql.ToString(), parameters.ToArray());
                XErrorLogToolMDM.WriteLog("托标签插入语句执行结果" + v);
            }
            catch (Exception ex)
            {
                Console.WriteLine("批量插入 chamStackLabel 失败：" + ex.Message);
                throw;
            }
        }


        /// <summary>
        /// 根据出货指示单找到对应的托标签
        /// </summary>
        /// <param name="shippingCode"></param>
        /// <returns></returns>
        public List<CHAMStackLabel> FindStackLabelByShippingCode(string shippingCode)
        {
            var strsql = @"select distinct shippingCode, stackLabelCode, stackLabelName, inputPath from chamStackLabel where shippingCode = '{0}';";
            strsql = string.Format(strsql, shippingCode);
            DataTable dataTable      = DataHelper.Query(strsql);
            XErrorLogToolMDM.WriteLog("根据传票号找托标签" +strsql);
            List<CHAMStackLabel>       chamStackLabels = DataHelper.MapToEntities<CHAMStackLabel>(dataTable);
            return chamStackLabels;
        }

        /// <summary>
        /// 根据部品番号,线别,搬入日期，数量确认周订单
        /// </summary>
        /// <param name="materialCode"></param>
        /// <param name="projectCode"></param>
        /// <param name="demandedDate"></param>
        /// <param name="quantity"></param>
        /// <returns></returns>
        public CHAMOrdWeekWithOrig getChamOrdWeeks(string materialCode, string projectCode, string demandedDate,
            string                                        quantity)
        {
            string strSql = @"select ori.box_cap,ori.denso_num,ori.pallet_box,week.* from cham_ord_week week 
                            left join originalunit ori on week.material_code = ori.cust_num where week.material_code = '{0}' 
                            and week.project_code = '{1}' and week.demanded_date = '{2}' and week.quantity = '{3}'";
            strSql = string.Format(strSql, materialCode, projectCode, demandedDate, quantity);
            DataTable           chamOrdWeek  = DataHelper.Query(strSql);
            CHAMOrdWeekWithOrig chamOrdWeeks = DataHelper.MapToObject<CHAMOrdWeekWithOrig>(chamOrdWeek);
            return chamOrdWeeks;
        }

        public CHAMOrdWeekWithOrig getChamOrdWeeks(string materialCode, string projectCode, string demandedDate)
        {

            XErrorLogToolMDM.WriteLog("orderinfos");

            string strSql = @"select ori.box_cap,ori.denso_num,ori.pallet_box,week.* from cham_ord_week week 
                            left join originalunit ori on week.material_code = ori.cust_num where week.material_code = '{0}' 
                            and week.project_code = '{1}' and week.demanded_date = '{2}' and week.order_type like '%量产%'";
            strSql = string.Format(strSql, materialCode, projectCode, demandedDate);
            XErrorLogToolMDM.WriteLog(strSql);
            DataTable chamOrdWeek = DataHelper.Query(strSql);
            CHAMOrdWeekWithOrig chamOrdWeeks = DataHelper.MapToObject<CHAMOrdWeekWithOrig>(chamOrdWeek);
            return chamOrdWeeks;
        }

        public void updateSupply(orderImport ord)
        {
            try
            {
                var strSql = "update orderimport set hassupplies = '{0}' where id = '{1}'";
                strSql = string.Format(strSql, ord.hassupplies, ord.id);
                int result = DataHelper.ExecuteSql(strSql);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }




    }
}