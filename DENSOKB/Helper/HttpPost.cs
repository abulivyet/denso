using HelpLog.CommomMethod;
using System;
using System.IO;
using System.Net;
using System.Text;

namespace ModbusTester
{
    class HttpPost
    {
        private int _timeout = 20000;
        private int _readWriteTimeout = 60000;

        public string Postdata(string strpost, string url)
        {

            //表示空字符串，字段为只读
            string json = string.Empty;

            try
            {
                //创建restful请求
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                //请求的方法是post
                request.Method = "post";
                //请求的内容类型
                request.ContentType = "application/json";
                request.Timeout = this._timeout;
                request.ReadWriteTimeout = this._readWriteTimeout;

                //得到参数
                string data = strpost;
                //将字节序列存储到数组中，编码方式为UTF-8
                byte[] byteData = UTF8Encoding.UTF8.GetBytes(data.ToString());
                //将数组的元素总和给请求的标头
                request.ContentLength = byteData.Length;

                //以流的形式附加参数
                using (Stream postStream = request.GetRequestStream())
                {
                    postStream.Write(byteData, 0, byteData.Length);
                }

                //执行请求，达到json
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    //以流的形式读取，返回的就是字符串的json格式
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    json = reader.ReadToEnd();
                }
            }
            catch (WebException wex) {

                    json = wex.ToString();
            }
            return json;
        }
        public static string PostXmlToUrl(string url, string json, string logmess)
        {
            
            XErrorLogToolMDM.WriteLog(string.Format("调用类型【{0}】;\r\n url:{1},传入参数：{2}", logmess, url, json));
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(json);
                if (string.IsNullOrEmpty(url))
                {
                    return "请传入url";
                }
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST"; // 
                request.Accept = "application/json";
                request.ContentType = "application/json";
                request.Timeout = 5*60*1000 ; // 设置3分钟0秒超时
                //request.Headers.Add(HttpRequestHeader.Authorization, getMd5Code());
                request.ContentLength = data.Length;
                request.ServicePoint.Expect100Continue = false; //加了这一行代码 就OK了
                request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; SV1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";
                using (Stream reqStream = request.GetRequestStream())
                {
                    reqStream.Write(data, 0, data.Length);
                    reqStream.Close();
                }

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream myResponseStream = response.GetResponseStream();

                StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.UTF8);
                string retString = myStreamReader.ReadToEnd();

                myStreamReader.Close();
                myResponseStream.Close();
                XErrorLogToolMDM.WriteLog(string.Format("接口返回结果，调用类型【{0}】;\r\n url:{1},传入参数：{2};返回结果：{3}", logmess, url, json, retString));
               
                return retString;
            }
            catch (Exception e)
            {
                XErrorLogToolMDM.WriteLog(string.Format("接口调用错误，接口类型【{0}】;\r\n url:{1}；\r\n 传入参数：{2} ；\r\n 错误日志：{3}",
                    logmess,
                    url,
                    json,
                    e.Message
                    ));// 记录日志
                return null;
            }
        }
    }
}
