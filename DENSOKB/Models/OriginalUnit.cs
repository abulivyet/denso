using System.ComponentModel.DataAnnotations.Schema;

namespace DENSOKB.Models
{
    public class OriginalUnit
    {
        /// <summary>
        /// 客户简称
        /// </summary>
        [Column("cust_name")]
        public string CustName { get; set; }

        /// <summary>
        /// 客户代码
        /// </summary>
        [Column("cust_code")]
        public string CustCode { get; set; }

        /// <summary>
        /// 受入口
        /// </summary>
        [Column("recv_port")]
        public string RecvPort { get; set; }

        /// <summary>
        /// 车型
        /// </summary>
        [Column("veh_model")]
        public string VehModel { get; set; }

        /// <summary>
        /// back_num
        /// </summary>
        [Column("back_num")]
        public string BackNum { get; set; }

        /// <summary>
        /// 受入区代码
        /// </summary>
        [Column("recv_area")]
        public string RecvArea { get; set; }

        /// <summary>
        /// 交货地点
        /// </summary>
        [Column("del_loc")]
        public string DelLoc { get; set; }

        /// <summary>
        /// 电装品番
        /// </summary>
        [Column("denso_num")]
        public string DensoNum { get; set; }

        /// <summary>
        /// 客户品番
        /// </summary>
        [Column("cust_num")]
        public string CustNum { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Column("custNum")]
        public string CustNum2 { get; set; }

        /// <summary>
        /// 品番名称
        /// </summary>
        [Column("part_name")]
        public string PartName { get; set; }

        /// <summary>
        /// 箱收容数
        /// </summary>
        [Column("box_cap")]
        public int? BoxCap { get; set; }

        /// <summary>
        /// 托标准箱数
        /// </summary>
        [Column("pallet_box")]
        public int? PalletBox { get; set; }

        /// <summary>
        /// 箱型号
        /// </summary>
        [Column("box_type")]
        public string BoxType { get; set; }

        /// <summary>
        /// 库位号
        /// </summary>
        [Column("loc_num")]
        public string LocNum { get; set; }

        /// <summary>
        /// 托占比
        /// </summary>
        [Column("pallet_pct")]
        public decimal PalletPct { get; set; }

        /// <summary>
        /// 组合打托
        /// </summary>
        [Column("comb_pallet")]
        public string CombPallet { get; set; }
        
        /// <summary>
        /// 追溯标签
        /// </summary>
        [Column("traceability_label")]
        public string traceabilityLabel { get; set; }
        
        /// <summary>
        /// 品名中文
        /// </summary>
        [Column("product_name_cn")]
        public string productNameCn   { get; set; }
        
        /// <summary>
        /// 厂别
        /// </summary>
        [Column("factory")]
        public string factory   { get; set; }
        /// <summary>
        /// 使用标识
        /// </summary>
        [Column("UseFlag")]
        public string UseFlag { get; set; }
        
        [Column("sort")]
        public int? sort { get; set; }
        
        
    }
}
