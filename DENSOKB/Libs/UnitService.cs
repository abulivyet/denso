using DENSOKB.Helper;
using DENSOKB.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DENSOKB.Libs
{
    public class UnitService
    {

        public void saveUnits(List<OriginalUnit> units,string type)
        {
            try {
                if (type == "toyota") {
                    string sql = "delete from originalunit where cust_name in ('FTEC','FTET')";
                    DataHelper.ExecuteSql(sql);
                }else if (type == "honda")
                {
                    string sql = "delete from originalunit where cust_name in ('DHEC','CHAM','GHAC')";
                    DataHelper.ExecuteSql(sql);
                }

                DataHelper.BulkInsert("originalunit", units);
            }
            catch(Exception e) {
                throw;
            }
            
        }

    }
}