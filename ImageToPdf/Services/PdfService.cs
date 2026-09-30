using System.Collections.Generic;
using System;
using System.Linq;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using PdfSharp;
using PdfSharp.Pdf.IO;
using ImageToPdf.Models;

namespace ImageToPdf.Services
{
    public class PdfService
    {
        /// <summary>
        /// Creates a PDF from a mixed sequence of PageItems. Items representing
        /// existing PDF pages will be imported; image items will be added as new pages.
        /// </summary>
        public void CreatePdfFromPageItems(IEnumerable<PageItem> items, string outputFilePath)
        {
            using var output = new PdfDocument();

            // Cache opened source PDFs so we don't reopen the same file repeatedly
            var opened = new Dictionary<string, PdfDocument>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in items)
            {
                if (item.IsPdfPage)
                {
                    if (!opened.TryGetValue(item.FilePath, out var srcDoc))
                    {
                        srcDoc = PdfReader.Open(item.FilePath, PdfDocumentOpenMode.Import);
                        opened[item.FilePath] = srcDoc;
                    }

                    var srcPage = srcDoc.Pages[item.PageIndex];
                    // AddPage copies the page into the target document when source opened in Import mode
                    output.AddPage(srcPage);
                }
                else
                {
                    using var xImage = XImage.FromFile(item.FilePath);
                    var page = output.AddPage();
                    page.Width = XUnit.FromPoint(xImage.PointWidth);
                    page.Height = XUnit.FromPoint(xImage.PointHeight);

                    using var gfx = XGraphics.FromPdfPage(page);
                    gfx.DrawImage(xImage, 0, 0, page.Width.Point, page.Height.Point);
                }
            }

            output.Save(outputFilePath);

            // Dispose opened source documents
            foreach (var kv in opened)
            {
                kv.Value.Dispose();
            }
        }

        // Backwards-compatible helper for simple image list
        public void CreatePdfFromImagePaths(IEnumerable<string> imagePaths, string outputFilePath)
        {
            var items = imagePaths.Select(p => new PageItem { FilePath = p, IsPdfPage = false });
            CreatePdfFromPageItems(items, outputFilePath);
        }
    }
}
