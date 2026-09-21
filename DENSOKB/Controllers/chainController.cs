using DENSOKB.Helper;
using DENSOKB.Libs;
using DENSOKB.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Script.Serialization;

namespace DENSOKB.Controllers
{
    public class chainController:ApiController
    {
        [JwtAuthorize]
        [HttpPost]
        public object GetPageList(Pagination paginationobj)
        {
            paginationobj.sidx = "ordercode";
            paginationobj.sord = "desc";
            DataTable data = chainService.getftet_chainPage(paginationobj, paginationobj.queryJson);
            Dictionary<string, object> jo = new Dictionary<string, object>();
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            jo.Add("success", true);
            jo.Add("total", paginationobj.records);
            jo.Add("data", PulicHelper.ConvertDataTableToListOfDictionaries(data));
            return jo;
        }
        [JwtAuthorize]
        [HttpPost]
        public object getUnitPage(Pagination paginationobj)
        {
            paginationobj.sidx = "cust_name,recv_port,denso_num";
            paginationobj.sord = "desc";
            DataTable data = chainService.getUnitPage(paginationobj, paginationobj.queryJson);
            Dictionary<string, object> jo = new Dictionary<string, object>();
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            jo.Add("success", true);
            jo.Add("total", paginationobj.records);
            jo.Add("data", PulicHelper.ConvertDataTableToListOfDictionaries(data));
            return jo;
        }
        //public void testPost(List<string> list)
        //{
        //    GHACController ghac = new GHACController();
        //    ghac.GHAC_CONFIRM(list);
        //}
    }
}