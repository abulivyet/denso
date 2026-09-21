using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DENSOKB.Models
{
    public class FtceDetail
    {
        public string orderCode { get; set; }
        public string mainId { get; set; }
        /// <summary>
        /// 捆包路径
        /// </summary>
        public string bale { get; set; }
        /// <summary>
        /// 货垛标签
        /// </summary>
        public string Stacklabel { get; set; }
        /// <summary>
        /// 小看板
        /// </summary>
        public string Board { get; set; }
        /// <summary>
        /// A4成套
        /// </summary>
        public string A4Board { get; set; }
        /// <summary>
        /// 受领书
        /// </summary>
        public string Receipt { get; set; }

        public string comfirm { get; set; }
        /// <summary>
        /// 链标签
        /// </summary>
        public string lian { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string remarks { get; set; }
    }
}