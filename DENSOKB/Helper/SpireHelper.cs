using DENSOKB.Controllers;
using DENSOKB.Models;
using HelpLog.CommomMethod;
using iText.Forms.Form.Element;
using QRCoder;
using Spire.Pdf;
using Spire.Pdf.Graphics;
using Spire.Pdf.Texts;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;

namespace DENSOKB.Helper
{
    /// <summary>
    /// 曹海阳，毕兆轩
    /// 解决水印问题；经测试光有Spire.PDF破解包只能操作PDF，此项目不涉及到word转PDF，以后再研究
    /// Excel转PDF使用Spire.Xls;Spire.PDF包 必须使用此类， 很好用
    /// </summary>
    public class SpireHelper

        //new PublicHelper publicHelper
    {
        public static void ActivateMemoryPatching()
        {
            Assembly[] arr = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in arr)
            {
                if (assembly.FullName.StartsWith("Spire.License"))
                    ActivateForAssembly(assembly);
            }
            AppDomain.CurrentDomain.AssemblyLoad += new AssemblyLoadEventHandler(ActivateOnLoad);
        }

        private static void ActivateOnLoad(object sender, AssemblyLoadEventArgs e)
        {
            string Name = e.LoadedAssembly.FullName;
            if (Name.IndexOf("Spire.License") != -1)
                ActivateForAssembly(e.LoadedAssembly);
        }

        private static void ActivateForAssembly(Assembly assembly)
        {
            bool isFound = false;

            Type[] arrType = null;
            try
            {
                arrType = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException err)
            {
                arrType = err.Types;
            }

            MethodInfo miLicensed = typeof(SpireHelper).GetMethod("InvokeMe", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo miEvaluation = null;

            foreach (Type type in arrType)
            {
                if (isFound) break;

                if (type == null) continue;

                MethodInfo[] arrMInfo = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Static);

                foreach (MethodInfo info in arrMInfo)
                {
                    if (isFound) break;

                    try
                    {
                        string strMethod = info.ToString();
                        if ((strMethod.StartsWith("Byte[]")) && (strMethod.IndexOf("(System.String)") > 0))
                        {
                            miEvaluation = info;
                            MemoryPatching(miEvaluation, miLicensed);
                            isFound = true;
                            break;
                        }
                    }
                    catch
                    {
                        throw new InvalidOperationException("MemoryPatching for " + assembly.FullName + " failed !");
                    }
                }
            }

            string LData = "PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0idXRmLTgiIHN0YW5kYWxvbmU9InllcyI/Pgo8TGljZW5zZSBLZXk9ImNyOERaN1hKMkR5MUs2UUJBTkRPSVRLdlpjTzZkelVod2lsSHBnVlluQ3k0cXlHV2V6TFZubFJGeFAxNU1mSWZnUmdNWm1XaEdOQWRFNFRqZWZnQ1ovbFR2b1BkSXRIbDZXdDVBNWk1TVhFbnFkQnVPMUthRnovRFFzYUdWTGhzdjlySG1ybnRxSElFRGxJeGRxYUpNcGtLb0Frd1A3d1N6T01KMVkrbUNmVTVVRmV6REwvTjd1enJ4M0Y0d2I1SGErd0E2VFQ5VFJ3SzAzejlFS01aRmwzU1lSL3o0YVU3TE0wZFNYWTlqU0ZKZ2dqZlZzRFVLaUJyVm5td1ljaXVyOUVrYmw5Q3RaWTAzdG1yZm01QlplKzZnaHRFTm4wb2gzMzh0WlJleWpjcjc0QWs3MWhnWWtuTE9CQzE1VllmalhzcXVBVW13MlI2TWNWMlBPT2JyY1RSYlhBZ3pvUWJPeWQ4U2JFWmN3aE43NktQd1dzUVFTMUowdGlZSFVLeE9tMnQ0ZkJWMGhQVmhhOUI4Y0swNHFKUVp0MDBaMWNKRGEwd2I4VWx6RWs5QkhVVzJlbk9mVDE0UnlIQ2krWUdlbVBLY2RDUXJoMXpyWVRGN0ltb0x4N3h1NGV2RFRZc2xzV0JrbFFJb3g4NnJWckVVa1N0dXErQUNTWS9xVTM5L1Zhd3Y5S0FmUjVUZUVicGt3RGhTYjBOQkFqVDhBeXRsRFZkR2ZpZzBxS0czVllpVHBYRnc1cHRMVmgrYmtkK2RnN3Z4dHZyNDVaVVdKZXlyekdOR0g3YUZZZDZwLzJNRy9YSlRsR3ovU05RbzJDUExraU83SlhuOU5HZXhaN3BIbTBkZ3pNWmJHRVhxVmR2bG04MTJhL1hMMVNxeEdVWStvNVpsVUM3WTV4Z2dhRCtGZVA5enpoeUpxSUVwcDk3My9ScTRteG1wQWZMcVNzTzJSeHlTcStpdjFDc3AwQ3JvMDc4OEhybDFteWt4dVQweWRSWVpDNkRTeDhNMi9MWTNkOXNud3U3NkFmYjVDOVF1ZE9Zc0wzREh2aGZncmNVSWUvcUhmVFo5QWF6Y3pUanlyM2RPQkFjczBLZk12Y0xVUzRSeHZDdW1NNDVyNDJnMXJ3UGluN2JBcmYvZnNMTzZtS3g0WWRoSURNWlF6V3RjbkhFSTF5TXJ6aU9pdXhMdE8xalRBV25uU2VLVDJ0cXI3Tm42Qmg5TURHNjZZK2lJaW4xV05TUCtMdDFYdXRkajNKTyt4b1FNUVB5ZFpoZkJYZXpVMEhRMnd0eEdwdzRNczRTMTVJbFg1TEdXR3dXeUdYTWNjVWd3b1RDeFRGYmgyZFo0Vkg3OVZHTEVFR1JRWEZrNTRBdlFLdFBpdUcxY0w4RFo3WEoyRHkxSzZUUWVORE9YeFl2NFNveitCMHNBS0VwTVRrNCtTYWpYNksrSjlUOFhZVXRTOE8wWWZGUFZqZkhIYTZORWQyODdVcUlqMnJnQlF1bjVDV3hCczFHUm5BYmd1Z3MyL2ZQakcwZmdQemdSYzR5Q3ZObFg4V2pKUnloc3U5VFRKTjd1R3NOdnprU2IyZWlyQmhEaG1vQ0Jqa0wyYnMzT3I2d2pnNnBUNVpmNGhEdDF0STBJNXo1aytxQXVSZnRhd1lmamhXYmpMS0xKOTlUVk1kRDZaTCtTenNtQkNWN05lYm96V0RUTWgrRnJPT292R09ZbUk1bWp4Smd1MVRXNnI1V0JUK2oxSjBFNmJIb2tEMWo0Wm1DWUQreVBPUW1PMm1yUTNGdC9jVmZwQWlJdzliRkgwZ1FIbXQ4QnNuZnQ2MVV3c1h6cSs2akNvY1hOOUMvRXZPblhTczZuVlNGSkVBL3l1QmNIazZxOWdqanBnRG1NTEcrNlpxR1VjRWMzZEp2THpuK3pNT0p3TDI4WUQxN3BLSXBUNnd6WFBFVFJwWS9qNHhoMkQvaFhJRVNHcTk1eTVmZE9MNmx1QT09IiBWZXJzaW9uPSI5LjkiPgogICAgPFR5cGU+UnVudGltZTwvVHlwZT4KICAgIDxVc2VybmFtZT5Vc2VyTmFtZTwvVXNlcm5hbWU+CiAgICA8RW1haWw+ZU1haWxAaG9zdC5jb208L0VtYWlsPgogICAgPE9yZ2FuaXphdGlvbj5Pcmdhbml6YXRpb248L09yZ2FuaXphdGlvbj4KICAgIDxMaWNlbnNlZERhdGU+MjAxNi0wMS0wMVQxMjowMDowMFo8L0xpY2Vuc2VkRGF0ZT4KICAgIDxFeHBpcmVkRGF0ZT4yMDk5LTEyLTMxVDEyOjAwOjAwWjwvRXhwaXJlZERhdGU+CiAgICA8UHJvZHVjdHM+CiAgICAgICAgPFByb2R1Y3Q+CiAgICAgICAgICAgIDxOYW1lPlNwaXJlLk9mZmljZSBQbGF0aW51bTwvTmFtZT4KICAgICAgICAgICAgPFZlcnNpb24+OS45OTwvVmVyc2lvbj4KICAgICAgICAgICAgPFN1YnNjcmlwdGlvbj4KICAgICAgICAgICAgICAgIDxOdW1iZXJPZlBlcm1pdHRlZERldmVsb3Blcj45OTk5OTwvTnVtYmVyT2ZQZXJtaXR0ZWREZXZlbG9wZXI+CiAgICAgICAgICAgICAgICA8TnVtYmVyT2ZQZXJtaXR0ZWRTaXRlPjk5OTk5PC9OdW1iZXJPZlBlcm1pdHRlZFNpdGU+CiAgICAgICAgICAgIDwvU3Vic2NyaXB0aW9uPgogICAgICAgIDwvUHJvZHVjdD4KICAgIDwvUHJvZHVjdHM+CiAgICA8SXNzdWVyPgogICAgICAgIDxOYW1lPklzc3VlcjwvTmFtZT4KICAgICAgICA8RW1haWw+aXNzdWVyQGlzc3Vlci5jb208L0VtYWlsPgogICAgICAgIDxVcmw+aHR0cDovL3d3dy5pc3N1ZXIuY29tPC9Vcmw+CiAgICA8L0lzc3Vlcj4KPC9MaWNlbnNlPg==";

            //Spire.License.LicenseProvider.SetLicenseKey(LData);
            Type type2 = assembly.GetType("Spire.License.LicenseProvider");
            MethodInfo mi = type2.GetMethod("SetLicenseKey", new Type[] { typeof(String) });
            mi.Invoke(Activator.CreateInstance(type2, null), new String[] { LData });
        }


