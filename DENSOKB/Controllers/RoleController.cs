
using System.Web.Http;
using System.Linq;
using DENSOKB.Libs;
using DENSOKB.Models;
using System.Data;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using DENSOKB.Helper;
using System;
using HelpLog.CommomMethod;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net;

public class RoleController : ApiController
{
    //private readonly AppDbContext _db = new AppDbContext();
    private UserService userservice = new UserService();
    [JwtAuthorize]
        [HttpPost]
        public object GetRolePageList(Pagination paginationobj)
    {
        paginationobj.sidx = "r_code";
        paginationobj.sord = "desc";
        DataTable data = userservice.getRolePageTB(paginationobj, paginationobj.queryJson);
        Dictionary<string, object> jo = new Dictionary<string, object>();
        JavaScriptSerializer serializer = new JavaScriptSerializer();
        jo.Add("success", true);
        jo.Add("total", paginationobj.records);
        jo.Add("data", PulicHelper.ConvertDataTableToListOfDictionaries(data));
        return jo;
    }
    public object Addrole(object role)
    {
        Dictionary<string, object> jo = new Dictionary<string, object>();
        int addnum = 0;
        var strSql = new StringBuilder();
        try
        {
            dynamic dynObj = role;
            string sql = string.Format(" select * from role where r_code={0}", dynObj.r_code.ToString());
            DataTable dt= DataHelper.Query(sql);
            if (dt!=null&& dt.Rows.Count>0)
            {
                jo.Add("success", false);
                jo.Add("mess", string.Format("编码【{0}】重复", dynObj.r_code.ToString()));
                return jo;
            }

            strSql.Append(" insert into role(r_code,r_name,r_mark) ");
            strSql.Append(" values(@r_code, @r_name, @r_mark)");
            var parameters = new List<MySqlParameter>();
            parameters.Add(new MySqlParameter($"@r_code",  dynObj.r_code.ToString() ?? ""));
            parameters.Add(new MySqlParameter($"@r_name",  dynObj.r_name.ToString() ?? ""));
            parameters.Add(new MySqlParameter($"@r_mark",  dynObj.r_mark.ToString() ?? ""));
            addnum = DataHelper.ExecuteSql(strSql.ToString(), parameters.ToArray());
            if (addnum>0)
            {
                jo.Add("success", true);
                jo.Add("mess", "ok");
            }
        }
        catch (Exception e)
        {
            jo.Add("success", false);
            jo.Add("mess", string.Format("添加账号失败，原因{0}", e.Message));
        }
        return jo;
    }
    public object deleterole()
    {
        Dictionary<string, object> jo = new Dictionary<string, object>();

        return jo;
    }
    [HttpPost]
    public object getAllUser()
    {
        object query = new object();
        DataTable oldUserTB = userservice.getNotPageTB("{}");
        Dictionary<string, object> jo = new Dictionary<string, object>();
        JavaScriptSerializer serializer = new JavaScriptSerializer();
        jo.Add("success", true);
        jo.Add("data", PulicHelper.ConvertDataTableToListOfDictionaries(oldUserTB));
        return PulicHelper.ConvertDataTableToListOfDictionaries(oldUserTB);
    }

    public class RoleUserRequest
    {
        public string RoleId { get; set; }
        public List<string> userIds { get; set; }
    }
    [HttpPost]
    public object SetUserRole(RoleUserRequest ruleuser)
    {
        Dictionary<string, object> jo = new Dictionary<string, object>();
        try
        {
            string delsql = string.Format(" delete from user_role where roleid={0}", ruleuser.RoleId);
            DataHelper.ExecuteSql(delsql);
            if (ruleuser.userIds.Count>0)
            {
                string inssql = " insert into user_role(userid, roleid,createtime) values ";
                int i = 0;
                foreach (var item in ruleuser.userIds)
                {
                    inssql += string.Format("({0},{1},'{2}')", item, ruleuser.RoleId, DateTime.Now);
                    if (i < ruleuser.userIds.Count - 1)
                        inssql+=", ";
                    i++;
                }
                int isok=DataHelper.ExecuteSql(inssql);
                if (isok > 0)
                {
                    jo.Add("success", true);
                    jo.Add("mess", string.Format("操作成功" ));
                }
            }
        }
        catch (Exception e)
        {
            jo.Add("success", false);
            jo.Add("mess", string.Format("操作失败，原因{0}", e.Message));
        }
        return jo;
    }

