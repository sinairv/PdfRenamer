using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.IO;
using System.ComponentModel;

namespace PdfRenamer
{
    public class PdfItemCollection : ObservableCollection<PdfItemInfo>
    {
        private string _dirPath = "";
        public string DirectoryPath 
        {
            get { return _dirPath; }
            set
            {
                _dirPath = value; 
                Clear(); 
                OnPropertyChanged(new PropertyChangedEventArgs("DirectoryPath"));
            }
        }

        private string _replacementPattern = "";
        public string ReplacementPattern 
        {
            get { return _replacementPattern; }
            set { _replacementPattern = value; OnPropertyChanged(new PropertyChangedEventArgs("ReplacementPattern")); }
        }

        public void ReloadFolderContents()
        {
            string folderName = DirectoryPath;
            if (String.IsNullOrEmpty(folderName) || !Directory.Exists(folderName))
            {
                throw new IOException(String.Format("No such direcotry exists: '{0}'", folderName));
            }

            Clear();

            string pattern = ReplacementPattern;
            pattern = Regex.Replace(pattern, @"\$\(\s*title\s*\)", "{0}", RegexOptions.IgnoreCase);
            pattern = Regex.Replace(pattern, @"\$\(\s*author\s*\)", "{1}", RegexOptions.IgnoreCase);
            pattern = Regex.Replace(pattern, @"\$\(\s*publisher\s*\)", "{2}", RegexOptions.IgnoreCase);
            pattern = Regex.Replace(pattern, @"\$\(\s*year\s*\)", "{3}", RegexOptions.IgnoreCase);

            var pdfFiles = Directory.GetFiles(folderName, "*.pdf");
            //var textInfo = new CultureInfo("en-US", false).TextInfo;

            foreach (var pdfFile in pdfFiles)
            {
                var pdfData = ContentRetreivalUtils.GetExtractedInfo(pdfFile);
                var pdfFileName = Path.GetFileName(pdfFile);
                var sugFileName = pdfFileName;
                bool isValid = pdfData != null && !pdfData.IsTitleInValid();

                if (isValid)
                {
                    pdfData.CleanContent();

                    string title = "[NULL]";

                    if (!String.IsNullOrEmpty(pdfData.Title))
                    {
                        title = pdfData.Title;
                    }

                    string auths = "[NULL]";
                    if (pdfData.AuthorNames != null && pdfData.AuthorNames.Count > 0)
                    {
                        auths = pdfData.AuthorsToString;
                    }

                    string pub = "[NULL]";
                    if (!String.IsNullOrEmpty(pdfData.PubName))
                    {
                        pub = pdfData.PubName;
                    }

                    string year = "[NULL]";
                    if (pdfData.Year > 0)
                    {
                        year = pdfData.Year.ToString(CultureInfo.InvariantCulture);
                    }

                    string newName = String.Format(pattern, title, auths, pub, year);
                    newName = Regex.Replace(newName, @"(\p{P})(\p{P}*\s*\[NULL\]\s*)+\p{P}", "$1");
                    newName = Regex.Replace(newName, @"\s*\p{P}+\s*\[NULL\]\s*$", "");
                    newName = Regex.Replace(newName, @"^\s*\[NULL\]\s*\p{P}+\s*", "");

                    newName = newName.Replace("[NULL]", "");
                    newName = newName.Trim() + ".pdf";
                    newName = PostProcessFileName(newName);
                    sugFileName = newName;
                }

                var item = new PdfItemInfo(pdfFileName, sugFileName, isValid && (pdfFileName != sugFileName));
                item.IsValid = isValid;

                if(!isValid)
                {
                    item.Message = "Could not extract useful information from PDF content!";
                }
                else if(isValid && (pdfFileName == sugFileName))
                {
                    item.Message = "The suggested name and the original name are already the same!";
                }

                Add(item);
            }
        }

        private static string PostProcessFileName(string newName)
        {
            // remove invalid characters from file name
            // \/:?*"<>|
            return newName.Replace(" \\ ", " - ").Replace("\\ ", " - ").Replace("\\", " - ")
                .Replace(" / ", " - ").Replace("/ ", " - ").Replace("/", " - ")
                .Replace(" : ", " - ").Replace(": ", " - ").Replace(":", " - ")
                .Replace(" | ", " - ").Replace("| ", " - ").Replace("|", " - ")
                .Replace("<", "(").Replace(">", ")").Replace("?", "").Replace("*", ".")
                .Replace("\"", "-");
        }

        public void CheckAll()
        {
            foreach (var item in this)
            {
                item.Checked = true;
            }
        }

        public void UncheckAll()
        {
            foreach (var item in this)
            {
                item.Checked = false;
            }
        }

    }
}
