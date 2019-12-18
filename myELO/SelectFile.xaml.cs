using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace myELO
{
    /// <summary>
    /// Interaktionslogik für SelectFile.xaml
    /// </summary>
    public partial class SelectFile : Window
    {
        public List<string> allTags = new List<string>();
        public List<string> fileTags = new List<string>();
        public string fileName = string.Empty;
        public string filePath = string.Empty;
       
        public SelectFile(List<string> AllTags, List<string> FileTags, string FileName, string FilePath)
        {
            InitializeComponent();
            allTags = AllTags;
            fileTags = FileTags;
            fileName = FileName;
            filePath = FilePath;
            foreach (string s in allTags)
            {
                CB_Tags.Items.Add(s);
            }
            foreach (string s in fileTags)
            {
                CB_RemoveTag.Items.Add(s);
                TB_Tags.Text += "'" + s + "', ";
            }
            if (fileName != string.Empty && fileName != null)
                TB_Name.Text = fileName;
            if (filePath != string.Empty && filePath != null)
                TB_Path.Text = filePath;
        }

        private void B_SelectFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.FileOk += Ofd_FileOk;
            ofd.ShowDialog();
        }

        private void Ofd_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            TB_Path.Text = ( (OpenFileDialog)sender ).FileName;
        }

        private void CB_Tags_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            B_AddTag.IsEnabled = ( CB_Tags.SelectedItem != null ) ? true : false;
        }

        private void TB_NewTag_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (( (TextBox)sender ).Text != null && ( (TextBox)sender ).Text != string.Empty && ( (TextBox)sender ).Text.Replace(" ","") != string.Empty )
                B_NewTag.IsEnabled = true;
            else B_NewTag.IsEnabled = false;
        }

        private void TB_Path_TextChanged(object sender, TextChangedEventArgs e)
        {
            ( (TextBox)sender ).ToolTip = ( (TextBox)sender ).Text;
        }

        private void B_AddTag_Click(object sender, RoutedEventArgs e)
        {
            if (TB_Tags.Text.Contains("'" + CB_Tags.SelectedItem.ToString() + "',"))
            {
                MessageBox.Show("This Tag is already assigned to the file.");
                return;
            }
            TB_Tags.Text += "'" + CB_Tags.SelectedItem.ToString() + "', ";
            fileTags.Add(CB_Tags.SelectedItem.ToString());
            fileTags.Sort();
            CB_RemoveTag.Items.Clear();
            foreach (string s in fileTags)
            {
               CB_RemoveTag.Items.Add(s);
            }
        }

        private void B_NewTag_Click(object sender, RoutedEventArgs e)
        {
            if (TB_Tags.Text.Contains("'" + TB_NewTag.Text + "',"))
            {
                MessageBox.Show("The Tag '" + TB_NewTag.Text + "' is already assigned to the file.");
                return;
            }

            CB_RemoveTag.Items.Add(TB_NewTag.Text);
            TB_Tags.Text += "'" + TB_NewTag.Text + "', ";
            if (!allTags.Contains(TB_NewTag.Text))
            {
                CB_Tags.Items.Add(TB_NewTag.Text);
                allTags.Add(TB_NewTag.Text);
                allTags.Sort();
                CB_Tags.Items.Clear();
                foreach (string s in allTags)
                {
                    CB_Tags.Items.Add(s);
                }
                fileTags.Add(TB_NewTag.Text);
                fileTags.Sort();
                CB_RemoveTag.Items.Clear();
                foreach (string s in fileTags)
                {
                    CB_RemoveTag.Items.Add(s);
                }
            }
            else if (!fileTags.Contains(TB_NewTag.Text))
            {
                fileTags.Add(TB_NewTag.Text); 
            }            
        }

        private void B_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void B_Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            filePath = TB_Path.Text;
            fileName = TB_Name.Text;
            Close();
        }

        private void B_RemoveTag_Click(object sender, RoutedEventArgs e)
        {
            TB_Tags.Text = TB_Tags.Text.Replace("'" + CB_RemoveTag.SelectedItem.ToString() + "', ", "");
            fileTags.Remove(CB_RemoveTag.SelectedItem.ToString());
            CB_RemoveTag.Items.Remove(CB_RemoveTag.SelectedItem);
        }

        private void CB_RemoveTag_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            B_RemoveTag.IsEnabled = ( CB_RemoveTag.SelectedItem != null ) ? true : false;
        }
            

    }
}
