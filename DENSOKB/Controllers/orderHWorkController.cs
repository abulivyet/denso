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
    public class orderHWorkController: ApiController
    {
        private orderHWorkService HWorkService = new orderHWorkService();
        [JwtAuthorize]
        [HttpPost]
        public object GetPageList(Pagination paginationobj)
        {
            paginationobj.sidx = "createdate";
            paginationobj.sord = "desc";
            DataTable data = HWorkService.getPageTB(paginationobj, paginationobj.queryJson);
            Dictionary<string, object> jo = new Dictionary<string, object>();
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            jo.Add("success", true);
            jo.Add("total", paginationobj.records);
            jo.Add("data", PulicHelper.ConvertDataTableToListOfDictionaries(data));
            return jo;
        }
    }
}