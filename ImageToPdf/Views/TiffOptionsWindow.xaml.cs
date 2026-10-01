using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ImageToPdf.Views
{
    public partial class TiffOptionsWindow : Window
    {
        public TiffOptionsWindow()
        {
            InitializeComponent();
            btnOk.Click += BtnOk_Click;
        }

        private void BtnOk_Click(object? sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        public string SelectedOption
        {
            get
            {
                if (rbLzw.IsChecked == true) return "LZW";
                if (rbCcitt3.IsChecked == true) return "CCITT3";
                if (rbCcitt4.IsChecked == true) return "CCITT4";
                return "None";
            }
        }
    }
}
