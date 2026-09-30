using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageToPdf.Models;
using ImageToPdf.Services;
using System.Collections.Generic;
using System.Linq;

namespace ImageToPdf.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly PdfService _pdfService;
        private readonly IDialogService _dialogService;

        public ObservableCollection<PageItem> Items { get; } = new();

        public IRelayCommand<PageItem?> RemoveCommand { get; }
        public IRelayCommand<PageItem?> MoveUpCommand { get; }
        public IRelayCommand<PageItem?> MoveDownCommand { get; }
        public IRelayCommand<PageItem?> DuplicateCommand { get; }

        public IRelayCommand AddImagesCommand { get; }
        public IRelayCommand InsertImagesCommand { get; }
        public IRelayCommand OpenPdfCommand { get; }
        public IRelayCommand ExportAllCommand { get; }

        public MainViewModel(PdfService pdfService, IDialogService dialogService)
        {
            _pdfService = pdfService;
            _dialogService = dialogService;

            RemoveCommand = new RelayCommand<PageItem?>(RemoveItem);
            MoveUpCommand = new RelayCommand<PageItem?>(MoveUp);
            MoveDownCommand = new RelayCommand<PageItem?>(MoveDown);
            DuplicateCommand = new RelayCommand<PageItem?>(Duplicate);

            AddImagesCommand = new RelayCommand(AddImages);
            InsertImagesCommand = new RelayCommand(InsertImages);
            OpenPdfCommand = new RelayCommand(OpenPdf);
            ExportAllCommand = new RelayCommand(ExportAllCommandImpl);
        }

        public void AddItems(IEnumerable<string> paths, int? insertIndex = null)
        {
            if (paths == null) return;
            int index = insertIndex ?? Items.Count;
            foreach (var p in paths)
            {
                Items.Insert(index, new PageItem { FilePath = p, IsPdfPage = false });
                index++;
            }
        }

        private void AddImages()
        {
            var files = _dialogService.OpenImageFiles(true);
            if (files != null) AddItems(files);
        }

        private void InsertImages()
        {
            var files = _dialogService.OpenImageFiles(true);
            if (files == null) return;
            // insert after selected index - selection handling will be in view (set SelectedIndex)
            // find first selected index
            var selected = Items.FirstOrDefault(); // placeholder: view should call AddItems with index when inserting
            AddItems(files);
        }

        private void OpenPdf()
        {
            var path = _dialogService.OpenPdfFile();
            if (!string.IsNullOrEmpty(path)) ImportPdf(path);
        }

        private void ExportAllCommandImpl()
        {
            var outPath = _dialogService.SavePdfFile("output.pdf");
            if (!string.IsNullOrEmpty(outPath)) ExportAll(outPath);
        }

        public void ImportPdf(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            using var doc = PdfSharp.Pdf.IO.PdfReader.Open(path, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
            for (int i = 0; i < doc.PageCount; i++)
            {
                Items.Add(new PageItem { FilePath = path, IsPdfPage = true, PageIndex = i });
            }
        }

        public void RemoveItem(PageItem? item)
        {
            if (item == null) return;
            Items.Remove(item);
        }

        public void MoveUp(PageItem? item)
        {
            if (item == null) return;
            var idx = Items.IndexOf(item);
            if (idx > 0) Items.Move(idx, idx - 1);
        }

        public void MoveDown(PageItem? item)
        {
            if (item == null) return;
            var idx = Items.IndexOf(item);
            if (idx >= 0 && idx < Items.Count - 1) Items.Move(idx, idx + 1);
        }

        public void Duplicate(PageItem? item)
        {
            if (item == null) return;
            var idx = Items.IndexOf(item);
            if (idx >= 0)
            {
                var copy = new PageItem { FilePath = item.FilePath, IsPdfPage = item.IsPdfPage, PageIndex = item.PageIndex };
                Items.Insert(idx + 1, copy);
            }
        }

        public void ExportSingle(PageItem item, string outputPath)
        {
            if (item == null || string.IsNullOrEmpty(outputPath)) return;

            // Export a single item to a PDF
            if (item.IsPdfPage)
            {
                // Create a document containing only that page
                using var outDoc = new PdfSharp.Pdf.PdfDocument();
                using var src = PdfSharp.Pdf.IO.PdfReader.Open(item.FilePath, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
                outDoc.AddPage(src.Pages[item.PageIndex]);
                outDoc.Save(outputPath);
            }
            else
            {
                _pdfService.CreatePdfFromImagePaths(new[] { item.FilePath }, outputPath);
            }
        }

        public void ExportAll(string outputPath)
        {
            if (string.IsNullOrEmpty(outputPath)) return;
            _pdfService.CreatePdfFromPageItems(Items.ToList(), outputPath);
        }
    }
}

