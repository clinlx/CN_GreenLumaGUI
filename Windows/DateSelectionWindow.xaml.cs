using CN_GreenLumaGUI.ViewModels;
using System.Windows;

namespace CN_GreenLumaGUI.Windows
{
    public partial class DateSelectionWindow : Window
    {
        public DateSelectionWindow()
        {
            InitializeComponent();
            this.DataContext = new DateSelectionViewModel(this);
        }
    }
}
