using System.Collections.ObjectModel;
using ImageToPdf.Models;

namespace ImageToPdf.ViewModels
{
    public class MainViewModel
    {
        public ObservableCollection<PageItem> Items { get; } = new();

        public MainViewModel()
        {
            // ViewModel stub - move logic from code-behind here later
        }
    }
}
