using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DENSOKB.Models
{
    /// <summary>
    /// 直链配货实体
    /// </summary>
    public class ftet_chain
    {/// <summary>
    /// id自增不用管
    /// </summary>
        public int id { get; set; } 
        /// <summary>
        /// 序号
        /// </summary>
        public string no { get; set; } 
        /// <summary>
        /// 订单号
        /// </summary>
        public string ordercode { get; set; } 
        /// <summary>
        /// 标签日期
        /// </summary>
        public string labeldate { get; set; } 
        /// <summary>
        /// 供应商
        /// </summary>
        public string supname { get; set; } 
        /// <summary>
        /// 背番
        /// </summary>
        public string bei { get; set; } 
        /// <summary>
        /// 位置
        /// </summary>
        public string loction { get; set; } 
        /// <summary>
        /// 1链
        /// </summary>
        public string l1 { get; set; } 
        public string l2 { get; set; } 
        public string l3 { get; set; } 
        public string l4 { get; set; } 
        public string l5 { get; set; } 
        public string l6 { get; set; } 
        public string l7 { get; set; } 
        public string l8 { get; set; } 
        public string l9 { get; set; } 
        public string l10 { get; set; } 
        public string l11 { get; set; } 
        public string l12 { get; set; } 
        public string l13 { get; set; } 
        public string l14 { get; set; } 
        public string remark { get; set; } 

    }
}