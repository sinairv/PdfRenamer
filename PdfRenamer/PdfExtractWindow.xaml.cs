using System.IO;
using System.Windows;
using System.Diagnostics;
using System;
using System.Windows.Controls;

namespace PdfRenamer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class PdfExtractWindow : Window
    {
        public PdfExtractWindow()
        {
            InitializeComponent();
            ReloadFolderContents();
        }

        private void BtnExtractClick(object sender, RoutedEventArgs e)
        {
            string pdfFilePath = tbPdfPath.Text;
            if (!File.Exists(pdfFilePath))
            {
                MessageBox.Show("PDF file does not exist!");   
                return;
            }

            var pdfData = ContentRetreivalUtils.GetExtractedInfo(pdfFilePath);
            tbExtractedInfo.Text = pdfData == null ? "null" : pdfData.ToString();

            tbLayoutView.Text = PdfReadingUtils.ReadLayoutedView(pdfFilePath);
            //tbRawContent.Text = PdfReadingUtils.ReadRawContent(pdfFilePath);
            tbLayoutContent.Text = PdfReadingUtils.ReadVisibleLayoutContent(pdfFilePath);
            //tbHtmlContent.Text = PdfReadingUtils.ReadHtmlMetaContent(pdfFilePath);
            //try
            //{
            //    browser.NavigateToString(tbHtmlContent.Text);
            //}
            //catch
            //{
            //}
            //tbPdfInfo.Text = PdfReadingUtils.ReadPdfInfo(pdfFilePath);
        }
       

        private void tbCurFolder_LostFocus(object sender, RoutedEventArgs e)
        {
            ReloadFolderContents();
        }

        private void ReloadFolderContents()
        {
            string folderName = tbCurFolder.Text;
            if(!Directory.Exists(folderName))
            {
                MessageBox.Show(String.Format("No such direcotry exists: '{0}'", folderName));
                return;
            }

            var pdfFiles = Directory.GetFiles(folderName, "*.pdf");

            lstFiles.Items.Clear();
            foreach (var file in pdfFiles)
            {
                lstFiles.Items.Add(Path.GetFileName(file));
            }
        }

        private void lstFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(lstFiles.SelectedItem != null)
                tbPdfPath.Text = Path.Combine(tbCurFolder.Text, lstFiles.SelectedItem.ToString());
        }

        private void lstFiles_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if(lstFiles.SelectedItem != null)
            {
                string pdfFileName = Path.Combine(tbCurFolder.Text, lstFiles.SelectedItem.ToString());
                var psi = new ProcessStartInfo(pdfFileName) {UseShellExecute = true};
                var procOpen = new Process {StartInfo = psi};
                procOpen.Start();
            }

        }

        private void BtnCopyContentClick(object sender, RoutedEventArgs e)
        {
            var tabItem = tabCtrlMain.Items[tabCtrlMain.SelectedIndex] as TabItem;
            Debug.Assert(tabItem != null);
            var scrollViewer = tabItem.Content as ScrollViewer;
            Debug.Assert(scrollViewer != null);
            var textBox = scrollViewer.Content as TextBox;
            Debug.Assert(textBox != null);

            Clipboard.SetText(textBox.Text);
            

        }
    }
}
