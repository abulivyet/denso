using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DENSOKB.Models
{
    public class FTETdetailEntity
    {
        /// <summary>
        /// 品番
        /// </summary>
        public string pin { get; set; }
        /// <summary>
        /// 背番
        /// </summary>
        public string bei { get; set; }
        /// <summary>
        /// 收容数
        /// </summary>
        public int? shou { get; set; }
        /// <summary>
        /// 箱数
        /// </summary>
        public int? box { get; set; } 

        /// <summary>
        /// 收入代码
        /// </summary>
        public string receivecode { get; set; }
        /// <summary>
        /// 订货个数
        /// </summary>
        public int? number { get; set; }
        /// <summary>
        /// 箱拖比
        /// </summary>
        public decimal? boxTou { get; set; }
    }
}