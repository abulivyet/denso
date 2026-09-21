using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DENSOKB.Models;
using HelpLog.CommomMethod;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;

public class FTETnew
{
    private const float A4Height = 842f;
    private const float FooterAreaHeight = 60f;

    public bool ProcessPdf(string inputPath, string centeredPath, string splitPath,int box)
    {
        bool isok = false;
        try
        {
            // 取消页码 居中
            RemoveFooterAndCenter(inputPath, centeredPath);
            // 三等分
            VerticalSplitExcludeFooter(inputPath, splitPath, box, false);
            isok = true;
        }
        catch (Exception e)
        {
            XErrorLogToolMDM.WriteLog(string.Format("A4看板和小看板解析失败，原因[{0}]", e.Message));
            throw new InvalidOperationException(string.Format("A4看板和小看板解析失败，原因[{0}]",e.Message));
        }
        return isok;
    }

    // 需求1：移除页码并居中
    private void RemoveFooterAndCenter(string inputPath, string outputPath)
    {
        using (var reader = new PdfReader(inputPath))
        using (var fs = new FileStream(outputPath, FileMode.Create))
        using (var stamper = new PdfStamper(reader, fs))
        {
            for (int pageNum = 1; pageNum <= reader.NumberOfPages; pageNum++)
            {
                var pageSize = reader.GetPageSize(pageNum);
                var canvas = stamper.GetOverContent(pageNum);

                // 清除页脚
                ClearFooterArea(canvas, pageSize);

                // 获取内容高度
                var contentHeight = GetContentHeight(reader, pageNum);

                // 计算垂直居中偏移量（考虑保留底部空白）
                float yOffset = (pageSize.Height - contentHeight - FooterAreaHeight) / 2;

                // 直接应用平移变换
                canvas.ConcatCTM(1, 0, 0, 1, 0, yOffset);
            }
        }
    }
    public void VerticalSplitExcludeFooter(string inputPath, string outputPath,int box,bool IsBJ)
    {
        // 配置参数（单位：磅）
        int partitionInt = 3;//  分割数量
        float a4Height = 297f / 25.4f * 72;  // A4全高 ≈ 841.89磅
        float targetThirdHeight = a4Height / 3; // 目标每段高度 ≈ 280.63磅
        float footerHeight = 53f;             // 需排除的页脚高度
        if (IsBJ) footerHeight = 0;           //  补给品不用补偿
        float widthScaleFactor = 1.0f;        // 宽度缩放系数
        float heightTolerance = 1.02f;        // 高度补偿系数
        int remainder = box % partitionInt; // 计算余数  最后3-余数应该是需要删除的页数
        using (PdfReader reader = new PdfReader(inputPath))
        using (FileStream fs = new FileStream(outputPath, FileMode.Create))
        {
            Document doc = new Document();
            PdfWriter writer = PdfWriter.GetInstance(doc, fs);
            doc.Open();

            for (int pageNum = 1; pageNum <= reader.NumberOfPages; pageNum++)
            {
                Rectangle pageRect = reader.GetPageSizeWithRotation(pageNum);
                bool isRotated = (pageRect.Rotation % 180) != 0;

                // 获取实际尺寸（自动处理旋转）
                float srcWidth = isRotated ? pageRect.Height : pageRect.Width;
                float srcHeight = isRotated ? pageRect.Width : pageRect.Height;

                // 计算缩放比例（核心逻辑）
                float usableHeight = srcHeight - footerHeight;
                float scaleY = (targetThirdHeight * partitionInt) / usableHeight * heightTolerance;
                float scaleX = widthScaleFactor * (isRotated ? scaleY : 1f);

                // 缩放后实际尺寸
                float scaledWidth = srcWidth * scaleX;
                float scaledHeight = srcHeight * scaleY;

                PdfImportedPage sourcePage = writer.GetImportedPage(reader, pageNum);
                if (pageNum == reader.NumberOfPages&& box!=0 && remainder>0) 
                    partitionInt = remainder; // 当partitionInt=reader.NumberOfPages 时 需要修改最终的页 为余数
                for (int section = 0; section < partitionInt; section++)
                {
                    // 创建目标页面（固定为A4 1/3高度）
                    doc.SetPageSize(new Rectangle(scaledWidth, targetThirdHeight));
                    doc.NewPage();

                    // 计算源内容区域（基于缩放后坐标系）
                    float sectionTop = (usableHeight - section * usableHeight / partitionInt) * scaleY;
                    float sectionBottom = sectionTop - targetThirdHeight;

                    // 应用仿射变换（关键步骤）
                    writer.DirectContent.AddTemplate(
                        sourcePage,
                        scaleX, 0,
                        0, scaleY,
                        0,
                        -sectionBottom - (srcHeight - usableHeight) * scaleY
                    );
                }
            }
            doc.Close();
        }
    }
    private void ClearFooterArea(PdfContentByte canvas, Rectangle pageSize)
    {
        canvas.SaveState();
        canvas.SetColorFill(BaseColor.WHITE);
        canvas.Rectangle(
            pageSize.Left,
            pageSize.Bottom,
            pageSize.Width,
            FooterAreaHeight
        );
        canvas.Fill();
        canvas.RestoreState();
    }

