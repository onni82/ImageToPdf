using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Media;

namespace ImageToPdf.Views
{
    public class ImagePathToThumbnailConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var path = value as string;
                if (string.IsNullOrEmpty(path))
                    return null;

                // Only create thumbnails for common image extensions
                var ext = Path.GetExtension(path)?.ToLowerInvariant();
                if (ext is null)
                    return null;

                if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".gif")
                {
                    var bi = new BitmapImage();
                    bi.BeginInit();
                    bi.CacheOption = BitmapCacheOption.OnLoad; // release file handle
                    bi.UriSource = new Uri(path, UriKind.Absolute);
                    bi.DecodePixelWidth = 120;
                    bi.EndInit();
                    bi.Freeze();
                    return bi;
                }

                // Basic PDF placeholder thumbnail: render a small visual with "PDF" text
                if (ext == ".pdf")
                {
                    var vb = new DrawingVisual();
                    using (var dc = vb.RenderOpen())
                    {
                        dc.DrawRectangle(Brushes.LightGray, null, new Rect(0, 0, 120, 90));
                        var ft = new FormattedText("PDF",
                            System.Globalization.CultureInfo.InvariantCulture,
                            FlowDirection.LeftToRight,
                            new Typeface("Segoe UI"),
                            24, Brushes.Black, VisualTreeHelper.GetDpi(vb).PixelsPerDip);
                        dc.DrawText(ft, new Point(10, 30));
                    }
                    var rtb = new RenderTargetBitmap(120, 90, 96, 96, PixelFormats.Pbgra32);
                    rtb.Render(vb);
                    rtb.Freeze();
                    return rtb;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
