using System;

namespace ImageToPdf.Models
{
    public class PageItem
    {
        public string FilePath { get; set; } = string.Empty;

        public string DisplayName => System.IO.Path.GetFileName(FilePath);

        public bool IsPdfPage { get; set; }
    }
}