    private float GetContentHeight(PdfReader reader, int pageNum)
    {
        var parser = new PdfReaderContentParser(reader);
        var listener = new ContentBoundaryListener();
        parser.ProcessContent(pageNum, listener);
        return listener.MaxY - listener.MinY;
    }
    private class ContentBoundaryListener : IRenderListener
    {
        public float MinY { get; private set; } = float.MaxValue;
        public float MaxY { get; private set; } = float.MinValue;

        public void BeginTextBlock() { }
        public void EndTextBlock() { }

        public void RenderText(TextRenderInfo renderInfo)
        {
            var baseline = renderInfo.GetBaseline().GetLength();
            UpdateBounds(baseline); // Vector索引1对应Y坐标
        }

        public void RenderImage(ImageRenderInfo renderInfo)
        {
            var ctm = renderInfo.GetArea();
            UpdateBounds(ctm); // 矩阵索引5对应Y方向平移
        }

        private void UpdateBounds(float y)
        {
            MinY = Math.Min(MinY, y);
            MaxY = Math.Max(MaxY, y);
        }
    }
    #region             拆分 补给 看板
    /* 现状：每一个背番看板只给了一个 要根据实际箱数生成多个看板
     * 同样需求需要生成小纸张和A4纸
     */
    /// <summary>
    /// 补给品看板拆分
    /// </summary>
    /// <param name="inputPath"></param>
    /// <param name="centeredPath"></param>
    /// <param name="splitPath"></param>
    /// <param name="box"></param>
    public bool ProcessPdf_BJ(string inputPath, string centeredPath, string splitPath, FTETEntity ftet_BJ)
    {
        bool isok = false;
        try
        {
            // 思路1 先拆成单个 再做处理
            string filename = System.IO.Path.GetFileNameWithoutExtension(inputPath);
            string CFUrl = System.IO.Path.GetDirectoryName(splitPath);
            string NewCFPaht = CFUrl + "\\" + filename + "_拆分.pdf";
            VerticalSplitExcludeFooter(inputPath, NewCFPaht, 0, false);

            // 思路2 先把PDF整理好标准的正中间 再处理
            //RemoveFooterAndCenter(inputPath, NewCFPaht);

            // 转换思路 拆分后的小纸张转A4或者小纸张
            //A4 纸
            OutA4(NewCFPaht, centeredPath, ftet_BJ);
            //小纸张
            int box = (int)ftet_BJ.FTETdetailList.Sum(t => t.box);
            // 上边的去页码 在比例位移 不能直接复用 防止改乱 重写方法
            VerticalSplitExcludeFooter(centeredPath, splitPath, box, true);
            isok = true;
        }
        catch(Exception e)
        {
            XErrorLogToolMDM.WriteLog("FTET补给品小看板和A4处理失败，"+e.Message);
        }
        return isok = true;
    }
    private void OutA4(string inputPath, string outputPath, FTETEntity ftet_BJ)
    {
        using (PdfReader reader = new PdfReader(inputPath))
        using (FileStream fs = new FileStream(outputPath, FileMode.Create))
        {
            Document outputDoc = new Document(PageSize.A4);
            PdfWriter writer = PdfWriter.GetInstance(outputDoc, fs);
            outputDoc.Open();

            // 参数定义
            const int SECTIONS_PER_PAGE = 3;
            Rectangle srcPageSize = reader.GetPageSize(1);
            float srcThirdHeight = srcPageSize.Height / 3f;
            float a4SectionHeight = PageSize.A4.Height / SECTIONS_PER_PAGE;
            int currentSection = 0;
            foreach (var item in ftet_BJ.FTETdetailList)
            {

                //var position = FindTextPosition(reader, item.pin);
                var position= FindTextPage(reader, item.pin);  // 该用自己的写法
                int sourcePageNum = position;
                // 用3-上中下得到需要缩进截取的位置
                //int textThree = SECTIONS_PER_PAGE-(int)Math.Floor(position.Bottom/ srcThirdHeight);// 可以判断在上中下哪部分 上 1 中 2 下 3 然后 修改底部这个数据*7                
               // int section = Math.Max(0, Math.Min((int)(position.Bottom / srcThirdHeight), 2));
               // textThree = SECTIONS_PER_PAGE - section;
                // 测试数据：复制4次
                for (int copyCount = 0; copyCount < item.box; copyCount++)
                {

                #region 原来写法
                // 需要新建页面的情况
                if (currentSection == 0)
                {
                    outputDoc.NewPage();
                }
                var newsrc = srcThirdHeight ;
                // 创建模板（保持原有逻辑）
                PdfTemplate template = CreateCroppedTemplate(writer, reader, sourcePageNum,5, srcPageSize.Height);

                #endregion


                // 关键修正：垂直定位计算（反向排列）
                float yOffset = (SECTIONS_PER_PAGE - 1 - currentSection) * a4SectionHeight;

                    // 添加带边距的内容（上下左右各20pt）
                    writer.DirectContent.AddTemplate(template,
                        1, 0, 0, 0.95,
                        0, // X轴固定边距
                        yOffset+ 10f  // Y轴动态边距
                    );
                    currentSection = (currentSection + 1) % SECTIONS_PER_PAGE;
                }
            }
            outputDoc.Close();
        }
    }

