using System.Collections.ObjectModel;
using Microsoft.Win32;
using ImageToPdf.Models;
using ImageToPdf.Services;
using System.Linq;
using System.Windows;

namespace ImageToPdf.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<PageItem> _items = new();
        private readonly PdfService _pdfService = new();

        public MainWindow()
        {
            InitializeComponent();
            ItemsListBox.ItemsSource = _items;

            AddImagesButton.Click += AddImagesButton_Click;
            RemoveButton.Click += RemoveButton_Click;
            ExportButton.Click += ExportButton_Click;
            OpenPdfButton.Click += OpenPdfButton_Click;
        }

        private void AddImagesButton_Click(object? sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Multiselect = true;
            dlg.Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files|*.*";

            if (dlg.ShowDialog(this) == true)
            {
                foreach (var file in dlg.FileNames)
                {
                    _items.Add(new PageItem { FilePath = file, IsPdfPage = false });
                }
            }
        }

        private void RemoveButton_Click(object? sender, RoutedEventArgs e)
        {
            if (ItemsListBox.SelectedItem is PageItem pi)
            {
                _items.Remove(pi);
            }
        }

        private void ExportButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_items.Count == 0)
            {
                MessageBox.Show(this, "No items to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new SaveFileDialog();
            dlg.Filter = "PDF file|*.pdf";
            dlg.FileName = "output.pdf";
            if (dlg.ShowDialog(this) == true)
            {
                var imagePaths = _items.Where(i => !i.IsPdfPage).Select(i => i.FilePath).ToList();

                try
                {
                    _pdfService.CreatePdfFromImagePaths(imagePaths, dlg.FileName);
                    MessageBox.Show(this, "PDF exported.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(this, "Error exporting PDF: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OpenPdfButton_Click(object? sender, RoutedEventArgs e)
        {
            MessageBox.Show(this, "Open PDF / add pages is not implemented yet.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
