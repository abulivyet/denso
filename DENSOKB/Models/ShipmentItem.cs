using System;

namespace DENSOKB.Models
{
    /// <summary>
    /// 送货单中
    /// </summary>
    public class ShipmentItem
    {
        public string   PartNumber { get; set; } // 部品番号
        public string   PartName   { get; set; } // 部品名称
        public string   Line       { get; set; } // 线别
        public DateTime InDate     { get; set; } // 搬入日期
        public int      BoxCount   { get; set; } // 箱数
        public int      Quantity   { get; set; } // 部品数量

        public string OutDate { get; set; }  //取货日期
        
        public override string ToString()
        {
            return $"{PartNumber} | {PartName} | {Line} | {InDate:yyyy-MM-dd} | {BoxCount} | {Quantity}";
        }
    }
}