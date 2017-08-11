using DataSetExtractor.Model;
using Newtonsoft.Json;
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
        public List<string> ColumnNames = null;
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
            checkBoxFullRow.IsChecked = FileSetting.FullRow;
            textBoxColumnNumber.Text = FileSetting.Output.Count.ToString();
            RefreshGrid();
            ReadFirstRow();
        }

        private string GetExcelColumnName(int columnNumber)
        {
            int dividend = columnNumber;
            string columnName = String.Empty;
            int modulo;
            int positionA = 'A';
            int AlphabetLength = 'Z' - positionA + 1;
            while (dividend > 0)
            {
                modulo = (dividend - 1) % AlphabetLength;
                columnName = Convert.ToChar(positionA + modulo).ToString() + columnName;
                dividend = (dividend - modulo) / AlphabetLength;
            }

            return columnName;
        }

        private void ReadFirstRow(bool force = false)
        {
            if (FileSetting != null)
            {
                if (force || ColumnNames == null || !ColumnNames.Any())
                {
                    if (force || ColumnNames == null)
                    {
                        ColumnNames = new List<string>();
                    }
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
                    ColumnNames.AddRange(row);
                }
                comboBoxKeyColumn.Items.Clear();
                comboBoxColumn.Items.Clear();
                if (ColumnNames != null && ColumnNames.Any())
                {
                    bool excelIndex = checkBoxExcelIndex.IsChecked == true;
                    for (int i = 0; i < ColumnNames.Count; i++)
                    {
                        string columnIndex = (excelIndex) ? GetExcelColumnName(i + 1).PadRight(5) : (i + 1).ToString().PadRight(5);
                        string columnName = (!string.IsNullOrEmpty(ColumnNames[i])) ? ColumnNames[i] : "NO NAME";
                        comboBoxKeyColumn.Items.Add(string.Format("{0} - {1}", columnIndex, ColumnNames[i]));
                        comboBoxColumn.Items.Add(string.Format("{0} - {1}", columnIndex, ColumnNames[i]));
                    }
                }
                comboBoxKeyColumn.SelectedIndex = FileSetting.KeyColumn.SourceNumber;
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
            dataGridColumns.ItemsSource = FileSetting.Output.Select(x => new
            {
                Number = x.Number + 1,
                x.Name,
                EmptyTest = x.IsEmptyTest,
                SourceNumber = (x.SourceNumber + 1),
                x.SourceName,
            }).OrderBy(x => x.Number).ToArray();
            dataGridColumns.Items.Refresh();
        }

        private void buttonAddColumn_Click(object sender, RoutedEventArgs e)
        {
            if (comboBoxColumn.SelectedIndex >= 0 && int.TryParse(textBoxColumnNumber.Text, out int number))
            {
                number--;
                if (number < 0)
                {
                    number = 0;
                }
                else if (number > FileSetting.Output.Count)
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
                textBoxColumnNumber.Text = (FileSetting.Output.Count + 1).ToString();
            }
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            if (comboBoxKeyColumn.SelectedIndex >=0)
            {
                FinalizeFileSettings();
                DialogResult = true;
                Close();
            }
        }

        private void FinalizeFileSettings()
        {
            FileSetting.KeyColumn.SourceNumber = comboBoxKeyColumn.SelectedIndex;
            FileSetting.FullRow = checkBoxFullRow.IsChecked == true;
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
                    string separator = " - ";
                    var fisrtindex = value.IndexOf(separator);
                    return value.Substring(fisrtindex + separator.Length);
                }
            }
            return null;
        }

        private void dataGridColumns_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var grid = (DataGrid)sender;
            if (grid.SelectedIndex >= 0)
            {
                var item = FileSetting.Output[grid.SelectedIndex];
                checkBoxtestEmpty.IsChecked = item.IsEmptyTest;
                comboBoxColumn.SelectedIndex = item.SourceNumber;
                textBoxColumnName.Text = item.Name;
                textBoxColumnNumber.Text = (item.Number + 1).ToString();
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

        private void buttonConfig_Click(object sender, RoutedEventArgs e)
        {
            FinalizeFileSettings();
            var text = JsonConvert.SerializeObject(FileSetting, Formatting.Indented);
            OutputWindow window = new OutputWindow(text)
            {
                Owner = this
            };
            var result = window.ShowDialog();
            if (window.OutputText != text)
            {
                var dialogResult = MessageBox.Show("Want to overide setting?", "File Settings override", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No, MessageBoxOptions.None);
                if (dialogResult == MessageBoxResult.Yes)
                {
                    try
                    {
                        var clone = (FileSetting)FileSetting.Clone();
                        FileSetting.Import(JsonConvert.DeserializeObject<FileSetting>(window.OutputText));
                        FileSetting.Source = clone.Source;
                        FileSetting.FileName = clone.FileName;
                        InitUI();
                        comboBoxKeyColumn.SelectedIndex = FileSetting.KeyColumn.SourceNumber;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                }
            }
        }

        private void checkBoxExcelIndex_Checked(object sender, RoutedEventArgs e)
        {
            ReadFirstRow();
        }

        private void checkBoxExcelIndex_Unchecked(object sender, RoutedEventArgs e)
        {
            ReadFirstRow();
        }
    }
}
