using application.src.attachedProperties;
using application.src.core.disk;
using controller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace application
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private DiskController diskController;
        private List<String> disks;
        private Dictionary<string, TreeViewItem> hierarchyDict;
        private Dictionary<string, ToggleButton> scanButtonDict;
        private enum ButtonAction { start, pause, resume, end }


        public MainWindow()
        {
            this.diskController = new DiskController(this);
            this.disks = diskController.listDisks();
            this.hierarchyDict = new Dictionary<string, TreeViewItem>();
            this.scanButtonDict = new Dictionary<string, ToggleButton>();

            InitializeComponent();

            foreach (string disk in this.disks)
            {
                ToggleButton tb = new ToggleButton();
                tb.Content = disk;
                tb.Height = 50;
                tb.Margin = new Thickness(20, 5, 20, 5);
                tb.FontSize = 14;
                tb.Style = (Style)FindResource("DiskButtonTheme");
                tb.Tag = ButtonAction.start;
                tb.SetValue(ScanToggleButtonProperties.ActionTextProperty, "Start Scan");
                tb.Click += OnScanDiskClick;

                this.scanButtonDict.Add(disk, tb);
                spDiskList.Children.Add(tb);
            }
        }

        private string GetFolderNameWithPrefix(Folder folder)
        {
            return ".\\" + folder.GetFolderName();
        }

        private string DecorateWithStatInfo(Folder folder, string description)
        {
            return description + " (Files:" + folder.GetFileCount() + " | Total Size:" + folder.GetSize() + "MB)";
        }

        private string DecorateWithNotTracked(string description)
        {
            return description + " (Not tracked)";
        }

        private bool UpdateFolder(Folder folder)
        {
            if (!this.hierarchyDict.ContainsKey(folder.GetAbsolutePath())) { return false; }

            TreeViewItem tvItem = this.hierarchyDict[folder.GetAbsolutePath()];
            tvItem.Header = GetFolderDescription(folder, folder.IsTracked());

            return true;
        }

        private string GetFolderDescription(Folder folder, bool tracked)
        {
            string folderDesc = GetFolderNameWithPrefix(folder);

            if (tracked) { folderDesc = DecorateWithStatInfo(folder, folderDesc); }
            else { folderDesc = DecorateWithNotTracked(folderDesc); }

            return folderDesc;
        }

        private List<Folder> FindFolderRoot(Folder folder)
        {
            Folder? root = folder;
            List<Folder> stack = new List<Folder>();

            while (root?.GetParent() != null)
            {
                stack.Add(root);
                root = root.GetParent();
            }

            if (root != null)
            {
                stack.Add(root);
            }

            return stack;
        }

        private void RegisterFolderTreeView(Folder folder)
        {
            List<Folder> path = FindFolderRoot(folder);
            (int index, TreeViewItem tvRoot) = GetTreeViewRoot(path);

            for (int i = index - 1; i >= 0; i--)
            {
                TreeViewItem tvItem = TreeViewItemBuilder(path[i], i == 0);
                tvRoot.Items.Add(tvItem);
                tvRoot = tvItem;
            }
        }

        private TreeViewItem TreeViewItemBuilder(Folder folder, bool tracked)
        {
            TreeViewItem tvItem = new TreeViewItem();
            tvItem.Header = GetFolderDescription(folder, tracked);
            tvItem.IsExpanded = true;

            this.hierarchyDict.Add(folder.GetAbsolutePath(), tvItem);

            return tvItem;
        }

        public Tuple<int, TreeViewItem> GetTreeViewRoot(List<Folder> path)
        {
            // Path[end] = root (C:/)
            Folder folder = path.Last();
            TreeViewItem tvRoot;

            int i = path.Count - 1;

            // Finds the first folder that isn't registered on the tree
            while (i >= 0 && this.hierarchyDict.ContainsKey(path[i].GetAbsolutePath()))
            {
                i--;
            }

            // If it's none are registered create element
            if (i + 1 >= path.Count)
            {
                tvRoot = TreeViewItemBuilder(folder, false);
                this.tvFolderDiscovery.Items.Add(tvRoot);
            }
            else
            {
                tvRoot = this.hierarchyDict[path[i + 1].GetAbsolutePath()];
                i++;
            }

            return new Tuple<int, TreeViewItem>(i, tvRoot);
        }

        public void OnFolderChanged(Folder folder)
        {
            if (UpdateFolder(folder))
            {
                return;
            }

            RegisterFolderTreeView(folder);
        }

        public void OnScanDiskClick(object sender, RoutedEventArgs e)
        {
            ToggleButton tb = (ToggleButton)sender;
            string message = "";

            // Triggers action and moves to next state
            switch (tb.Tag)
            {
                case ButtonAction.start:
                    diskController.StartDiskAnalysis((string)tb.Content);
                    tb.Tag = ButtonAction.pause;
                    message = "Pause scan";
                    break;

                case ButtonAction.pause:
                    if ((bool)tb.GetValue(ScanToggleButtonProperties.HasEndedProperty))
                    {
                        tb.IsEnabled = false;
                        tb.Tag = ButtonAction.end;
                        message = "Scan Successful";
                        break;
                    }
                    else
                    {
                        diskController.PauseDiskAnalysis((string)tb.Content);
                        tb.Tag = ButtonAction.resume;
                        message = "Resume scan";
                        break;
                    }


                case ButtonAction.resume:
                    diskController.ResumeDiskAnalysis((string)tb.Content);
                    tb.Tag = ButtonAction.pause;
                    message = "Pause scan";
                    break;
            }

            tb.SetValue(ScanToggleButtonProperties.ActionTextProperty, message);
        }

        public void DiscoveryFinished(string identifier)
        {
            this.scanButtonDict[identifier].SetValue(ScanToggleButtonProperties.HasEndedProperty, true);
            this.scanButtonDict[identifier].RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }

        public void OnFolderDeleted(string absolutePath)
        {
            if (this.hierarchyDict.ContainsKey(absolutePath))
            {
                TreeViewItem item = this.hierarchyDict[absolutePath];
                ItemsControl parent = ItemsControl.ItemsControlFromItemContainer(item);
                parent?.Items.Remove(item);
                this.hierarchyDict.Remove(absolutePath);
            }
        }
    }
}
