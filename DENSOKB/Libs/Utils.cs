using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Web;

namespace DENSOKB.Libs
{
    public class Utils
    {
        /// <summary>
        /// 解压缩zip文件
        /// </summary>
        public static void decompressZip(string input, string output)
        {
            if (File.Exists(input))
            {
                DirectoryInfo dir = new DirectoryInfo(output);
                if (Directory.Exists(output))
                    dir.Delete(true);
                ZipFile.ExtractToDirectory(input, output);
            }
        }

        /// <summary>
        /// 压缩zip文件
        /// </summary>
        public static void compressZip(string input, string output)
        {
            if (Directory.Exists(input))
            {
                if (File.Exists(output))
                    File.Delete(output);
                
                ZipFile.CreateFromDirectory(input, output);
            }
        }
    }
}