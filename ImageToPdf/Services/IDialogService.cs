using System.Collections.Generic;

namespace ImageToPdf.Services
{
    public interface IDialogService
    {
        string[]? OpenImageFiles(bool multiSelect);
        string? SavePdfFile(string defaultName);
        string? OpenPdfFile();
    }
}
