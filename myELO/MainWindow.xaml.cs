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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Configuration;
using System.IO;
using Microsoft.Win32;
using System.Timers;
using System.Windows.Xps.Packaging;
using System.ComponentModel;

namespace myELO
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Liste der von den FileWatchers zu Überwachenden Dateien
        /// </summary>
        private List<watchedFile> filesToWatch_List = new List<watchedFile>();
        /// <summary>
        /// Liste der Temporär gelöschten Dateien (für verschieben der Dateien)
        /// </summary>
        private List<watchedFile> filesDeleted = new List<watchedFile>();
        private List<string> existingTags = new List<string>();
        private List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();
        private Timer saveTimer = new Timer(600000); //10minuten = 600000ms

        public MainWindow()
        {
            InitializeComponent();
            InitializeWatchers();
            InitializeFiles();
            InitializeTreeView();
            saveTimer.Elapsed += SaveTimer_Elapsed;
            saveTimer.Start();

            // öffnet die Datei mit Standardprogramm
            // System.Diagnostics.Process.Start(@"C:\Users\vm_user\Desktop\configuration - Kopie.xsd");
            // öffnet den angegebenen Ordner im Windowsexplorer
            // System.Diagnostics.Process.Start(@"C:\Users\vm_user\Desktop\");    
            // öffnet die Datei mit Standardprogramm
            
        }        
        /// <summary>
        /// initializes fileWatchers für each fileEnding within the watchers.elo file
        /// </summary>
        private void InitializeWatchers()
        {
            watchers.Clear();
            if (!File.Exists(@"..\watchers.elo"))
                return;            
            foreach (string s in File.ReadAllLines(@"..\watchers.elo"))
            {
                FileSystemWatcher fsw = new FileSystemWatcher();
                fsw.IncludeSubdirectories = false;
                fsw.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                fsw.Filter = s;
                fsw.Path = @"C:\";
                fsw.IncludeSubdirectories = true;
                fsw.Created += new FileSystemEventHandler(OnFileChanged);
                fsw.Deleted += new FileSystemEventHandler(OnFileChanged);
                fsw.Renamed += new RenamedEventHandler(OnFileRenamed);
                fsw.EnableRaisingEvents = true;
                watchers.Add(fsw);
            }           

        }
        /// <summary>
        /// creates a list of files to be watched from the files.elo file
        /// </summary>
        private void InitializeFiles()
        {
            filesToWatch_List.Clear();
            if (!File.Exists(@"..\files.elo"))
                return;
            foreach (string s in File.ReadAllLines(@"..\files.elo"))
            {
                string[] parts = s.Split(';');
                List<string> tags = new List<string>(parts[2].Split(','));
                watchedFile w = new watchedFile(parts[0], parts[1], tags);
                filesToWatch_List.Add(w);
                foreach (string tag in tags)
                {
                    if (!existingTags.Contains(tag))
                        existingTags.Add(tag);
                }
            }
        }
        /// <summary>
        /// initializes the treeview with all items of the treeView.elo file
        /// </summary>
        private void InitializeTreeView()
        {
            TV_Sort.Items.Clear();
            if (!File.Exists(@"..\treeView.elo"))
                return;

            List<List<string>> filesAndFolders = new List<List<string>>();
            List<string> addList = new List<string>();
            
            //erstelle für jedes level 1 element neue liste
            foreach (string s in File.ReadAllLines(@"..\treeView.elo"))
            {
                int level = Convert.ToInt32(s.Split(new string[] { "###" }, StringSplitOptions.None)[0].Split(' ')[1]);                
                if (level == 1 && addList.Count>0)
                {
                    filesAndFolders.Add(addList);
                    addList = new List<string>();
                }
                addList.Add(s);
            }
            filesAndFolders.Add(addList);

            // erstelle für jedes liste ein wurzelelement, füge unterelemente aus listenelementen hinzu, füge wurzelelemnt dem treeview hinzu 
            foreach (List<string> l in filesAndFolders)
            {
                if (l.Count == 0)
                    continue;

                if (l[0].StartsWith("File"))
                {   // bei einer datei gibt es keine unterelemente
                    int level = Convert.ToInt32(l[0].Split(new string[] { "###" }, StringSplitOptions.None)[0].Split(' ')[1]);
                    string NameInTV = l[0].Split(new string[] { "###" }, StringSplitOptions.None)[1];
                    string path = l[0].Split(new string[] { "###" }, StringSplitOptions.None)[2];
                    TV_Sort.Items.Add(createFile(NameInTV, path));
                }
                else
                {   //wurzelelement erstellen
                    TreeViewItem parent = createFolder(l[0].Split(new string[] { "###" }, StringSplitOptions.None)[1]);
                    int nextItem = 1;
                    while (nextItem < l.Count)
                    {   // unterelemente hinzufügen
                        parent.Items.Add(addSubs(l, ref nextItem));
                    }
                    // wurzelelement dem treeview hinzufügen
                    TV_Sort.Items.Add(parent);
                }
            }
        }


        // File Handling
        /// <summary>
        /// Used when file that is watched in added or deleted
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnFileChanged(object source, FileSystemEventArgs e)
        {
            string fullPath = e.FullPath;
            watchedFile w = filesToWatch_List.Find(x => x.path == fullPath);  
            string file = e.FullPath.Remove(0, e.FullPath.LastIndexOf(@"\") + 1);
                  
            // Datei wurde gelöscht, entferne aus Überwachungsliste, füge zu gelöscht liste hinzu
            if (e.ChangeType == WatcherChangeTypes.Deleted)
            {
                filesDeleted.Add(w);
                int a = filesToWatch_List.FindIndex(x => x.path == fullPath);
                if (a >= 0)
                    filesToWatch_List.RemoveAt(a);
            }
            // Datei wurde erstellt
            if (e.ChangeType == WatcherChangeTypes.Created)
            {
                // Überprüfe ob hinzugefügte Datei in gelöscht-Liste
                List<int> indizes = new List<int>();
                for (int i = 0; i < filesDeleted.Count; i++)
                {
                    if (filesDeleted[i].path.EndsWith(file))
                        indizes.Add(i);
                }
                if (indizes.Count > 1)
                    MessageBox.Show("mist, die datei existiert mehrfach in der überwachungsliste");
                // Wenn ja ist die hinzugefügte Datei nur verschoben worden -> lösche aus gelöscht, füge mit neuem pfad zu überwachung hinzu
                if (indizes.Count == 1)
                {
                    filesDeleted.RemoveAt(indizes[0]);
                    filesToWatch_List.Add(w);
                }
            }
            
        }
        /// <summary>
        /// Used when file that is watched is renamed
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnFileRenamed(object source, RenamedEventArgs e)
        {
            string oldFullPath = e.OldFullPath;
            string oldFile = e.OldFullPath.Remove(0, e.OldFullPath.LastIndexOf(@"\") + 1);
            string fullPath = e.FullPath;
            string file = e.FullPath.Remove(0, e.FullPath.LastIndexOf(@"\") + 1);

            // ist die umbenannte datei in Überwachungsliste?
            bool exists = false;
            for (int i=0;i<filesToWatch_List.Count;i++)
            {
                if ( filesToWatch_List[i].path.EndsWith(file) || filesToWatch_List[i].path.EndsWith(oldFile))
                {
                    exists = true;
                    break;
                }
            }
            
            // wenn ja, dann dateinamen aktualisieren
            if (exists)
            {
                MessageBox.Show(string.Format("File: {0} renamed to {1}", oldFullPath, fullPath));
                List<int> indizes = new List<int>();
                for (int i = 0; i < filesToWatch_List.Count; i++)
                { // entsprechenden index in der übwachungsliste raussuchen
                    if (filesToWatch_List[i].path==oldFullPath || filesToWatch_List[i].path.EndsWith(oldFile))
                        indizes.Add(i);
                }

                if (indizes.Count > 1)
                {
                    MessageBox.Show("mist, die datei ist zweimal in der Überwachungsliste");
                }
                if (indizes.Count == 1)
                {   // daten der überwachungsdatei aktualisieren
                    watchedFile w = new watchedFile(filesToWatch_List[indizes[0]].name, fullPath, filesToWatch_List[indizes[0]].tags);
                    filesToWatch_List.RemoveAt(indizes[0]);
                    filesToWatch_List.Add(w);
                }
            }
        }    

        //Menu Items Main Menu
        private void MI_Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void MI_AddFile_Click(object sender, RoutedEventArgs e)
        {
            showFileEditor();
        }

        private void showFileEditor()
        {
            List<string> fileTags = new List<string>(new string[] { "Tag1" });

            SelectFile winSF = new myELO.SelectFile(existingTags, fileTags, "NeuerDateiName", null);
            if (winSF.ShowDialog() == true)
            {
                createNewFileWatcher(new watchedFile(winSF.fileName, winSF.filePath, winSF.fileTags));
                if (TV_Sort.SelectedItem == null)
                    TV_Sort.Items.Add(createFile(winSF.fileName, winSF.filePath));
                else ( (TreeViewItem)TV_Sort.SelectedItem ).Items.Add(createFile(winSF.fileName, winSF.filePath));
            }
        }

        /// <summary>
        /// Creates a filewatcher for the FileEnding of the watched file
        /// </summary>
        /// <param name="w">the file to watch for</param>
        private void createNewFileWatcher(watchedFile w)
        {
            if (w.path == null || w.path == string.Empty)
                return;
            string fileName = w.path.Remove(0, w.path.LastIndexOf(@"\") + 1);
            filesToWatch_List.Add(w);

            // neuen fileWatcher für Dateiendung erstellen
            FileSystemWatcher fsw = new FileSystemWatcher();
            fsw.Filter = "*" + new FileInfo(w.path).Extension;
           
            foreach (FileSystemWatcher watcher in watchers)
            { //gibt es für die Endung schon eine Überwachung?
                if (watcher.Filter == fsw.Filter)
                    return;
            }

            fsw.IncludeSubdirectories = false;
            fsw.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            fsw.Path = @"C:\";
            fsw.IncludeSubdirectories = true;
            fsw.Created += new FileSystemEventHandler(OnFileChanged);
            fsw.Deleted += new FileSystemEventHandler(OnFileChanged);
            fsw.Renamed += new RenamedEventHandler(OnFileRenamed);
            fsw.EnableRaisingEvents = true;

            watchers.Add(fsw);

        }

        //Tree View
        void TV_Sort_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {                
                TreeViewItem dragTvi = null;
                switch (((DependencyObject)e.Source).DependencyObjectType.SystemType.Name) 
                {
                    case "TextBlock":
                        dragTvi = ( (TextBlock)e.Source ).Parent as TreeViewItem;
                        break;
                    case "Image":
                        dragTvi = ( (StackPanel)( (Image)e.Source ).Parent ).Parent as TreeViewItem;
                        break;
                    case "StackPanel":
                        dragTvi = ( (StackPanel)e.Source ).Parent  as TreeViewItem;
                        break;
                    case "Label":
                        dragTvi = ( (StackPanel)( (Label)e.Source ).Parent ).Parent as TreeViewItem;
                        break;
                    case "TreeViewItem":
                        dragTvi = e.Source as TreeViewItem;
                        break;
                }

                if (dragTvi != null)
                {
                    DragDrop.DoDragDrop(TV_Sort, dragTvi, DragDropEffects.Move);
                    e.Handled = true;
                }
            }
        }
        void TV_Sort_Drop(object sender, DragEventArgs e)
        {
            //DependencyObject uie = TV_Sort.InputHitTest(e.GetPosition(TV_Sort)) as DependencyObject;
            TreeViewItem dragTvi = e.Data.GetData(typeof(TreeViewItem)) as TreeViewItem;
            if (dragTvi == null)
                return;
            TreeViewItem dropTvi = null; 
            switch (((DependencyObject)e.Source).DependencyObjectType.SystemType.Name)
            {
                case "TextBlock":
                    dropTvi = ((TextBlock)e.Source).Parent as TreeViewItem;
                    break;
                case "Image":
                    dropTvi = ( (StackPanel)( (Image)e.Source ).Parent ).Parent as TreeViewItem;
                    break;
                case "StackPanel":
                    dropTvi = ( (StackPanel)e.Source ).Parent as TreeViewItem;
                    break;
                case "Label":
                    dropTvi = ( (StackPanel)( (Label)e.Source ).Parent ).Parent as TreeViewItem;
                    break;
                case "TreeViewItem":
                    dropTvi = e.Source as TreeViewItem;
                    break;
            }

            
                      
            if (dropTvi!=null)
            {
                if (dropTvi == dragTvi)
                    return;
                if (( (StackPanel)dropTvi.Header ).Name == "SP_File")
                {
                    MessageBox.Show("geht ni");
                    return;
                }                 

                if (( dragTvi.Parent as TreeViewItem ) != null)
                    ( dragTvi.Parent as TreeViewItem ).Items.Remove(dragTvi);
                else
                    TV_Sort.Items.Remove(dragTvi);

                dropTvi.Items.Add(dragTvi);
                e.Handled = true;
            }

            if (e.Source is TreeView)
            {
                TreeView tv = e.Source as TreeView;                

                if (( dragTvi.Parent as TreeViewItem ) != null)
                    ( dragTvi.Parent as TreeViewItem ).Items.Remove(dragTvi);
                else
                    TV_Sort.Items.Remove(dragTvi);

                tv.Items.Add(dragTvi);
                e.Handled = true;
            }

        }
        private TreeViewItem createFolder(string Name)
        {
            TreeViewItem tvi = new TreeViewItem();            
            // items of treeviewitem header
            StackPanel sp = new StackPanel();
            Image i = new Image();
            Label l = new Label();

            l.Content = Name;

            System.Drawing.Bitmap dImg = (System.Drawing.Bitmap)Properties.Resources.ResourceManager.GetObject("Folder");
            MemoryStream ms = new MemoryStream();
            dImg.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            BitmapImage bImg = new BitmapImage();
            bImg.BeginInit();
            bImg.StreamSource = new MemoryStream(ms.ToArray());
            bImg.EndInit();

            i.Source = bImg;
            i.Height = 20;
            i.Width = 20;

            sp.Name = "SP_Folder";
            sp.Orientation = Orientation.Horizontal;
            sp.Children.Add(i);
            sp.Children.Add(l);
                        
            tvi.Header = sp;            

            // create contextmenu
            ContextMenu cm = new ContextMenu();
            MenuItem mi1 = new MenuItem();
            mi1.Header = "Add Folder";
            mi1.Click += Mi1_Click;
            MenuItem mi2 = new MenuItem();
            mi2.Header = "Add File";
            mi2.Click += Mi2_Click;
            cm.Items.Add(mi1);
            cm.Items.Add(mi2);
            tvi.ContextMenu = cm;

            tvi.MouseRightButtonUp += Tvi_MouseRightButtonUp;

            return tvi;
        }
        private TreeViewItem createFile(string Name, string soureFile)
        {
            TreeViewItem tvi = new TreeViewItem();
            StackPanel sp = new StackPanel();
            Image i = new Image();
            Label l_Text = new Label();
            Label l_FileName = new Label();

            l_FileName.Content = soureFile;
            l_FileName.Width = 0;
            l_Text.Content = Name;

            System.Drawing.Bitmap dImg = (System.Drawing.Bitmap)Properties.Resources.ResourceManager.GetObject("Picture");
            MemoryStream ms = new MemoryStream();
            dImg.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            BitmapImage bImg = new BitmapImage();
            bImg.BeginInit();
            bImg.StreamSource = new MemoryStream(ms.ToArray());
            bImg.EndInit();
            i.Source = bImg;
            i.Height = 20;
            i.Width = 20;

            sp.Name = "SP_File";
            sp.Orientation = Orientation.Horizontal;
            sp.Children.Add(i);
            sp.Children.Add(l_Text);
            sp.Children.Add(l_FileName);
            tvi.Header = sp;

            return tvi;
        }
        private void CMI_Addfolder_Click(object sender, RoutedEventArgs e)
        {
            SelectFolder sfd = new myELO.SelectFolder();
            if (sfd.ShowDialog() == true)
            {
                TV_Sort.Items.Add(createFolder(sfd.folderName));
            }
        }
        private void CMI_AddFile_Click(object sender, RoutedEventArgs e)
        {
            showFileEditor();
        }
        /// <summary>
        /// used for iterative adding sub items to the treeview
        /// </summary>
        /// <param name="l">list of subitems of one node after root</param>
        /// <param name="nextItem">the next item in the list</param>
        /// <returns></returns>
        private TreeViewItem addSubs(List<string> l, ref int nextItem)
        {   //alle Daten erstellen die in item vorhanden sein können
            bool file = l[nextItem].StartsWith("File");
            int level = Convert.ToInt32(l[nextItem].Split(new string[] { "###" }, StringSplitOptions.None)[0].Split(' ')[1]);
            string NameInTV = l[nextItem].Split(new string[] { "###" }, StringSplitOptions.None)[1];
            string path = file ? l[nextItem].Split(new string[] { "###" }, StringSplitOptions.None)[2] : null;

            // wenn es eine Datei ist kann diese keine weiteren Äste/Blätter haben
            if (file)
            {
                nextItem++;
                return createFile(NameInTV, path);
            }
            // bei Ordner alle unterelemente (in der liste) durchsuchen und hinzufügen
            else
            {
                TreeViewItem parent = createFolder(l[nextItem].Split(new string[] { "###" }, StringSplitOptions.None)[1]);
                nextItem++;
                //                                    wenn nextItem.level == aktParent.level dann  aktParent.Close() und aktParent.parent=nextItem.parent
                while (nextItem < l.Count && level < Convert.ToInt32(l[nextItem].Split(new string[] { "###" }, StringSplitOptions.None)[0].Split(' ')[1]))
                {
                    parent.Items.Add(addSubs(l, ref nextItem));
                }
                return parent;
            }
        }

        private void Mi2_Click(object sender, RoutedEventArgs e)
        {
            showFileEditor();
        }

        private void Tvi_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ( (TreeViewItem)sender ).IsSelected = true;
            ( (TreeViewItem)sender ).ContextMenu.IsOpen = true;
            e.Handled = true;
        }

        private void Mi1_Click(object sender, RoutedEventArgs e)
        {
            SelectFolder sfd = new myELO.SelectFolder();
            if (sfd.ShowDialog() == true)
            {
                if ((TreeViewItem)TV_Sort.SelectedItem != null)
                    ( (TreeViewItem)TV_Sort.SelectedItem ).Items.Add(createFolder(sfd.folderName));
                else TV_Sort.Items.Add(createFolder(sfd.folderName));
            }

        }

        //AutoSave
        private void SaveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            save();
        }

        // MainWindow
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            saveTimer.Stop();
            save();
        }

        private void save()
        {
            List<string> watcherText = new List<string>();
            List<string> filesText = new List<string>();
            List<string> successionText = new List<string>();

            foreach (watchedFile w in filesToWatch_List)
            {
                string tags = string.Empty;
                foreach (string s in w.tags)
                {
                    tags += s + ",";
                }
                tags = tags.Remove(tags.Length - 1, 1);
                filesText.Add(w.name + ";" + w.path + ";" + tags);
                if (!watcherText.Contains("*" + new FileInfo(w.path).Extension))
                    watcherText.Add("*" + new FileInfo(w.path).Extension);
            }
            
            foreach (TreeViewItem tvi in TV_Sort.Items)
            {
                if (((StackPanel)tvi.Header).Children.Count==2)
                {
                    successionText.Add("FolderLevel 1###"+( (Label)( (StackPanel)tvi.Header ).Children[1] ).Content.ToString());
                    if (tvi.HasItems)
                        successionText.AddRange(treeViewSuccession(tvi).ToArray());
                }
                else if (( (StackPanel)tvi.Header ).Children.Count == 3)
                { //file level###nameintreeview###filepath
                    successionText.Add("FileLevel 1###" + ( (Label)( (StackPanel)tvi.Header ).Children[1] ).Content.ToString()
                        + "###" + ( (Label)( (StackPanel)tvi.Header ).Children[2] ).Content.ToString());
                }
            }

            File.WriteAllLines(@"..\watchers.elo", watcherText.ToArray());
            File.WriteAllLines(@"..\files.elo", filesText.ToArray());
            File.WriteAllLines(@"..\treeView.elo", successionText.ToArray());
        }

        private List<string> treeViewSuccession(TreeViewItem Parent)
        {
            List<string> successionReturn = new List<string>();
            foreach (TreeViewItem tvi in Parent.Items)
            {
                int iteration = 1;
                TreeViewItem tviIteration = tvi;
                while (tviIteration.Parent != TV_Sort)
                {
                    tviIteration = (TreeViewItem)tviIteration.Parent;
                    iteration++;
                }

                if (( (StackPanel)tvi.Header ).Children.Count == 2)
                {
                    successionReturn.Add("FolderLevel "+iteration.ToString()+"###"+( (Label)( (StackPanel)tvi.Header ).Children[1] ).Content.ToString());
                    if (tvi.HasItems)
                        successionReturn.AddRange(treeViewSuccession(tvi).ToArray());
                }
                else if (( (StackPanel)tvi.Header ).Children.Count == 3)
                {
                    successionReturn.Add("FileLevel " + iteration.ToString() + "###" + ( (Label)( (StackPanel)tvi.Header ).Children[1] ).Content.ToString()
                        +"###"+ ( (Label)( (StackPanel)tvi.Header ).Children[2] ).Content.ToString());
                }
            }
            return successionReturn;
        }
        
    }

    class watchedFile
    {
        public string name { get; private set; }
        public string path { get; private set; }
        public List<string> tags { get; private set; }

        public watchedFile(string Name, string Path, List<string> Tags)
        {
            name = Name;
            path = Path;
            tags = Tags;
        }        
    }
}
