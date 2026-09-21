namespace DENSOKB.Models
{
    public class Bundle
    {
        public string   itemCode       { get; set; } // 品番
        public string   referenceNumber{ get; set; } // 背番号
        public int      capacityPerBox { get; set; } // 收容数
        public int      totalBoxes     { get; set; } // 总箱数
        public string   shelfNumber    { get; set; } // 货架号
        public string   palletNumber   { get; set; } // 托盘编号
        public int      quantity       { get; set; } // 数量
        
        public string itemName { get; set; } // 品名
        public string densoNum { get; set; } // DENSO品番
        public string vehModel { get; set; } // 车型

    }
}