using System.Collections.ObjectModel;
using Microsoft.Win32;
using ImageToPdf.Models;
using ImageToPdf.Services;
using System.Linq;
using System.Windows;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf;
using System.Windows.Controls;
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

            // If Move buttons are not present in XAML, add them dynamically to the bottom StackPanel
            var grid = this.Content as Grid;
            if (grid != null)
            {
                var bottomPanel = grid.Children.OfType<StackPanel>().FirstOrDefault(sp => Grid.GetRow(sp) == 2);
                if (bottomPanel != null)
                {
                    var moveUp = new Button { Content = "Move Up", Width = 120, Margin = new Thickness(0, 0, 8, 0) };
                    var moveDown = new Button { Content = "Move Down", Width = 120 };
                    moveUp.Click += MoveUpButton_Click;
                    moveDown.Click += MoveDownButton_Click;
                    bottomPanel.Children.Add(moveUp);
                    bottomPanel.Children.Add(moveDown);
                }
            }
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
                try
                {
                    _pdfService.CreatePdfFromPageItems(_items, dlg.FileName);
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
            var dlg = new OpenFileDialog();
            dlg.Filter = "PDF files|*.pdf";
            if (dlg.ShowDialog(this) == true)
            {
                var path = dlg.FileName;
                // Open in Import mode to be able to read pages
                using var doc = PdfReader.Open(path, PdfDocumentOpenMode.Import);
                for (int i = 0; i < doc.PageCount; i++)
                {
                    _items.Add(new PageItem { FilePath = path, IsPdfPage = true, PageIndex = i });
                }
            }
        }

        private void MoveUpButton_Click(object? sender, RoutedEventArgs e)
        {
            var idx = ItemsListBox.SelectedIndex;
            if (idx > 0)
            {
                _items.Move(idx, idx - 1);
                ItemsListBox.SelectedIndex = idx - 1;
            }
        }

        private void MoveDownButton_Click(object? sender, RoutedEventArgs e)
        {
            var idx = ItemsListBox.SelectedIndex;
            if (idx >= 0 && idx < _items.Count - 1)
            {
                _items.Move(idx, idx + 1);
                ItemsListBox.SelectedIndex = idx + 1;
            }
        }
    }
}
