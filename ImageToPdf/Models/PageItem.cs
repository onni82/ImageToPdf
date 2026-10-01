using System;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media.Imaging;

namespace ImageToPdf.Models
{
    public class PageItem : ObservableObject
    {
        private string _filePath = string.Empty;
        public string FilePath
        {
            get => _filePath;
            set
            {
                if (SetProperty(ref _filePath, value))
                {
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        /// <summary>
        /// For PDF pages this is the source PDF file. For images it's the image path.
        /// </summary>
        public string SourcePath => FilePath;

        private bool _isPdfPage;
        /// <summary>
        /// If true this item represents a page from an existing PDF.
        /// </summary>
        public bool IsPdfPage
        {
            get => _isPdfPage;
            set
            {
                if (SetProperty(ref _isPdfPage, value))
                {
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        private int _pageIndex;
        /// <summary>
        /// For PDF pages, the zero-based page index in the source PDF.
        /// </summary>
        public int PageIndex
        {
            get => _pageIndex;
            set
            {
                if (SetProperty(ref _pageIndex, value))
                {
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

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

        private BitmapSource? _thumbnail;
        public BitmapSource? Thumbnail
        {
            get => _thumbnail;
            set => SetProperty(ref _thumbnail, value);
        }
    }
}
