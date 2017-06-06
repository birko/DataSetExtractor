using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
/// <summary>
///     Copy from finstat.BLL.Tools.Csv
///     Modified to use StreamReader
/// </summary>
/// <author>Vlko</author
/// <author>Birko</author>
namespace DataSetExtractor.Tools
{
    public class CsvParser
    {
        ///
        /// Reader automat states.
        ///
        public enum ReadType
        {
            CurrentLine,
            QuoteText,
            NewLine
        }

        private int _TotalLines;

        private Encoding _encoding;
        /// <summary>
        /// Gets or sets a value indicating whether this instance can read.
        /// </summary>
        /// <value><c>true</c> if this instance can read; otherwise, <c>false</c>.</value>
        public bool CanRead { get; set; }

        /// <summary>
        /// Gets the line.
        /// </summary>
        /// <value>The line.</value>
        public int Line { get; protected set; }

        /// <summary>
        /// Gets or sets the CSV data enclosure.
        /// </summary>
        /// <value>The enclosure character.</value>
        public char? cEnclosure { get; private set; }

        /// <summary>
        /// Gets or sets the CSV data delimiter.
        /// </summary>
        /// <value>The delimiter character.</value>
        public char cDelimiter { get; private set; }

        /// <summary>
        /// StreamReader for the csv file
        /// </summary>
        /// <value>The delimiter character.</value>
        public StreamReader CsvReader { get; private set; }

        /// <summary>
        /// Gets or sets the total lines.
        /// </summary>
        /// <value>The total lines.</value>
        public int TotalLines
        {
            get
            {
                if (_TotalLines < 0)
                {
                    _TotalLines = GetTotalLines();
                }
                return _TotalLines;
            }
            private set
            {
                _TotalLines = value;
            }
        }

