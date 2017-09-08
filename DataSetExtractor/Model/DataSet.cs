using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataSetExtractor.Model
{
    class DataSet
    {
        /// <summary>
        /// dataset file name in source
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// File Source physical path to zip or directory
        /// </summary>
        public string Source { get; set; }
    }

    class DataSetItem : DataSet
    {
        /// <summary>
        /// Copies all collumns from dataset
        /// </summary>
        public bool FullRow { get; set; }
        // <summary>
        /// Columns count to copy if not full row
        /// </summary>
        public int? Columns { get; set; }
    }
}
