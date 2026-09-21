using System;
using System.Data;
using DENSOKB.Helper;
using DENSOKB.Models;

namespace DENSOKB.Libs
{
    public class MenuService
    {
        
        /// <summary>
        /// 获取树状菜单列表
        /// </summary>
        /// <returns></returns>
        public DataTable GetTree()
        {
            try
            {
                string    strSql = @"select * from menu order by OrderIndex";
                DataTable table  = DataHelper.Query(strSql);
                return table;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }
        
        /// <summary>
        /// 根据id获取菜单详情
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="Exception"></exception>
        public MenuDto GetMenuById(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new ArgumentException("获取菜单信息失败，id不能为空");
                }
                
                string    strSql = $"select * from menu where id = '{id}'";
                DataTable table  = DataHelper.Query(strSql);
                
                if (table.Rows.Count == 0)
                {
                    throw new Exception("获取菜单信息失败，未找到对应的菜单");
                }
                
                return DataHelper.MapToObject<MenuDto>(table);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        /// <summary>
        /// 根据id删除菜单
        /// </summary>
        /// <param name="id"></param>
        /// <exception cref="ArgumentException"></exception>
        public void deleteMenuById(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new ArgumentException("删除菜单失败，id不能为空");
                }
                
                string strSql = $"delete from menu where id = '{id}'";
                DataHelper.ExecuteSql(strSql);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        /// <summary>
        /// 根据id更新菜单详情
        /// </summary>
        /// <param name="menuDto"></param>
        /// <exception cref="ArgumentException"></exception>
        public void UpdateMenu(MenuDto menuDto)
        {
            try
            {
                if (menuDto == null || string.IsNullOrEmpty(menuDto.id))
                {
                    throw new ArgumentException("更新菜单失败，菜单信息不能为空");
                }
                
                string strSql = $@"
                    update menu 
                    set parentid = '{menuDto.parentid}', 
                        title = '{menuDto.text}', 
                        Url = '{menuDto.url}', 
                        IsLeaf = {(menuDto.leaf ? 1 : 0)}, 
                        Expanded = {(menuDto.expanded ? 1 : 0)}, 
                        IconCls = '{menuDto.iconCls}', 
                        OrderIndex = {menuDto.orderIndex} 
                    where id = '{menuDto.id}'";
                
                DataHelper.ExecuteSql(strSql);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        
        public void InsertMenu(MenuDto menuDto)
        {
            try
            {

                
                string strSql = $@"
                    insert into menu (id,parentid,title,Url,IsLeaf,Expanded,IconCls,OrderIndex)
                    values ('{menuDto.id}',
                            '{menuDto.parentid}',
                            '{menuDto.text}',
                            '{menuDto.url}',
                            '{(menuDto.leaf ? 1 : 0)}',
                            '{(menuDto.expanded ? 1 : 0)}',
                            '{menuDto.iconCls}',
                            '{menuDto.orderIndex}'
                            )";
                
                DataHelper.ExecuteSql(strSql);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        public bool isExitMenu(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new ArgumentException("查询菜单是否存在失败，id不能为空");
                }
                
                string    strSql = $"select count(*) from menu where id = '{id}'";
                DataTable table  = DataHelper.Query(strSql);
                
                if (table.Rows.Count == 0)
                {
                    return false;
                }
                
                return Convert.ToInt32(table.Rows[0][0]) > 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }


    }
}