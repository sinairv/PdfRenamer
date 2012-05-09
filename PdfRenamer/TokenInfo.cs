using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToTextViewer
{
    /// <summary>
    /// This class must remain immutable
    /// </summary>
    public class TokenInfo
    {
        public string Value { get; private set; }
        public int Index { get; private set; }

        public TokenInfo(string token, int index)
        {
            Value = token;
            Index = index;
        }

        public int Length { get { return Value.Length; } }

        public int EndIndex
        {
            get { return Index + Length - 1; }
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
