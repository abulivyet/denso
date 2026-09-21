using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DENSOKB.Models
{
    public class OrdWeekOrder
    {
        public int? id { get; set; }
        /// <summary>
        /// 序号
        /// </summary>
        public string o_number { get; set; }
        /// <summary>
        /// 供应商编号
        /// </summary>
        public string sup_code { get; set; }
        /// <summary>
        /// 收货地点
        /// </summary>
        public string r_location { get; set; }
        /// <summary>
        /// 上线地点
        /// </summary>
        public string online_v { get; set; }
        /// <summary>
        /// 零件号
        /// </summary>
        public string part_code { get; set; }
        /// <summary>
        /// 零件颜色
        /// </summary>
        public string part_color { get; set; }
        /// <summary>
        /// 零件名称
        /// </summary>
        public string part_name { get; set; }
        /// <summary>
        /// 计划代码
        /// </summary>
        public string plan_code { get; set; }
        /// <summary>
        /// 厂内收货日期
        /// </summary>
        public DateTime? r_date{ get; set; }
        /// <summary>
        /// 厂内收货时间
        /// </summary>
        public string r_time{ get; set; }
        /// <summary>
        /// 收货日期
        /// </summary>
        public DateTime? c_date { get; set; }
        /// <summary>
        /// 收货时间
        /// </summary>
        public string c_time { get; set; }
        /// <summary>
        /// 订货数量
        /// </summary>
        public decimal? order_num { get; set; }
        /// <summary>
        /// 传票号码
        /// </summary>
        public string p_code { get; set; }
        /// <summary>
        /// 版本
        /// </summary>
        public string o_version { get; set; }
        /// <summary>
        /// 周次
        /// </summary>
        public string o_week { get; set; }
        /// <summary>
        /// 分类
        /// </summary>
        public string o_class { get; set; }
        /// <summary>
        /// 交货规则
        /// </summary>
        public string rule { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string remarks { get; set; }
        /// <summary>
        /// 订单编号
        /// </summary>
        public string order_code { get; set; }
        /// <summary>
        /// 车型
        /// </summary>
        public string carmodel { get; set; }

    }
}