
namespace DENSOKB.Models
{
    public class ResParameter
    {
      
            /// <summary>
            /// 接口响应码 200正常
            /// </summary>
            public string code { get; set; }
            /// <summary>
            /// 接口响应消息
            /// </summary>
            public string info { get; set; }
            /// <summary>
            /// 接口响应数据
            /// </summary>
            public object data { get; set; }
            /// <summary>
            ///  返回订单号
            /// </summary>
            public string ordercode { get; set; }
        /// <summary>
        ///  返回便次
        /// </summary>
        public string bin { get; set; }
        /// <summary>
        /// 发货日期
        /// </summary>

        public string fdate { get; set; }
        /// <summary>
        /// 到货日期，规定到货日期  必须要
        /// </summary>
        public string ddate { get; set; }
        //成功相应
        public static ResParameter Success(object data, string ordercode, string bin, string fdate, string message = "请求成功")
        {
            return new ResParameter
            {
                code      = "200",
                info      = message,
                data      = data,
                ordercode = ordercode,
                bin       = bin,
                fdate     = fdate
            };
        }


        //  成功响应（有数据）
        public static ResParameter Success(object data, string message = "请求成功")
        {
            return new ResParameter
            {
                code = "200",
                info = message,
                data = data
            };
        }

        //  成功响应（无数据）
        public static ResParameter Success(string message = "请求成功")
        {
            return new ResParameter
            {
                code = "200",
                info = message,
                data = null
            };
        }

        //  失败响应
        public static ResParameter Fail(string message = "请求失败", string errorCode = "500")
        {
            return new ResParameter
            {
                code = errorCode,
                info = message,
                data = null
            };
        }
    }
}