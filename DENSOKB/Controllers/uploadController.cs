using DENSOKB.Libs;

using HelpLog.CommomMethod;
using ModbusTester;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace DENSOKB.Controllers
{
    public class uploadController : ApiController
    {
        string apiPath = ConfigurationManager.AppSettings["apiPath"].ToString();//apiPath
        public string success = "200";
        [HttpPost]
        public baseMessage uploadFile()
        {
            baseMessage rtn = new baseMessage();

            HttpFileCollection files = System.Web.HttpContext.Current.Request.Files;

            if (files.Count == 0)
            {
                rtn.success = false;
                rtn.result = "error";
                //rtn.msg = "未选择文件";
                return rtn;
            }

            string savePath = System.Web.HttpContext.Current.Server.MapPath("~/UploadFiles/") + files[0].FileName;

            files[0].SaveAs(savePath);

            return rtn;
        }
       
        public async Task<string> analysisAPI(string apiUrl)
        {
            string mess = "数据处理";
            if (string.IsNullOrEmpty(apiUrl)) return "";
            string setfilePath = apiPath + apiUrl;
            XErrorLogToolMDM.WriteLog("-----开始解析数据------::"+ setfilePath);
            try
            {
                // 获取数据  不能获取所有数据，逻辑修改后 上传哪个解析那个 获取所有掉的接口是不一样的
                DataTable data = orderimportService.getNotHead();
                List<orderImport> reouseList = new List<orderImport>();
                if (data != null && data.Rows.Count > 0)
                {
                    //JArray jarr = JArray.Parse(Jlist["data"].ToString());

                    //调用接口 拆分文件
                    for (int i = 0; i < data.Rows.Count; i++)
                    {
                        orderImport reouse = new orderImport();
                        reouse.handlebegin = DateTime.Now;
                        var item = data.Rows[i];
                        JObject jo = new JObject();
                        jo.Add("zipPath", item["redundancy1"].ToString());
                        string uploadPath = ConfigurationManager.AppSettings["fileUpload"].ToString();

                        jo.Add("basePath", uploadPath);
                        jo.Add("createPerson", item["createperson"].ToString());// 
                        jo.Add("mainId", item["id"].ToString());// 
                        JObject PostOut = null;
                        try
                        {
                            string PostSetfile = HttpPost.PostXmlToUrl(setfilePath, jo.ToString(), "调用接口发送路径");
                            PostOut=  (JObject)JsonConvert.DeserializeObject(PostSetfile);
                        }
                        catch (Exception e)
                        {
                            reouse.handlestate = "3";
                            reouse.handlemess = "调用错误:"+e.Message;
                        }
                        

                        reouse.id = Convert.ToInt32(item["id"].ToString());
                        if (PostOut != null) {
                            if (PostOut["code"].ToString().Equals(success))
                            {
                                reouse.handlestate = "2";
                                reouse.handlemess = "解析成功";

                            }
                            else
                            {
                                reouse.handlestate = "3";
                                reouse.handlemess = PostOut["info"].ToString();
                            }
                            reouse.ordercode = PostOut["ordercode"].ToString();
                            reouse.orderbin = PostOut["bin"].ToString(); //bin
                            DateTime? result = null;
                            if (!string.IsNullOrEmpty(PostOut["fdate"]?.ToString()))
                            {
                                DateTime temp;
                                if (DateTime.TryParse(PostOut["fdate"].ToString(), out temp))
                                {
                                    result = temp;
                                }
                            }
                            reouse.podate = result;// fdate
                        }
                        
                        reouse.handleend = DateTime.Now;
                        TimeSpan duration = (DateTime)reouse.handleend - (DateTime)reouse.handlebegin;
                        double totalSeconds = duration.TotalSeconds;
                        reouse.handletime = (decimal)totalSeconds;
                        reouseList.Add(reouse);
                    }
                    //写入数据库
                    orderimportService.setState(reouseList);
                }
                
            }
            catch (Exception e)
            {
                XErrorLogToolMDM.WriteLog("-----解析异常------::" + e.Message);
            }
            return mess;
        }

        // 批量处理
        public async Task<string> analysisAPI_batch(List<orderImport> reList)
        {
            try
            {
                string mess = "数据已重新发送并处理";
                XErrorLogToolMDM.WriteLog("-----开始批量解析数据------");
                // 获取数据

                List<orderImport> reouseList = new List<orderImport>();
                if (reList != null && reList.Count > 0)
                {
                    string apiUrl = "";

                    //调用接口 拆分文件
                    for (int i = 0; i < reList.Count; i++)
                    {
                        orderImport reouse = new orderImport();
                        reouse.handlebegin = DateTime.Now;
                        apiUrl             = getApiPath(reList[i]);
                        string  setfilePath = apiPath + apiUrl;
                        JObject jo          = new JObject();
                        jo.Add("zipPath", reList[i].redundancy1);
                        string uploadPath = ConfigurationManager.AppSettings["fileUpload"].ToString();
                        jo.Add("basePath", uploadPath);
                        jo.Add("createPerson", reList[i].createperson); // 
                        jo.Add("mainId", reList[i].id);                 // 
                        JObject PostOut = null;
                        if (!string.IsNullOrEmpty(apiUrl))
                        {
                            try
                            {
                                string PostSetfile = HttpPost.PostXmlToUrl(setfilePath, jo.ToString(), "调用接口发送路径");
                                PostOut = (JObject)JsonConvert.DeserializeObject(PostSetfile);
                            }
                            catch (Exception e)
                            {
                                reouse.handlestate = "3";
                                reouse.handlemess = "调用错误：" + e.Message;
                            }
                        }
                        else {
                            reouse.handlestate = "2";
                            reouse.handlemess = "解析成功";
                        }


                            /* 佛山：传票，看板，拖标签小毕直接写了方法 没接口 要么增加接口 要么调方法*/
                            reouse.id = reList[i].id;
                        if (PostOut != null)
                        {


                            if (PostOut["code"].ToString().Equals(success))
                            {
                                reouse.handlestate = "2";
                                reouse.handlemess = "解析成功";
                            }
                            else
                            {
                                reouse.handlestate = "3";
                                reouse.handlemess = PostOut["info"].ToString();
                                if (reouse.handlemess.IndexOf("另一进程使用") > -1)
                                {
                                    // 需排查其他api文件占用原因，实在处理不了再此处强制处理
                                }
                            }

                            reouse.ordercode = PostOut["ordercode"].ToString();
                            reouse.orderbin = PostOut["bin"].ToString(); //bin
                            DateTime? result = null;
                            if (!string.IsNullOrEmpty(PostOut["fdate"]?.ToString()))
                            {
                                DateTime temp;
                                if (DateTime.TryParse(PostOut["fdate"].ToString(), out temp))
                                {
                                    result = temp;
                                }
                            }

                            reouse.podate = result; // fdate
                            DateTime? r7 = null;
                            if (!string.IsNullOrEmpty(PostOut["ddate"]?.ToString()))
                            {
                                DateTime temp;
                                if (DateTime.TryParse(PostOut["ddate"].ToString(), out temp))
                                {
                                    r7 = temp;
                                }
                            }
                            reouse.redundancy7 = r7;
                        }
                      
                        reouse.handleend   = DateTime.Now;
                        TimeSpan duration     = (DateTime)reouse.handleend - (DateTime)reouse.handlebegin;
                        double   totalSeconds = duration.TotalSeconds;
                        reouse.handletime = (decimal)totalSeconds;

                        reouseList.Add(reouse);
                        //写入数据库
                        orderimportService.setState(reouse);
                    }
                }

                return mess;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return e.Message;
            }
        }

        public string getApiPath(orderImport order)
        {
            string apiPath = "";
            if (order.cuscode.Equals("FTET")) // 一汽量产+ 补给
            {
                apiPath = ConfigurationManager.AppSettings["FTETPath"].ToString();
            }
            else if (order.redundancy2.Equals("量产") && order.cuscode.Equals("FTCE")) // 长春量产
            {
                apiPath = ConfigurationManager.AppSettings["FTCElcPath"].ToString();
                ;
            }
            else if (order.redundancy2.Equals("补给") && order.cuscode.Equals("FTCE")) // 长春补给
            {
                apiPath = ConfigurationManager.AppSettings["FTCEbjPath"].ToString();
            }
            else if (order.cuscode.Equals("CHAM") && order.redundancy2.Equals("送货单"))
            {
                apiPath = ConfigurationManager.AppSettings["CHAMPath"].ToString(); // 佛山本田
            }
            else if (order.cuscode.Equals("CHAM") && order.redundancy2.Equals("传票"))
            {
                apiPath = ConfigurationManager.AppSettings["CHAMDisposeSummonPdf"].ToString();
            }
            else if (order.cuscode.Equals("CHAM") && order.redundancy2.Equals("托标签"))
            {
                apiPath = ConfigurationManager.AppSettings["CHAMDisposeStackLabel"].ToString();
            }
            else if (order.cuscode.Equals("CHAM") && order.redundancy2.Equals("看板"))
            {
                apiPath = ConfigurationManager.AppSettings["CHAMDisposeBoard"].ToString();
            }


            else if (order.cuscode.Equals("DHEC") && order.redundancy1.Contains("pdf"))
            {
                apiPath = ConfigurationManager.AppSettings["DHECPath"].ToString(); // 东风本田
            }

            return apiPath;
        }
    }
}
