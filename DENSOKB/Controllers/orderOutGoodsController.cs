
using DENSOKB.Helper;
using DENSOKB.Libs;
using DENSOKB.Models;
using System.Collections.Generic;
using System.Data;
using System.Web.Http;
using System.Web.Script.Serialization;

namespace DENSOKB.Controllers
{
   
    public class orderOutGoodsController: ApiController
    {
        private orderOutGoodsService outStrvice = new orderOutGoodsService();
        [JwtAuthorize]
        [HttpPost]
        public object GetPageList(Pagination paginationobj)
        {
            paginationobj.sidx = "c_time";
            paginationobj.sord = "desc";
            DataTable data = outStrvice.getPageTB(paginationobj, paginationobj.queryJson);
            Dictionary<string, object> jo = new Dictionary<string, object>();
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            jo.Add("success", true);
            jo.Add("total", paginationobj.records);
            jo.Add("data", PulicHelper.ConvertDataTableToListOfDictionaries(data));
            return jo;
        }
    }
}