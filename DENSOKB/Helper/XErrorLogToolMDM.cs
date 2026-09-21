using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;

namespace HelpLog.CommomMethod
{
    /// <summary>
    /// XErrorLogTool 类
    /// 名称: 
    /// 功能: 
    /// 时间: 9/7/2011 10:40:16 AM
    /// 作者: wss
    /// </summary>
    public class XErrorLogToolMDM
    {
        #region 构造和析构

        /// <summary>
        /// 构造
        /// </summary>
        public XErrorLogToolMDM()
        {
            //
            // TODO: 在此处添加构造函数逻辑
            //
        }

        #endregion // 构造和析构

        #region 公有方法
        public static void WriteLog(string strLog)
        {
            string sFilePath = AppDomain.CurrentDomain.BaseDirectory + "\\ChyLog\\" + DateTime.Now.ToString("yyyyMMdd");
            string sFileName = DateTime.Now.ToString("yyyyMMddHHmm") + ".log";
            sFileName = sFilePath + "\\" + sFileName; //文件的绝对路径
            if (!Directory.Exists(sFilePath))//验证路径是否存在
            {
                Directory.CreateDirectory(sFilePath);
                //不存在则创建
            }
            FileStream fs;
            StreamWriter sw;
            if (File.Exists(sFileName))
            //验证文件是否存在，有则追加，无则创建
            {
                fs = new FileStream(sFileName, FileMode.Append, FileAccess.Write);
            }
            else
            {
                fs = new FileStream(sFileName, FileMode.Create, FileAccess.Write);
            }
            sw = new StreamWriter(fs);
            sw.WriteLine("---------" + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + "---------\r\n" + strLog);
            sw.Close();
            fs.Close();
            DeleteLog();


        }
        /// <summary>
        /// 删除历史日志，如果需要可调用此方法
        /// </summary>
        public static void DeleteLog()
        {
            string sFilePath = AppDomain.CurrentDomain.BaseDirectory + "\\ChyLog";
            DirectoryInfo[] directories = new DirectoryInfo(sFilePath).GetDirectories();

            foreach (DirectoryInfo directory in directories)
            {
                // 尝试将文件夹名称解析为日期
                if (DateTime.TryParseExact(directory.Name, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime folderDate))
                {
                    // 如果文件夹日期小于今天，则删除该文件夹及其内容
                    if (folderDate < DateTime.Now- TimeSpan.FromDays(30))
                    {
                        try
                        {
                            directory.Delete(true); // true 表示递归删除文件夹及其内容
                            Console.WriteLine($"Deleted folder: {directory.FullName}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to delete folder {directory.FullName}: {ex.Message}");
                        }
                    }
                }
                // 如果文件夹名称不是有效的日期格式，则跳过（可以根据需要处理）
                else
                {
                    Console.WriteLine($"Skipped non-date folder: {directory.Name}");
                }
            }
        }
        public static void WriteLog(string exception, string logFileDirectory = "", string logFileName = "")
        {
            m_Exception = exception;
            m_logFileDirectory = logFileDirectory;
            m_logFileName = logFileName;
            Thread thread = new Thread(new ThreadStart(WriteLogByThread));
            thread.Start();
        }

        private static void WriteLogByThread()
        {
            try
            {
                if (m_logFileDirectory == string.Empty)
                {
                    m_logFileDirectory = AppDomain.CurrentDomain.BaseDirectory + "\\ErrorLog";
                }

                if (!Directory.Exists(m_logFileDirectory))
                {
                    Directory.CreateDirectory(m_logFileDirectory);
                }

                if (m_logFileName.Trim() == string.Empty)
                {
                    m_logFileName = DateTime.Now.Date.ToString("yyyy-MM-dd") + "_ErrorLog.txt";
                }
                FileMode fileMode = FileMode.CreateNew;
                string logFileFullName = m_logFileDirectory + "\\" + m_logFileName;
                if (File.Exists(logFileFullName))
                {
                    fileMode = FileMode.Append;
                }
                using (FileStream fs = new System.IO.FileStream(logFileFullName,
                        fileMode, FileAccess.Write))
                {

                    using (System.IO.StreamWriter w = new System.IO.StreamWriter(fs,

                         System.Text.Encoding.UTF8))
                    {
                        string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\r\n";
                        string seperator = "*********************************************\r\n";
                        w.WriteLine(seperator + currentTime + m_Exception + "\r\n" + seperator);
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        #endregion // 公有方法

        #region 保护方法
        #endregion // 保护方法

        #region 私有方法
        #endregion // 私有方法

        #region 属性及其私有变量

        private static string m_Exception = "";

        private static string m_logFileDirectory = "";

        private static string m_logFileName = "";

        #endregion // 属性及其私有变量

    }
}