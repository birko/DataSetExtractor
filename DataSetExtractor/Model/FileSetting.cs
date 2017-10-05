using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataSetExtractor.Model
{
    public class Column : ICloneable
    {
        /// <summary>
        /// column index in dataset
        /// </summary>
        public int SourceNumber { get; set; } = 0;
        /// <summary>
        /// column name in dataset
        /// </summary>
        public string SourceName { get; set; }

        public virtual object Clone()
        {
            return new Column()
            {
                SourceName = SourceName,
                SourceNumber = SourceNumber,
            };
        }
    }


    public class OutputColumn : Column, ICloneable
    {
        /// <summary>
        /// column export name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// column order
        /// </summary>
        public int Number { get; set; }
        /// <summary>
        /// empty test in output if true value will be replaced with Yes/No
        /// </summary>
        public bool IsEmptyTest { get; set; } = false;

        public override object Clone()
        {
            return new OutputColumn()
            {
                SourceName = SourceName,
                SourceNumber = SourceNumber,
                Name = Name,
                Number = Number,
                IsEmptyTest = IsEmptyTest,
            };
        }
    }

    public enum FileType
    {
        Zip,
        Csv
    }

    public class FileSetting : ICloneable
    {
        public FileType Type { get; set; }
        /// <summary>
        /// dataset file name in source
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// File Source physical path to zip or directory
        /// </summary>
        public string Source { get; set; }
        /// <summary>
        /// Copies all collumns from dataset
        /// </summary>
        public bool FullRow { get; set; } = true;
        /// <summary>
        /// Key column in dataset to compare with keys
        /// </summary>
        public Column KeyColumn { get; set; } = new Column() { SourceNumber = 0 };
        /// <summary>
        /// Output column settings if not full row
        /// </summary>
        public List<OutputColumn> Output { get; set; }
        /// <summary>
        /// Encoding of the file defaukt UTF-8
        /// </summary>
        public int FileEncoding { get; set; } = Encoding.UTF8.CodePage;

        public object Clone()
        {
            return new FileSetting()
            {
                FileName = FileName,
                Type = Type,
                Source = Source,
                FullRow = FullRow,
                KeyColumn = (Column)KeyColumn.Clone(),
                Output = Output?.Select(x => (OutputColumn)x.Clone()).ToList(),
                FileEncoding = FileEncoding
            };
        }

        public void Import(FileSetting fileSetting)
        {
            if (fileSetting != null)
            {
                FileName = fileSetting.FileName;
                Type = fileSetting.Type;
                Source = fileSetting.Source;
                FullRow = fileSetting.FullRow;
                KeyColumn = fileSetting.KeyColumn;
                Output = fileSetting.Output;
                FileEncoding = fileSetting.FileEncoding;
            }
        }

        public IEnumerable<string> GetRow()
        {
            StreamReader reader = null;
            Encoding encoding = Encoding.GetEncoding(FileEncoding);
            if (Type == FileType.Zip)
            {
                var zip = new ZipArchive(File.OpenRead(Source), ZipArchiveMode.Read);
                var entry = zip.GetEntry(FileName);
                if (entry != null)
                {
                    reader = new StreamReader(entry.Open(), encoding);
                }
            }
            else
            {
                reader = new StreamReader(File.OpenRead(Source + "/" + FileName), encoding);
            }
            string[] row = null;
            if (reader != null)
            {
                var parser = new Tools.CsvParser(reader, ';', encoding: encoding);
                foreach (var splitLine in parser.Parse())
                {
                    if (splitLine != null && splitLine.Count > 0)
                    {
                        return splitLine.ToArray();
                    }
                }
            }
            return row;
        }
    }
}
