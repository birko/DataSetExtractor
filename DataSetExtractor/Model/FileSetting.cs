using System;
using System.Collections.Generic;
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
        /// Outoput column settings if not full row
        /// </summary>
        public List<OutputColumn> Output { get; set; }

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
            }
        }
    }
}