        private static byte[] InvokeMe(String string_0)
        {
            return Convert.FromBase64String("PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0idXRmLTgiIHN0YW5kYWxvbmU9InllcyI/Pgo8TGljZW5zZSBLZXk9IngiIFZlcnNpb249IjkuOSI+CiAgICA8VHlwZT5SdW50aW1lPC9UeXBlPgogICAgPFVzZXJuYW1lPlVzZXJOYW1lPC9Vc2VybmFtZT4KICAgIDxFbWFpbD5lTWFpbEBob3N0LmNvbTwvRW1haWw+CiAgICA8T3JnYW5pemF0aW9uPk9yZ2FuaXphdGlvbjwvT3JnYW5pemF0aW9uPgogICAgPExpY2Vuc2VkRGF0ZT4yMDE2LTAxLTAxVDEyOjAwOjAwWjwvTGljZW5zZWREYXRlPgogICAgPEV4cGlyZWREYXRlPjIwOTktMTItMzFUMTI6MDA6MDBaPC9FeHBpcmVkRGF0ZT4KICAgIDxQcm9kdWN0cz4KICAgICAgICA8UHJvZHVjdD4KICAgICAgICAgICAgPE5hbWU+U3BpcmUuT2ZmaWNlIFBsYXRpbnVtPC9OYW1lPgogICAgICAgICAgICA8VmVyc2lvbj45Ljk5PC9WZXJzaW9uPgogICAgICAgICAgICA8U3Vic2NyaXB0aW9uPgogICAgICAgICAgICAgICAgPE51bWJlck9mUGVybWl0dGVkRGV2ZWxvcGVyPjk5OTk5PC9OdW1iZXJPZlBlcm1pdHRlZERldmVsb3Blcj4KICAgICAgICAgICAgICAgIDxOdW1iZXJPZlBlcm1pdHRlZFNpdGU+OTk5OTk8L051bWJlck9mUGVybWl0dGVkU2l0ZT4KICAgICAgICAgICAgPC9TdWJzY3JpcHRpb24+CiAgICAgICAgPC9Qcm9kdWN0PgogICAgPC9Qcm9kdWN0cz4KICAgIDxJc3N1ZXI+CiAgICAgICAgPE5hbWU+SXNzdWVyPC9OYW1lPgogICAgICAgIDxFbWFpbD5pc3N1ZXJAaXNzdWVyLmNvbTwvRW1haWw+CiAgICAgICAgPFVybD5odHRwOi8vd3d3Lmlzc3Vlci5jb208L1VybD4KICAgIDwvSXNzdWVyPgo8L0xpY2Vuc2U+Cg==");
        }

        private static unsafe void MemoryPatching(MethodBase miEvaluation, MethodBase miLicensed)
        {
            IntPtr IntPtrEval = GetMemoryAddress(miEvaluation);
            IntPtr IntPtrLicensed = GetMemoryAddress(miLicensed);

            if (IntPtr.Size == 8)
                *((long*)IntPtrEval.ToPointer()) = *((long*)IntPtrLicensed.ToPointer());
            else
                *((int*)IntPtrEval.ToPointer()) = *((int*)IntPtrLicensed.ToPointer());
        }

        private static unsafe IntPtr GetMemoryAddress(MethodBase mb)
        {
            RuntimeHelpers.PrepareMethod(mb.MethodHandle);

            if ((Environment.Version.Major >= 4) || ((Environment.Version.Major == 2) && (Environment.Version.MinorRevision >= 3053)))
            {
                return new IntPtr(((int*)mb.MethodHandle.Value.ToPointer() + 2));
            }

            UInt64* location = (UInt64*)(mb.MethodHandle.Value.ToPointer());
            int index = (int)(((*location) >> 32) & 0xFF);
            if (IntPtr.Size == 8)
            {
                ulong* classStart = (ulong*)mb.DeclaringType.TypeHandle.Value.ToPointer();
                ulong* address = classStart + index + 10;
                return new IntPtr(address);
            }
            else
            {
                uint* classStart = (uint*)mb.DeclaringType.TypeHandle.Value.ToPointer();
                uint* address = classStart + index + 10;
                return new IntPtr(address);
            }
        }

        public static void SaveFirstPage(string sourcePdf, string targetPdf)
        {
            // 加载整个 PDF
            PdfDocument doc = new PdfDocument();
            doc.LoadFromFile(sourcePdf);

            // 创建一个新的 PDF 用来存第一页
            PdfDocument newDoc = new PdfDocument();

            // 将第 0 页（第一页）复制到新文档
            newDoc.InsertPage(doc, 0);

            // 保存新的单页 PDF
            newDoc.SaveToFile(targetPdf);

            // 关闭文档
            newDoc.Close();
            doc.Close();
        }


