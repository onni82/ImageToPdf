using System.Collections.ObjectModel;
using Microsoft.Win32;
using ImageToPdf.Models;
using ImageToPdf.Services;
using System.Linq;
using System.Windows;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf;
using System.Windows.Controls;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System;

namespace ImageToPdf.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<PageItem> _items = new();
        private readonly PdfService _pdfService = new();
        private readonly PdfThumbnailService _thumbService = new();
        private System.Windows.Point _dragStartPoint;

        public MainWindow()
        {
            InitializeComponent();
            ItemsListBox.ItemsSource = _items;

            AddImagesButton.Click += AddImagesButton_Click;
            RemoveButton.Click += RemoveButton_Click;
            ExportButton.Click += ExportButton_Click;
            OpenPdfButton.Click += OpenPdfButton_Click;

            // InsertImages button: if present in XAML attach, otherwise add dynamically to top panel
            var grid = this.Content as Grid;
            if (grid != null)
            {
                var topPanel = grid.Children.OfType<StackPanel>().FirstOrDefault(sp => Grid.GetRow(sp) == 0);
                if (topPanel != null)
                {
                    // If InsertImagesButton is defined in XAML it will already be wired; otherwise add it
                    if (this.FindName("InsertImagesButton") is Button existingInsert)
                    {
                        existingInsert.Click += InsertImagesButton_Click;
                    }
                    else
                    {
                        var insertBtn = new Button { Name = "InsertImagesButton", Content = "Insert Images...", Width = 140, Margin = new Thickness(0, 0, 8, 0) };
                        insertBtn.Click += InsertImagesButton_Click;
                        topPanel.Children.Insert(1, insertBtn);
                    }

                    // Add Move Up / Move Down buttons to bottom panel if not present
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

                // Create a simple ItemTemplate with thumbnail + name in code so we don't need to modify XAML further
                var factory = new System.Windows.FrameworkElementFactory(typeof(System.Windows.Controls.StackPanel));
                factory.SetValue(System.Windows.Controls.StackPanel.OrientationProperty, System.Windows.Controls.Orientation.Horizontal);
                factory.SetValue(System.Windows.FrameworkElement.MarginProperty, new Thickness(4));

                var imgFactory = new System.Windows.FrameworkElementFactory(typeof(System.Windows.Controls.Image));
                imgFactory.SetValue(System.Windows.FrameworkElement.WidthProperty, 120.0);
                imgFactory.SetValue(System.Windows.FrameworkElement.HeightProperty, 90.0);
                imgFactory.SetValue(System.Windows.Controls.Image.StretchProperty, System.Windows.Media.Stretch.Uniform);
                var imgBinding = new System.Windows.Data.Binding("Thumbnail");
                imgFactory.SetBinding(System.Windows.Controls.Image.SourceProperty, imgBinding);
                factory.AppendChild(imgFactory);

                var txtFactory = new System.Windows.FrameworkElementFactory(typeof(System.Windows.Controls.TextBlock));
                txtFactory.SetValue(System.Windows.FrameworkElement.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Center);
                txtFactory.SetValue(System.Windows.FrameworkElement.MarginProperty, new Thickness(8, 0, 0, 0));
                txtFactory.SetBinding(System.Windows.Controls.TextBlock.TextProperty, new System.Windows.Data.Binding("DisplayName"));
                factory.AppendChild(txtFactory);

                var dataTemplate = new System.Windows.DataTemplate { VisualTree = factory };
                ItemsListBox.ItemTemplate = dataTemplate;

                // Enable drag & drop handlers
                ItemsListBox.AllowDrop = true;
                ItemsListBox.PreviewMouseLeftButtonDown += ItemsListBox_PreviewMouseLeftButtonDown;
                ItemsListBox.MouseMove += ItemsListBox_MouseMove;
                ItemsListBox.Drop += ItemsListBox_Drop;
                ItemsListBox.DragOver += ItemsListBox_DragOver;
                ItemsListBox.ContextMenuOpening += ItemsListBox_ContextMenuOpening;
            }
        }

        private void AddImagesButton_Click(object? sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Multiselect = true;
            dlg.Filter = "Image files|*.png;*.jpg;*.jpeg;*.gif;*.tif;*.tiff|All files|*.*";

            if (dlg.ShowDialog(this) == true)
            {
                foreach (var file in dlg.FileNames)
                {
                    var item = new PageItem { FilePath = file, IsPdfPage = false };
                    _items.Add(item);
                    // generate thumbnail asynchronously
                    GenerateImageThumbnailAsync(item);
                }
            }
        }

        private void InsertImagesButton_Click(object? sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Multiselect = true;
            dlg.Filter = "Image files|*.png;*.jpg;*.jpeg;*.gif;*.tif;*.tiff|All files|*.*";

            if (dlg.ShowDialog(this) == true)
            {
                var insertIndex = ItemsListBox.SelectedIndex >= 0 ? ItemsListBox.SelectedIndex + 1 : _items.Count;
                foreach (var file in dlg.FileNames)
                {
                    var item = new PageItem { FilePath = file, IsPdfPage = false };
                    _items.Insert(insertIndex, item);
                    GenerateImageThumbnailAsync(item);
                    insertIndex++;
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
                    var item = new PageItem { FilePath = path, IsPdfPage = true, PageIndex = i };
                    _items.Add(item);
                    GeneratePdfThumbnailAsync(item);
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

        private void ItemsListBox_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void ItemsListBox_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton != System.Windows.Input.MouseButtonState.Pressed)
                return;

            var pos = e.GetPosition(null);
            var diff = _dragStartPoint - pos;
            if (System.Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance || System.Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                if (ItemsListBox.SelectedItem is PageItem selected)
                {
                    var data = new DataObject("PageItem", selected);
                    DragDrop.DoDragDrop(ItemsListBox, data, DragDropEffects.Move);
                }
            }
        }

        private void ItemsListBox_DragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("PageItem"))
                e.Effects = DragDropEffects.None;
            else
                e.Effects = DragDropEffects.Move;

            e.Handled = true;
        }

        private void ItemsListBox_Drop(object sender, DragEventArgs e)
        {
            // Handle external file drops (from Explorer)
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    var point = e.GetPosition(ItemsListBox);
                    int index = GetCurrentIndex(point);
                    if (index < 0) index = _items.Count;

                    // Insert files at index preserving order
                    foreach (var f in files)
                    {
                        // If it's a PDF, import pages, otherwise add as image
                        var ext = System.IO.Path.GetExtension(f)?.ToLowerInvariant();
                        if (ext == ".pdf")
                        {
                            // import pages at index
                            using var doc = PdfReader.Open(f, PdfDocumentOpenMode.Import);
                            for (int i = 0; i < doc.PageCount; i++)
                            {
                                var item = new PageItem { FilePath = f, IsPdfPage = true, PageIndex = i };
                                _items.Insert(index++, item);
                                GeneratePdfThumbnailAsync(item);
                            }
                        }
                        else
                        {
                            var item = new PageItem { FilePath = f, IsPdfPage = false };
                            _items.Insert(index++, item);
                            GenerateImageThumbnailAsync(item);
                        }
                    }
                }

                e.Handled = true;
                return;
            }

            // Internal reordering via PageItem drag
            if (!e.Data.GetDataPresent("PageItem"))
                return;

            var droppedData = e.Data.GetData("PageItem") as PageItem;
            if (droppedData == null)
                return;

            // Find target index
            var pt = e.GetPosition(ItemsListBox);
            int tgtIndex = GetCurrentIndex(pt);
            if (tgtIndex < 0)
                tgtIndex = _items.Count - 1;

            var oldIndex = _items.IndexOf(droppedData);
            if (oldIndex < 0)
                return;

            if (oldIndex < tgtIndex) tgtIndex--;

            _items.Move(oldIndex, tgtIndex);
            ItemsListBox.SelectedItem = droppedData;
        }

        private void ItemsListBox_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // Build a context menu for the item under the mouse
            var item = ItemsListBox.SelectedItem as PageItem;
            if (item == null)
            {
                e.Handled = true;
                return;
            }

            var cm = new ContextMenu();

            var miInsertBefore = new MenuItem { Header = "Insert Before..." };
            miInsertBefore.Click += (s, ea) => Context_InsertBefore_Click(item);
            cm.Items.Add(miInsertBefore);

            var miInsertAfter = new MenuItem { Header = "Insert After..." };
            miInsertAfter.Click += (s, ea) => Context_InsertAfter_Click(item);
            cm.Items.Add(miInsertAfter);

            cm.Items.Add(new Separator());

            var miDup = new MenuItem { Header = "Duplicate" };
            miDup.Click += (s, ea) => Context_Duplicate_Click(item);
            cm.Items.Add(miDup);

            var miExport = new MenuItem { Header = "Export Page..." };
            miExport.Click += (s, ea) => Context_ExportSingle_Click(item);
            cm.Items.Add(miExport);

            var miExportImg = new MenuItem { Header = "Export Page as Image..." };
            // Audit: keep explicit click wiring for export-as-image menu item.
            miExportImg.Click += (s, ea) => Context_ExportAsImage_Click(item);
            cm.Items.Add(miExportImg);

            cm.Items.Add(new Separator());

            var miRemove = new MenuItem { Header = "Remove" };
            miRemove.Click += (s, ea) => Context_Remove_Click(item);
            cm.Items.Add(miRemove);

            ItemsListBox.ContextMenu = cm;
        }

        private void Context_InsertBefore_Click(PageItem item)
        {
                var dlg = new OpenFileDialog();
                dlg.Multiselect = true;
                dlg.Filter = "Image files|*.png;*.jpg;*.jpeg;*.gif;*.tif;*.tiff|All files|*.*";
            if (dlg.ShowDialog(this) == true)
            {
                var idx = _items.IndexOf(item);
                if (idx < 0) idx = _items.Count;
                foreach (var f in dlg.FileNames)
                {
                    var newItem = new PageItem { FilePath = f, IsPdfPage = false };
                    _items.Insert(idx++, newItem);
                    GenerateImageThumbnailAsync(newItem);
                }
            }
        }

        private void Context_InsertAfter_Click(PageItem item)
        {
            var dlg = new OpenFileDialog();
            dlg.Multiselect = true;
            dlg.Filter = "Image files|*.png;*.jpg;*.jpeg;*.gif;*.tif;*.tiff|All files|*.*";
            if (dlg.ShowDialog(this) == true)
            {
                var idx = _items.IndexOf(item);
                if (idx < 0) idx = _items.Count - 1;
                idx++;
                foreach (var f in dlg.FileNames)
                {
                    var newItem = new PageItem { FilePath = f, IsPdfPage = false };
                    _items.Insert(idx++, newItem);
                    GenerateImageThumbnailAsync(newItem);
                }
            }
        }

        private void Context_Duplicate_Click(PageItem item)
        {
            var idx = _items.IndexOf(item);
            if (idx >= 0)
            {
                var copy = new PageItem { FilePath = item.FilePath, IsPdfPage = item.IsPdfPage, PageIndex = item.PageIndex };
                // copy thumbnail reference if available
                copy.Thumbnail = item.Thumbnail;
                _items.Insert(idx + 1, copy);
            }
        }

        private void Context_ExportAsImage_Click(PageItem item)
        {
            var dlg = new SaveFileDialog();
            dlg.Filter = "PNG Image|*.png|JPEG Image|*.jpg;*.jpeg|Bitmap Image|*.bmp";
            var defaultName = System.IO.Path.GetFileNameWithoutExtension(item.FilePath);
            if (item.IsPdfPage)
                defaultName += $"_page{item.PageIndex + 1}";
            dlg.FileName = defaultName + ".png";

            if (dlg.ShowDialog(this) == true)
            {
                var filename = dlg.FileName;
                var ext = System.IO.Path.GetExtension(filename).ToLowerInvariant();

                try
                {
                    BitmapSource? bmpSrc = null;
                    if (item.IsPdfPage)
                    {
                        // Render at 300 DPI for good quality
                        bmpSrc = _thumbService.RenderPageAtDpi(item.FilePath, item.PageIndex, 300);
                    }
                    else
                    {
                        var bi = new BitmapImage();
                        bi.BeginInit();
                        bi.CacheOption = BitmapCacheOption.OnLoad;
                        bi.UriSource = new Uri(item.FilePath, UriKind.Absolute);
                        bi.EndInit();
                        bi.Freeze();
                        bmpSrc = bi;
                    }

                    if (bmpSrc == null)
                    {
                        MessageBox.Show(this, "Failed to render image.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    BitmapEncoder? encoder = null;
                    if (ext == ".jpg" || ext == ".jpeg")
                    {
                        encoder = new JpegBitmapEncoder { QualityLevel = 90 };
                    }
                    else if (ext == ".gif")
                    {
                        encoder = new GifBitmapEncoder();
                    }
                    else if (ext == ".tif" || ext == ".tiff")
                    {
                        // Ask user for TIFF compression options
                        if (!ShowTiffOptionsDialog(out var comp))
                        {
                            // user cancelled
                            return;
                        }
                        var tiff = new TiffBitmapEncoder();
                        tiff.Compression = comp;
                        encoder = tiff;
                    }
                    else
                    {
                        encoder = new PngBitmapEncoder();
                    }

                    encoder.Frames.Add(BitmapFrame.Create(bmpSrc));
                    using var outputStream = System.IO.File.OpenWrite(filename);
                    encoder.Save(outputStream);

                    MessageBox.Show(this, "Image exported.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(this, "Error exporting image: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool ShowTiffOptionsDialog(out TiffCompressOption compression)
        {
            var dlg = new TiffOptionsWindow { Owner = this };
            var result = dlg.ShowDialog();
            if (result != true)
            {
                compression = TiffCompressOption.None;
                return false;
            }

            var sel = dlg.SelectedOption;
            compression = sel switch
            {
                "LZW" => TiffCompressOption.Lzw,
                "CCITT3" => TiffCompressOption.Ccitt3,
                "CCITT4" => TiffCompressOption.Ccitt4,
                _ => TiffCompressOption.None,
            };
            return true;
        }

        private void Context_ExportSingle_Click(PageItem item)
        {
            var dlg = new SaveFileDialog();
            dlg.Filter = "PDF file|*.pdf";
            dlg.FileName = System.IO.Path.GetFileNameWithoutExtension(item.FilePath) + "_page" + (item.PageIndex + 1) + ".pdf";
            if (dlg.ShowDialog(this) == true)
            {
                if (item.IsPdfPage)
                {
                    using var outDoc = new PdfDocument();
                    using var src = PdfReader.Open(item.FilePath, PdfDocumentOpenMode.Import);
                    outDoc.AddPage(src.Pages[item.PageIndex]);
                    outDoc.Save(dlg.FileName);
                }
                else
                {
                    _pdfService.CreatePdfFromImagePaths(new[] { item.FilePath }, dlg.FileName);
                }
            }
        }

        private void GenerateImageThumbnailAsync(PageItem item)
        {
            Task.Run(() =>
            {
                try
                {
                    var bi = new BitmapImage();
                    bi.BeginInit();
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.UriSource = new Uri(item.FilePath, UriKind.Absolute);
                    bi.DecodePixelWidth = 120;
                    bi.EndInit();
                    bi.Freeze();
                    Dispatcher.Invoke(() => item.Thumbnail = bi);
                }
                catch
                {
                    // ignore thumbnail errors
                }
            });
        }

        private void GeneratePdfThumbnailAsync(PageItem item)
        {
            Task.Run(() =>
            {
                try
                {
                    var bmp = _thumbService.RenderThumbnail(item.FilePath, item.PageIndex, 120, 90);
                    if (bmp != null)
                        Dispatcher.Invoke(() => item.Thumbnail = bmp);
                }
                catch
                {
                    // ignore
                }
            });
        }

        private void Context_Remove_Click(PageItem item)
        {
            _items.Remove(item);
        }

        private int GetCurrentIndex(System.Windows.Point point)
        {
            for (int i = 0; i < ItemsListBox.Items.Count; i++)
            {
                var item = ItemsListBox.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                if (item == null) continue;
                var bounds = VisualTreeHelper.GetDescendantBounds(item);
                var topLeft = item.TranslatePoint(new System.Windows.Point(), ItemsListBox);
                var rect = new System.Windows.Rect(topLeft, bounds.Size);
                if (rect.Contains(point))
                    return i;
            }
            return -1;
        }
    }
}
