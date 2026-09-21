using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DENSOKB.Models
{
    public class OrderDownload
    {
        public string type { get; set; }

        public int id { get; set; }

        public string zipfilename { get; set; }

        public string query { get; set; }
        /// <summary>
        /// 供应商
        /// </summary>
        public string cuscode { get; set; }
        /// <summary>
        /// 数据类型（量产，补给...）
        /// </summary>
        public string db_type { get; set; }
        /// <summary>
        /// 单号
        /// </summary>
        public string ordercode { get; set; }
    }
}