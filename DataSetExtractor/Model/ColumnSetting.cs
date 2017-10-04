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
        public string Column { get; set; }
    }
}
