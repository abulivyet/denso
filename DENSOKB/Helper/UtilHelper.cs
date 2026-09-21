using DinkToPdf.Contracts;
using DinkToPdf;
using System;
using System.IO;
using OfficeOpenXml;

namespace DENSOKB.Helper
{
    public class UtilHelper
    {
        private readonly IConverter _converter;

        public UtilHelper()
        {
            _converter = new BasicConverter(new PdfTools());
        }
    
        public void ConvertExcelSheetToPdf(string excelPath, string pdfPath, string sheetName)
        {

            using (var package = new ExcelPackage(new FileInfo(excelPath)))
            {
                var sheet = package.Workbook.Worksheets[sheetName];
                if (sheet == null)
                {
                    throw new Exception($"Sheet {sheetName} 不存在");
                }

                // **1. 解析 Sheet 并转换为 HTML**
                string htmlContent = ConvertSheetToHtml(sheet);

                // **2. 使用 DinkToPdf (WkHtmlToPdf) 转换 HTML 到 PDF**
                var doc = new HtmlToPdfDocument()
                {
                    GlobalSettings = new GlobalSettings
                    {
                        ColorMode = ColorMode.Color,
                        Orientation = Orientation.Portrait,
                        PaperSize = PaperKind.A4
                    },
                    Objects = { new ObjectSettings { HtmlContent = htmlContent } }
                };

                byte[] pdfBytes = _converter.Convert(doc);
                File.WriteAllBytes(pdfPath, pdfBytes);
            }
        }

        public string ConvertSheetToHtml(ExcelWorksheet sheet)
        {
            int rows = sheet.Dimension.Rows;
            int cols = sheet.Dimension.Columns;
            string html = "<html><body><table border='1' style='border-collapse: collapse;'>";

            for (int row = 1; row <= rows; row++)
            {
                html += "<tr>";
                for (int col = 1; col <= cols; col++)
                {
                    var cellValue = sheet.Cells[row, col].Text;
                    html += $"<td style='padding: 5px; border: 1px solid black;'>{cellValue}</td>";
                }
                html += "</tr>";
            }

            html += "</table></body></html>";
            return html;
        }
    }
}