        /// <summary>
        /// Gets or sets the file encoding.
        /// </summary>
        /// <value>The file encoding.</value>
        public Encoding FileEncoding
        {
            get
            {
                if (_encoding == null)
                {
                    GetAttributes(null);
                }
                return _encoding;
            }
            set
            {
                _encoding = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CsvParser" /> class.
        /// </summary>
        /// <param name="reader">File stream Reader.</param>
        /// <param name="delimiter">Optional character data delimiter.</param>
        /// <param name="enclosure">Optional character data enclosure.</param>
        /// <param name="encoding">Optional encoding.</param>
        public CsvParser(StreamReader reader, char delimiter = ',', char? enclosure = '"', Encoding encoding = null)
        {
            CsvReader = reader;
            Line = 0;
            TotalLines = -1;
            cDelimiter = delimiter;
            cEnclosure = enclosure;
            try
            {
                GetAttributes(encoding);
                TotalLines = GetTotalLines();
            }
            catch
            {
                CanRead = false;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CsvParser" /> class.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="delimiter">Optional character data delimiter.</param>
        /// <param name="enclosure">Optional character data enclosure.</param>
        /// <param name="encoding">Optional encoding.</param>
        public CsvParser(string fileName, char delimiter = ',', char? enclosure = '"', Encoding encoding = null)
            :this(new StreamReader(fileName), delimiter, enclosure, encoding)
        {
        }

        public CsvParser SetFileEncoding(Encoding encoding)
        {
            FileEncoding = encoding;

            return this;
        }
        /// <summary>
        /// Gets the total lines.
        /// </summary>
        /// <returns>Number of total lines.</returns>
        private int GetTotalLines()
        {
            int result = -1;
            try
            {
                if (CsvReader.BaseStream.CanRead && CsvReader.BaseStream.CanSeek)
                {
                    result = 0;
                    CsvReader.BaseStream.Seek(0, SeekOrigin.Begin);
                    while (!CsvReader.EndOfStream)
                    {
                        string line = CsvReader.ReadLine();
                        if (line.Length > 0) result++;
                    }
                }
            }
            catch
            { }
            return result;
        }
        /// <summary>
        /// Sets the attributes.
        /// </summary>
        private void GetAttributes(Encoding encoding)
        {
            if (encoding != null)
            {
                _encoding = encoding;
                return;
            }
            _encoding = Encoding.Unicode;
            CanRead = CsvReader.BaseStream.CanRead;
            if (CsvReader.BaseStream.CanSeek && CanRead)
            {
                CsvReader.BaseStream.Seek(0, SeekOrigin.Begin);
                byte[] bom = new byte[4]; // Get the byte-order mark, if there is one
                CsvReader.BaseStream.Read(bom, 0, 4);
                if ((bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) || // utf-8
                    (bom[0] == 0xff && bom[1] == 0xfe) || // ucs-2le, ucs-4le, and ucs-16le
                    (bom[0] == 0xfe && bom[1] == 0xff) || // utf-16 and ucs-2
                    (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff)) // ucs-4
                {
                    _encoding = Encoding.Unicode;
                }
                else
                {
                    _encoding = Encoding.ASCII;
                }
            }
            else
            {
                // The file cannot be randomly accessed, so the default encoding is set as ASCII
                _encoding = Encoding.ASCII;
            }
        }

        /// <summary>
        /// Parses the specified fill item action.
        /// </summary>
        /// <param name="fillItemAction">The optional fill item action.</param>
        public virtual IEnumerable<IList<string>> Parse(Action<IList<string>> fillItemAction = null)
        {
            Line = 0;
            // this value contains current string value of item
            StringBuilder currentItem = new StringBuilder();
            // this value contains current row values
            List<string> items = new List<string>();
            // set initial state of reader automat
            ReadType readType = ReadType.CurrentLine;
            // set initial state of previous char
            char previousChar = char.MinValue;

            if (CsvReader.BaseStream.CanSeek)
            {
                CsvReader.BaseStream.Seek(0, SeekOrigin.Begin);
            }
            // read while not end of stream
            while (!CsvReader.EndOfStream)
            {
                // read single char
                int chValue = CsvReader.Read();
                char ch = (char)chValue;
                // if char is
                // new line type
                if (ch == (char)10 || ch == (char)13)
                {
                    // if current line is in quoted text, add this character to text
                    if (readType == ReadType.QuoteText)
                    {
                        currentItem.Append(ch);
                    }
                    // else set reader automat to new line state
                    else
                    {
                        // if reader not in new line status
                        if (readType != ReadType.NewLine)
                        {
                            // add what we have as new item to items list
                            items.Add(currentItem.ToString());
                            // call fill item function
                            Line++;
                            if (fillItemAction != null)
                            {
                                fillItemAction(items);
                            }
                            yield return items;
                            // reset items and current item text
                            items.Clear();
                            currentItem.Length = 0;
                        }
                        readType = ReadType.NewLine;
                    }
                }
                else if (this.cEnclosure != null && ch == this.cEnclosure.Value)
                {
                    // if new line then change back to current line reader state
                    if (readType == ReadType.NewLine)
                    {
                        readType = ReadType.CurrentLine;
                    }
                    // if reader automat is not in quote text, then set state to quote text
                    if (readType != ReadType.QuoteText && previousChar == this.cDelimiter)
                    {
                        // workaround to allow add quotes to quote text with ex. "" are normal quote
                        if (previousChar == this.cEnclosure)
                        {
                            currentItem.Append(ch);
                        }
                        readType = ReadType.QuoteText;
                    }
                    // turn off quote text state of reader automat
                    else
                    {
                        readType = ReadType.CurrentLine;
                    }
                }
                else if (ch == this.cDelimiter)
                {
                    // if new line then change back to current line reader state
                    if (readType == ReadType.NewLine)
                    {
                        readType = ReadType.CurrentLine;
                    }
                    // if in quotes allow add delimiter to text
                    if (readType == ReadType.QuoteText)
                    {
                        currentItem.Append(ch);
                    }
                    // if normal delimiter, then add current item to items list and reset current item text
                    else
                    {
                        items.Add(currentItem.ToString());
                        currentItem.Length = 0;
                    }
                }
                else
                {
                    // if new line then change back to current line reader state
                    if (readType == ReadType.NewLine)
                    {
                        readType = ReadType.CurrentLine;
                    }
                    // add char to current item
                    currentItem.Append(ch);
                }
                // set previous char
                previousChar = ch;
            }
            // workaround for those files not ending with new line
            if ((items.Count > 0) || (currentItem.Length > 0))
            {
                Line++;
                items.Add(currentItem.ToString());
                if (fillItemAction != null)
                {
                    fillItemAction(items);
                }
                yield return items;
            }
        }
    }
}
