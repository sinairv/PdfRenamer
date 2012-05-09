using System.Collections.Generic;

namespace PdfRenamer
{
    public class PdfFirstPartData
    {
        public List<string> LineContents; //= new List<string>();
        public List<int> LineLengths; //= new List<int>();
        public List<int> LineStarts; //= new List<int>();
        public List<int> WordsPerLine;// = new List<int>();

        //public PdfStyles PdfStyle;

        public int Count
        {
            get
            {
                if (LineContents != null)
                    return LineContents.Count;
                return 0;
            }
        }
    }
}
