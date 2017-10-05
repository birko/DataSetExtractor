using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataSetExtractor.Model
{
    public class ColumnSetting
    {
        public int Index { get; set; } = 0;
        public bool Key { get; set; } = false;
        public bool Export { get; set; } = true;
        public bool EmptyTest { get; set; } = false;
        public string ColumnIndex { get; set; }
        public string Column { get; set; }
        public string ColumnName { get; set; }
        public string DisplayColumn
        {
            get
            {
                return (!string.IsNullOrEmpty(ColumnName) && !string.IsNullOrWhiteSpace(ColumnName)) ? ColumnName : Column;
            }
            set
            {
                ColumnName = (!string.IsNullOrEmpty(value) && !string.IsNullOrWhiteSpace(value)) ? value : null;
            }
        }
    }
}
