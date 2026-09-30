using System.Collections.Generic;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using PdfSharp;

namespace ImageToPdf.Services
{
    public class PdfService
    {
        /// <summary>
        /// Creates a PDF where each image in imagePaths becomes a single PDF page.
        /// </summary>
        public void CreatePdfFromImagePaths(IEnumerable<string> imagePaths, string outputFilePath)
        {
            using var document = new PdfDocument();

            foreach (var path in imagePaths)
            {
                using var xImage = XImage.FromFile(path);

                var page = document.AddPage();
                // Size the PDF page to the image size in points
                page.Width = XUnit.FromPoint(xImage.PointWidth);
                page.Height = XUnit.FromPoint(xImage.PointHeight);

                using var gfx = XGraphics.FromPdfPage(page);
                // Use explicit Point values to avoid obsolete implicit conversion to double
                gfx.DrawImage(xImage, 0, 0, page.Width.Point, page.Height.Point);
            }

            document.Save(outputFilePath);
        }
    }
}
