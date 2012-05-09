using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PdfRenamer
{
    /// <summary>
    /// Interaction logic for EditBoxWindow.xaml
    /// </summary>
    public partial class EditBoxWindow : Window
    {
        public EditBoxWindow()
        {
            InitializeComponent();
        }

        public static string ShowDialog(string message, string formCaption, string text)
        {
            var dlg = new EditBoxWindow();
            dlg.Title = formCaption;
            dlg.lblMessage.Text = message;
            dlg.txtText.Text = text;
            dlg.txtText.Focus();
            dlg.txtText.SelectAll();
            dlg.ShowDialog();
            if (dlg.isOKPressed)
                return dlg.txtText.Text;
            else
                return null;
        }

        bool isOKPressed = false;
        private void BtnCancelPressed(object sender, RoutedEventArgs e)
        {
            isOKPressed = false;
            Close();
        }

        private void BtnOKPressed(object sender, RoutedEventArgs e)
        {
            isOKPressed = true;
            Close();
        }
    }
}
