using DataSetExtractor.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
    /// Interaction logic for FileWindow.xaml
    /// </summary>
    public partial class FileWindow : Window
    {
        public FileSetting FileSetting = null;
        public FileWindow(FileSetting fileSetting)
        {
            InitializeComponent();
            FileSetting = fileSetting ?? new FileSetting();
            if (FileSetting.Output == null)
            {
                FileSetting.Output = new List<OutputColumn>();
            }
            InitUI();
        }

        private void InitUI()
        {
            comboBoxKeyColumn.SelectedIndex = FileSetting.KeyColumn.SourceNumber;
            checkBoxFullRow.IsChecked = FileSetting.FullRow;
            textBoxColumnNumber.Text = FileSetting.Output.Count.ToString();
            RefreshGrid();
            ReadFirstRow();
        }

        private void ReadFirstRow()
        {
            string[] row = null;
            if (FileSetting.Type == FileType.Zip)
            {
                using (var zip = new ZipArchive(File.OpenRead(FileSetting.Source), ZipArchiveMode.Read))
                {
                    var entry = zip.GetEntry(FileSetting.FileName);
                    if (entry != null)
                    {
                        row = GetRow(new StreamReader(entry.Open()));
                    }
                }
            }
            else
            {
                row = GetRow(new StreamReader(File.OpenRead(FileSetting.Source)));
            }
            comboBoxKeyColumn.Items.Clear();
            comboBoxColumn.Items.Clear();
            if (row != null && row.Any())
            {
                for (int i = 0; i < row.Length; i++)
                {
                    comboBoxKeyColumn.Items.Add(string.Format("{0:####} - {1}", i + 1, row[i]));
                    comboBoxColumn.Items.Add(string.Format("{0:####} - {1}", i + 1, row[i]));
                }
            }
        }

        private static string[] GetRow(StreamReader reader)
        {
            var parser = new Tools.CsvParser(reader, ';');
            foreach (var splitLine in parser.Parse())
            {
                if (splitLine != null && splitLine.Count > 0)
                {
                    return splitLine.ToArray();
                }
            }
            return new string [0];
        }

        private void RefreshGrid()
        {
            dataGridColumns.ItemsSource = FileSetting.Output.OrderBy(x=>x.Number).ToArray();
            dataGridColumns.Items.Refresh();
        }

        private void buttonAddColumn_Click(object sender, RoutedEventArgs e)
        {
            if (comboBoxColumn.SelectedIndex >= 0 && int.TryParse(textBoxColumnNumber.Text, out int number))
            {
                if (number > FileSetting.Output.Count)
                {
                    number = FileSetting.Output.Count;
                }

                var item = new  OutputColumn();
                if (FileSetting.Output.Any(x => x.Number == number))
                {
                    item = FileSetting.Output.First(x => x.Number == number);
                }
                else
                {
                    FileSetting.Output.Add(item);
                }

                item.IsEmptyTest = checkBoxtestEmpty.IsChecked == true;
                item.SourceNumber = comboBoxColumn.SelectedIndex;
                item.SourceName = GetSourceName();
                item.Name = textBoxColumnName.Text;
                item.Number = number;

                RefreshGrid();
                checkBoxFullRow.IsChecked = false;
                checkBoxtestEmpty.IsChecked = false;
                comboBoxColumn.SelectedIndex = -1;
                textBoxColumnName.Text = string.Empty;
                textBoxColumnNumber.Text = FileSetting.Output.Count.ToString();
            }
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            if (comboBoxKeyColumn.SelectedIndex >=0)
            {
                FileSetting.KeyColumn.SourceNumber = comboBoxKeyColumn.SelectedIndex;
                FileSetting.FullRow = checkBoxFullRow.IsChecked == true;
                DialogResult = true;
                Close();
            }
        }

        private void dataGridColumns_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
        }

        private void comboBoxColumn_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            textBoxColumnName.Text = GetSourceName();
        }

        private string GetSourceName()
        {
            if (comboBoxColumn.SelectedIndex >= 0)
            {
                var value = (string)comboBoxColumn.SelectedValue;
                if (!string.IsNullOrEmpty(value))
                {
                    var fisrtindex = value.IndexOf(" - ");
                    return value.Substring(fisrtindex + 3);
                }
            }
            return null;
        }

        private void dataGridColumns_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var grid = (DataGrid)sender;
            if (grid.SelectedIndex >= 0)
            {
                var item = (OutputColumn)grid.SelectedItem;
                checkBoxtestEmpty.IsChecked = item.IsEmptyTest;
                comboBoxColumn.SelectedIndex = item.SourceNumber;
                textBoxColumnName.Text = item.Name;
                textBoxColumnNumber.Text = item.Number.ToString();
            }
        }

        private void dataGridColumns_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var grid = (DataGrid)sender;
            if (Key.Delete == e.Key)
            {
                var index = grid.SelectedIndex;
                if (index >= 0 && index < FileSetting.Output.Count)
                {
                    FileSetting.Output.RemoveAt(index);
                }
                checkBoxFullRow.IsChecked = (FileSetting.Output.Count == 0);
                RefreshGrid();
            }

        }
    }
}
