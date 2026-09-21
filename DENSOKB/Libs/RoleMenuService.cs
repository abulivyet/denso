using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using DENSOKB.Helper;
using DENSOKB.Models;
using MySql.Data.MySqlClient;

namespace DENSOKB.Libs
{
    public class RoleMenuService
    {
        public DataTable GetAllRoles()
        {
            try
            {
                string sql = "select * from Roles";
                DataTable dt = DataHelper.Query(sql);
                return dt;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        
        public List<string> GetRoleMenus(string roleId)
        {
            try
            {
                string strSql = @"select menuid from role_menu where roleid = '{0}' ";
                strSql = string.Format(strSql, roleId);
                DataTable table = DataHelper.Query(strSql);
                List<string> list = table.AsEnumerable()
                    .Select(row => row[0].ToString())
                    .ToList();

                return list;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }


        public void SaveRoleMenus(RoleMenuDto roleMenu)
        {
            try
            {
                string strSql1 = @"delete from role_menu where roleid = '{0}'";
                strSql1 = string.Format(strSql1, roleMenu.RoleId);
                
                StringBuilder sb = new StringBuilder();
                sb.Append("INSERT INTO role_menu (roleid, menuid) VALUES ");

                for (int i = 0; i < roleMenu.MenuIds.Count; i++)
                {
                    sb.AppendFormat("({0}, '{1}')", roleMenu.RoleId, MySqlHelper.EscapeString(roleMenu.MenuIds[i]));
                    if (i != roleMenu.MenuIds.Count - 1)
                        sb.Append(",");
                }

                sb.Append(";");
                string strSql2 = sb.ToString();
                List<string>    sqlList    = new List<string>();
                sqlList.Add(strSql1);
                sqlList.Add(strSql2);
                DataHelper.ExecuteSqlTransaction(sqlList);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }


    }
}