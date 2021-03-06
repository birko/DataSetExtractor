﻿using DataSetExtractor.Model;
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
        private List<FileSetting> _SelectedFiles { get; set; } = new List<FileSetting>();
        private List<DataSet> _DataSets { get; set; } = new List<DataSet>();
        private bool? firstline;
        private bool? fullCheck = false;
        private bool? keyLengthCheck = true;
        private bool? fileOutput = true;
        private int? keyLength = 8;
        private StatusWindow statusWindow = null;
        private string noDataConst = "*??##??*";
        private string fisrtLineKeyConst = "########";

        public MainWindow()
        {
            InitializeComponent();
            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Exception ex = e.Exception;
            ShowException(ex);
        }

        public void ShowException(Exception ex, string message = null)
        {
            Application.Current.Dispatcher.Invoke((Action)delegate
            {

                if (ex != null && MessageBox.Show(((!String.IsNullOrEmpty(message)) ? message : "An error occured.") + " Want to see more details?", "Error", MessageBoxButton.YesNo, MessageBoxImage.Error, MessageBoxResult.No) == MessageBoxResult.Yes)
                {
                    var text = ex.Message + "\n-------------\n" + ex.StackTrace;
                    OutputWindow window = new OutputWindow(text)
                    {
                        Owner = this,
                        Title = "Error"
                    };
                    var result = window.ShowDialog();
                }
                else if (ex == null)
                {
                    MessageBox.Show("An error occured. Want to see more details?", "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                }
            });
        }

        private void buttonDataSetLoad_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true,
                Filter = "DataSet Files (*.zip; *.csv)|*.zip; *.csv|Zip files (*.zip)|*.zip|CSV files (*.csv)|*.csv"
            };
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
                                AddToSelectedFiles(AddDataSetToList(fileInfo.DirectoryName, fileInfo.Name, FileType.Csv), FileType.Csv);
                            }
                            else if (fileInfo.Extension.ToLower() == ".zip")
                            {
                                using (var zip = new ZipArchive(File.OpenRead(fileNames[i]), ZipArchiveMode.Read))
                                {
                                    foreach (var entry in zip.Entries.Where(x => x.FullName.EndsWith(".csv")))
                                    {
                                        AddDataSetToList(fileNames[i], entry.FullName, FileType.Zip);
                                    }
                                }
                            }
                        }
                    }
                    RefreshFilesGrid();
                    RefreshGrid();
                }
                catch (Exception ex)
                {
                    ShowException(ex);
                }
                finally
                {
                    buttonDataSetLoad.Content = buttonText;
                }
            }
        }

        private DataSet AddDataSetToList(string path, string fileName, FileType type)
        {
            if (_DataSets == null)
            {
                _DataSets = new List<DataSet>();
            }
            if (!_DataSets.Any(x => x.Source == path && x.FileName == fileName && x.Type == type))
            {
                var item = new DataSet()
                {
                    Source = path,
                    FileName = fileName,
                    Type = type,
                };
                _DataSets.Add(item);
                return item;
            }
            return null;
        }

        private FileSetting AddToSelectedFiles(DataSet item, FileType type)
        {
            if (item != null)
            {
                if (_SelectedFiles == null)
                {
                    _SelectedFiles = new List<FileSetting>();
                }
                if (!_SelectedFiles.Any(x => x.Source == item.Source && x.FileName == item.FileName))
                {
                    var fileitem = new FileSetting()
                    {
                        Source = item.Source,
                        FileName = item.FileName,
                        Type = type,
                    };
                    _SelectedFiles.Add(fileitem);
                    return fileitem;
                }
            }
            return null;
        }


        private void buttonSelectFile_Click(object sender, RoutedEventArgs e)
        {
            AddSelectedFiles();
        }

        private IEnumerable<FileSetting> AddSelectedFiles()
        {
            var result = new List<FileSetting>();
            if (dataGridEntries.SelectedItems != null && dataGridEntries.SelectedItems.Count > 0)
            {
                foreach (DataSet item in dataGridEntries.SelectedItems)
                {
                    var fileitem = AddToSelectedFiles(item, item.Type);
                    if (fileitem != null)
                    {
                        result.Add(fileitem);
                    }
                }
            }
            RefreshGrid();
            return result;
        }

        private void RefreshFilesGrid()
        {
            dataGridEntries.IsEnabled = false;
            dataGridEntries.ItemsSource = null;
            if (_DataSets != null && _DataSets.Any())
            {
                dataGridEntries.ItemsSource = _DataSets.Where(x => x != null);
                dataGridEntries.IsEnabled = true;
            }
        }

        private void RefreshGrid()
        {
            dataGridSelectedFiles.IsEnabled = false;
            dataGridSelectedFiles.ItemsSource = null;
            if (_SelectedFiles != null && _SelectedFiles.Any())
            {
                dataGridSelectedFiles.ItemsSource = _SelectedFiles.Where(x => x != null).Select(x =>
                {
                    return new DataSetItem {
                        FullRow = x.FullRow,
                        Columns = (!x.FullRow) ? x.Output.Count : (int?)null,
                        FileName = x.FileName,
                        Source = x.Source,
                    };
                });
                dataGridSelectedFiles.IsEnabled = true;
            }
        }

        private static Microsoft.Win32.SaveFileDialog CreateDialog()
        {
            return new Microsoft.Win32.SaveFileDialog()
            {
                FileName = "DataSet-" + DateTime.Now.ToString("yyyy-MM-dd"),
                DefaultExt = ".csv",
                Filter = "CSV files (.csv)|*.csv|Text documents (.txt)|*.txt"
            };
        }

        private void BeforeGenerate()
        {
            buttonGenerate.IsEnabled = false;
            fullCheck = checkBoxFullCheck.IsChecked;
            firstline = checkBoxFirstLine.IsChecked;
            keyLengthCheck = checkBoxKeyLength.IsChecked;
            fileOutput = checkBoxFile.IsChecked == true;
            if (keyLengthCheck != true)
            {
                keyLength = null;
            }
            else if (int.TryParse(textBoxKeyLength.Text?.Trim(), out int result))
            {
                keyLength = result;
            }
        }

        private void AfterGenerate(IDictionary<string, IEnumerable<IEnumerable<string>>> resultData = null, string fileName = null)
        {
            DisplayResult(resultData, fileName);
        }

        private void buttonGenerate_Click(object sender, RoutedEventArgs e)
        {
            bool dialogResult = false;
            Microsoft.Win32.SaveFileDialog dlg = CreateDialog();
            dialogResult = (checkBoxFile.IsChecked != true || (checkBoxFile.IsChecked == true && dlg.ShowDialog() == true));
            string fileName = null;
            if (dialogResult == true)
            {
                fileName = dlg.FileName;
            }
            BeforeGenerate();
            string keysText = textBoxKeyList.Text.Trim();
            IDictionary<string, IEnumerable<IEnumerable<string>>> resultData = null;
            statusWindow = new StatusWindow((_SelectedFiles != null) ? _SelectedFiles.Count : 0)
            {
                Owner = this,
            };
            statusWindow.Start(() =>
            {
                resultData = Generate(dialogResult, keysText);
            },
            () =>
            {
                AfterGenerate(resultData, fileName);
            });
        }

        private IDictionary<string, IEnumerable<IEnumerable<string>>> Generate(bool generate, string keysText = null)
        {
            if (generate)
            {
                IDictionary<string, IEnumerable<IEnumerable<IEnumerable<string>>>> data = new Dictionary<string, IEnumerable<IEnumerable<IEnumerable<string>>>>();
                var count = _SelectedFiles.Count;
                var keyList = (keysText != null) ? keysText.Split(new[] { ",", "\n", "\r", ";" }, StringSplitOptions.RemoveEmptyEntries)
                       .Select(x =>
                       {
                           var result = x?.Trim().Replace(" ", string.Empty);
                           if (keyLength != null)
                           {
                               result = result?.PadLeft(keyLength.Value, '0');
                           }
                           return result;
                       })
                       .Where(x => !string.IsNullOrEmpty(x)).Distinct().ToArray() : new string[0];
                if (keyLengthCheck != true && keyLength != null)
                {
                    keyList = keyList.Where(x => x.Length == keyLength).ToArray();
                }
                Parallel.For(0, count, (i) =>
                {
                    var item = _SelectedFiles[i];
                    try
                    {

                        UpdateStatusWindow(i, string.Format("Started Processing File: {0}", i + 1));
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
                        else if (item.Type == FileType.Csv)
                        {
                            data = LoadData(keyList, item, File.OpenRead(item.Source + "/" + item.FileName), data, i);
                        }
                        UpdateStatusWindow(i + 1, string.Format("Done Processing File: {0}", i + 1));
                    }
                    catch (IOException ex)
                    {
                        ShowException(ex, String.Format("Could not read file: {0}. Check if is is not open by another program or deleted.", item.Source + "/" + item.FileName));
                    }
                });
                var outputdata = data.Where(ico => ico.Value.Any(section => section != null && section.Any(row => row != null && row.Any(value => !string.IsNullOrEmpty(value)))));
                if (outputdata.Any())
                {
                    IDictionary<string, IEnumerable<IEnumerable<string>>> finalData = new Dictionary<string, IEnumerable<IEnumerable<string>>>();
                    int max = outputdata.Sum(x => x.Value.Max( y=> y.Count()));
                    MaxStatusWindow(max);
                    var rowItem = outputdata.First();
                    var sectionsCount = rowItem.Value.Count();
                    int[] maxColumns = new int[sectionsCount];
                    for (int sectionIndex = 0; sectionIndex < sectionsCount; sectionIndex++)
                    {
                        maxColumns[sectionIndex] = outputdata.Max(x => (x.Value.Count() > sectionIndex) ? (x.Value.ElementAt(sectionIndex).Count() > 0) ? x.Value.ElementAt(sectionIndex).Max(rows => (rows != null) ? rows.Count() : 0) : 0 : 0);
                    }
                    int lineIndex = 0;
                    UpdateStatusWindow(lineIndex, string.Format("Start Processing items. Processed items: {0}", lineIndex));
                    Parallel.ForEach(outputdata.OrderBy(x => x.Key), (kvp) =>
                    {
                        List<string[]> rows = new List<string[]>();
                        int maxRows = kvp.Value.Max(x => x.Count());
                        for (int rowIndex = 0; rowIndex < maxRows; rowIndex++)
                        {
                            rows.Add(new string[0]);
                        }
                        for(int sectionIndex = 0; sectionIndex < sectionsCount; sectionIndex ++)
                        {
                            var keyIndex = (_SelectedFiles[sectionIndex].FullRow) ? _SelectedFiles[sectionIndex].KeyColumn.SourceNumber : _SelectedFiles[sectionIndex].Output.FirstOrDefault(x => x.SourceNumber == _SelectedFiles[sectionIndex].KeyColumn.SourceNumber)?.Number ?? 0;
                            for( int rowIndex  = 0; rowIndex < maxRows; rowIndex ++)
                            {
                                string[] subrow = new string[maxColumns[sectionIndex]];
                                // copy values and replace nodataconst
                                if (kvp.Value.ElementAt(sectionIndex).Count() > rowIndex)
                                {
                                    if (kvp.Value.ElementAt(sectionIndex).ElementAt(rowIndex).Count() > 0)
                                    {
                                        Parallel.For(0, kvp.Value.ElementAt(sectionIndex).ElementAt(rowIndex).Count(), (valueIndex) =>
                                        {
                                            subrow[valueIndex] = kvp.Value.ElementAt(sectionIndex).ElementAt(rowIndex).ElementAt(valueIndex).Replace(noDataConst, "Bez dát");
                                        });
                                    }
                                }
                                // is section has less rows than max rows  in all sections per key add no data sign
                                else if (_SelectedFiles[sectionIndex].FullRow || _SelectedFiles[sectionIndex].Output.Any(x => x.SourceNumber == _SelectedFiles[sectionIndex].KeyColumn.SourceNumber))
                                {
                                    if (fullCheck.HasValue && fullCheck.Value)
                                    {
                                        subrow[0] = "Bez dát";
                                    }
                                }
                                rows[rowIndex] = rows[rowIndex].Concat(subrow).ToArray();
                                if ((lineIndex + 1) % 10 == 0)
                                {
                                    UpdateStatusWindow(lineIndex + 1, string.Format("Processing items. Processed items: {0}", lineIndex + 1));
                                }
                                lineIndex++;
                            };
                        }
                        finalData.Add(kvp.Key, rows.ToArray());
                    });
                    UpdateStatusWindow(lineIndex, string.Format("Done Processing items. Total items: {0}", lineIndex));
                    return finalData;
                }
                else
                {
                    MessageBox.Show("Given result is empty", "No Data", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.None);
                }
            }
            return null;
        }

        private IDictionary<string, IEnumerable<IEnumerable<IEnumerable<string>>>> LoadData(string[] keyList, FileSetting item, Stream stream, IDictionary<string, IEnumerable<IEnumerable<IEnumerable<string>>>> data, int filePosition)
        {
            if (data == null)
            {
                data = new Dictionary<string, IEnumerable<IEnumerable<IEnumerable<string>>>>();
            }
            ProcessStream(item, keyList, stream, data, filePosition);
            return data;
        }

        private void ProcessStream(FileSetting item, string[] keyList, Stream entry, IDictionary<string, IEnumerable<IEnumerable<IEnumerable<string>>>> data, int filePosition)
        {
            ProcessStream(item, keyList, new Tools.CsvParser(new StreamReader(entry, Encoding.GetEncoding(item.FileEncoding)), ';'), data, filePosition);
        }

        private void ProcessStream(FileSetting item, string[] keyList, Tools.CsvParser reader, IDictionary<string, IEnumerable<IEnumerable<IEnumerable<string>>>> data, int sectionIndex)
        {
            long lineIndex = 0;
            Dictionary<string, string> found = new Dictionary<string, string>();
            int lastlength = 0;
            UpdateStatusWindow(sectionIndex, string.Format("Start Reading Dataset {0} items. Processed items: {1}", sectionIndex + 1, lineIndex));
            foreach(var splitLine in  reader.Parse())
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
                            key = fisrtLineKeyConst;// ensure fisrt line
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
                        data = InitDataItem(data, sectionIndex, key, datarow);
                    }
                }
                if ((lineIndex + 1) % 1000 == 0)
                {
                    UpdateStatusWindow(sectionIndex, string.Format("Reading Dataset {0} items. Processed items: {1}", sectionIndex + 1, lineIndex + 1));
                }
                lineIndex++;
            }
            var notfound = keyList.Where(x => !found.ContainsKey(x));
            Parallel.ForEach(notfound, (notfounditem) =>
            {
                var datarow = new string[0];
                if (fullCheck.HasValue && fullCheck.Value)
                {
                    datarow = new string[1];
                    datarow[0] = noDataConst;
                }
                data = InitDataItem(data, sectionIndex, notfounditem, datarow);
                if ((lineIndex + 1) % 1000 == 0)
                {
                    UpdateStatusWindow(sectionIndex, string.Format("Reading Dataset {0} items. Processed items: {1}", sectionIndex + 1, lineIndex + 1));
                }
                lineIndex++;
            });
            UpdateStatusWindow(sectionIndex + 1, string.Format("Done Reading Dataset {0} items. Processed items: {1}", sectionIndex + 1, lineIndex));
        }

        private IDictionary<string, IEnumerable<IEnumerable<IEnumerable<string>>>> InitDataItem(IDictionary<string, IEnumerable<IEnumerable<IEnumerable<string>>>> data, int sectionIndex, string key, IEnumerable<string> datarow)
        {
            if (!data.ContainsKey(key))
            {
                var list = new List<IEnumerable<IEnumerable<string>>>();
                for (int i = 0; i < _SelectedFiles.Count; i++)
                {
                    var first = new List<IEnumerable<string>>();
                    list.Add(first.ToArray());
                }
                data.Add(key, list.ToArray());
            }
            if (data.TryGetValue(key, value: out IEnumerable<IEnumerable<IEnumerable<string>>> val))
            {
                var row = val.ToArray();
                row[sectionIndex] = row[sectionIndex].Concat(new[] { datarow });
                data.Remove(key);
                data.Add(key, row);
            }
            return data;
        }

        private void DisplayResult(IDictionary<string, IEnumerable<IEnumerable<string>>> finalData = null, string fileName = null)
        {
            statusWindow = new StatusWindow((_SelectedFiles != null) ? _SelectedFiles.Count : 0)
            {
                Owner = this,
            };
            statusWindow.Start(() =>
            {
                if (fileOutput == true && finalData != null)
                {
                    try
                    {
                        using (var writer = new StreamWriter(fileName, false, Encoding.UTF8))
                        {
                            int line = 0;
                            UpdateStatusWindow(line, string.Format("Start Writing File"));
                            foreach (var kvp in finalData.OrderBy(x => x.Key))
                            {
                                foreach (var row in kvp.Value)
                                {
                                    var o = "\"" + kvp.Key.Replace(fisrtLineKeyConst, "Key") + "\";" + string.Join(";", row.Select(x => "\"" + x + "\""));
                                    writer.WriteLine(o);
                                    if ((line + 1) % 1000 == 0)
                                    {
                                        UpdateStatusWindow(line + 1, string.Format("Writing File"));
                                        writer.Flush();
                                    }
                                    line++;
                                }
                            }
                            UpdateStatusWindow(line, string.Format("Closing File"));
                            writer.Flush();
                        }
                    }
                    catch (Exception ex)
                    {
                        ShowOutputWindow(finalData);
                        throw ex;
                    }
                }
            },
            () =>
            {
                if (fileOutput != true && finalData != null)
                {
                    ShowOutputWindow(finalData);
                }
                else
                {
                    if (Dispatcher.CheckAccess())
                    {
                        buttonGenerate.IsEnabled = true;
                    }
                    else
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            buttonGenerate.IsEnabled = true;
                        }));
                    }
                }
            });
        }

        private void ShowOutputWindow(IDictionary<string, IEnumerable<IEnumerable<string>>> outputdata)
        {
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                StringBuilder text = new StringBuilder();
                statusWindow = new StatusWindow((_SelectedFiles != null) ? _SelectedFiles.Count : 0)
                {
                    Owner = this,
                };
                statusWindow.Start(() =>
                {
                    int line = 0;
                    MaxStatusWindow(outputdata.Sum(x => x.Value.Count()));
                    UpdateStatusWindow(line, string.Format("Start Generating Output"));
                    foreach (var kvp in outputdata.OrderBy(x => x.Key))
                    {
                        foreach (var row in kvp.Value)
                        {
                            text.AppendLine("\"" + kvp.Key.Replace(fisrtLineKeyConst, "Key") + "\";" + string.Join(";", row.Select(x => "\"" + x + "\"")));
                            if ((line + 1) % 1000 == 0)
                            {
                                UpdateStatusWindow(line + 1, string.Format("Generating Output"));
                            }
                            line++;
                        }
                    }
                    UpdateStatusWindow(line, string.Format("Loading Output"));
                },
                () =>
                {
                    if (Dispatcher.CheckAccess())
                    {
                        buttonGenerate.IsEnabled = true;
                        InitOutputWindow(text.ToString());
                    }
                    else
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            buttonGenerate.IsEnabled = true;
                            InitOutputWindow(text.ToString());
                        }));
                    }
                });
            });
        }

        private void InitOutputWindow(string text)
        {
            OutputWindow window = new OutputWindow(text)
            {
                Owner = this
            };
            var result = window.ShowDialog();
        }

        private void MaxStatusWindow(int max)
        {
            if (statusWindow != null)
            {
                statusWindow.SetMaxStatus(max);
            }
        }

        private void UpdateStatusWindow(int i, string text)
        {
            if (statusWindow != null && statusWindow.Worker != null)
            {
                statusWindow.Worker.ReportProgress(i, text);
            }
        }

        private void dataGridSelectedFiles_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var grid = (DataGrid)sender;
            FileColumnSettings(grid);
        }

        private void FileColumnSettings(DataGrid grid)
        {
            var index = grid.SelectedIndex;
            if (index >= 0 && index < _SelectedFiles.Count)
            {
                FileSetting fileSetting = _SelectedFiles[index];
                var cloneFileSetting = (FileSetting)fileSetting.Clone();
                if (fileSetting != null)
                {
                    ColumnWindow window = new ColumnWindow(fileSetting)
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

        private void EditSelectedFiles(DataGrid grid)
        {
            var index = grid.SelectedIndex;
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

        private void dataGridSelectedFiles_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var grid = (DataGrid)sender;
            if (Key.Delete == e.Key)
            {
                DeleteSelectedFromGrid(grid);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedFromGrid(dataGridSelectedFiles);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            EditSelectedFiles(dataGridSelectedFiles);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            FileColumnSettings(dataGridSelectedFiles);
        }

        private void dataGridEntries_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            AddSelectedFiles();
        }

        private void DeleteSelectedFromGrid(DataGrid grid)
        {
            var index = grid.SelectedIndex;
            if (index >= 0 && index < _SelectedFiles.Count)
            {
                _SelectedFiles.RemoveAt(index);
            }
            RefreshGrid();
        }

        private void buttonShowConfig_Click(object sender, RoutedEventArgs e)
        {
            var text = JsonConvert.SerializeObject(_SelectedFiles, Formatting.Indented);
            OutputWindow window = new OutputWindow(text)
            {
                Owner = this,
                Title = "Config",
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
                        ShowException(ex);
                    }
                }
            }
        }
    }
}