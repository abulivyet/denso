using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DENSOKB.Models
{
    public class User
    {
       /// <summary>
       /// 主键
       /// </summary>
        public int id { get; set; }
        public string name { get; set; }
        public string username { get; set; }

        public string password { get; set; }
    }
}