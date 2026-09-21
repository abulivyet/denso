using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DENSOKB.Models
{
    public class Login
    {
        public string name { get; set; }
        public string username { get; set; }

        public string password { get; set; }

        public string token { get; set; }
    }
}