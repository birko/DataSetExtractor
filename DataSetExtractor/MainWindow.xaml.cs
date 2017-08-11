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
        private bool? keyLengthCheck = true;
        private int? keyLength = 8;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void buttonDataSetLoad_Click(object sender, RoutedEventArgs e)
        {
            comboBoxEntries.SelectedIndex = -1;
            comboBoxEntries.Items.Clear();
            loadedFile = null;
            comboBoxEntries.IsEnabled = false;
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "DataSet Files (*.zip; *.csv)|*.zip; *.csv|Zip files (*.zip)|*.zip|CSV files (*.csv)|*.csv";
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

                                    foreach (var entry in zip.Entries.Where(x=>x.FullName.EndsWith(".csv")))
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
                FileName = "DataSet-" + DateTime.Now.ToString("yyyy-MM-dd"),
                DefaultExt = ".csv",
                Filter = "CSV files (.csv)|*.csv|Text documents (.txt)|*.txt"
            };
            if (checkBoxFile.IsChecked != true || (checkBoxFile.IsChecked == true && dlg.ShowDialog() == true))
            {
                buttonGenerate.IsEnabled = false;
                fullCheck = checkBoxFullCheck.IsChecked;
                firstline = checkBoxFirstLine.IsChecked;
                keyLengthCheck = checkBoxKeyLength.IsChecked;

                if (keyLengthCheck != true)
                {
                    keyLength = null;
                }
                else if (int.TryParse(textBoxKeyLength.Text?.Trim(), out int result))
                {
                    keyLength = result;
                }
                string keysText = textBoxKeyList.Text.Trim();
                var keyList = keysText.Split(new[] { ",", "\n", ";" }, StringSplitOptions.RemoveEmptyEntries)
                       .Select(x => {
                           var result = x?.Trim().Replace(" ", string.Empty);
                           if (keyLength != null)
                           {
                               result = result?.PadLeft(keyLength.Value, '0');
                           }
                           return result;
                       })
                       .Where(x => !string.IsNullOrEmpty(x)).Distinct().ToArray();
                if (keyLengthCheck != true && keyLength != null)
                {
                    keyList = keyList.Where(x => x.Length == keyLength).ToArray();
                }
                Dictionary<string, List<string[]>> data = new Dictionary<string, List<string[]>>();
                buttonGenerate.Content = "Loading Data ...";
                var count = _SelectedFiles.Count;
                for (int i = 0; i < count; i++)
                {
                    var item = _SelectedFiles[i];
                    buttonGenerate.Content = "Loading Data (" + i + "/" + count + ") ...";
                    if (item.Type == FileType.Zip)
                    {
                        using (var zip = new ZipArchive(File.OpenRead(item.Source), ZipArchiveMode.Read))
                        {
                            var entry = zip.GetEntry(item.FileName);
                            if (entry != null)
                            {
                                data = LoadData(keyList, item, entry.Open(), data, i);
                            }
                        }
                    }
                    else
                    {
                        data = LoadData(keyList, item, File.OpenRead(item.Source), data, i);
                    }
                }
                buttonGenerate.Content = "Writing file ...";

                var outputdata = data.Where(x => x.Value.Any(y => y != null && y.Length > 0 && y.Any(z => !string.IsNullOrEmpty(z)))).ToDictionary(x => x.Key, x => x.Value);
                if (outputdata.Any())
                {
                    var rowItem = outputdata.First();
                    var sectionsCount = rowItem.Value.Count;
                    int[] maxColumns = new int[sectionsCount];
                    for (int i = 0; i < sectionsCount; i++)
                    {
                        maxColumns[i] = outputdata.Max(x => x.Value[i].Length);
                    }
                    Dictionary<string, string[]> finalData = new Dictionary<string, string[]>();
                    foreach (var kvp in outputdata.OrderBy(x => x.Key))
                    {
                        string[] row = new string[0];
                        for (int i = 0; i < sectionsCount; i++)
                        {
                            string[] subrow = new string[maxColumns[i]];
                            for (int j = 0; j < kvp.Value[i].Length; j ++)
                            {
                                subrow[j] = kvp.Value[i][j];
                            }
                            row = row.Concat(subrow).ToArray();
                        }
                        finalData.Add(kvp.Key, row);
                    }
                    if (checkBoxFile.IsChecked == true)
                    {
                        try
                        {
                            using (var writer = new StreamWriter(dlg.FileName, false, Encoding.UTF8))
                            {
                                int line = 0;
                                foreach (var kvp in finalData.OrderBy(x => x.Key))
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
                                    }
                                    else if (line % 3 == 2)
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
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "File Write Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.None);
                            ShowOutputWindow(finalData);
                        }
                    }
                    else
                    {
                        ShowOutputWindow(finalData);
                    }
                }
                buttonGenerate.Content = "Generate";
                buttonGenerate.IsEnabled = true;
            }
        }

        private void ShowOutputWindow(IEnumerable<KeyValuePair<string, string[]>> outputdata)
        {
            StringBuilder text = new StringBuilder();
            foreach (var kvp in outputdata.OrderBy(x => x.Key))
            {
                text.AppendLine(string.Join(";", kvp.Value.Select(x => "\"" + x + "\"")));
            }
            OutputWindow window = new OutputWindow(text.ToString())
            {
                Owner = this
            };

            var result = window.ShowDialog();
        }

        private Dictionary<string, List<string[]>> LoadData(string[] keyList, FileSetting item, Stream stream, Dictionary<string, List<string[]>> data, int filePosition)
        {
            if (data == null)
            {
                data = new Dictionary<string, List<string[]>>();
            }

            var buttonText = buttonGenerate.Content;
            buttonGenerate.Content = "Generating ...";
            Task.Factory.StartNew(() => { return ProcessStream(item, keyList, stream, data, filePosition); }).ContinueWith((task) =>{}).Wait();
            buttonGenerate.Content = buttonText;
            progressBarGeenerate.Value = progressBarGeenerate.Maximum;

            return data;
        }

        private int ProcessStream(FileSetting item, string[] keyList, Stream entry, Dictionary<string, List<string[]>> data, int filePosition)
        {
            return ProcessStream(item, keyList, new Tools.CsvParser(new StreamReader(entry), ';'), data, filePosition);
        }

        private int ProcessStream(FileSetting item, string[] keyList, Tools.CsvParser reader, Dictionary<string, List<string[]>> data, int filePosition)
        {
            int processed = 0;
            long lineIndex = 0;
            Dictionary<string, string> found = new Dictionary<string, string>();
            int lastlength = 0;
            foreach (var splitLine in reader.Parse())
            {
                var isFisrtLine = (lineIndex == 0 && firstline.Value == true);
                if (splitLine != null && splitLine.Count > 0)
                {
                    var key = splitLine[item.KeyColumn.SourceNumber].Trim().Replace("=", string.Empty).Replace("\"", string.Empty).Replace(" ", string.Empty);
                    if (keyLength != null)
                    {
                        key = key.PadLeft(keyLength.Value, '0');
                    }
                    bool contains = (keyList == null || keyList.Length == 0 || keyList.Contains(key));
                    if (contains || isFisrtLine)
                    {
                        if (isFisrtLine)
                        {
                            key = "########";// ensure fisrt line
                        }
                        if (contains && !found.ContainsKey(key))
                        {
                            found.Add(key, splitLine[item.KeyColumn.SourceNumber]);
                        }
                        var rowlength = (item.FullRow) ? splitLine.Count : (item.Output != null) ? item.Output.Count : 0;
                        var datarow = new string[rowlength];
                        for (int i = 0; i < splitLine.Count; i++)
                        {
                            var itemcolumn = item.Output?.FirstOrDefault(x => x.SourceNumber == i);
                            var index = (!item.FullRow && itemcolumn != null) ? itemcolumn.Number : i;
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
                        lastlength = datarow.Length;
                        if (!data.ContainsKey(key))
                        {
                            var list = new List<string[]>();
                            for (int i = 0; i < _SelectedFiles.Count; i++)
                            {
                                list.Add(new string[0]);
                            }
                            data.Add(key, list);
                        }
                        data[key][filePosition] = datarow;
                    }
                }
                lineIndex++;
            }
            var notfound = keyList.Where(x => !found.ContainsKey(x));
            foreach (var notfounditem in notfound)
            {
                var datarow = new string[0];
                if (fullCheck.HasValue && fullCheck.Value)
                {
                    datarow = new string[1];
                    datarow[0] = "Bez dát";
                }
                if (!data.ContainsKey(notfounditem))
                {
                    var list = new List<string[]>();
                    for (int i = 0; i < _SelectedFiles.Count; i++)
                    {
                        list.Add(new string[0]);
                    }
                    data.Add(notfounditem, list);
                }
                data[notfounditem][filePosition] = datarow;
                lineIndex++;
                processed++;
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

        private void buttonShowConfig_Click(object sender, RoutedEventArgs e)
        {
            var text = JsonConvert.SerializeObject(_SelectedFiles, Formatting.Indented);
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
                        var files = JsonConvert.DeserializeObject<List<FileSetting>>(window.OutputText);
                        var count = (files != null) ? files.Count : 0;
                        if (_SelectedFiles != null && _SelectedFiles.Count > count)
                        {
                            count = _SelectedFiles.Count;
                        }
                        for (int i = 0; i < count; i++)
                        {
                            if ((_SelectedFiles != null && _SelectedFiles.Count > i) && (files != null && files.Count > i))
                            {
                                var clone = (FileSetting)_SelectedFiles[i].Clone();
                                _SelectedFiles[i].Import(files[i]);
                                _SelectedFiles[i].FileName = clone.FileName;
                                _SelectedFiles[i].Source = clone.Source;
                            }
                        }
                        RefreshGrid();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                }
            }
        }
    }
}