    private PdfTemplate CreateCroppedTemplate(PdfWriter writer, PdfReader reader, int sourcePageNum, float cropY, float cropHeight)
    {
        // 创建一个标准宽 280高的PDF块
        PdfTemplate template = writer.DirectContent.CreateTemplate(
            reader.GetPageSize(1).Width,
            cropHeight
        );

        // 坐标系调整（重要修正点）
        template.ConcatCTM(1, 0, 0, 1, 0, -cropY);  // cropY 是Y偏移量 控制什么

        template.AddTemplate(writer.GetImportedPage(reader, sourcePageNum), 0, 0);
        return template;
    }

    static int FindTextPage(PdfReader reader, string searchText)
    {
        // 添加参数校验
        if (reader == null || string.IsNullOrEmpty(searchText))
            return -1;

        // 使用正确的属性获取页面数
        for (int i = 1; i <= reader.NumberOfPages; i++)
        {
            // 修正页面访问语法错误
            //var page = PdfTextExtractor.GetTextFromPage(reader, i); // iText页面索引从1开始
            string text = PdfTextExtractor.GetTextFromPage(reader, i);
            string[] textarr= Regex.Split(text, "供应商", RegexOptions.IgnoreCase);
            int page = (i / 3);
            for (int e = 0; e < textarr.Length; e++)
            {
                // en 
                if (textarr[e].IndexOf(searchText) >= 0)
                {
                    return (page +1)* (e);
                }
            }
           
        }
        return -1;
    }
    private TextPosition FindTextPosition(PdfReader reader, string searchText)
    {
        for (int physicalPageNum = 1; physicalPageNum <= reader.NumberOfPages; physicalPageNum++)
        {
            // 获取当前页尺寸并计算1/3高度
            Rectangle pageSize = reader.GetPageSize(physicalPageNum);
            float sectionHeight = pageSize.Height ;

            var strategy = new TextLocationStrategy(searchText, physicalPageNum, sectionHeight);
            PdfTextExtractor.GetTextFromPage(reader, physicalPageNum, strategy);

            if (strategy.FoundPosition != null)
            {
                // 确保段号在0-2范围内（0=上，1=中，2=下）
                int sectionIndex = (int)(strategy.FoundPosition.Bottom / sectionHeight);
                sectionIndex = Math.Min(2, Math.Max(0, sectionIndex));

                // 计算逻辑页码：(物理页数-1)*3 + 段号 + 1
                int logicalPageNum = (physicalPageNum - 1) * 3 + sectionIndex + 1;

                // 调试输出
                Console.WriteLine($"找到文本 '{searchText}'");
                Console.WriteLine($"物理页号: {physicalPageNum}");
                Console.WriteLine($"所在部分: {(sectionIndex == 0 ? "上" : sectionIndex == 1 ? "中" : "下")}段");
                Console.WriteLine($"计算得到的逻辑页码: {logicalPageNum}");

                strategy.FoundPosition.PageNum = logicalPageNum;
                return strategy.FoundPosition;
            }
        }

        Console.WriteLine($"未找到文本 '{searchText}'");
        return null;
    }

