namespace DENSOKB.Models
{
    public class CHAMStackLabel
    {
        public string id             { get; set; }
        /// <summary>
        /// 出货指示单编号
        /// </summary>
        public string shippingCode   { get; set; }
        /// <summary>
        /// 托标签号
        /// </summary>
        public string stackLabelCode { get; set; }
        /// <summary>
        /// 路径
        /// </summary>
        public string stackLabelName { get; set; }
        
        /// <summary>
        /// 文件路径
        /// </summary>
        public string inputPath  { get; set; }
    }
}