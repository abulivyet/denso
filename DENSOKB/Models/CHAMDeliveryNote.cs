using System;

namespace DENSOKB.Models
{
    public class CHAMDeliveryNote
    {
        /// <summary>
        /// 出货指示号
        /// </summary>
        public string deliveryNoteCode { get; set; }
        /// <summary>
        /// 取货日期
        /// </summary>
        public string PickupDate       { get; set; }
        /// <summary>
        /// 路线
        /// </summary>
        public string route            { get; set; }
        /// <summary>
        /// 趟次
        /// </summary>
        public string trip             { get; set; }
        /// <summary>
        /// 供应商编号
        /// </summary>
        public string SupplierCode     { get; set; }
        /// <summary>
        /// 供应商简称
        /// </summary>
        public string SupplierName     { get; set; }
        /// <summary>
        /// 计划取货日期
        /// </summary>
        public string PlanPickUpTime   { get; set; }
        /// <summary>
        /// 托数
        /// </summary>
        public int tCount              { get; set; }
        /// <summary>
        /// 计划到货日期
        /// </summary>
        public string PlanArrivalTime  { get; set; }
        
    }
}