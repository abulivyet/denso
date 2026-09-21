using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace DENSOKB
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            string LicenseCategory = ConfigurationManager.AppSettings["LicenseCategory"].ToString();
            string key = ConfigurationManager.AppSettings["SpirePdfLicense"].ToString();

            if(LicenseCategory == "debug")
            {
                //Spire.Pdf.License.LicenseProvider.SetLicenseKey(key);
                Spire.Pdf.License.LicenseProvider.LoadLicense();
            }
            else
            {
                Spire.Pdf.License.LicenseProvider.SetLicenseKey(key);
            }

        }
    }
}
