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
    /// Interaction logic for InputWindow.xaml
    /// </summary>
    public partial class InputWindow : Window
    {
        public string Value
        {
            get
            {
                return textBoxValue.Text;
            }
            set
            {
                textBoxValue.Text = value;
            }
        }

        public HorizontalAlignment Aligment
        {
            get
            {
                return textBoxValue.HorizontalContentAlignment;
            }
            set
            {
                textBoxValue.HorizontalContentAlignment = value;
            }
        }
        public InputWindow()
        {
            InitializeComponent();
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
