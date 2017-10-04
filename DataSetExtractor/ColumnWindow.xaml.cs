using DataSetExtractor.Model;
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
            if(Settings != null)
            {
                var row = Settings.GetRow();
                if (row != null)
                {
                    Columns = row.Select(x => new ColumnSetting()
                    {
                        Column = x,
                        Export = true,
                        Key = false
                    }).ToArray();
                    for (int i = 0; i < Columns.Length; i++)
                    {
                        Columns[i].Index = i;
                        if (Settings.FullRow)
                        {
                            Columns[i].Key = i == 0;
                        }
                        else if (Settings.Output != null && Settings.Output.Any())
                        {
                            if (Settings.KeyColumn.SourceNumber == i)
                            {
                                Columns[i].Key = true;
                            }
                            Columns[i].Export = Settings.Output.Any(x => x.SourceNumber == i);
                        }
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
                Settings.FullRow = Columns.All(x => x.Export);
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
                            IsEmptyTest = false,
                            Name = c.Column,
                            Number = i,
                            SourceNumber =c.Index,
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
                if (cell.Column.DisplayIndex == 0)
                {
                    foreach (var c in Columns)
                    {
                        c.Key = false;
                    }
                    Columns[index].Key = true;
                }
                if (cell.Column.DisplayIndex == 1)
                {
                    Columns[index].Export = !Columns[index].Export;
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
    }
}
