using System.Collections.Generic;

namespace DENSOKB.Models
{
    public class CHAMconfirm
    {
        /// <summary>
        /// excel标题
        /// </summary>
        public string title        { get; set; }
        /// <summary>
        /// 客户名称
        /// </summary>
        public string cusName      { get; set; }
        /// <summary>
        /// 取货车组
        /// </summary>
        public string carGroup     { get; set; }
        /// <summary>
        /// 客户编码
        /// </summary>
        public string cusCode      { get; set; }
        /// <summary>
        /// 出荷日期
        /// </summary>
        public string shipmentDate { get; set; }
        /// <summary>
        /// 星期
        /// </summary>
        public string Week { get; set; }
        /// <summary>
        /// 传票作成者
        /// </summary>
        public string summonsCreatePerson { get; set; }
        /// <summary>
        /// 传票做成时间
        /// </summary>
        public string summonsCreateTime { get; set; }
        /// <summary>
        /// 集货做成者
        /// </summary>
        public string csGoodCreatePerson { get; set; }
        /// <summary>
        /// 集货做成时间
        /// </summary>
        public string csGoodCreateTime { get; set; }
        /// <summary>
        /// 三点昭合者
        /// </summary>
        public string tpCheckCreatePerson { get; set; }
        /// <summary>
        /// 三点照合时刻
        /// </summary>
        public string tpCheckCreateTime { get; set; }

        public string confirmRemark { get; set; }
        
        public string type { get; set; } 

        public List<CHAMOrdWeekWithOrig> details { get; set; } = new List<CHAMOrdWeekWithOrig>();

    }
}