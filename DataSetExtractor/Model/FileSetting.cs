using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataSetExtractor.Model
{
    public class Column
    {
        public int SourceNumber { get; set; } = 0;
        public string SourceName { get; set; }
    }

    public class OutputColumn : Column
    {
        public string Name { get; set; }
        public int? Number { get; set; }
        public bool IsEmptyTest { get; set; } = false;
    }

    public enum FileType
    {
        Zip,
        Csv
    }

    public class FileSetting
    {
        public string FileName { get; set; }
        public FileType Type { get; set; }
        public string Source { get; set; }
        public bool FullRow { get; set; } = true;
        public Column KeyColumn { get; set; } = new Column() { SourceNumber = 0 };
        public List<OutputColumn> Output { get; set; }
    }
}
