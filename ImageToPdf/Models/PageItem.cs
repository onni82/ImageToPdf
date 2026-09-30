using System;

namespace ImageToPdf.Models
{
    public class PageItem
    {
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// For PDF pages this is the source PDF file. For images it's the image path.
        /// </summary>
        public string SourcePath => FilePath;

        /// <summary>
        /// If true this item represents a page from an existing PDF.
        /// </summary>
        public bool IsPdfPage { get; set; }

        /// <summary>
        /// For PDF pages, the zero-based page index in the source PDF.
        /// </summary>
        public int PageIndex { get; set; }

        public string DisplayName
        {
            get
            {
                var name = System.IO.Path.GetFileName(FilePath);
                if (IsPdfPage)
                {
                    return $"{name} - page {PageIndex + 1}";
                }
                return name;
            }
        }
    }
}
