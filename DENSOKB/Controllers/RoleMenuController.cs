using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Http;
using System.Web.Script.Serialization;
using DENSOKB.Helper;
using DENSOKB.Libs;
using DENSOKB.Models;

namespace DENSOKB.Controllers
{
    public class RoleMenuController: ApiController
    {
        private RoleMenuService _roleMenuService =  new RoleMenuService();
        private UserService userservice = new UserService();
        
        [HttpPost]
        public object GetRolePageList(Pagination paginationobj)
        {
            paginationobj.sidx = "r_code";
            paginationobj.sord = "desc";
            DataTable                  data       = userservice.getRolePageTB(paginationobj, paginationobj.queryJson);
            Dictionary<string, object> jo         = new Dictionary<string, object>();
            JavaScriptSerializer       serializer = new JavaScriptSerializer();
            jo.Add("success", true);
            jo.Add("total", paginationobj.records);
            jo.Add("data", PulicHelper.ConvertDataTableToListOfDictionaries(data));
            return jo;
        }

        [HttpGet]
        public ResParameter GetRoleMenus(string roleId)
        {
            try
            {
                if (string.IsNullOrEmpty(roleId))
                {
                    return ResParameter.Fail("获取角色菜单失败，角色ID不能为空");
                }

                List<string> roleMenus = _roleMenuService.GetRoleMenus(roleId);
                return ResParameter.Success(roleMenus);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return ResParameter.Fail(e.Message);
            }
        }

        [HttpPost]
        public ResParameter SaveRoleMenus(RoleMenuDto roleMenu)
        {
            try
            {
                if (roleMenu == null)
                {
                    return ResParameter.Fail("保存失败，角色菜单信息不能为空");
                }
                _roleMenuService.SaveRoleMenus(roleMenu);
                return ResParameter.Success();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return ResParameter.Fail(e.Message);
                // throw;
            }
        }

    }
}