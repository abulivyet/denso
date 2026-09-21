using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Http;
using DENSOKB.Helper;
using DENSOKB.Libs;
using DENSOKB.Models;

namespace DENSOKB.Controllers
{
    public class MenuController: ApiController
    {
        private MenuService _menuService = new MenuService();
        
        // [HttpGet]
        // public ResParameter GetTree()
        // {
        //     try
        //     {
        //         DataTable     tree      = _menuService.GetTree();
        //         List<MenuDto> menuDtos  = DataHelper.MapToEntities<MenuDto>(tree);
        //         List<MenuDto> buildTree = BuildTree(menuDtos, "0");
        //         return ResParameter.Success(buildTree);
        //     }
        //     catch (Exception e)
        //     {
        //         Console.WriteLine(e);
        //         return ResParameter.Fail(e.Message);
        //     }
        //
        // }
        
        [HttpGet]
        public ResParameter GetTree(string node = "0")
        {
            try
            {
                // 原始数据一次性查出来（或者你可以直接查指定 parentId 下的子项）
                DataTable     allMenus = _menuService.GetTree();
                List<MenuDto> menuDtos = DataHelper.MapToEntities<MenuDto>(allMenus);

                // 只获取当前 parentId 的子项
                var children = menuDtos
                    .Where(x => x.parentid == node)
                    .OrderBy(x => x.orderIndex)
                    .Select(x =>
                    {
                        bool hasChildren = menuDtos.Any(m => m.parentid == x.id);
                        return new MenuDto
                        {
                            id         = x.id,
                            text      = x.text,
                            parentid   = x.parentid,
                            url        = x.url,
                            orderIndex = x.orderIndex,
                            leaf       = !hasChildren,
                            expanded   = false,
                            children   = new List<MenuDto>() // 
                        };
                    })
                    .ToList();

                return ResParameter.Success(children);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return ResParameter.Fail(e.Message);
            }
        }


        
        private List<MenuDto> BuildTree(List<MenuDto> nodes, string parentId)
        {
            return nodes
                .Where(x => x.parentid == parentId)
                .OrderBy(x => x.orderIndex)
                .Select(x =>
                {
                    var children = BuildTree(nodes, x.id);
                    x.children = children;        // 无论有没有都赋值，避免 null
                    x.leaf     = !children.Any(); // 有子节点 → leaf = false；否则 true
                    return x;
                })
                .ToList();


        }

        [HttpPost]
        public ResParameter GetMenuById(dynamic obj)
        {
            try
            {
                if (obj["id"] == null)
                {
                    return ResParameter.Fail("获取菜单信息失败，id不能为空");
                }
                string id       = obj["id"];
                MenuDto    menuDto = _menuService.GetMenuById(id);
                return ResParameter.Success(menuDto);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return ResParameter.Fail(e.Message);
                // throw;
            }
        }

        [HttpGet]
        public ResParameter DeleteMenu(string id)
        {
            try
            {
                _menuService.deleteMenuById(id);
                return ResParameter.Success();

            }catch (Exception e)
            {
                Console.WriteLine(e);
                return ResParameter.Fail(e.Message);
                // throw;
            }
        }
        
        [HttpPost]
        public ResParameter Save(dynamic obj)
        {
            try
            {
                if (obj == null )
                {
                    return ResParameter.Fail("更新菜单失败，菜单信息不能为空");
                }
                MenuDto menuDto = new MenuDto
                {
                    id         = obj["id"],
                    parentid   = obj["parentid"],
                    text      = obj["text"],
                    url        = obj["url"],
                    leaf       = obj["leaf"],
                    expanded   = obj["expanded"],
                    iconCls    = obj["iconCls"],
                    orderIndex = Convert.ToInt32(obj["orderIndex"])
                };
                if (!_menuService.isExitMenu(menuDto.id)) //新增
                {
                    _menuService.InsertMenu(menuDto);
                }
                else  //编辑
                {
                    _menuService.UpdateMenu(menuDto);
                }
                
                return ResParameter.Success();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return ResParameter.Fail(e.Message);
            }
        }
        
        
        
        
    }
    
}