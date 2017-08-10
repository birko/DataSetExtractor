using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DataSetExtractor
{
    /// <summary>
    /// Interaction logic for OutputWindow.xaml
    /// </summary>
    public partial class OutputWindow : Window
    {
        private string _text = null;
        public OutputWindow(string text)
        {
            InitializeComponent();
            _text = text;
            RefreshTextBox();
        }

        private void RefreshTextBox()
        {
            textBoxKeyOutput.Text = _text;
        }

        private void buttonRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshTextBox();
        }

        private void buttonCopy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(_text);
        }
    }
}
