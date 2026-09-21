using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DENSOKB.Models
{
    public class FTETEntity
    {
        /// <summary>
        /// 发注单号
        /// </summary>
        public string FZcode { get; set; }
        /// <summary>
        /// k3订单号
        /// </summary>
        public string k3code { get; set; }
        /// <summary>
        /// 出荷日期
        /// </summary>
        public string outdate { get; set; }
        /// <summary>
        /// 规定到货时间
        /// </summary>
        public string arriveDate { get; set; }
        public List<FTETdetailEntity> FTETdetailList { get;set; }
    }
}