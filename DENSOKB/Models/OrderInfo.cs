using System;

namespace DENSOKB.Models
{
    public class OrderInfo
    {
        public string ShipmentDate       { get; set; } // 出货日期       //cham订单纳入日
        public string ArrivalDueDate     { get; set; } // 到货纳期      //cham到货日期
        public string OrderCode          { get; set; } // 订单号
        public string InclusionNumber    { get; set; } // 纳入番号
        public string CustomerPartNumber { get; set; } // 客户品番
        public string DnPartNumber       { get; set; } // DN品番
        public string BackNumber         { get; set; } // 背番号
        public int    Capacity           { get; set; } // 收容数
        public int    BoardCount         { get; set; } // 看板张数
        public int    InclusionQuantity  { get; set; } // 纳入个数
        public string Category           { get; set; } // 分类
        public string kCode              { get; set; } // 

        public string planCode          { get; set; } // 计划代码

        public string ReceiptPlace { get; set; }   //收货地点


    }

}