using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompareFilePDF.Class
{
    public class ComparePDF
    {
        // Get location text
        public class TextElement
        {
            public string Text { get; set; }
            public float X { get; set; }
            public float Y { get; set; }
        }
        public class TextElementFromTo
        {
            public string Text { get; set; }
            public float XFrom { get; set; }
            public float YFrom { get; set; }
            public float XTo { get; set; }
            public float YTo { get; set; }
        }

        //Extract custom text to capture text location
        public class SimpleTextExtractionStrategyWithPosition : LocationTextExtractionStrategy
        {
            // Store text position
            public List<RectAndText> TextPosition = new List<RectAndText>();

            // Override the RenderText method to capture text position
            public override void RenderText(TextRenderInfo renderInfo)
            {
                base.RenderText(renderInfo);

                foreach (var charInfo in renderInfo.GetCharacterRenderInfos())
                {
                    var bottomLeft = charInfo.GetDescentLine().GetStartPoint();
                    var topRight = charInfo.GetAscentLine().GetEndPoint();

                    TextPosition.Add(new RectAndText
                    {
                        Text = charInfo.GetText(),
                        Rect = new iTextSharp.text.Rectangle(
                            bottomLeft[Vector.I1],
                            bottomLeft[Vector.I2],
                            topRight[Vector.I1],
                            topRight[Vector.I2])
                    });
                }
            }


            // Get text position
            public List<RectAndText> GetTextPosition()
            {
                return TextPosition;
            }
        }

        // Class to store text and its position
        public class RectAndText
        {
            public string Text { get; set; }
            public iTextSharp.text.Rectangle Rect { get; set; }
        }


        public static string ComparePDFFiles(string filePath1, string filePath2, string outputFilePath, int distance)
        {
            string errorList = string.Empty;
            try
            {
                using (PdfReader reader1 = new PdfReader(filePath1))
                using (PdfReader reader2 = new PdfReader(filePath2))
                using (FileStream fs = new FileStream(outputFilePath, FileMode.Create))
                using (PdfStamper stamper = new PdfStamper(reader1, fs))
                {
                    int numberOfPages1 = reader1.NumberOfPages;
                    int numberOfPages2 = reader2.NumberOfPages;

                    if (numberOfPages1 != numberOfPages2)
                    {
                        return errorList += $"\nPage numbers do not match!";
                    }

                    for (int page = 1; page <= numberOfPages1; page++)
                    {
                        // Get text elements with coordinates
                        List<TextElementFromTo> elements1 = GetTextElementsFromPage(reader1, page, distance);
                        List<TextElementFromTo> elements2 = GetTextElementsFromPage(reader2, page, distance);
                        var diff1 = elements1.Where(e1 => !elements2.Any(e2 => string.Equals(e2.Text.Trim(), e1.Text.Trim(), StringComparison.Ordinal))).ToArray();
                        var diff2 = elements2.Where(e1 => !elements1.Any(e2 => string.Equals(e2.Text.Trim(), e1.Text.Trim(), StringComparison.Ordinal))).ToArray();
                        var diff = diff1.Concat(diff2).ToArray();
                        Console.WriteLine("Check page: " + page);

                        PdfContentByte cb = stamper.GetOverContent(page);
                        foreach (var itemError in diff)
                        {
                            // Get the position and size of the wrong word
                            float xFrom = itemError.XFrom;
                            float yFrom = itemError.YFrom;
                            float xTo = itemError.XTo;
                            float yTo = itemError.YTo;

                            // Draw a line between the text in red
                            cb.SetLineWidth(0.8f);
                            cb.SetColorStroke(BaseColor.RED);
                            cb.MoveTo(xFrom - 2, yFrom + 4);
                            cb.LineTo(xTo + 5, yFrom + 4);
                            cb.Stroke();
                        }
                        foreach (var item in diff1)
                        {
                            errorList += $"\nError Page1:" + item.Text;
                        }
                        foreach (var item in diff2)
                        {
                            errorList += $"\nError Page2:" + item.Text;
                        }
                    }
                }

                return errorList;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                return errorList += ex;
            }
        }

        private static List<TextElementFromTo> GetTextElementsFromPage(PdfReader reader, int pageNumber, int distance = 8)
        {
            List<TextElement> elements = new List<TextElement>();

            // Create a strategy for parsing text with coordinates
            var strategy = new SimpleTextExtractionStrategyWithPosition();
            string text = PdfTextExtractor.GetTextFromPage(reader, pageNumber, strategy);

            // Extract text elements with coordinates
            foreach (var pos in strategy.GetTextPosition())
            {
                elements.Add(new TextElement
                {
                    Text = pos.Text,
                    X = pos.Rect.Left,
                    Y = pos.Rect.Bottom
                });
            }

            // Group text elements into clusters based on Y and X positions
            List<TextElementFromTo> clusters = new List<TextElementFromTo>();
            TextElementFromTo currentCluster = null;

            foreach (var element in elements)
            {
                //check if element text is on one line if element.Y == currentCluster.YFrom => is on one line
                if (currentCluster == null || Math.Abs(element.Y - currentCluster.YFrom) > 0.1 || element.Text == " ")
                {
                    if (currentCluster != null)
                        clusters.Add(currentCluster);

                    currentCluster = new TextElementFromTo
                    {
                        Text = element.Text,
                        XFrom = element.X,
                        YFrom = element.Y,
                        XTo = element.X,
                        YTo = element.Y,

                    };
                    if (element.Text == " ")
                    {
                        currentCluster = null;
                    }
                }
                else
                {
                    //spacing to form a paragraph of text
                    //depending on the font, set the distance to create a paragraph of text.
                    //font distance = 8
                    //VD: 数: X=580 Y = 100 , 量 X = 588 Y = 100
                    //into 数量
                    if (element.X - currentCluster.XTo <= distance)
                    {
                        currentCluster.Text += element.Text;
                        // Update X position of current cluster
                        currentCluster.XTo = element.X;
                    }
                    //same row but text spacing exceeds 8
                    //VD: 数: X=580 Y = 100, 量: X = 588 Y = 100, 台: X = 610 Y = 100
                    //into 数量 && 台
                    //because 610 - 588 > 8 
                    else
                    {
                        clusters.Add(currentCluster);
                        currentCluster = new TextElementFromTo
                        {
                            Text = element.Text,
                            XFrom = element.X,
                            YFrom = element.Y,
                            XTo = element.X,
                            YTo = element.Y
                        };
                    }
                }
            }

            // Add the last cluster
            if (currentCluster != null)
                clusters.Add(currentCluster);

            return clusters;
        }
    }
}
