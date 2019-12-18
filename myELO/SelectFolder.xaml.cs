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

namespace myELO
{
    /// <summary>
    /// Interaktionslogik für SelectFolder.xaml
    /// </summary>
    public partial class SelectFolder : Window
    {
        public string folderName = null;
        public SelectFolder()
        {
            InitializeComponent();
        }

        private void B_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void B_Apply_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            folderName = TB_FolderName.Text;
            Close();
        }
    }
}
