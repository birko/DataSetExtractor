using DataSetExtractor.Model;
using DataSetExtractor.Tools;
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
    /// Interaction logic for ColumnWindow.xaml
    /// </summary>
    public partial class ColumnWindow : Window
    {
        public FileSetting Settings { get; set; }
        public ColumnSetting[] Columns { get; set; }
        private bool _lock = false;

        public ColumnWindow(FileSetting setting)
        {
            InitializeComponent();
            Settings = setting;
            var items = Helper.GetEncodingList();
            comboBoxEncoding.ItemsSource = items;
            if (Settings != null)
            {
                comboBoxEncoding.SelectedValue = items.FirstOrDefault(x => x.ID == Settings.FileEncoding).ID;
                var row = Settings.GetRow();
                if (row != null)
                {
                    Columns = row.Select(x => new ColumnSetting()
                    {
                        Column = x,
                        Export = true,
                        Key = false
                    }).ToArray();
                    bool excelIndex = checkBoxExcelIndex.IsChecked == true;
                    for (int i = 0; i < Columns.Length; i++)
                    {
                        Columns[i].Index = i;
                        Columns[i].ColumnIndex = (excelIndex) ? Helper.GetExcelColumnName(i + 1).PadRight(5) : (i + 1).ToString().PadRight(5);
                        if (Settings.Output != null && Settings.Output.Any())
                        {
                            if (Settings.KeyColumn.SourceNumber == i)
                            {
                                Columns[i].Key = true;
                            }
                            var col = Settings.Output.FirstOrDefault(x => x.SourceNumber == i);
                            Columns[i].Export = col != null;
                            Columns[i].ColumnName = col?.Name;
                            Columns[i].EmptyTest = col?.IsEmptyTest == true;
                        }
                    }
                    if(Columns != null && Columns.Any() && !Columns.Any(x=>x.Key))
                    {
                        Columns[0].Key = true;
                    }
                }
            }
            CheckSelectAll();
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            if (Columns != null)
            {
                dataGridColumns.ItemsSource = Columns.ToArray();
                dataGridColumns.Items.Refresh();
            }
            else
            {
                dataGridColumns.ItemsSource = null;
            }
        }

        public void SaveFileSettings()
        {
            if (Settings != null && Columns != null)
            {
                Settings.FullRow = Columns.All(x => x.Export && !x.EmptyTest && string.IsNullOrEmpty(x.ColumnName));
                var key = Columns.First(x => x.Key);
                Settings.KeyColumn = new Column() {
                    SourceName = key.Column,
                    SourceNumber = key.Index,
                };
                Settings.Output = new List<OutputColumn>();
                if (!Settings.FullRow)
                {
                    int i = 0;
                    foreach (var c in Columns.Where(x => x.Export))
                    {
                        Settings.Output.Add(new OutputColumn()
                        {
                            IsEmptyTest = c.EmptyTest,
                            Name = c.DisplayColumn,
                            Number = i,
                            SourceNumber = c.Index,
                            SourceName = c.Column
                        });
                        i++;
                    }
                }
            }
        }

        private void checkBoxSelectAll_Checked(object sender, RoutedEventArgs e)
        {
            SetAllColumns(true);
        }

        private void checkBoxSelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            SetAllColumns(false);
        }

        private void SetAllColumns(bool value)
        {
            if (!_lock)
            {
                if (Columns != null && Columns.Any())
                {
                    foreach (var c in Columns)
                    {
                        c.Export = value;
                    }
                }
                SaveFileSettings();
                RefreshGrid();
            }
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFileSettings();
            DialogResult = true;
            Close();
        }

        private void dataGridColumns_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dataGridColumns.SelectedCells != null && dataGridColumns.SelectedCells.Any())
            {
                var cell = dataGridColumns.SelectedCells.First();
                var index = (cell.Item as ColumnSetting).Index;
                if (cell.Column.DisplayIndex == 1)
                {
                    foreach (var c in Columns)
                    {
                        c.Key = false;
                    }
                    Columns[index].Key = true;
                }
                if (cell.Column.DisplayIndex == 2)
                {
                    Columns[index].Export = !Columns[index].Export;
                }
                if (cell.Column.DisplayIndex == 3)
                {
                    Columns[index].EmptyTest = !Columns[index].EmptyTest;
                }
                if (cell.Column.DisplayIndex == 4)
                {
                    InputWindow window = new InputWindow()
                    {
                        Owner = this,
                        Title = "Name",
                        Value = Columns[index].DisplayColumn
                    };
                    var result = window.ShowDialog();
                    if (result == true)
                    {
                        Columns[index].DisplayColumn = window.Value?.Trim();
                    }
                }
            }
            SaveFileSettings();
            CheckSelectAll();
            RefreshGrid();
        }

        private void CheckSelectAll()
        {
            _lock = true;
            if (Columns != null && Columns.Any())
            {
                checkBoxSelectAll.IsChecked = Columns.All(x => x.Export);
            }
            else
            {
                checkBoxSelectAll.IsChecked = false;
            }
            _lock = false;
        }

        private void checkBoxExcelIndex_Checked(object sender, RoutedEventArgs e)
        {
            SetColumnIndex(true);
        }

        private void checkBoxExcelIndex_Unchecked(object sender, RoutedEventArgs e)
        {
            SetColumnIndex(false);
        }

        private void SetColumnIndex(bool excelIndex)
        {
            if (Columns != null && Columns.Any())
            {
                int i = 0;
                foreach (var c in Columns)
                {
                    c.ColumnIndex = (excelIndex) ? Helper.GetExcelColumnName(i + 1).PadRight(5) : (i + 1).ToString().PadRight(5);
                    i++;
                }
            }
            RefreshGrid();
        }

        private void comboBoxEncoding_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Settings != null && comboBoxEncoding.SelectedIndex >=0 && comboBoxEncoding.SelectedItem != null)
            {
                var item = (EncodingItem)comboBoxEncoding.SelectedItem;
                if (Settings.FileEncoding != item.ID)
                {
                    Settings.FileEncoding = item.ID;
                    if (Columns != null && Columns.Any())
                    {
                        var row = Settings.GetRow().ToArray();
                        if (row != null && Columns.Length == row.Length)
                        {
                            for (int i = 0; i < row.Length; i++)
                            {
                                Columns[i].Column = row[i];
                            }
                        }
                    }
                    SaveFileSettings();
                }
            }
            RefreshGrid();
        }
    }
}