    [HttpGet]
    public HttpResponseMessage GetRoleMenu(string user)
    {
        List<int> roleId =new List<int>();
        // 考虑多角色咋办
        string getroleSql = $" select * from user_role where userid=(select id from user where name='{user}')";
        DataTable dt = DataHelper.Query(getroleSql);
        if (dt!=null&&dt.Rows.Count>0)
        {
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                var item = dt.Rows[i];
                roleId.Add(Convert.ToInt32(item["roleid"]));
            }
        }
        
        var menuTree = GetUserMenuTree(roleId, user);
        var response = Request.CreateResponse(HttpStatusCode.OK);
        var json = JsonConvert.SerializeObject(menuTree, Formatting.None);
        response.Content = new StringContent(json, Encoding.UTF8, "application/json");
        return response;
    }

    public List<MenuTreePanel> GetUserMenuTree(List<int> roleId, string sys)
    {
        // 1. 获取菜单数据
        string command = "";
        //if (sys == "system" || sys == "admin")
        //{
            command = @"SELECT m.id, m.parentid, m.title as text, m.url, m.isleaf, m.expanded, m.OrderIndex 
                    FROM Menu m 
                    ORDER BY m.OrderIndex";
        //}
        //else
        //{
        //    var escapedCodes = roleId.Select(code => $"'{code.ToString().Replace("'", "''")}'");
        //    string inClause = string.Join(", ", escapedCodes);
        //    command = $@"
        //    SELECT m.id, m.parentid, m.title as text, m.url, m.isleaf, m.expanded, m.OrderIndex
        //    FROM Menu m
        //    INNER JOIN role_menu rm ON m.Id = rm.MenuId
        //    WHERE 1=1";

        //    if (roleId.Count > 0)
        //    {
        //        command += $" AND rm.RoleId IN ({inClause})";
        //    }
        //    command += " ORDER BY m.OrderIndex";
        //}

        XErrorLogToolMDM.WriteLog("登录GetUserMenuTree sql" + command);

        DataTable dt = DataHelper.Query(command);
        List<MenuNode> allMenuNodes = DataHelper.MapToEntities<MenuNode>(dt);

        // 2. 构建节点字典和父子关系
        var nodeDict = allMenuNodes.ToDictionary(n => n.id);
        var topLevelNodes = new List<MenuNode>();

        foreach (var node in allMenuNodes)
        {
            if (node.ParentId == "0")
            {
                topLevelNodes.Add(node);
            }
            else if (nodeDict.TryGetValue(node.ParentId, out var parent))
            {
                parent.children.Add(node);
            }
        }

        // 3. 设置leaf属性
        foreach (var node in allMenuNodes)
        {
            node.leaf = node.children.Count == 0;
        }

        // 4. 为每个顶级节点创建树面板（修复多余层级问题）
        var treePanels = new List<MenuTreePanel>();

        foreach (var topNode in topLevelNodes.OrderBy(n => n.OrderIndex))
        {
            // 直接使用顶级节点作为根节点
            treePanels.Add(new MenuTreePanel
            {
                title = topNode.text,
                rootVisible = false,
                lines = false,
                xtype = "treepanel",
                root = new MenuNode // 创建虚拟根节点
                {
                    id = "root_" + topNode.id,
                    text = "..",
                    expanded = true, // 确保展开
                    children = topNode.children // 直接使用顶级节点的子节点
                }
            });
        }

        return treePanels;
    }

    private List<MenuNode> BuildMenuTree(List<MenuNode> allNodes)
    {
        var rootNodes = new List<MenuNode>();
        var nodeDict = allNodes.ToDictionary(n => n.id);

        foreach (var node in allNodes)
        {
            if (node.ParentId == "0")
            {
                rootNodes.Add(node);
            }
            else if (nodeDict.ContainsKey(node.ParentId))
            {
                var parent = nodeDict[node.ParentId];
                parent.children.Add(node);
            }
        }

        // 3. 设置leaf属性（根据子节点自动判断）
        foreach (var node in allNodes)
        {
            node.leaf = node.children.Count == 0;
        }

        return rootNodes;
    }


    // 树面板配置类
    public class MenuTreePanel
    {
        public string title { get; set; }
        public bool rootVisible { get; set; }
        public bool lines { get; set; }
        public string xtype { get; set; }
        public MenuNode root { get; set; }
    }

    // 菜单节点类

    // 菜单节点类
    public class MenuNode
    {
        public string id { get; set; }
        public string text { get; set; } // 对应前端d text属性
        public string url { get; set; }
        public bool leaf { get; set; }
        public bool expanded { get; set; }
        public string ParentId { get; set; }
        public int OrderIndex { get; set; } // 排序字段
        public List<MenuNode> children { get; set; } = new List<MenuNode>();
        public string iconCls { get; set; }
    }


}
