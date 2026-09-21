using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace DENSOKB.Models
{
    public class CHAMOrdWeek
    {
        /// <summary>
        /// id
        /// </summary>
        [Column("id")]
        public string id { get; set; }

        /// <summary>
        /// 订单状态
        /// </summary>
        [Column("ord_status")]
        public string ordStatus { get; set; }
        /// <summary>
        /// 订单号
        /// </summary>
        [Column("orderCode")]
        public string orderCode { get; set; }

        /// <summary>
        /// 发布日期
        /// </summary>
        [Column("publishDate")]
        public string  publishDate { get; set; }
        /// <summary>
        /// 客户名称
        /// </summary>
        [Column("cusname")]
        public string cusName { get; set; }
        /// <summary>
        /// 传票
        /// </summary>
        [Column("court_summons")]
        public string courtSummons { get; set; }
        /// <summary>
        /// 物料编码
        /// </summary>
        [Column("material_code")]
        public string materialCode { get; set; }
        /// <summary>
        /// 物料名称
        /// </summary>
        [Column("material_name")]
        public string materialName { get; set; }
        /// <summary>
        /// 计划代码
        /// </summary>
        [Column("project_code")]
        public string projectCode { get; set; }
        /// <summary>
        /// 需求日期
        /// </summary>
        [Column("demanded_date")]
        public DateTime? demandedDate { get; set; }
        /// <summary>
        /// 收货时间
        /// </summary>
        [Column("receiving_time")]
        public string receivingTime { get; set; }
        /// <summary>
        /// 数量
        /// </summary>
        [Column("quantity")]
        public string quantity { get; set; }
        /// <summary>
        /// 单位
        /// </summary>
        [Column("unit")]
        public string unit { get; set; }
        /// <summary>
        /// 订单类型
        /// </summary>
        [Column("order_type")]
        public string orderType { get; set; }
        /// <summary>
        /// 收获地址
        /// </summary>
        [Column("receiving_address")]
        public string receivingAddress { get; set; }
        /// <summary>
        /// 搬入场所代码
        /// </summary>
        [Column("movingIn_code")]
        public string movingInCode { get; set; }
        /// <summary>
        /// 新收荷日期
        /// </summary>
        [Column("demanded_date_new")]
        public string demandeDatedNew { get; set; }
    }
}