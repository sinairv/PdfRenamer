using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using Path = System.Windows.Shapes.Path;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Collections.Generic;

namespace PdfRenamer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool m_sugNameChanged = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void FolderContentRefresh(object sender, RoutedEventArgs e)
        {
            var col = GetPdfCollection();
            col.ReloadFolderContents();
        }

        private void BtnApplyClicked(object sender, RoutedEventArgs e)
        {
            var col = GetPdfCollection();
            if (col.Count <= 0)
                return;

            var lstRemoved = new List<PdfItemInfo>();

            foreach(var item in col)
            {
                if(item.Checked)
                {
                    if(TryRenameItem(item, col))
                        lstRemoved.Add(item);
                }
                else
                {
                    //lstRemoved.Add(item);
                }
            }

            foreach(var item in lstRemoved)
            {
                col.Remove(item);
            }
        }

        private bool TryRenameItem(PdfItemInfo item, PdfItemCollection col)
        {
            string dir = col.DirectoryPath;
            string oldFileName = System.IO.Path.Combine(dir, item.OriginalName);
            try
            {
                string newFileName = System.IO.Path.Combine(dir, item.SuggestedName);
                if (File.Exists(newFileName))
                {
                    const int maxTries = 100;
                    int counter = 1;
                    for (; counter < maxTries; counter++)
                    {
                        string ext = System.IO.Path.GetExtension(newFileName);
                        string baseFileName =
                            System.IO.Path.GetFileNameWithoutExtension(newFileName) +
                            String.Format(" ({0})", counter) + ext;

                        string dirName = System.IO.Path.GetDirectoryName(newFileName);
                        newFileName = System.IO.Path.Combine(dirName, baseFileName);
                        if (!File.Exists(newFileName))
                            break;
                    }

                    if (counter == maxTries)
                    {
                        item.Message = "Could not rename file! Reason: files with this name and its numbered variants already exist.";
                        return false;
                    }
                }

                if (oldFileName != newFileName)
                {
                    try
                    {
                        File.Move(oldFileName, newFileName);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        item.Message = "Could not rename file! Reason: " + ex.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                item.Message = "Could not rename file! Reason: " + ex.Message;
            }

            return false;
        }

        private void RenameItem(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var listItem = ((ListBoxItem)lstPdfItems.ContainerFromElement((DependencyObject)sender));
            if (listItem == null) return;
            var pdfItem = (PdfItemInfo)listItem.Content;
            if (pdfItem == null) return;

            var col = GetPdfCollection();
            if(TryRenameItem(pdfItem, col))
                col.Remove(pdfItem);
        }

        private PdfItemCollection GetPdfCollection()
        {
            return (PdfItemCollection)this.Resources["PdfCollection"];
        }

        private void BtnBrowseClicked(object sender, RoutedEventArgs e)
        {
            var col = GetPdfCollection();
            var dlg = new System.Windows.Forms.FolderBrowserDialog();
            if (Directory.Exists(col.DirectoryPath))
                dlg.SelectedPath = col.DirectoryPath;

            var dlgRes = dlg.ShowDialog();
            if(dlgRes == System.Windows.Forms.DialogResult.OK)
            {
                col.DirectoryPath = dlg.SelectedPath;
            }
        }
        
        private void OpenSelectedPdfFile(string fileName)
        {
            var psi = new ProcessStartInfo(fileName) { UseShellExecute = true };
            var procOpen = new Process { StartInfo = psi };
            procOpen.Start();
        }

        private void BtnCheckAllClicked(object sender, RoutedEventArgs e)
        {
            var col = GetPdfCollection();
            col.CheckAll();
        }

        private void BtnCheckNoneClicked(object sender, RoutedEventArgs e)
        {
            var col = GetPdfCollection();
            col.UncheckAll();
        }

        private void BtnHideEqualNamesClicked(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var col = GetPdfCollection();
            var lstToDel = new List<PdfItemInfo>();
            foreach(var item in col)
            {
                if (item.IsValid && item.SuggestedName == item.OriginalName)
                    lstToDel.Add(item);
            }

            foreach (var item in lstToDel)
                col.Remove(item);
        }

        private void BtnHideLargerOrigNamesClicked(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var col = GetPdfCollection();
            var lstToDel = new List<PdfItemInfo>();
            foreach (var item in col)
            {
                if (item.SuggestedName.Length < item.OriginalName.Length)
                    lstToDel.Add(item);
            }

            foreach (var item in lstToDel)
                col.Remove(item);
        }

        private void ViewPdf(object sender, RoutedEventArgs e)
        {
            var listItem = ((ListBoxItem) lstPdfItems.ContainerFromElement((DependencyObject) sender));
            if (listItem == null) return;
            var pdfItem = (PdfItemInfo)listItem.Content;
            if (pdfItem == null) return;

            var col = GetPdfCollection();
            string filePath = System.IO.Path.Combine(col.DirectoryPath, pdfItem.OriginalName);
            OpenSelectedPdfFile(filePath);
        }

        private void HideItem(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var listItem = ((ListBoxItem)lstPdfItems.ContainerFromElement((DependencyObject)sender));
            if (listItem == null) return;
            var pdfItem = (PdfItemInfo)listItem.Content;
            if (pdfItem == null) return;

            var col = GetPdfCollection();
            col.Remove(pdfItem);
        }


        private void ControlGotFocused(object sender, RoutedEventArgs e)
        {
            var listItem = ((ListBoxItem)lstPdfItems.ContainerFromElement((DependencyObject)sender));
            if (listItem == null) return;
            listItem.IsSelected = true;
        }

        //private void ListBoxKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        //{
        //    if(e.Key == System.Windows.Input.Key.Delete)
        //    {
        //        var selItems = lstPdfItems.SelectedItems;
        //        int ind = lstPdfItems.SelectedIndex;

        //        var list = new System.Collections.ArrayList(selItems);
        //        var col = GetPdfCollection();
        //        foreach (var item in list)
        //            col.Remove((PdfItemInfo)item);

        //        if(col.Count > 0 && ind >= 0)
        //        {
        //            if (ind >= col.Count) ind = col.Count - 1;
        //            lstPdfItems.SelectedIndex = ind;
        //        }
        //    }
        //    else if(e.Key == System.Windows.Input.Key.Space)
        //    {
        //        var item = (PdfItemInfo) lstPdfItems.Items[lstPdfItems.SelectedIndex];
        //        item.Checked = !item.Checked;
        //    }
        //}

        private void SugNameLostFocus(object sender, RoutedEventArgs e)
        {
            if (m_sugNameChanged)
            {
                m_sugNameChanged = false;
                var textBox = (TextBox) sender;
                var listItem = (ListBoxItem) lstPdfItems.ContainerFromElement(textBox);
                if (listItem == null) return;
                var item = (PdfItemInfo) listItem.Content;
                if (item == null) return;
                item.Checked = true;
            }
        }

        private void SugNameTextChanged(object sender, TextChangedEventArgs e)
        {
            m_sugNameChanged = true;
        }
    }
}
