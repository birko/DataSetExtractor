using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataSetExtractor.Tools
{
    public class EncodingItem
    {
        public int ID { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return string.Format("{0} - {1}", ID.ToString().PadRight(5, ' '), Name);
        }
    }

    public static class Helper
    {
        public static IEnumerable<EncodingItem> GetEncodingList()
        {
            return Encoding.GetEncodings().Select(x => new EncodingItem { ID = x.CodePage, Name = x.DisplayName });
        }

        public static string GetExcelColumnName(int columnNumber)
        {
            int dividend = columnNumber;
            string columnName = String.Empty;
            int modulo;
            int positionA = 'A';
            int AlphabetLength = 'Z' - positionA + 1;
            while (dividend > 0)
            {
                modulo = (dividend - 1) % AlphabetLength;
                columnName = Convert.ToChar(positionA + modulo).ToString() + columnName;
                dividend = (dividend - modulo) / AlphabetLength;
            }

            return columnName;
        }
    }
}
