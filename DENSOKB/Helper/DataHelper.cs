using HelpLog.CommomMethod;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DENSOKB.Helper
{
    public static class DataHelper
    {

        public static string connectionString = ConfigurationManager.AppSettings["connectionString"].ToString();

        public static DataTable Query(string sql)
        {
            DataTable dt = new DataTable();

            try
            {
                MySqlConnection conn = new MySqlConnection(connectionString);

                MySqlDataAdapter adapter = new MySqlDataAdapter(sql, conn);

                adapter.Fill(dt);
            }
            catch (Exception ex)
            {
                //Utils.log(ex.Message);
            }

            return dt;
        }

        public static int ExecuteSql(string sql)
        {
            MySqlConnection conn = new MySqlConnection(connectionString);

            try
            {  
                MySqlCommand comm = new MySqlCommand(sql, conn);

                conn.Open();
                int n = comm.ExecuteNonQuery();
                conn.Close();

                return n;
            }
            catch(Exception ex)
            {
                conn.Close();
                XErrorLogToolMDM.WriteLog("执行SQL错误" + sql);
                return 0;
            }

        }


        public static int ExecuteSql(string sql, MySqlParameter[] parameters)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    MySqlCommand comm = new MySqlCommand(sql, conn);
                    if (parameters != null)
                    {
                        comm.Parameters.AddRange(parameters);
                    }

                    conn.Open();
                    int n = comm.ExecuteNonQuery();
                    return n;
                }
                catch (Exception ex)
                {
                   string mes= ex.Message;
                    return 0;
                }
                finally
                {
                    conn.Close();
                }
            }
        }
        
        
        public static T MapToObject<T>(DataTable dataTable) where T : new()
        {
            if (dataTable.Rows.Count == 0)
                return default;

            var row   = dataTable.Rows[0];
            var obj   = new T();
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
            {
                if (!prop.CanWrite) continue;

                // 获取属性上的 Column 特性
                var columnAttr = prop.GetCustomAttribute<ColumnAttribute>();
                var columnName = columnAttr?.Name ?? prop.Name;

                if (!dataTable.Columns.Contains(columnName)) continue;

                var value = row[columnName];
                if (value == DBNull.Value) continue;

                var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                prop.SetValue(obj, Convert.ChangeType(value, targetType));
            }

            return obj;
        }
        
        
        public static List<T> MapToEntities<T>(DataTable dataTable) where T : new()
        {
            var list  = new List<T>();
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (DataRow row in dataTable.Rows)
            {
                var obj = new T();

                foreach (var prop in props)
                {
                    if (!prop.CanWrite) continue;

                    var columnAttr = prop.GetCustomAttribute<ColumnAttribute>();
                    var columnName = columnAttr?.Name ?? prop.Name;

                    if (!dataTable.Columns.Contains(columnName)) continue;

                    var value = row[columnName];
                    if (value == DBNull.Value) continue;

                    var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                    prop.SetValue(obj, Convert.ChangeType(value, targetType));
                }

                list.Add(obj);
            }

            return list;
        }

        public static string SqlFilters(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return "";
            }
            //半角括号替换为全角括号
            source = source.Replace("'", "'''");
            //去除执行SQL语句的命令关键字
            source = Regex.Replace(source, "select", "", RegexOptions.IgnoreCase);
            source = Regex.Replace(source, "insert", "", RegexOptions.IgnoreCase);
            source = Regex.Replace(source, "update ", "", RegexOptions.IgnoreCase);
            source = Regex.Replace(source, "delete", "", RegexOptions.IgnoreCase);
            source = Regex.Replace(source, "drop", "", RegexOptions.IgnoreCase);
            source = Regex.Replace(source, "truncate", "", RegexOptions.IgnoreCase);
            source = Regex.Replace(source, "declare", "", RegexOptions.IgnoreCase);
            source = Regex.Replace(source, "xp_cmdshell", "", RegexOptions.IgnoreCase);
            source = Regex.Replace(source, "/add", "", RegexOptions.IgnoreCase);
            source = Regex.Replace(source, "net user", "", RegexOptions.IgnoreCase);
            //去除执行存储过程的命令关键字 
            source = Regex.Replace(source, "exec", "", RegexOptions.IgnoreCase);
            source = Regex.Replace(source, "execute", "", RegexOptions.IgnoreCase);
            //去除系统存储过程或扩展存储过程关键字
            source = Regex.Replace(source, "xp_", "x p_", RegexOptions.IgnoreCase);
            source = Regex.Replace(source, "sp_", "s p_", RegexOptions.IgnoreCase);
            //防止16进制注入
            source = Regex.Replace(source, "0x", "0 x", RegexOptions.IgnoreCase);
            return source;
        }
        public static StringBuilder SqlPageSql(string strSql, string orderField, bool isAsc, int pageSize, int pageIndex)
        {
            StringBuilder sb = new StringBuilder();
            if (pageIndex == 0)
            {
                pageIndex = 1;
            }
            int num = (pageIndex - 1) * pageSize;
            int num1 = (pageIndex) * pageSize;
            string OrderBy = "";

            if (!string.IsNullOrEmpty(orderField))
            {
                if (orderField.ToUpper().IndexOf("ASC") + orderField.ToUpper().IndexOf("DESC") > 0)
                {
                    OrderBy = " Order By " + orderField;
                }
                else
                {
                    OrderBy = " Order By " + orderField + " " + (isAsc ? "ASC" : "DESC");
                }
            }
            else
            {
                OrderBy = "order by (select 0)";
            }
            sb.Append("Select * From (Select ROW_NUMBER() Over (" + OrderBy + ")");
            sb.Append(" As rowNum, T.* From (" + strSql + ")  T ) As N Where rowNum > " + num + " And rowNum <= " + num1 + "");
            return sb;
        }
        public static JObject ToJObject(this string Json)
        {
            return Json == null ? JObject.Parse("{}") : JObject.Parse(Json.Replace("&nbsp;", ""));
        }
        
        /// <summary>
        /// 事务
        /// </summary>
        public static bool ExecuteSqlTransaction(List<string> sqlList)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                MySqlTransaction transaction = conn.BeginTransaction();
                MySqlCommand     command     = conn.CreateCommand();
                command.Connection  = conn;
                command.Transaction = transaction;

                try
                {
                    foreach (string sql in sqlList)
                    {
                        if (!string.IsNullOrWhiteSpace(sql))
                        {
                            command.CommandText = sql;
                            command.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    XErrorLogToolMDM.WriteLog("事务执行失败：" + ex.Message);
                    return false;
                }
            }
        }

        public static int BulkInsert<T>(string tableName, IEnumerable<T> entities)
        {
            if (entities == null || !entities.Any())
                return 0;

            var properties = typeof(T).GetProperties()
                .Where(p => Attribute.IsDefined(p, typeof(ColumnAttribute)))
                .ToArray();

            if (!properties.Any())
                throw new InvalidOperationException("实体类未包含 [Column] 属性");

            var columnNames = properties
                .Select(p => p.GetCustomAttribute<ColumnAttribute>()?.Name ?? p.Name)
                .ToArray();

            var valueList = new List<string>();

            foreach (var entity in entities)
            {
                var values = new List<string>();
                foreach (var prop in properties)
                {
                    var value = prop.GetValue(entity, null);
                    if (value == null)
                    {
                        values.Add("NULL");
                    }
                    else if (value is string || value is DateTime)
                    {
                        values.Add($"'{MySqlHelper.EscapeString(value.ToString())}'");
                    }
                    else
                    {
                        values.Add(value.ToString());
                    }
                }

                valueList.Add($"({string.Join(",", values)})");
            }

            string sql = $"INSERT INTO `{tableName}` ({string.Join(",", columnNames)}) VALUES {string.Join(",", valueList)};";

            return ExecuteSql(sql); // 调用你已有的执行方法
        }




    }
}
