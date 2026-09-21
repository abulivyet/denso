using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DENSOKB.Models
{
    public class FtceSuppliesDto
    {
        /// <summary>
        /// 上传之后的excel路径
        /// </summary>
        public string excelPath { get; set; }
        /// <summary>
        /// 处理之后的输出路径
        /// </summary>
        public string basePath { get; set; }
        /// <summary>
        /// 创建人
        /// </summary>
        public string createPerson { get; set; }
        /// <summary>
        /// 主表id
        /// </summary>
        public string mainId { get; set; }
    }
}