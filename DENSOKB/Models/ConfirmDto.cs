namespace DENSOKB.Models
{
    public class ConfirmDto
    {
        public string id { get; set; } // 行id
        public string confirmRemark { get; set; } // 确认表备注
        
        public string ordercode { get; set; } // 订单号
        public string type { get; set; }

        public string carno  { get; set; } //东风本田车组
        public string p_code { get; set; } //东风本田传票号
        
    }
}