using System.ComponentModel.DataAnnotations.Schema;

namespace DENSOKB.Models
{
    public class CHAMOrdWeekWithOrig : CHAMOrdWeek
    {
        /// <summary>
        /// 电装品番
        /// </summary>
        [Column("denso_num")]
        public string DensoNum { get; set; }
        [Column("supplier")]
        public string supplier { get; set; }
        
        /// <summary>
        /// 收容数
        /// </summary>
        [Column("box_cap")]
        public int? BoxCap { get; set; }
        
        /// <summary>
        /// 托标准箱数
        /// </summary>
        [Column("pallet_box")]
        public int? PalletBox { get; set; }
        /// <summary>
        /// 传票日期
        /// </summary>
        [Column("cpdate")]
        public string cpdate { get; set; }
        /// <summary>
        /// 传票时间
        /// </summary>
        [Column("cptime")]
        public string cptime { get; set; }

        [Column("traceability_label")]
        public string traceabilityLabel { get; set; }
    }
}