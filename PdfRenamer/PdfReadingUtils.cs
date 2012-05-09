using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;

namespace PdfRenamer
{
    public class PdfReadingUtils
    {
        private const string XpdfBinsFolder = "./XPdfBins";
        private const string Pdf2TextExe = "pdftotext.exe";
        private const string PdfInfoExe = "pdfinfo.exe";

        public static string ReadPdfContent(string pdfPath, int page = -1)
        {
            var execPath = Path.Combine(XpdfBinsFolder, Pdf2TextExe);
            if (!File.Exists(execPath))
            {
                MessageBox.Show("Exec file does not exist!");
                return "";
            }

            string tmpRawContent = Path.GetTempFileName();
            string args;
            if (page >= 1)
            {
                args = String.Format("{0} -f {3} -l {4} \"{1}\" \"{2}\"", "-enc UTF-8 -layout -nopgbrk", pdfPath, tmpRawContent, page, page);
            }
            else
            {
                args = String.Format("{0} \"{1}\" \"{2}\"", "-enc UTF-8 -layout -nopgbrk", pdfPath, tmpRawContent);

            }
            var psi = new ProcessStartInfo(execPath, args)
            {
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            };

            var proc = new Process { StartInfo = psi };
            proc.Start();
            proc.WaitForExit();

            var sb = new StringBuilder();

            string fileContent = File.ReadAllText(tmpRawContent);
            return fileContent;
        }

        public static string ReadPdfInfo(string pdfFilePath)
        {
            var execPath = Path.Combine(XpdfBinsFolder, PdfInfoExe);
            if (!File.Exists(execPath))
            {
                MessageBox.Show("Exec file does not exist!");
                return "";
            }

            //string tmpRawContent = Path.GetTempFileName();
            string args = String.Format("\"{0}\"", pdfFilePath);
            var psi = new ProcessStartInfo(execPath, args)
            {
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var proc = new Process { StartInfo = psi };
            proc.Start();
            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();
            return output;
        }

        public static string ReadLayoutedView(string pdfFilePath)
        {
            return ReadPdfContent(pdfFilePath);
        }

        public static string ReadHtmlMetaContent(string pdfPath)
        {
            var execPath = Path.Combine(XpdfBinsFolder, Pdf2TextExe);
            if (!File.Exists(execPath))
            {
                MessageBox.Show("Exec file does not exist!");
                return "";
            }

            string tmpRawContent = Path.GetTempFileName();
            string args = String.Format("{0} \"{1}\" \"{2}\"", "-htmlmeta", pdfPath, tmpRawContent);
            var psi = new ProcessStartInfo(execPath, args)
            {
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            };

            var proc = new Process { StartInfo = psi };
            proc.Start();
            proc.WaitForExit();

            return File.ReadAllText(tmpRawContent);
        }

        public static string ReadVisibleLayoutContent(string pdfPath)
        {
            var execPath = Path.Combine(XpdfBinsFolder, Pdf2TextExe);
            if (!File.Exists(execPath))
            {
                MessageBox.Show("Exec file does not exist!");
                return "";
            }

            string tmpRawContent = Path.GetTempFileName();
            string args = String.Format("{0} \"{1}\" \"{2}\"", "-enc UTF-8 -layout -nopgbrk", pdfPath, tmpRawContent);
            var psi = new ProcessStartInfo(execPath, args)
            {
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            };

            var proc = new Process { StartInfo = psi };
            proc.Start();
            proc.WaitForExit();

            return StringUtils.GetVisibleFileContent(tmpRawContent);
        }

        public static string ReadRawContent(string pdfPath)
        {
            var execPath = Path.Combine(XpdfBinsFolder, Pdf2TextExe);
            if (!File.Exists(execPath))
            {
                MessageBox.Show("Exec file does not exist!");
                return "";
            }

            string tmpRawContent = Path.GetTempFileName();
            string args = String.Format("{0} \"{1}\" \"{2}\"", "-raw", pdfPath, tmpRawContent);
            var psi = new ProcessStartInfo(execPath, args)
            {
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            };

            var proc = new Process { StartInfo = psi };
            proc.Start();
            proc.WaitForExit();

            return StringUtils.GetVisibleFileContent(tmpRawContent);
        }

    }
}
