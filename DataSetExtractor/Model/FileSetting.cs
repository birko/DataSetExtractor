using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataSetExtractor.Model
{
    public class Column : ICloneable
    {
        public int SourceNumber { get; set; } = 0;
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
        public string Name { get; set; }
        public int? Number { get; set; }
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
        public string FileName { get; set; }
        public FileType Type { get; set; }
        public string Source { get; set; }
        public bool FullRow { get; set; } = true;
        public Column KeyColumn { get; set; } = new Column() { SourceNumber = 0 };
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
                Output = Output.Select(x => (OutputColumn)x.Clone()).ToList(),
            };
        }
    }
}
