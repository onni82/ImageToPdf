using System;
using System.Windows.Media.Imaging;
using Patagames.Pdf.Net;
using Patagames.Pdf.Enums;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows;
using System.Drawing;

namespace ImageToPdf.Services
{
    public interface IPdfThumbnailService
    {
        BitmapSource RenderThumbnail(string pdfPath, int pageIndex, int width, int height);
    }

    public class PdfThumbnailService : IPdfThumbnailService
    {
        public BitmapSource RenderThumbnail(string pdfPath, int pageIndex, int width, int height)
        {
            if (!File.Exists(pdfPath)) return null!;

            // Initialize Pdfium (no-op if already initialized)
            PdfCommon.Initialize();

            using var doc = PdfDocument.Load(pdfPath);
            using var page = doc.Pages[pageIndex];

            // Determine render size preserving aspect ratio
            int pageW = (int)page.Width;
            int pageH = (int)page.Height;
            double scale = Math.Min((double)width / pageW, (double)height / pageH);
            int renderW = Math.Max(1, (int)(pageW * scale));
            int renderH = Math.Max(1, (int)(pageH * scale));

            using var bmp = new PdfBitmap(renderW, renderH, true);
            // Render page into the PdfBitmap
            page.Render(bmp, 0, 0, renderW, renderH, PageRotate.Normal, RenderFlags.FPDF_LCD_TEXT);

            // Get System.Drawing.Bitmap and convert to BitmapSource
            using Bitmap sysBmp = (Bitmap)bmp.GetImage();
            var hBitmap = sysBmp.GetHbitmap();
            try
            {
                var src = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(renderW, renderH));
                src.Freeze();
                return src;
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);
    }
}
