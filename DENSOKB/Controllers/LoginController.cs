using DENSOKB.Models;
using System.Web.Http;
using System.Data;
using DENSOKB.Helper;
using System.Configuration;
using System.Collections.Generic;
using System;
using JWT.Algorithms;
using JWT;
using JWT.Serializers;
using System.Security.Cryptography;
using System.Text;
using DENSOKB.Libs;
using System.Web.Script.Serialization;
using Newtonsoft.Json;

namespace DENSOKB.Controllers
{
    public class LoginController: ApiController
    {
        private UserService userservice = new UserService();
        [HttpPost]
        public ResParameter login(User user) {
            //user.password
            string md5_pwd = getMD5(user.password);
            string sql = "select username,password,name from user where username='" + user.username + "' and password='" + md5_pwd + "'";
            DataTable data = DataHelper.Query(sql);
            ResParameter res = new ResParameter();
            if (data.Rows.Count < 1)
            {
                res.code = "100";
                res.info = "无效的账号或密码";
                res.data = null;
                return res;
            }
            string password = data.Rows[0][1].ToString();
            string name = data.Rows[0][2].ToString();
            res.code = "200";
            res.info = "成功";

            // 2. 生成JWT Token
            var secret = "your-secret-key"; // 密钥，应存储在 web.config 中
            var token = GenerateToken(user.username, secret);

            object obj = new {
                //password= password, // chy 为啥要返回密码
                name= name,
                token=token
            };
            res.data = obj;
            return res;
        }

        private string GenerateToken(string username, string secret)
        {
            var payload = new Dictionary<string, object>
            {
                { "sub", username },
                { "exp", DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds() }
            };

            IJwtAlgorithm algorithm = new HMACSHA256Algorithm();
            IJsonSerializer serializer = new JsonNetSerializer();
            IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
            IJwtEncoder encoder = new JwtEncoder(algorithm, serializer, urlEncoder);

            return encoder.Encode(payload, secret);
        }

        private string getMD5(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);  // 字符串转字节数组
                byte[] hashBytes = md5.ComputeHash(inputBytes);     // 计算 MD5 哈希值

                // 将字节数组转换为 16 进制字符串
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2")); // "x2" 表示 2 位小写 16 进制
                }
                return sb.ToString();
            }
        }
        [JwtAuthorize]
        [HttpPost]
        public object GetUserPageList(Pagination paginationobj)
        {
            paginationobj.sidx = "username";
            paginationobj.sord = "desc";
            DataTable data = userservice.getPageTB(paginationobj, paginationobj.queryJson);
            Dictionary<string, object> jo = new Dictionary<string, object>();
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            jo.Add("success", true);
            jo.Add("total", paginationobj.records);
            jo.Add("data", PulicHelper.ConvertDataTableToListOfDictionaries(data));
            return jo;
        }
        public object Adduser(User user)
        {
            Dictionary<string, object> jo = new Dictionary<string, object>();
            try
            {   // 验重
                var obj = new { username = user.username };
                DataTable oldUserTB= userservice.getNotPageTB(JsonConvert.SerializeObject(obj));
                if (oldUserTB!=null&& oldUserTB.Rows.Count>0)
                {
                    jo.Add("success", false);
                    string mess = string.Format("账号【{0}】已存在。",user.username);
                    jo.Add("mess", mess);
                    return jo;
                }
                else
                {
                    user.password = getMD5(user.password);
                    userservice.AddUser(user);
                    jo.Add("success", true);
                    jo.Add("mess", "ok");
                }
                
            }
            catch (Exception e)
            {
                jo.Add("success", false);
                jo.Add("mess", string.Format("添加账号失败，原因{0}",e.Message));
            }
            return jo;
        }
        public object pudatepswd(User user)
        {
            Dictionary<string, object> jo = new Dictionary<string, object>();
            try { 

                    user.password = getMD5(user.password);
                    userservice.UpdateUser(user);
                    jo.Add("success", true);
                    jo.Add("mess", "ok");                

            }
            catch (Exception e)
            {
                jo.Add("success", false);
                jo.Add("mess", string.Format("修改密码失败，原因{0}", e.Message));
            }
            return jo;
        }
        public object deluser(User user)
        {
            Dictionary<string, object> jo = new Dictionary<string, object>();
            try
            {
                userservice.deluser(user.id);
                jo.Add("success", true);
                jo.Add("mess", "ok");

            }
            catch (Exception e)
            {
                jo.Add("success", false);
                jo.Add("mess", string.Format("删除失败，原因{0}", e.Message));
            }
            return jo;
        }

    }

}