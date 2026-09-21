using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DENSOKB.Models
{
    public class inpdetail2
    {
        public int? id         {get;set;}
        public string mainId     {get;set;}
        /// <summary>
        /// 文件路径
        /// </summary>
        public string Stacklabel {get;set;}
        /// <summary>
        ///  类型：码垛单，小看板，a4穿透...
        /// </summary>
        public string filetype   {get;set;}
        /// <summary>
        /// 单据号
        /// </summary>
        public string ordercode  {get;set;}
        /// <summary>
        /// 解析备注、错误记录
        /// </summary>
        public string remarks    {get;set;}
        /// <summary>
        /// 备用字段，如果使用把此注释改掉
        /// </summary>
        public string r1 { get; set; }
        /// <summary>
        /// 排序号 必须传
        /// </summary>
        public int? rowcode { get; set; }

    }
}