using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DENSOKB.Libs
{
    public class orderImport
    {
        public int id { get; set; }
        /// <summary>
        /// 导入时间
        /// </summary>
        public DateTime? inportdate { get; set; }
        /// <summary>
        /// 客户名称
        /// </summary>
        public string cusname{ get; set; }
        /// <summary>
        /// 客户代码
        /// </summary>
        public string cuscode{ get; set; }
        /// <summary>
        /// 订单号
        /// </summary>
        public string ordercode{ get; set; }
        /// <summary>
        /// 订单便次
        /// </summary>
        public string orderbin{ get; set; }
        /// <summary>
        /// 发货日期
        /// </summary>
        public DateTime? podate { get; set; }
        /// <summary>
        /// 传票作成者
        /// </summary>
        public string createperson { get; set; }
        /// <summary>
        /// 传票作成时刻
        /// </summary>
        public DateTime? createdate { get; set; }
        /// <summary>
        /// 处理状态 1 解析中 2 成功 3 失败 4重新解析中
        /// </summary>
        public string handlestate { get; set; }
        /// <summary>
        /// 处理消息
        /// </summary>
        public string handlemess { get; set; }
        public string redundancy1 { get; set; }
        public string redundancy2 { get; set; }
        public string redundancy3 { get; set; }
        /// <summary>
        /// 导入记录唯一主键
        /// </summary>
        public string redundancy4 { get; set; }
        public decimal? redundancy5 { get; set; }
        public decimal? redundancy6 { get; set; }
        public DateTime? redundancy7 { get; set; }
        public DateTime? redundancy8 { get; set; }
        /// <summary>
        /// 处理开始时间
        /// </summary>
        public DateTime? handlebegin { get; set; }
        /// <summary>
        /// 处理结束时间
        /// </summary>
        public DateTime? handleend { get; set; }
        /// <summary>
        /// 处理耗时
        /// </summary>
        public decimal? handletime { get; set; }
        /// <summary>
        /// 是否含有补给品
        /// </summary>
        public string hassupplies { get; set; }
        /// <summary>
        /// 确认表备注
        /// </summary>
        public string confirmRemark { get; set; }

        public string filehash { get; set; }
    }
}