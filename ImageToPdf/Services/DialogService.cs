using Microsoft.Win32;
using System;

namespace ImageToPdf.Services
{
    public class DialogService : IDialogService
    {
        public string[]? OpenImageFiles(bool multiSelect)
        {
            var dlg = new OpenFileDialog();
            dlg.Multiselect = multiSelect;
            dlg.Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files|*.*";
            var ok = dlg.ShowDialog();
            return ok == true ? dlg.FileNames : null;
        }

        public string? SavePdfFile(string defaultName)
        {
            var dlg = new SaveFileDialog();
            dlg.Filter = "PDF file|*.pdf";
            dlg.FileName = defaultName;
            var ok = dlg.ShowDialog();
            return ok == true ? dlg.FileName : null;
        }

        public string? OpenPdfFile()
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "PDF files|*.pdf";
            var ok = dlg.ShowDialog();
            return ok == true ? dlg.FileName : null;
        }
    }
}