    private class TextLocationStrategy : ITextExtractionStrategy
    {
        private readonly string _searchText;
        private readonly int _physicalPageNum;
        private readonly float _sectionHeight;

        public TextPosition FoundPosition { get; private set; }

        public TextLocationStrategy(string searchText, int physicalPageNum, float sectionHeight)
        {
            _searchText = searchText.Trim();
            _physicalPageNum = physicalPageNum;
            _sectionHeight = sectionHeight;
        }

        public void RenderText(TextRenderInfo renderInfo)
        {
            if (FoundPosition != null) return;

            string text = renderInfo.GetText().Trim();
            if (text.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var baseline = renderInfo.GetBaseline();
                var ascent = renderInfo.GetAscentLine();

                float yPos = baseline.GetStartPoint()[1];
                int sectionIndex = (int)(yPos / _sectionHeight);

                FoundPosition = new TextPosition
                {
                    Text = text,
                    Left = baseline.GetStartPoint()[0],
                    Bottom = yPos,
                    Top = ascent.GetEndPoint()[1],
                    Width = baseline.GetEndPoint()[0] - baseline.GetStartPoint()[0],
                    Height = ascent.GetEndPoint()[1] - baseline.GetStartPoint()[1],
                    PageNum = _physicalPageNum, // 临时存储物理页号
                    SectionIndex = sectionIndex // 0=上,1=中,2=下
                };
            }
        }

        public string GetResultantText() => string.Empty;
        public void BeginTextBlock() { }
        public void EndTextBlock() { }
        public void RenderImage(ImageRenderInfo renderInfo) { }
    }

    private class TextPosition
    {
        public string Text { get; set; }
        public float Left { get; set; }
        public float Bottom { get; set; }
        public float Top { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public int PageNum { get; set; }      // 最终存储逻辑页码
        public int SectionIndex { get; set; } // 0=上段,1=中段,2=下段
    }

    #endregion
    #region 多个文件通过路径生成一个PDF
    public  string  MergeFiles(List<inpdetail2> detail, string outputPath,string filename)
    {
        string outurl = "";
        try
        {
            // 创建最终合并的PDF文档
            Spire.Pdf.PdfDocument mergedPdf = new Spire.Pdf.PdfDocument();
            var row = 0;
            foreach (var item in detail)
            {
                string filePath = item.Stacklabel.ToString();
                if (filePath.EndsWith(".pdf", System.StringComparison.OrdinalIgnoreCase))
                {
                    // 直接合并PDF文件
                    Spire.Pdf.PdfDocument pdf = new Spire.Pdf.PdfDocument();
                    pdf.LoadFromFile(filePath);
                    mergedPdf.AppendPage(pdf);
                }
                else if (filePath.EndsWith(".xls", System.StringComparison.OrdinalIgnoreCase) ||
                          filePath.EndsWith(".xlsx", System.StringComparison.OrdinalIgnoreCase))
                {
                    //using (var ms = new MemoryStream())
                    //{
                    //    Spire.Xls.Workbook workbook = new Spire.Xls.Workbook();
                    //    workbook.LoadFromFile(filePath);
                    //    workbook.SaveToStream(ms, Spire.Xls.FileFormat.PDF);

                    //    Spire.Pdf.PdfDocument pdf = new Spire.Pdf.PdfDocument(ms);
                    //    mergedPdf.AppendPage(pdf);

                    //}
                }
                row++;
            }
            string newFileName = $"{filename}_套票.pdf";
            // 保存最终PDF
            mergedPdf.SaveToFile(outputPath+ newFileName, Spire.Pdf.FileFormat.PDF);
            mergedPdf.Close();
            outurl = outputPath + newFileName;
        }
        catch (Exception e)
        {
            outurl = null;
        }
        return outurl;
    }
    #endregion
}
