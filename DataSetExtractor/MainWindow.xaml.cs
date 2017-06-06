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
using System.Windows.Input;

namespace DataSetExtractor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string loadedFile = null;
        private List<FileSetting> _SelectedFiles { get; set; } = new List<FileSetting>();
        private bool? firstline;
        private bool? fullCheck = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void buttonDataSetLoad_Click(object sender, RoutedEventArgs e)
        {
            loadedFile = null;
            comboBoxEntries.IsEnabled = false;
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "Zip files (*.zip)|*.zip|CSV files (.csv)|*.csv";
            var result = dialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                var buttonText = buttonDataSetLoad.Content;
                buttonDataSetLoad.Content = "Reading ...";
                var fileNames = dialog.FileNames;
                try
                {
                    if (fileNames != null && fileNames.Any(x=>!string.IsNullOrEmpty(x)))
                    {
                        for (int i = 0; i < fileNames.Length; i++)
                        {
                            if (string.IsNullOrEmpty(fileNames[i]))
                            {
                                continue;
                            }
                            var fileInfo = new FileInfo(fileNames[i]);
                            if (fileInfo.Extension.ToLower() == ".csv")
                            {
                                loadedFile = fileNames[i];
                                _SelectedFiles.Add(new FileSetting()
                                {
                                    Source = fileNames[i],
                                    FileName = fileNames[i],
                                    Type = FileType.Csv,
                                });
                                RefreshGrid();
                                comboBoxEntries.IsEnabled = false;
                            }
                            else if (fileInfo.Extension.ToLower() == ".zip")
                            {
                                loadedFile = fileNames[i];
                                using (var zip = new ZipArchive(File.OpenRead(fileNames[i]), ZipArchiveMode.Read))
                                {
                                    comboBoxEntries.Items.Clear();
                                    foreach (var entry in zip.Entries)
                                    {
                                        comboBoxEntries.Items.Add(entry.FullName);
                                    }
                                    comboBoxEntries.IsEnabled = true;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    comboBoxEntries.IsEnabled = false;
                    MessageBox.Show(ex.Message, "Wrong File", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                finally
                {
                    buttonDataSetLoad.Content = buttonText;
                }
            }
        }

        private void buttonSelectFile_Click(object sender, RoutedEventArgs e)
        {
            FileSetting fileSetting = AddSelectedFile();
        }

        private void dataGridSelectedFiles_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var index = dataGridSelectedFiles.SelectedIndex;
            if (index >= 0 && index < _SelectedFiles.Count)
            {
                FileSetting fileSetting = _SelectedFiles[index];
                var cloneFileSetting = (FileSetting)fileSetting.Clone();
                if (fileSetting != null)
                {
                    FileWindow window = new FileWindow(fileSetting)
                    {
                        Owner = this
                    };
                    var result = window.ShowDialog();
                    if (result == true)
                    {
                        RefreshGrid();
                    }
                    else
                    {
                        _SelectedFiles[index] = cloneFileSetting;
                    }
                }
            }
        }

        private FileSetting AddSelectedFile()
        {
            FileSetting result = null;
            if (_SelectedFiles == null)
            {
                _SelectedFiles = new List<FileSetting>();
            }
            string entryName = (string)comboBoxEntries.SelectedValue;
            var filename = (!string.IsNullOrEmpty(entryName)) ? entryName : loadedFile;
            if (!_SelectedFiles.Any(x => x.Source == loadedFile && x.FileName == filename))
            {
                _SelectedFiles.Add(new FileSetting()
                {
                    Source = loadedFile,
                    FileName = filename,
                    Type = FileType.Zip,
                });
            }
            RefreshGrid();
            return result;
        }

        private void RefreshGrid()
        {
            dataGridSelectedFiles.IsEnabled = false;
            if (_SelectedFiles != null && _SelectedFiles.Any())
            {
                dataGridSelectedFiles.ItemsSource = _SelectedFiles.Where(x => x != null).Select(x =>
                {
                    var fileInfo = new FileInfo(x.Source);
                    return new {
                        x.FullRow,
                        Columns = (!x.FullRow) ? x.Output.Count : (int?)null,
                        Set = x.FileName,
                        Source = fileInfo.Name,
                    };
                });
                dataGridSelectedFiles.IsEnabled = true;
            }
        }

        private void buttonGenerate_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog()
            {
                FileName = "Document",
                DefaultExt = ".csv",
                Filter = "CSV files (.csv)|*.csv|Text documents (.txt)|*.txt"
            };
            if (dlg.ShowDialog() == true)
            {
                buttonGenerate.IsEnabled = false;
                string keysText = textBoxKeyList.Text.Trim();
                var keyList = keysText.Split(new[] { ",", "\n", ";" }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x?.Trim().Replace(" ", string.Empty).PadLeft(8, '0'))
                        .Where(x => !string.IsNullOrEmpty(x)).Distinct().ToArray();
                fullCheck = checkBoxFullCheck.IsChecked;
                firstline = checkBoxFirstLine.IsChecked;
                if (fullCheck != true)
                {
                    keyList = keyList.Where(x => x.Length == 8).ToArray();
                }
                Dictionary<string, string[]> data = new Dictionary<string, string[]>();
                buttonGenerate.Content = "Loading Data ...";
                var i = 1;
                var count = _SelectedFiles.Count;
                foreach (var item in _SelectedFiles)
                {
                    buttonGenerate.Content = "Loading Data (" + i + "/" + count + ") ...";
                    if (item.Type == FileType.Zip)
                    {
                        using (var zip = new ZipArchive(File.OpenRead(item.Source), ZipArchiveMode.Read))
                        {
                            var entry = zip.GetEntry(item.FileName);
                            if (entry != null)
                            {
                                data = LoadData(keyList, item, entry.Open(), data);
                            }
                        }
                    }
                    else
                    {
                        data = LoadData(keyList, item, File.OpenRead(item.Source), data);
                    }
                    i++;
                }
                buttonGenerate.Content = "Writing file ...";
                if (data.Any())
                {
                    using (var writer = new StreamWriter(dlg.FileName, false, Encoding.UTF8))
                    {
                        int line = 0;
                        foreach (var kvp in data.OrderBy(x => x.Key))
                        {
                            writer.WriteLine(string.Join(";", kvp.Value.Select(x => "\"" + x + "\"")));
                            line++;
                            if (line % 5000 == 0)
                            {
                                writer.Flush();
                                buttonGenerate.Content = "Writing file .";
                            }
                            if (line % 3 == 1)
                            {
                            } else if (line % 3 == 2)
                            {
                                buttonGenerate.Content = "Writing file ..";
                            }
                            if (line % 3 == 0)
                            {
                                buttonGenerate.Content = "Writing file ...";
                            }
                        }
                        writer.Flush();
                    }
                }
                buttonGenerate.Content = "Generate";
                buttonGenerate.IsEnabled = true;
            }
        }

        private Dictionary<string, string[]> LoadData(string[] keyList, FileSetting item, Stream stream, Dictionary<string, string[]> data)
        {
            if (data == null)
            {
                data = new Dictionary<string, string[]>();
            }

            var buttonText = buttonGenerate.Content;
            buttonGenerate.Content = "Generating ...";
            Task.Factory.StartNew(() => { return ProcessStream(item, keyList, stream, data); }).ContinueWith((task) =>{}).Wait();
            buttonGenerate.Content = buttonText;
            progressBarGeenerate.Value = progressBarGeenerate.Maximum;

            return data;
        }

        private int ProcessStream(FileSetting item, string[] keyList, Stream entry, Dictionary<string, string[]> data)
        {
            return ProcessStream(item, keyList, new Tools.CsvParser(new StreamReader(entry), ';'), data);
        }

        private int ProcessStream(FileSetting item, string[] keyList, Tools.CsvParser reader, Dictionary<string, string[]> data)
        {
            int processed = 0;
            long lineIndex = 0;
            Dictionary<string, string> found = new Dictionary<string, string>();
            int lastlenght = 0;
            foreach (var splitLine in reader.Parse())
            {
                var isFisrtLine = (lineIndex == 0 && firstline.Value == true);
                if (splitLine != null && splitLine.Count > 0)
                {
                    var key = splitLine[item.KeyColumn.SourceNumber].Trim().Replace("=", string.Empty).Replace("\"", string.Empty).Replace(" ", string.Empty);
                    key = key.PadLeft(8, '0');
                    bool contains = (keyList == null && keyList.Length == 0 || keyList.Contains(key));
                    if (contains || isFisrtLine)
                    {
                        if (isFisrtLine)
                        {
                            key = "########";// ensire fisrt line
                        }
                        if (contains)
                        {
                            found.Add(key, splitLine[item.KeyColumn.SourceNumber]);
                        }
                        var rowlenght = (item.FullRow) ? splitLine.Count : (item.Output != null) ? item.Output.Count : 0;
                        var datarow = new string[rowlenght];
                        for (int i = 0; i < splitLine.Count; i++)
                        {
                            var itemcolumn = item.Output?.FirstOrDefault(x => x.SourceNumber == i);
                            var index = (!item.FullRow && itemcolumn != null && itemcolumn.Number.HasValue) ? itemcolumn.Number.Value : i;
                            if (item.FullRow || itemcolumn != null)
                            {
                                datarow[index] = splitLine[i];
                                if (isFisrtLine && itemcolumn != null && !string.IsNullOrEmpty(itemcolumn.Name))
                                {
                                    datarow[index] = itemcolumn.Name;
                                }
                                else if (itemcolumn != null && itemcolumn.IsEmptyTest)
                                {
                                    datarow[index] = (!string.IsNullOrEmpty(datarow[index])) ? "OK" : string.Empty;
                                }
                            }
                        }
                        lastlenght = datarow.Length;
                        if (!data.ContainsKey(key))
                        {
                            data.Add(key, datarow);
                        }
                        else
                        {
                            data[key] = data[key].Concat(datarow).ToArray();
                        }
                    }
                }
                lineIndex++;
            }
            if (fullCheck.HasValue && fullCheck.Value)
            {
                var notfound = keyList.Where(x => !found.ContainsKey(x));
                lineIndex = 0;
                foreach (var notfounditem in notfound)
                {
                    var datarow = new string[lastlenght];
                    datarow[0] = "Bez dát";
                    if (!data.ContainsKey(notfounditem))
                    {
                        data.Add(notfounditem, datarow);
                    }
                    else
                    {
                        data[notfounditem] = data[notfounditem].Concat(datarow).ToArray();
                    }
                    lineIndex++;
                    processed++;
                }
            }
            return processed;
        }

        private void dataGridSelectedFiles_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var grid = (DataGrid)sender;
            if (Key.Delete == e.Key)
            {
                var index = grid.SelectedIndex;
                if (index >= 0 && index < _SelectedFiles.Count)
                {
                    _SelectedFiles.RemoveAt(index);
                }
                RefreshGrid();
            }
        }
    }
}