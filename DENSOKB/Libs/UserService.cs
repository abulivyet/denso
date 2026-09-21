using DENSOKB.Helper;
using DENSOKB.Models;
using HelpLog.CommomMethod;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Web;

namespace DENSOKB.Libs
{
    public class UserService
    {
        public DataTable getPageTB(Pagination paginationobj, string queryJson)
        {
            var strSql = new StringBuilder();
            strSql.Append("Select * from user where 1=1");
            try
            {
                var queryParam = DataHelper.ToJObject(queryJson);
                if (!IsEmpty(queryParam["username"]))
                {
                    strSql.Append(string.Format(" and username like '%{0}%'", queryParam["username"].ToString()));
                }
                if (!IsEmpty(queryParam["name"]))
                {
                    strSql.Append(string.Format(" and name like '%{0}%'", queryParam["name"].ToString()));
                }
                string quyersql = DataBaseQuery.OutPageSql(strSql.ToString(), paginationobj);
                return DataHelper.Query(quyersql);
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        
        public DataTable getNotPageTB( string queryJson)
        {
            var strSql = new StringBuilder();
            strSql.Append("Select * from user where 1=1");
            try
            {
                var queryParam = DataHelper.ToJObject(queryJson); 
                // 验重使用 所以只能用=
                if (!IsEmpty(queryParam["username"]))
                {
                    strSql.Append(string.Format(" and username = '{0}'", queryParam["username"].ToString()));
                }
                if (!IsEmpty(queryParam["name"]))
                {
                    strSql.Append(string.Format(" and name = '{0}'", queryParam["name"].ToString()));
                }
                return DataHelper.Query(strSql.ToString());
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public int AddUser(User user)
        {
            int addnum = 0;
            var strSql = new StringBuilder();
            try
            {

                strSql.Append(" insert into user(username,password,name) ");
                strSql.Append(" values(@username, @password, @name)");
                var parameters = new List<MySqlParameter>();
                parameters.Add(new MySqlParameter($"@username", user.username ?? ""));
                parameters.Add(new MySqlParameter($"@password", user.password ?? ""));
                parameters.Add(new MySqlParameter($"@name", user.name ?? ""));
                addnum = DataHelper.ExecuteSql(strSql.ToString(), parameters.ToArray());
            }
            catch (Exception e)
            {
                XErrorLogToolMDM.WriteLog("新增用户失败：" + e.Message);
                throw;
            }
            return addnum;
        }
        public int UpdateUser(User user)
        {
            int addnum = 0;
            var strSql = new StringBuilder();
            try
            {

                strSql.Append(" update  user set ");
                List<string> updates = new List<string>();
                if (!IsEmpty(user.name))
                {
                    updates.Add($"name = '{MySqlHelper.EscapeString(user.name)}'");
                }
                if (!IsEmpty(user.password))
                {
                    updates.Add($"password = '{MySqlHelper.EscapeString(user.password)}'");
                }
                strSql.Append(string.Join(", ", updates));
                strSql.Append($" WHERE id = {user.id}");
                addnum = DataHelper.ExecuteSql(strSql.ToString());
            }
            catch (Exception e)
            {
                XErrorLogToolMDM.WriteLog("修改用户失败：" + e.Message);
            }
            return addnum;

        }
        public int deluser(int id)
        {
            int addnum = 0;
            var strSql = new StringBuilder();
            try
            {

                strSql.Append(" delete  from user  ");
                strSql.Append($" WHERE id = {id}");
                addnum = DataHelper.ExecuteSql(strSql.ToString());
            }
            catch (Exception e)
            {
                XErrorLogToolMDM.WriteLog("修改用户失败：" + e.Message);
            }
            return addnum;
        }
        public DataTable getRolePageTB(Pagination paginationobj, string queryJson)
        {
            var strSql = new StringBuilder();
            strSql.Append("Select * from role where 1=1");
            try
            {
                var queryParam = DataHelper.ToJObject(queryJson);
                if (!IsEmpty(queryParam["r_code"]))
                {
                    strSql.Append(string.Format(" and r_code like '%{0}%'", queryParam["r_code"].ToString()));
                }
                if (!IsEmpty(queryParam["r_name"]))
                {
                    strSql.Append(string.Format(" and r_name like '%{0}%'", queryParam["r_name"].ToString()));
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