    public static void PdfSplitEndPage(
    string inputFile,
    string outputFolder,
    int StartPage,
    int EndPage,
    int VerticalCount,
    int HorizontalCount,
    SpireSplit.EnumNameRule nameRule,
    Rectangle[] NameRectangle,
    Rectangle DataRectangle,
    bool bsortno = false,
    bool getLimit = false,
    bool rename = false)
        {
            try
            {
                XErrorLogToolMDM.WriteLog("PDF拆分开始");
                //需要重写收容数的话，先去原单位表
                Dictionary<string, int> dict_originalunit = new Dictionary<string, int>();


                //原始文档
                Spire.Pdf.PdfDocument pdf = new Spire.Pdf.PdfDocument();
                pdf.LoadFromFile(inputFile);
                XErrorLogToolMDM.WriteLog("原始文档" + inputFile);
                //通过转向情况获得实际宽高
                bool isLandscape = pdf.Pages[StartPage].Rotation == PdfPageRotateAngle.RotateAngle90 ||
                      pdf.Pages[StartPage].Rotation == PdfPageRotateAngle.RotateAngle270;
                float pdfWidth = isLandscape ? pdf.Pages[StartPage].Size.Height : pdf.Pages[StartPage].Size.Width;
                float pdfHeight = isLandscape ? pdf.Pages[StartPage].Size.Width : pdf.Pages[StartPage].Size.Height;

                //创建新PDF文档
                Spire.Pdf.PdfDocument newPdf = new Spire.Pdf.PdfDocument();

                //移除新文档的页边距
                newPdf.PageSettings.Margins.All = 0;

                //按照拆分横纵数，计算新页面尺寸
                newPdf.PageSettings.Width = pdfWidth / HorizontalCount;
                newPdf.PageSettings.Height = pdfHeight / VerticalCount;

                if (HorizontalCount == 1 && VerticalCount == 3)
                {
                    newPdf.PageSettings.Width = pdfWidth / HorizontalCount;
                    newPdf.PageSettings.Height = 254;
                }

                //原始文档有效范围，传入参数
                if (DataRectangle == new Rectangle())
                    DataRectangle = new Rectangle(0, 0, (int)pdfWidth, (int)pdfHeight);
                //单页有效尺寸，计算出
                SizeF youxiaoSize = new SizeF(DataRectangle.Width / HorizontalCount, DataRectangle.Height / VerticalCount);
                XErrorLogToolMDM.WriteLog("新文档");
                //获取第一页
                for (int i = StartPage; i < EndPage+1; i++)
                {
                    PdfPageBase page = pdf.Pages[i];

                    PdfTemplate temp = page.CreateTemplate();

                    for (int y = 0; y < VerticalCount; y++)
                    {
                        for (int x = 0; x < HorizontalCount; x++)
                        {
                            //创建一个页面
                            PdfPageBase newPage = newPdf.Pages.Add(
                                newPdf.PageSettings.Size, //size就是拆分后的纸张大小
                                new PdfMargins((newPdf.PageSettings.Width - youxiaoSize.Width) / 2, (newPdf.PageSettings.Height - youxiaoSize.Height) / 2));
                            //边距是纸张大小减去有效范围/2


                            if (HorizontalCount == 1 && VerticalCount == 3)
                            {
                                newPage.Canvas.DrawTemplate(temp,
                                    new PointF(-DataRectangle.X - youxiaoSize.Width * x, -DataRectangle.Y - youxiaoSize.Height * y + 5.4f),
                                    new SizeF(pdfWidth, pdfHeight));
                            }
                            else
                            {
                                newPage.Canvas.DrawTemplate(temp,
                                    new PointF(-DataRectangle.X - youxiaoSize.Width * x, -DataRectangle.Y - youxiaoSize.Height * y),
                                    new SizeF(pdfWidth, pdfHeight));
                            }
                        }
                    }
                }

                if (!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }
                XErrorLogToolMDM.WriteLog("拆分保存");
                //拆分保存
                for (int i = 0; i < newPdf.Pages.Count; i++)
                {
                    string str = string.Empty;

                    for (int j = 0; j < NameRectangle.Length; j++)
                    {
                        RectangleF extractRegion = NameRectangle[j];
                        //new RectangleF(newPdf2.Pages[i].Size.Width-200, newPdf2.Pages[i].Size.Height - 30,200 , 30); // x, y, width, height
                        //extractRegion = new RectangleF(75, 240, 210, 35);
                        PdfTextExtractor extractor = new PdfTextExtractor(newPdf.Pages[i]);
                        PdfTextExtractOptions options = new PdfTextExtractOptions();
                        options.ExtractArea = extractRegion;

                        string str_temp = extractor.ExtractText(options);

                        str += str_temp.Replace("\r\n", "").Replace(" ", "");
                    }

                    //检测空白页     
                    if (str == string.Empty && nameRule == SpireSplit.EnumNameRule.Customize)
                    {
                        continue;
                    }

                    Spire.Pdf.PdfDocument splitPdf = new Spire.Pdf.PdfDocument();

                    splitPdf.InsertPage(newPdf, i);

                    string fileName = string.Empty;
                    string outputFile = string.Empty;
                    //命名
                    if (nameRule == SpireSplit.EnumNameRule.Customize)
                    {
                        fileName = str.Replace("\r\n", "").Replace(" ", "").Replace("/", "");
                        if (bsortno)
                        {
                            string nameindex = "0000" + (i).ToString();
                            fileName += $"_{nameindex.Substring(nameindex.Length - 4)}";
                        }
                        outputFile = Path.Combine(outputFolder, $"{fileName}.pdf");
                    }
                    else if (nameRule == SpireSplit.EnumNameRule.Auto)
                    {
                        fileName = Path.GetFileNameWithoutExtension(inputFile);
                        string nameindex = "0000" + (i + 1).ToString();
                        outputFile = Path.Combine(outputFolder, $"{fileName}_Page_{nameindex.Substring(nameindex.Length - 4)}.pdf");
                    }
                    else if (nameRule == SpireSplit.EnumNameRule.CustomizeAuto)
                    {
                        int n = 0;
                        fileName = str.Replace("\r\n", "").Replace(" ", "");
                        if (string.IsNullOrEmpty(fileName))
                        {
                            continue;
                        }

                        do
                        {
                            if (n == 0)
                            {
                                outputFile = Path.Combine(outputFolder, $"{fileName}.pdf");
                                //XErrorLogToolMDM.WriteLog($"PDF拆分文件名：{outputFile}");
                            }
                            else
                            {
                                outputFile = Path.Combine(outputFolder, $"{fileName}_Page_{i + n}.pdf");
                                //XErrorLogToolMDM.WriteLog($"PDF拆分文件名：{outputFile}");
                            }
                            n++;
                        } while (File.Exists(outputFile));
                    }

                    //需要重写收容数
                    if (getLimit)
                    {
                        string limit = string.Empty;
                        string custCode = string.Empty;

                        RectangleF extractRegion = new RectangleF(300, 105, 60, 18);
                        PdfTextExtractor extractor = new PdfTextExtractor(splitPdf.Pages[0]);
                        PdfTextExtractOptions options = new PdfTextExtractOptions();
                        options.ExtractArea = extractRegion;

                        string str_temp = extractor.ExtractText(options);

                        limit = str_temp.Replace("\r\n", "").Replace(" ", "");

                        if (limit == "0")
                        {
                            extractRegion = new RectangleF(420, 90, 120, 30);
                            extractor = new PdfTextExtractor(splitPdf.Pages[0]);
                            options = new PdfTextExtractOptions();
                            options.ExtractArea = extractRegion;
                            str_temp = extractor.ExtractText(options);
                            custCode = str_temp.Replace("\r\n", "").Replace(" ", "");

                            if (dict_originalunit.ContainsKey(custCode))
                            {
                                limit = dict_originalunit[custCode].ToString();

                                // 获取特定的页面
                                PdfPageBase pageR = splitPdf.Pages[0];

                                // 根据页面创建 PdfTextReplacer 对象
                                PdfTextReplacer textReplacer = new PdfTextReplacer(pageR);

                                // 创建PdfTextReplaceOptions 对象
                                PdfTextReplaceOptions textReplaceOptions = new PdfTextReplaceOptions();
                                textReplaceOptions.SetReplacementArea(new RectangleF(300, 105, 60, 18));

                                // 指定文本替换的选项
                                textReplaceOptions.ReplaceType = PdfTextReplaceOptions.ReplaceActionType.WholeWord;
                                textReplaceOptions.ReplaceType = PdfTextReplaceOptions.ReplaceActionType.AutofitWidth;
                                textReplacer.Options = textReplaceOptions;

                                // 将所有目标文本的出现替换为新文本
                                textReplacer.ReplaceAllText("0", limit);
                            }
                        }
                    }

                    if (rename)
                    {
                        outputFile = outputFile.Replace("-", "");
                    }

                    // 保存为单独文件
                    splitPdf.SaveToFile(outputFile, FileFormat.PDF);

                    // 关闭当前文档
                    splitPdf.Close();

                }
                newPdf.Close();
                pdf.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }


        

        /// <summary>
        /// PDF拆分
        /// </summary>
        /// <param name="inputFile">输入原始PDF文件</param>
        /// <param name="outputFolder">输出路径</param>
        /// <param name="StartPage">起始页，如果不确定，调用起始页查询函数获取</param>
        /// <param name="VerticalCount">垂直分几页</param>
        /// <param name="HorizontalCount">水平分几页</param>
        /// <param name="nameRule">命名规则</param>
        /// <param name="NameRectangle">命名取值区域，如果是自定义命名，必须有值</param>
        /// <param name="DataRectangle">原始PDF有效数据范围，如果全页等分，传入new Rectangle()即可</param>
        public static void PdfSplit(
            string inputFile,
            string outputFolder,
            int StartPage,
            int VerticalCount,
            int HorizontalCount,
            SpireSplit.EnumNameRule nameRule,
            Rectangle[] NameRectangle,
            Rectangle DataRectangle,
            bool bsortno = false,
            bool getLimit = false,
            bool rename = false)
        {
            try
            {
                string LicenseCategory = ConfigurationManager.AppSettings["LicenseCategory"].ToString();
                XErrorLogToolMDM.WriteLog("PDF拆分开始");
                //需要重写收容数的话，先去原单位表
                Dictionary<string, int> dict_originalunit = new Dictionary<string, int>();
                if(getLimit)
                {
                    //目前只有FTET需要
                    string sql = "select DENSO_NUM,CUST_NUM,custnum,box_cap from originalunit where cust_name = 'FTET'";
                    DataTable dt = DataHelper.Query(sql);

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        if(!dict_originalunit.ContainsKey(dt.Rows[i]["cust_num"].ToString()))
                        {
                            dict_originalunit.Add(dt.Rows[i]["cust_num"].ToString(),
                                Convert.ToInt32(dt.Rows[i]["box_cap"].ToString()));
                        }
                    }
                }

                //原始文档
                Spire.Pdf.PdfDocument pdf = new Spire.Pdf.PdfDocument();
                pdf.LoadFromFile(inputFile);
                XErrorLogToolMDM.WriteLog("原始文档" + inputFile);
                //通过转向情况获得实际宽高
                bool isLandscape = pdf.Pages[StartPage].Rotation == PdfPageRotateAngle.RotateAngle90 ||
                      pdf.Pages[StartPage].Rotation == PdfPageRotateAngle.RotateAngle270;
                float pdfWidth = isLandscape ? pdf.Pages[StartPage].Size.Height : pdf.Pages[StartPage].Size.Width;
                float pdfHeight = isLandscape ? pdf.Pages[StartPage].Size.Width : pdf.Pages[StartPage].Size.Height;

                //创建新PDF文档
                Spire.Pdf.PdfDocument newPdf = new Spire.Pdf.PdfDocument();

                //移除新文档的页边距
                newPdf.PageSettings.Margins.All = 0;

                //按照拆分横纵数，计算新页面尺寸
                newPdf.PageSettings.Width = pdfWidth / HorizontalCount;
                newPdf.PageSettings.Height = pdfHeight / VerticalCount;

                if(HorizontalCount ==1 && VerticalCount == 3)
                {
                    newPdf.PageSettings.Width = pdfWidth / HorizontalCount;
                    newPdf.PageSettings.Height = 254;
                }

                //原始文档有效范围，传入参数
                if (DataRectangle == new Rectangle())
                    DataRectangle = new Rectangle(0, 0, (int)pdfWidth, (int)pdfHeight);
                //单页有效尺寸，计算出
                SizeF youxiaoSize = new SizeF(DataRectangle.Width / HorizontalCount, DataRectangle.Height / VerticalCount);
                XErrorLogToolMDM.WriteLog("新文档");
                //获取第一页
                for (int i = StartPage; i < pdf.Pages.Count; i++)
                {
                    PdfPageBase page = pdf.Pages[i];

                    PdfTemplate temp = page.CreateTemplate();

                    for (int y = 0; y < VerticalCount; y++)
                    {
                        for (int x = 0; x < HorizontalCount; x++)
                        {
                            //创建一个页面
                            PdfPageBase newPage = newPdf.Pages.Add(
                                newPdf.PageSettings.Size, //size就是拆分后的纸张大小
                                new PdfMargins((newPdf.PageSettings.Width - youxiaoSize.Width) / 2, (newPdf.PageSettings.Height - youxiaoSize.Height) / 2));
                            //边距是纸张大小减去有效范围/2


                            if (HorizontalCount == 1 && VerticalCount == 3)
                            {
                                newPage.Canvas.DrawTemplate(temp,
                                    new PointF(-DataRectangle.X - youxiaoSize.Width * x, -DataRectangle.Y - youxiaoSize.Height * y + 5.4f),
                                    new SizeF(pdfWidth, pdfHeight));
                            }
                            else
                            {
                                newPage.Canvas.DrawTemplate(temp,
                                    new PointF(-DataRectangle.X - youxiaoSize.Width * x, -DataRectangle.Y - youxiaoSize.Height * y),
                                    new SizeF(pdfWidth, pdfHeight));
                            }                            
                        }
                    }
                }

                if (!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }
                XErrorLogToolMDM.WriteLog("拆分保存" );
                //拆分保存
                for (int i = 0; i < newPdf.Pages.Count; i++)
                {
                    string str = string.Empty;

                    for (int j = 0; j < NameRectangle.Length; j++)
                    {
                        RectangleF extractRegion = NameRectangle[j];
                        //new RectangleF(newPdf2.Pages[i].Size.Width-200, newPdf2.Pages[i].Size.Height - 30,200 , 30); // x, y, width, height
                        //extractRegion = new RectangleF(75, 240, 210, 35);
                        PdfTextExtractor extractor = new PdfTextExtractor(newPdf.Pages[i]);
                        PdfTextExtractOptions options = new PdfTextExtractOptions();
                        options.ExtractArea = extractRegion;

                        //PdfTextExtractor extractor = new PdfTextExtractor(newPdf.Pages[i]);
                        //PdfTextExtractOptions options = new PdfTextExtractOptions( );


                        string str_temp = extractor.ExtractText(options);

                        //if (LicenseCategory == "debug") {
                        //    str_temp = str_temp.Substring(str_temp.IndexOf("\r\n"));
                        //}
                        
                        str += str_temp.Replace("\r\n", "").Replace(" ", "");
                    }

                    //检测空白页     
                    if (str == string.Empty && nameRule == SpireSplit.EnumNameRule.Customize)
                    {
                        continue;
                    }

                    Spire.Pdf.PdfDocument splitPdf = new Spire.Pdf.PdfDocument();

                    splitPdf.InsertPage(newPdf, i);

                    string fileName = string.Empty;
                    string outputFile = string.Empty;
                    //命名
                    if (nameRule == SpireSplit.EnumNameRule.Customize)
                    {
                        fileName = str.Replace("\r\n", "").Replace(" ", "").Replace("/","");
                        if(bsortno)
                        {
                            string nameindex = "0000" + (i).ToString();
                            fileName += $"_{nameindex.Substring(nameindex.Length - 4)}";
                        }
                         outputFile = Path.Combine(outputFolder, $"{fileName}.pdf");
                    }
                    else if (nameRule == SpireSplit.EnumNameRule.Auto)
                    {
                        fileName = Path.GetFileNameWithoutExtension(inputFile);
                        string nameindex = "0000" + (i + 1).ToString();
                        outputFile = Path.Combine(outputFolder, $"{fileName}_Page_{nameindex.Substring(nameindex.Length-4)}.pdf");
                    }
                    else if (nameRule == SpireSplit.EnumNameRule.CustomizeAuto)
                    {
                        int n = 0;
                        fileName = str.Replace("\r\n", "").Replace(" ", "");
                        if (string.IsNullOrEmpty(fileName))
                        {
                            continue;
                        }

                        do
                        {
                            if (n == 0)
                            {
                                outputFile = Path.Combine(outputFolder, $"{fileName}.pdf");
                                //XErrorLogToolMDM.WriteLog($"PDF拆分文件名：{outputFile}");
                            }
                            else
                            {
                                outputFile = Path.Combine(outputFolder, $"{fileName}_Page_{i + n}.pdf");
                                //XErrorLogToolMDM.WriteLog($"PDF拆分文件名：{outputFile}");
                            }
                            n++;
                        } while (File.Exists(outputFile));
                    }

                    //需要重写收容数
                    if(getLimit)
                    {
                        string limit = string.Empty;
                        string custCode = string.Empty;

                        RectangleF extractRegion = new RectangleF(300,105 ,60,18);
                        PdfTextExtractor extractor = new PdfTextExtractor(splitPdf.Pages[0]);
                        PdfTextExtractOptions options = new PdfTextExtractOptions();
                        options.ExtractArea = extractRegion;

                        string str_temp = extractor.ExtractText(options);

                        limit = str_temp.Replace("\r\n", "").Replace(" ", "");

                        if(limit == "0")
                        {
                            extractRegion = new RectangleF(420, 90, 120, 30); 
                            extractor = new PdfTextExtractor(splitPdf.Pages[0]);
                            options = new PdfTextExtractOptions();
                            options.ExtractArea = extractRegion;
                            str_temp = extractor.ExtractText(options);
                            custCode = str_temp.Replace("\r\n", "").Replace(" ", "");

                            if(dict_originalunit.ContainsKey(custCode))
                            {
                                limit = dict_originalunit[custCode].ToString();

                                // 获取特定的页面
                                PdfPageBase pageR = splitPdf.Pages[0];

                                // 根据页面创建 PdfTextReplacer 对象
                                PdfTextReplacer textReplacer = new PdfTextReplacer(pageR);

                                // 创建PdfTextReplaceOptions 对象
                                PdfTextReplaceOptions textReplaceOptions = new PdfTextReplaceOptions();
                                textReplaceOptions.SetReplacementArea(new RectangleF(300, 105, 60, 18));

                                // 指定文本替换的选项
                                textReplaceOptions.ReplaceType = PdfTextReplaceOptions.ReplaceActionType.WholeWord;
                                textReplaceOptions.ReplaceType = PdfTextReplaceOptions.ReplaceActionType.AutofitWidth;
                                textReplacer.Options = textReplaceOptions;

                                // 将所有目标文本的出现替换为新文本
                                textReplacer.ReplaceAllText("0", limit);
                            }
                        }
                    }

                    if (rename) {
                        outputFile = outputFile.Replace("-", "");
                    }

                    // 保存为单独文件
                    splitPdf.SaveToFile(outputFile, FileFormat.PDF);

                    // 关闭当前文档
                    splitPdf.Close();

                }
                newPdf.Close();
                pdf.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        /// PDF拆分
        /// </summary>
        /// <param name="inputFile">输入原始PDF文件</param>
        /// <param name="outputFolder">输出路径</param>
        /// <param name="StartPage">起始页，如果不确定，调用起始页查询函数获取</param>
        /// <param name="VerticalCount">垂直分几页</param>
        /// <param name="HorizontalCount">水平分几页</param>
        /// <param name="nameRule">命名规则</param>
        /// <param name="NameRectangle">命名取值区域，如果是自定义命名，必须有值</param>
        /// <param name="DataRectangle">原始PDF有效数据范围，如果全页等分，传入new Rectangle()即可</param>
        public static void PdfSplit(
            string inputFile,
            string outputFolder,
            int StartPage,
            int VerticalCount,
            int HorizontalCount,
            SpireSplit.EnumNameRule nameRule,
            Rectangle NameRectangle,
            Rectangle DataRectangle)
        {
            XErrorLogToolMDM.WriteLog("PDF拆分开始");
            List<Rectangle> NameRectangles = new List<Rectangle>();
            NameRectangles.Add(NameRectangle);
            PdfSplit(inputFile, outputFolder, StartPage, VerticalCount, HorizontalCount, nameRule, NameRectangles.ToArray(), DataRectangle);
        }




        /// <summary>
        /// PDF拆分
        /// </summary>
        /// <param name="inputFile">输入原始PDF文件</param>
        /// <param name="outputFolder">输出路径</param>
        /// <param name="StartPage">起始页，如果不确定，调用起始页查询函数获取</param>
        /// <param name="VerticalCount">垂直分几页</param>
        /// <param name="HorizontalCount">水平分几页</param>
        /// <param name="nameRule">命名规则</param>
        /// <param name="NameRectangle">命名取值区域，如果是自定义命名，必须有值</param>
        /// <param name="DataRectangle">原始PDF有效数据范围，如果全页等分，传入new Rectangle()即可</param>
        public static void PdfSplitEndPage(
            string inputFile,
            string outputFolder,
            int StartPage,
            int EndPage,
            int VerticalCount,
            int HorizontalCount,
            SpireSplit.EnumNameRule nameRule,
            Rectangle NameRectangle,
            Rectangle DataRectangle)
        {
            XErrorLogToolMDM.WriteLog("PDF拆分开始");
            List<Rectangle> NameRectangles = new List<Rectangle>();
            NameRectangles.Add(NameRectangle);
            PdfSplitEndPage(inputFile, outputFolder, StartPage, EndPage, VerticalCount, HorizontalCount, nameRule, NameRectangles.ToArray(), DataRectangle);
        }

        /// <summary>
        /// PDF看板合并成A4
        /// </summary>
        /// <param name="VerticalCount">纵向个数</param>
        /// <param name="HorizontalCount">横向个数</param>
        /// <param name="Files">输入文件数组</param>
        /// <param name="OutputFile">输出文件路径及文件名</param>
        public static void Merge(int VerticalCount, int HorizontalCount, FileInfo[] Files, string OutputFile, bool isA4 = false, float leftOffset = 0, float rightOffset = 0, bool vert = false)
        {
            Merge(VerticalCount, HorizontalCount, Files, OutputFile, new Point(0, 0), isA4,leftOffset, rightOffset, vert);
        }
        public static void Merge(int VerticalCount, int HorizontalCount, FileInfo[] Files, string OutputFile, Point Margin , bool isA4 = false)
        
        {
            if(isA4)
            {
                Files = SortA4(Files, VerticalCount,HorizontalCount);
            }

            PdfDocument mergedDoc = new PdfDocument();

            PdfDocument docBase = new PdfDocument();
            docBase.LoadFromFile(Files[0].FullName); // 上半部分文档

            SizeF sizeSingle = new SizeF(docBase.Pages[0].Size.Width, docBase.Pages[0].Size.Height);

            SizeF sizeBase = new SizeF(docBase.Pages[0].Size.Width * HorizontalCount, docBase.Pages[0].Size.Height * VerticalCount);

            if (isA4)
                sizeBase = new SizeF(595, 842);

            List<PdfDocument> tempDocments = new List<PdfDocument>();

            int CountPerPage = VerticalCount * HorizontalCount;

            int PageCount = Files.Length / CountPerPage;

            if (Files.Length % CountPerPage != 0) //最后一页不是满页，多加一页
                PageCount++;

            for (int i = 0; i < PageCount; i++)
            {
                // 创建一个新的A4文档

                PdfPageBase page = mergedDoc.Pages.Add(sizeBase, new PdfMargins(Margin.X, Margin.Y));
                for (int y = 0; y < VerticalCount; y++)
                {
                    for (int x = 0; x < HorizontalCount; x++)
                    {

                        int fileIndex = i * VerticalCount * HorizontalCount + y * HorizontalCount + x;

                        if (fileIndex >= Files.Length)
                        {
                            continue;
                        }

                        //空白页
                        if (Files[fileIndex] == null)
                            continue;
                        if (!Files[fileIndex].Exists)
                            continue;

                        PdfDocument doc = new PdfDocument();
                        doc.LoadFromFile(Files[fileIndex].FullName); // 上半部分文档

                        PdfTemplate template1 = doc.Pages[0].CreateTemplate();
                        page.Canvas.DrawTemplate(
                            template1,
                            new PointF(sizeSingle.Width * x, sizeSingle.Height * y),
                            sizeSingle);
                        tempDocments.Add(doc);
                        //doc.Close();
                    }
                }

                PdfTextExtractor extractor = new PdfTextExtractor(page);
                PdfTextExtractOptions options = new PdfTextExtractOptions();

                string str = extractor.ExtractText(options);

                if (str.Replace("\r\n", "").Replace(" ", "") == string.Empty)
                    mergedDoc.Pages.Remove(page);
            }
            //mergedDoc.Pages[0].ExtractText()
            // 保存合并后的文档
            mergedDoc.SaveToFile(OutputFile);
            mergedDoc.Close();

            for (int i = 0; i < tempDocments.Count; i++)
            {
                tempDocments[i].Close();
            }

        }

        //public static void Merge(int VerticalCount, int HorizontalCount, FileInfo[] Files, string OutputFile, bool isA4 = false,float leftOffset = 0)
        //{
        //    Merge(VerticalCount, HorizontalCount, Files, OutputFile, new Point(0, 0), isA4);
        //}

        public static void Merge(int VerticalCount, int HorizontalCount, FileInfo[] Files, string OutputFile, Point Margin, bool isA4 = false, float leftOffset = 0, float rightOffset = 0, bool vert = false)
        {
            if (isA4)
            {
                Files = SortA4(Files, VerticalCount, HorizontalCount);
            }

            PdfDocument mergedDoc = new PdfDocument();
            PdfDocument docBase = new PdfDocument();
            docBase.LoadFromFile(Files[0].FullName);

            SizeF sizeSingle = new SizeF(docBase.Pages[0].Size.Width, docBase.Pages[0].Size.Height);
            SizeF sizeBase = new SizeF(docBase.Pages[0].Size.Width * HorizontalCount, docBase.Pages[0].Size.Height * VerticalCount);

            if (isA4)
            {
                if (vert) { sizeBase = new SizeF(842,595); }
                else { sizeBase = new SizeF(595, 842); }
                    
            }
            else
            {
                // 当isA4为false时应用左侧偏移
                //sizeBase = new SizeF(sizeBase.Width - leftOffset, sizeBase.Height);
                sizeBase = new SizeF(sizeBase.Width, sizeBase.Height);
            }

            List<PdfDocument> tempDocments = new List<PdfDocument>();
            int CountPerPage = VerticalCount * HorizontalCount;
            int PageCount = Files.Length / CountPerPage;

            if (Files.Length % CountPerPage != 0)
                PageCount++;

            for (int i = 0; i < PageCount; i++)
            {
                PdfPageBase page = mergedDoc.Pages.Add(sizeBase, new PdfMargins(Margin.X, Margin.Y));

                for (int y = 0; y < VerticalCount; y++)
                {
                    for (int x = 0; x < HorizontalCount; x++)
                    {
                        int fileIndex = i * VerticalCount * HorizontalCount + y * HorizontalCount + x;
                        if (fileIndex >= Files.Length) continue;
                        if (Files[fileIndex] == null || !Files[fileIndex].Exists) continue;

                        PdfDocument doc = new PdfDocument();
                        doc.LoadFromFile(Files[fileIndex].FullName);
                        PdfTemplate template1 = doc.Pages[0].CreateTemplate();

                        float xPos = sizeSingle.Width * x;
                        //if (!isA4)
                        //{
                            //xPos -= leftOffset;
                            xPos = xPos - leftOffset + rightOffset;
                        //}

                        page.Canvas.DrawTemplate(
                            template1,
                            new PointF(xPos, sizeSingle.Height * y),
                            sizeSingle);

                        tempDocments.Add(doc);
                    }
                }

                // 检查空白页逻辑保持不变
                PdfTextExtractor extractor = new PdfTextExtractor(page);
                PdfTextExtractOptions options = new PdfTextExtractOptions();
                string str = extractor.ExtractText(options);

                if (str.Replace("\r\n", "").Replace(" ", "") == string.Empty)
                    mergedDoc.Pages.Remove(page);
            }

            mergedDoc.SaveToFile(OutputFile);
            mergedDoc.Close();

            foreach (var doc in tempDocments)
            {
                doc.Close();
            }
        }
        /// <summary>
        /// A4穿透排序
        /// </summary>
        /// <param name="files">按照业务要求排序后的files</param>
        /// <param name="VerticalCount">输出结果垂直个数</param>
        /// <param name="HorizontalCount">输出结果水平个数</param>
        /// <returns></returns>
        public static FileInfo[] SortA4(FileInfo[] files, int VerticalCount, int HorizontalCount)
        {
            List<FileInfo> SortedFiles = new List<FileInfo>();

            List<List<FileInfo>> pageFiles = new List<List<FileInfo>>();

            int CountPerPage = VerticalCount * HorizontalCount;

            int PageCount = files.Length / CountPerPage;

            if (files.Length % CountPerPage != 0) //最后一页不是满页，多加一页
                PageCount++;

            //有几页就创建几个页对象
            for (int i = 0; i < PageCount; i++)
            {
                pageFiles.Add(new List<FileInfo>());
            }

            //把所有文件一页一页的轮流放进去
            int pageIdx = 0;
            for (int i = 0; i < files.Length; i++)
            {
                pageFiles[pageIdx].Add(files[i]);
                if (++pageIdx >= PageCount)
                    pageIdx = 0;
            }

            //单页文件数不够的，补个空
            for (int i = 0; i < pageFiles.Count; i++)
            {
                while(pageFiles[i].Count< CountPerPage)
                {
                    pageFiles[i].Add(null);
                }
            }

            for (int i = 0; i < pageFiles.Count; i++)
            {
                for (int j = 0; j < pageFiles[i].Count; j++)
                {
                    SortedFiles.Add(pageFiles[i][j]);
                }
            }

            return SortedFiles.ToArray();
        }

        /// <summary>
        /// 使用关键字查找第一次出现的页索引
        /// </summary>
        /// <param name="inputFile">输入PDF文件</param>
        /// <param name="KeyWords">查询关键字</param>
        /// <returns>返回第一次出现关键字所在的页索引</returns>
        public static int GetStartPage(string inputFile,string KeyWords)
        {
            Spire.Pdf.PdfDocument pdf = new Spire.Pdf.PdfDocument();
            pdf.LoadFromFile(inputFile);

            for (int i = 0; i < pdf.Pages.Count; i++)
            {
                Spire.Pdf.Texts.PdfTextFinder textFinder = new PdfTextFinder(pdf.Pages[i]);
                List<PdfTextFragment> Finder = textFinder.Find(KeyWords);
                if (Finder.Count > 0)
                    return i;
            }

            return -1;
        }

        public static void SetBJboard(FileInfo[] fileInfos) {
            try
            {
                string fileName = "";

                foreach (var item in fileInfos)
                {
                    string Name = GetText(item.FullName, new Rectangle(400, 80, 160, 26), 0);
                    string boxCap = PulicHelper.GetBoxCap("FTET", Name, "BJ");

                    if (string.IsNullOrEmpty(boxCap))
                        continue; // 如果没有文本，跳过

                    // 加载 PDF 文件
                    PdfDocument doc = new PdfDocument();
                    doc.LoadFromFile(item.FullName);

                    // 获取第一页（或指定页）
                    PdfPageBase page = doc.Pages[0];

                    // 1. 先绘制白色背景（覆盖原有内容）
                    float x = 314;  // X 坐标（可以调整）
                    float y = 98;  // Y 坐标（可以调整）
                    float width = 30; // 背景宽度（根据文本长度调整）
                    float height = 15; // 背景高度

                    // 创建白色矩形
                    PdfSolidBrush whiteBrush = new PdfSolidBrush(Color.White);
                    page.Canvas.DrawRectangle(whiteBrush, x, y, width, height);

                    // 2. 写入文本
                    PdfFont font = new PdfFont(PdfFontFamily.Helvetica, 14); // 设置字体
                    PdfSolidBrush textBrush = new PdfSolidBrush(Color.Black); // 文本颜色

                    // 在相同位置写入文本
                    page.Canvas.DrawString(boxCap, font, textBrush, x, y);

                    // 保存修改后的 PDF（可以覆盖原文件或另存为新文件）
                    //string outputPath = Path.Combine(Path.GetDirectoryName(item.FullName), "Modified_" + item.Name);
                    doc.SaveToFile(item.FullName, FileFormat.PDF);
                    doc.Close();
                }
            }
            catch (Exception)
            {

                throw;
            }
        }


        public static List<FileInfo> SetBJboard(FileInfo[] fileInfos, List<FTETdetailEntity> detailList)
        {
            List<FileInfo> resultFiles = new List<FileInfo>();

            try
            {
                foreach (var file in fileInfos)
                {
                    // 找到对应的实体（用 bei 关联）
                    var matchEntity = detailList
                        .FirstOrDefault(e => file.Name.StartsWith(e.bei.ToString()));

                    if (matchEntity == null)
                    {
                        // 没找到就原样返回
                        resultFiles.Add(file);
                        continue;
                    }

                    int shou = matchEntity.shou??0;     // 收容数
                    int number = matchEntity.number??0; // 实际数量

                    if (shou <= 0 || number <= 0)
                    {
                        resultFiles.Add(file);
                        continue;
                    }

                    // 拆分数量
                    List<int> splitNumbers = new List<int>();
                    while (number > 0)
                    {
                        int fill = Math.Min(shou, number);
                        splitNumbers.Add(fill);
                        number -= fill;
                    }

                    // 每个数量生成一份文件
                    for (int i = 0; i < splitNumbers.Count; i++)
                    {
                        int fillNumber = splitNumbers[i];

                        // 加载 PDF
                        PdfDocument doc = new PdfDocument();
                        doc.LoadFromFile(file.FullName);

                        PdfPageBase page = doc.Pages[0];

                        // 覆盖原背景
                        float x = 314, y = 98, width = 30, height = 15;
                        page.Canvas.DrawRectangle(new PdfSolidBrush(Color.White), x, y, width, height);

                        // 写入数量
                        PdfFont font = new PdfFont(PdfFontFamily.Helvetica, 14);
                        page.Canvas.DrawString(fillNumber.ToString(), font, new PdfSolidBrush(Color.Black), x, y);

                        // 输出路径：避免覆盖，生成多个文件
                        string outputPath = Path.Combine(
                            file.DirectoryName,
                            Path.GetFileNameWithoutExtension(file.Name) + $"_{i + 1}.pdf"
                        );

                        doc.SaveToFile(outputPath, FileFormat.PDF);
                        doc.Close();

                        resultFiles.Add(new FileInfo(outputPath));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("生成看板时出错: " + ex.Message);
            }

            return resultFiles;
        }


        /// <summary>
        /// 获取pdf指定区域文本
        /// </summary>
        /// <param name="inputFile"></param> pdf文件路径
        /// <param name="location"></param> 位置坐标
        /// <param name="pageNum"></param> 页数
        /// <returns></returns>
        public static string GetText(string inputFile, Rectangle location, int pageNum)
        {
            PdfDocument           doc       = new PdfDocument();
            doc.LoadFromFile(inputFile);
            PdfTextExtractor      extractor = new PdfTextExtractor(doc.Pages[pageNum]);
            PdfTextExtractOptions options   = new PdfTextExtractOptions();
            options.ExtractArea = location;
            string str = extractor.ExtractText(options);
            str  = Regex.Replace(str, "[ ]{2,}", " "); // 连续两个及以上空格
            str = str.Replace("\r\n", "\n").Trim();          // 统一换行格式
            // string[] textNjson = str.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            return str;
        }

        /// <summary>
        /// 根据取货计划书pdf获取明细
        /// </summary>
        /// <returns></returns>
        public static DataTable GetOutPlan(string filePath) {
            try
            {
                if (string.IsNullOrEmpty(filePath)) { return null; }
                List<Rectangle> locations = new List<Rectangle>();
                Rectangle locationOrderCode = new Rectangle(142,152,55,400);
                Rectangle locationQuantity = new Rectangle(408,152,28,400);
                locations.Add(locationOrderCode);
                locations.Add(locationQuantity);
                PdfDocument doc = new PdfDocument();
                doc.LoadFromFile(filePath);
                DataTable dt = new DataTable();
                dt.Columns.Add("orderCode", typeof(string));
                dt.Columns.Add("quantity", typeof(int));
                PdfTextExtractOptions optionOrderCode = new PdfTextExtractOptions();
                PdfTextExtractOptions optionQuantity = new PdfTextExtractOptions();
                string orderCode = "";
                string quantity = "";

                // 遍历每一页
                for (int i = 0; i < doc.Pages.Count; i++)
                {
                    //PdfPageBase page = doc.Pages[i];
                    PdfTextExtractor extractor = new PdfTextExtractor(doc.Pages[i]);

                    optionOrderCode.ExtractArea = locations[0];
                    optionQuantity.ExtractArea = locations[1];
                    orderCode += extractor.ExtractText(optionOrderCode);
                    quantity += extractor.ExtractText(optionQuantity);

                }

                string[] orderCodes = orderCode
                        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                string[] quantitiesStr = quantity
                        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                int[] quantities = Array.ConvertAll(quantitiesStr, s => int.Parse(s.Trim()));

                for (int i = 0; i < orderCodes.Length; i++)
                {
                    dt.Rows.Add(orderCodes[i].Trim(), quantities[i]);
                }

                return dt;
            }
            catch (Exception)
            {

                throw;
            }
        }


        /// <summary>
        /// 从指定页码到末尾，读取指定的一个或多个区域文字
        /// </summary>
        /// <param name="inputFile">PDF 文件路径</param>
        /// <param name="locations">要提取的矩形区域集合</param>
        /// <param name="startPageNum">起始页 (0-based)</param>
        /// <returns>每页提取结果，字典: 页码 -> 该页所有区域文字</returns>
        public static Dictionary<int, List<string>> GetTextFromPages(string inputFile, List<Rectangle> locations, int startPageNum)
        {
            Dictionary<int, List<string>> results = new Dictionary<int, List<string>>();

            using (PdfDocument doc = new PdfDocument())
            {
                doc.LoadFromFile(inputFile);

                for (int pageNum = startPageNum; pageNum < doc.Pages.Count; pageNum++)
                {
                    PdfTextExtractor extractor = new PdfTextExtractor(doc.Pages[pageNum]);
                    List<string> pageTexts = new List<string>();

                    foreach (var location in locations)
                    {
                        PdfTextExtractOptions options = new PdfTextExtractOptions
                        {
                            ExtractArea = location
                        };

                        string str = extractor.ExtractText(options);
                        str = Regex.Replace(str, "[ ]{2,}", " "); // 去掉连续空格
                        str = str.Replace("\r\n", "\n").Trim();   // 统一换行格式

                        pageTexts.Add(str);
                    }

                    results[pageNum] = pageTexts;
                }
            }

            return results;
        }
        /// <summary>
        /// 多个PDF合成一个PDF；目前项目中后缀只能是PDF
        /// </summary>
        /// <param name="fileList">多个文件路径集合</param>
        /// <param name="outPath">输出文件路径需要带文件名+后缀名</param>
        /// <returns></returns>
        public static string MergeFiles(List<string> fileList,string outPath)
        {
            string outurl = "";
            try
            {
                // 创建最终合并的PDF文档
                PdfDocument mergedPdf = new PdfDocument();
                var row = 0;
                foreach (var item in fileList)
                {
                    string filePath = item;
                    if (filePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        // 直接合并PDF文件
                        PdfDocument pdf = new PdfDocument();
                        pdf.LoadFromFile(filePath);
                        mergedPdf.AppendPage(pdf);
                    }
                    else if (filePath.EndsWith(".xls", StringComparison.OrdinalIgnoreCase) ||
                              filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    {
                        //using (var ms = new MemoryStream())
                        //{
                        //    //Spire.Xls.Workbook workbook = new Spire.Xls.Workbook();
                        //    //workbook.LoadFromFile(filePath);
                        //    //workbook.SaveToStream(ms, Spire.Xls.FileFormat.PDF);

                        //    //Spire.Pdf.PdfDocument pdf = new Spire.Pdf.PdfDocument(ms);
                        //    //mergedPdf.AppendPage(pdf);
                        //    ////////
                        //    Spire.Xls.Workbook workbook = new Spire.Xls.Workbook();
                        //    workbook.LoadFromFile(filePath);

                        //    // 新增代码：强制Excel内容缩放到一页
                        //    foreach (Spire.Xls.Worksheet sheet in workbook.Worksheets)
                        //    {
                        //        // 设置页面缩放选项
                        //        sheet.PageSetup.FitToPagesTall = 1;  // 所有行缩放到1页
                        //        sheet.PageSetup.FitToPagesWide = 1;  // 所有列缩放到1页

                        //        // 可选：设置打印方向（根据内容自动调整方向）
                        //        //if (sheet.PageSetup.PageOrientation == Spire.Xls.PageOrientationType.Landscape)
                        //        //    sheet.PageSetup.PageOrientation = Spire.Xls.PageOrientationType.Portrait;
                        //    }

                        //    workbook.SaveToStream(ms, Spire.Xls.FileFormat.PDF);

                        //    Spire.Pdf.PdfDocument pdf = new Spire.Pdf.PdfDocument(ms);
                        //    mergedPdf.AppendPage(pdf);
                        //}
                    }
                    row++;
                }
                string newFileName = outPath;//filename 需要带扩展名传入
                // 保存最终PDF
                mergedPdf.SaveToFile(newFileName, FileFormat.PDF);
                mergedPdf.Close();
                outurl = newFileName;
            }
            catch (Exception e)
            {
                outurl = null;
            }
            return outurl;
        }

        /// <summary>
        /// 八千代看板生成
        /// </summary>
        /// <param name="ListKB"></param>
        public static string CreateBaQianDaiKB(List<BaQianDaiKB> ListKB)
        {
            string templatePath = HttpContext.Current.Server.MapPath("~/UploadFiles");
            string templateFile = Path.Combine(templatePath, "八千代KB模板.pdf");

            string outputPath = HttpContext.Current.Server.MapPath($"~/UploadFiles/{Guid.NewGuid().ToString()}");

            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            PdfTrueTypeFont chineseFont1 = new PdfTrueTypeFont(new Font("宋体", 16), true);
            PdfTrueTypeFont chineseFont2 = new PdfTrueTypeFont(new Font("宋体", 12), true);
            PdfTrueTypeFont chineseFont3 = new PdfTrueTypeFont(new Font("宋体", 24), true);
            PdfTrueTypeFont chineseFont4 = new PdfTrueTypeFont(new Font("宋体", 12), true);
            PdfSolidBrush brush = new PdfSolidBrush(Color.Black); // 设置文本颜色

            //原始文档
            for (int i = 0; i < ListKB.Count; i++)
            {
                Spire.Pdf.PdfDocument doc = new Spire.Pdf.PdfDocument();
                doc.LoadFromFile(templateFile);

                PdfPageBase page = doc.Pages[0];
                page.Canvas.DrawString(ListKB[i].factory, chineseFont1, brush, new RectangleF(46, 2, 200,30), new PdfStringFormat() { Alignment = PdfTextAlignment.Center, LineAlignment = PdfVerticalAlignment.Middle });

                page.Canvas.DrawString(ListKB[i].veh_model, ListKB[i].veh_model.Length>3 ? chineseFont2 :  chineseFont1, brush, 102, 57);
                page.Canvas.DrawString(ListKB[i].part_name, chineseFont2, brush, new RectangleF(56, 78, 200, 20), new PdfStringFormat() { Alignment = PdfTextAlignment.Center, LineAlignment = PdfVerticalAlignment.Middle }) ;
                page.Canvas.DrawString(ListKB[i].product_name_cn, chineseFont2, brush, new RectangleF(56, 97, 200, 20), new PdfStringFormat() { Alignment = PdfTextAlignment.Center, LineAlignment = PdfVerticalAlignment.Middle });
                page.Canvas.DrawString(ListKB[i].part_code, chineseFont2, brush, new RectangleF(56, 118, 200, 20), new PdfStringFormat() { Alignment = PdfTextAlignment.Center, LineAlignment = PdfVerticalAlignment.Middle });

                page.Canvas.DrawString(ListKB[i].qty, chineseFont3, brush, new RectangleF(56, 135, 44, 34), new PdfStringFormat() { Alignment = PdfTextAlignment.Center, LineAlignment = PdfVerticalAlignment.Middle });
                page.Canvas.DrawString(ListKB[i].box_cap, chineseFont1, brush, new RectangleF(146, 135, 44, 34), new PdfStringFormat() { Alignment = PdfTextAlignment.Center, LineAlignment = PdfVerticalAlignment.Middle });
                page.Canvas.DrawString(ListKB[i].r_location, chineseFont2, brush, new RectangleF(224, 134, 36, 34), new PdfStringFormat() { Alignment = PdfTextAlignment.Center, LineAlignment = PdfVerticalAlignment.Middle });

                page.Canvas.DrawString(ListKB[i].c_date, chineseFont2, brush, new RectangleF(56, 167, 140, 16), new PdfStringFormat() { Alignment = PdfTextAlignment.Center, LineAlignment = PdfVerticalAlignment.Middle });
                page.Canvas.DrawString(ListKB[i].c_time, chineseFont2, brush, new RectangleF(56, 186, 140, 16), new PdfStringFormat() { Alignment = PdfTextAlignment.Center, LineAlignment = PdfVerticalAlignment.Middle });
                page.Canvas.DrawString(ListKB[i].user, chineseFont2, brush, new RectangleF(194, 204, 65, 16), new PdfStringFormat() { Alignment = PdfTextAlignment.Center, LineAlignment = PdfVerticalAlignment.Middle });

                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(ListKB[i].qrcode, QRCodeGenerator.ECCLevel.H, requestedVersion: 4);

                // 3. 创建二维码图像
                QRCode qrCode = new QRCode(qrCodeData);
                string filePath = Path.Combine(outputPath, $"NewTemplate{i.ToString().PadLeft(4, '0')}.png");
                using (Bitmap qrCodeImage = qrCode.GetGraphic(5, Color.Black,Color.White,false))
                {
                    // 4. 保存为文件
                    PdfImage pdfImage = PdfImage.FromImage(qrCodeImage);
                    page.Canvas.DrawImage(pdfImage, 6, 33, 40, 40);
                }


                // 5. 保存为新文件
                doc.SaveToFile(Path.Combine(outputPath,$"NewTemplate{i.ToString().PadLeft(4,'0')}.pdf"), FileFormat.PDF);
                doc.Close();
            }

            DirectoryInfo dir = new DirectoryInfo(outputPath);
            FileInfo[] fils = dir.GetFiles();

            SpireHelper.Merge(3, 2, fils, outputPath + ".pdf", new Point(14,14), true);

            dir.Delete(true);

            return outputPath + ".pdf";
        }
        /// <summary>
        /// 八千代拖看板生成
        /// </summary>
        /// <param name="ListKB"></param>
        public static string CreateBaQianDaiT(List<BaQianDaiT> ListT)
        {
            string templatePath = HttpContext.Current.Server.MapPath("~/UploadFiles");
            string templateFile = Path.Combine(templatePath, "八千代T模板.pdf");

            string outputPath = HttpContext.Current.Server.MapPath($"~/UploadFiles/{Guid.NewGuid().ToString()}");

            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            PdfTrueTypeFont chineseFontBigRed = new PdfTrueTypeFont(new Font("宋体", 45), true);
            PdfTrueTypeFont chineseFontNormal = new PdfTrueTypeFont(new Font("黑体", 11), true);
            PdfTrueTypeFont chineseFont2VG = new PdfTrueTypeFont(new Font("宋体", 168), true);
            PdfSolidBrush brushBlack = new PdfSolidBrush(Color.Black); // 设置文本颜色
            PdfSolidBrush brushRed = new PdfSolidBrush(Color.Red); // 设置文本颜色

            //foreach (var item in ListT)
            //{
                
            //}

            for (int i = 0; i < ListT.Count; i++)
            {
                Spire.Pdf.PdfDocument doc = new Spire.Pdf.PdfDocument();
                doc.LoadFromFile(templateFile);

                PdfPageBase page = doc.Pages[0];

                page.Canvas.DrawString(ListT[i].no, 
                    chineseFontBigRed, brushRed, 
                    new RectangleF(205, 40, 100, 55), 
                    new PdfStringFormat() { Alignment = PdfTextAlignment.Center, LineAlignment = PdfVerticalAlignment.Middle });

                page.Canvas.DrawString(ListT[i].code,
                    chineseFontBigRed, brushRed,
                    new RectangleF(485, 40, 56, 55),
                    new PdfStringFormat() { Alignment = PdfTextAlignment.Center, LineAlignment = PdfVerticalAlignment.Middle });

                page.Canvas.DrawString(ListT[i].r_location,
                    chineseFontNormal, brushBlack,
                    new RectangleF(68, 164, 38, 20),
                    new PdfStringFormat() { Alignment = PdfTextAlignment.Center, LineAlignment = PdfVerticalAlignment.Middle });

                page.Canvas.DrawString(ListT[i].part_code,
                    chineseFontNormal, brushBlack,
                    new RectangleF(104, 164, 114, 20),
                    new PdfStringFormat() { Alignment = PdfTextAlignment.Center, LineAlignment = PdfVerticalAlignment.Middle });

                page.Canvas.DrawString(ListT[i].color,
                    chineseFontNormal, brushBlack,
                    new RectangleF(218, 164, 55, 20),
                    new PdfStringFormat() { Alignment = PdfTextAlignment.Center, LineAlignment = PdfVerticalAlignment.Middle });

                page.Canvas.DrawString(ListT[i].c_date,
                    chineseFontNormal, brushBlack,
                    new RectangleF(272, 164, 55, 20),
                    new PdfStringFormat() { Alignment = PdfTextAlignment.Center, LineAlignment = PdfVerticalAlignment.Middle });

                page.Canvas.DrawString(ListT[i].c_time,
                    chineseFontNormal, brushBlack,
                    new RectangleF(322, 164, 55, 20),
                    new PdfStringFormat() { Alignment = PdfTextAlignment.Center, LineAlignment = PdfVerticalAlignment.Middle });

                page.Canvas.DrawString(ListT[i].qty.ToString(),
                    chineseFontNormal, brushBlack,
                    new RectangleF(374, 164, 55, 20),
                    new PdfStringFormat() { Alignment = PdfTextAlignment.Center, LineAlignment = PdfVerticalAlignment.Middle });

                page.Canvas.DrawString(ListT[i].box_qty.ToString(),
                    chineseFontNormal, brushBlack,
                    new RectangleF(426, 164, 65, 20),
                    new PdfStringFormat() { Alignment = PdfTextAlignment.Center, LineAlignment = PdfVerticalAlignment.Middle });

                if(ListT[i].Is2VG && ListT[i].plancode == "A4AF01")
                {
                    page.Canvas.DrawString("2VG用",
                        chineseFont2VG, brushBlack,
                        new RectangleF(80, 400, 440, 200),
                        new PdfStringFormat() { Alignment = PdfTextAlignment.Center, LineAlignment = PdfVerticalAlignment.Middle });
                }

                doc.SaveToFile(Path.Combine(outputPath, $"NewTemplate{i.ToString().PadLeft(4, '0')}.pdf"), FileFormat.PDF);
                doc.Close();
            }

            DirectoryInfo dir = new DirectoryInfo(outputPath);
            FileInfo[] fils = dir.GetFiles();

            SpireHelper.Merge(1, 1, fils, outputPath + ".pdf", new Point(0, 0), false);

            dir.Delete(true);

            return outputPath + ".pdf";
        }


        public static void FillPdf(
        string templatePath,
        string outputPath,
        string pn,
        string pnName,
        string model,
        string pNum,
        String arrivalDate)
        {
            // 加载 PDF 模板
            PdfDocument doc = new PdfDocument();
            doc.LoadFromFile(templatePath);

            // 取第一页
            PdfPageBase page = doc.Pages[0];

            // 设置字体（28号，等闲，如果没有该字体会自动回退）
            Font baseFont;
            try
            {
                baseFont = new Font("等线", 28f, FontStyle.Regular);
            }
            catch
            {
                baseFont = new Font("宋体", 28f, FontStyle.Regular);
            }
            PdfTrueTypeFont font = new PdfTrueTypeFont(baseFont, true);
            PdfBrush brush = PdfBrushes.Black;

            // 随便写几个位置（单位是 point，1 point ≈ 1/72 英寸）
            page.Canvas.DrawString(  pn, font, brush, 245, 170);
            page.Canvas.DrawString( pnName, font, brush, 245, 245);
            page.Canvas.DrawString(model, font, brush, 245, 315);
            page.Canvas.DrawString( pNum, font, brush, 245, 380);
            page.Canvas.DrawString( arrivalDate, font, brush, 245, 450);

            // 保存到输出文件
            doc.SaveToFile(outputPath);
            doc.Close();
        }

        public static enumPdfType getPdfType(string filepath)
        {
            FileInfo file = new FileInfo(filepath);
            if(file.Name.IndexOf("LABEL_D")>-1)
            {
                return enumPdfType.GHAC_PLS;
            }
            else if (file.Name.IndexOf("LABEL_T") > -1)
            {
                return enumPdfType.GHAC_T;
            }
            Spire.Pdf.PdfDocument pdf = new Spire.Pdf.PdfDocument();
            pdf.LoadFromFile(filepath);

            //for (int i = 0; i < pdf.Pages.Count; i++)
            //{
            //    Spire.Pdf.Texts.PdfTextFinder textFinder = new PdfTextFinder(pdf.Pages[i]);
            //    List<PdfTextFragment> Finder = textFinder.Find("传票更改清单");//textFinder.Find("取货指示书");

            //    if (Finder.Count > 0)
            //    {
            //        return enumPdfType.GHAC_01;
            //    }

            //    Finder = textFinder.Find("取货指示书");
            //    if (Finder.Count > 0)
            //        return enumPdfType.GHAC_INSTRUCTIONS;

            //    //发行顺序号
            //    Finder = textFinder.Find("发行顺序号");
            //    if (Finder.Count > 0)
            //        return enumPdfType.GHAC_TRACE;
            //}
            bool hasInstructions = false;
            bool hasSummons = false;

            for (int i = 0; i < pdf.Pages.Count; i++)
            {
                var textFinder = new PdfTextFinder(pdf.Pages[i]);

                var finderGHEC = textFinder.Find("GHEC");
                // 传票更改清单
                var finder = textFinder.Find("传票更改清单");
                if (finder != null && finder.Count > 0 && finderGHEC.Count ==0)
                {
                    return enumPdfType.GHAC_01;
                }

                finder = textFinder.Find("送货传票");
                if (finder != null && finder.Count >0) {
                    hasSummons = true;
                }

                // 先记住取货指示书，不立刻 return
                finder = textFinder.Find("取货指示书");
                if (finder != null && finder.Count > 0)
                {
                    hasInstructions = true;
                }

                // 发行顺序号（最低优先级，只有前面都没命中才用）
                finder = textFinder.Find("发行顺序号");
                if (finder != null && finder.Count > 0 && !hasInstructions)
                {
                    return enumPdfType.GHAC_TRACE;
                }
            }

            if (hasInstructions && hasSummons) {

                return enumPdfType.GHAC_01;
            }

            // 循环结束后再判断
            if (hasInstructions)
            {
                return enumPdfType.GHAC_INSTRUCTIONS;
            }


            for (int i = 0; i < pdf.Pages.Count; i++)
            {
                Spire.Pdf.Texts.PdfTextFinder textFinder = new PdfTextFinder(pdf.Pages[i]);
                List<PdfTextFragment> Finder = textFinder.Find("送货传票");
                if (Finder.Count > 0)
                {
                    Finder = textFinder.Find("CHAM");
                    if (Finder.Count > 0)
                    {
                        return enumPdfType.CHAM_SUMMONS;
                    }

                    Finder = textFinder.Find("GHAC"); 
                    if (Finder.Count > 0)
                    {
                        return enumPdfType.GHAC_SUMMONS;
                    }
                    Finder = textFinder.Find("DHEC");
                    if (Finder.Count > 0)
                    {
                        return enumPdfType.DHEC_D_SP;    //补给品传票
                    }
                }
                Finder = textFinder.Find("托标签-CHAM");
                if (Finder.Count > 0)
                {
                    return enumPdfType.CHAM_T;
                }
                Finder = textFinder.Find("部品明示卡");
                if (Finder.Count > 0)
                {
                    return enumPdfType.CHAM_D;
                }
                Finder = textFinder.Find("出货指示单");
                if (Finder.Count > 0)
                {
                    return enumPdfType.CHAM_INSTRUCTIONS;
                }
                Finder = textFinder.Find("零件明示卡");
                if (Finder.Count > 0)
                {
                    return enumPdfType.DHEC_D;
                }
                //Finder = textFinder.Find("传票更改清单");
                //if (Finder.Count > 0) {
                //    return enumPdfType.GHAC_01;
                //}
                //Finder = textFinder.Find("送货传票");
                //if (Finder.Count > 0 ) {
                //    return enumPdfType.DHEC_D_SP;    //补给品传票
                //}

            }
            return enumPdfType.NONE ;

        }

        public static void DrawLineInPdf(string pdfFilePath)
        {
            // 加载现有的 PDF 文档
            PdfDocument doc = new PdfDocument();
            doc.LoadFromFile(pdfFilePath);

            // 获取第一张页面
            PdfPageBase page = doc.Pages[0];

            // 获取页面画布
            PdfCanvas canvas = page.Canvas;

            // 设置线条的颜色和宽度
            PdfPen pen = new PdfPen(Color.Black, 1f);

            // 在指定位置绘制直线
            canvas.DrawLine(pen, 29f, 780f, 581f, 780f);

            // 保存更改到同一个 PDF 文件
            doc.SaveToFile(pdfFilePath);
            doc.Close();
        }



        public static void DrawLineFTEC(string pdfFilePath)
        {
            // 加载现有的 PDF 文档
            PdfDocument doc = new PdfDocument();
            doc.LoadFromFile(pdfFilePath);

            for (int i = 0; i < doc.Pages.Count; i++)
            {
                // 获取第一张页面
                PdfPageBase page = doc.Pages[i];

                // 获取页面画布
                PdfCanvas canvas = page.Canvas;

                // 设置线条的颜色和宽度
                PdfPen pen = new PdfPen(Color.Black, 1f);

                // 在指定位置绘制直线
                canvas.DrawLine(pen, 286.41f, 34f, 286.41f, 215f);
                canvas.DrawLine(pen, 286.41f, 230f, 286.41f, 412f);
                canvas.DrawLine(pen, 286.41f, 430f, 286.41f, 611f);
                canvas.DrawLine(pen, 286.41f, 626f, 286.41f, 808f);
            }



            // 保存更改到同一个 PDF 文件
            doc.SaveToFile(pdfFilePath);
            doc.Close();
        }

        /// <summary>
        /// 调整PDF页面大小为指定尺寸
        /// </summary>
        /// <param name="inputPath">输入PDF路径</param>
        /// <param name="outputPath">输出PDF路径</param>
        /// <param name="newSize">新页面尺寸</param>
        /// <param name="keepAspectRatio">是否保持原内容宽高比</param>
        /// <param name="centerContent">是否将内容居中</param>
        public static void ResizePdfPages(
            string inputPath,
            string outputPath,
            SizeF newSize,
            bool keepAspectRatio = true,
            bool centerContent = true)
        {
            // 加载原始PDF文件
            using (PdfDocument doc = new PdfDocument())
            {
                doc.LoadFromFile(inputPath);

                // 创建新文档
                using (PdfDocument newDoc = new PdfDocument())
                {
                    PdfMargins margins = new PdfMargins(0);

                    foreach (PdfPageBase oldPage in doc.Pages)
                    {
                        // 添加新页面
                        PdfPageBase page = newDoc.Pages.Add(newSize, margins);

                        // 创建模板
                        PdfTemplate template = oldPage.CreateTemplate();

                        // 计算缩放比例
                        float scaleX = newSize.Width / oldPage.Size.Width;
                        float scaleY = newSize.Height / oldPage.Size.Height;

                        if (keepAspectRatio)
                        {
                            scaleX = scaleY = Math.Min(scaleX, scaleY);
                        }

                        // 计算居中位置
                        float offsetX = centerContent ? (newSize.Width - oldPage.Size.Width * scaleX) / 2 : 0;
                        float offsetY = centerContent ? (newSize.Height - oldPage.Size.Height * scaleY) / 2 : 0;

                        // 应用变换并绘制
                        page.Canvas.Save();
                        page.Canvas.TranslateTransform(offsetX, offsetY);
                        page.Canvas.ScaleTransform(scaleX, scaleY);
                        page.Canvas.DrawTemplate(template, PointF.Empty);
                        page.Canvas.Restore();
                    }

                    // 保存新PDF文件
                    newDoc.SaveToFile(outputPath, FileFormat.PDF);
                }
            }
        }


        /// <summary>
        /// 调整PDF页面为A4横向
        /// </summary>
        public static void ResizeToA4Landscape(string inputPath, string outputPath)
        {
            SizeF a4Landscape = new SizeF(842, 595); // A4横向尺寸
            ResizePdfPages(inputPath, outputPath, a4Landscape);
        }

        /// <summary>
        /// 调整PDF页面为A4纵向
        /// </summary>
        public static void ResizeToA4Portrait(string inputPath, string outputPath)
        {
            SizeF a4Portrait = new SizeF(595, 842); // A4纵向尺寸
            ResizePdfPages(inputPath, outputPath, a4Portrait);
        }

        public enum enumPdfType
        {
            NONE,
            /// <summary>
            /// CHAM传票
            /// </summary>
            CHAM_SUMMONS,
            /// <summary>
            /// CHAM送货单
            /// </summary>
            CHAM_INSTRUCTIONS,
            /// <summary>
            /// CHAM托标签
            /// </summary>
            CHAM_T,
            /// <summary>
            /// CHAM箱标签（部 品 明 示 卡）
            /// </summary>
            CHAM_D,
            /// <summary>
            /// GHAC传票
            /// </summary>
            GHAC_SUMMONS,
            /// <summary>
            /// GHAC取货指示书
            /// </summary>
            GHAC_INSTRUCTIONS,
            /// <summary>
            /// GHAC箱标签
            /// </summary>
            GHAC_PLS,
            GHAC_PLS2,
            GHAC_TRACE,
            GHAC_01,
            /// <summary>
            /// GHAC托标签
            /// </summary>
            GHAC_T,
            DHEC_D,
            DHEC_D_SP
        }

    }
    
    

    public class SpireSplit
    {
        public enum EnumNameRule
        {
            Auto,
            Customize,
            CustomizeAuto
        }
    }
    

}