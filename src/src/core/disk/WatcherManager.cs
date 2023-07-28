using core.disk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static application.src.core.disk.FolderHelper;

namespace application.src.core.disk
{
    public class WatcherManager
    {
        private readonly object ManipulateLock = new object();
        private List<FileSystemWatcher> lstWatchers;
        private List<string> lstTracked;

        public static WatcherManager Instance { get; private set; }

        private WatcherManager()
        {
            this.lstWatchers = new List<FileSystemWatcher>();
            this.lstTracked = new List<string>();
        }

        static WatcherManager() { 
            Instance = new WatcherManager(); 
        }

        private FileSystemWatcher? GetResponsibleWatcher(string path)
        {
            foreach (FileSystemWatcher watcher in lstWatchers)
            {
                if (watcher.Path == path) { return watcher; }

                string parent = FolderHelper.GetParentPath(watcher.Path, path);

                if (watcher.Path == parent)
                {
                    return watcher;
                }
            }

            return null;
        }

        private List<string> GetChildren(string path)
        {
            List<string> children = new List<string>();
            
            // Returns a list with all the tracked paths that depends on the arg Path watcher
            if (path != "")
            {
                foreach (FileSystemWatcher watcher in lstWatchers)
                {
                    string parent = FolderHelper.GetParentPath(path, watcher.Path);
                    if (parent == path)
                    {
                        children.Add(watcher.Path);
                    }
                }
            }

            return children;
        }

        public void RemoveWatcher(string absolutePath)
        {
            lock (ManipulateLock)
            {
                FileSystemWatcher? responsible = GetResponsibleWatcher(absolutePath);

                if (responsible != null)
                {
                    lstWatchers.Remove(responsible);
                    lstTracked.Remove(absolutePath);

                    List<string> children = GetChildren(responsible.Path);
                    foreach (string child in children)
                    {
                        AddWatcher(child);
                    }

                    responsible?.Dispose();
                }
            }
        }

        private void RemoveRedundantWatchers(FileSystemWatcher watcher)
        {
            FileSystemWatcher? toRemove = null;
            foreach (FileSystemWatcher curr in lstWatchers)
            {
                if (watcher == curr) { continue; }

                string parent = FolderHelper.GetParentPath(curr.Path, watcher.Path);
                
                
                if (curr.Path == parent)
                {
                    toRemove = watcher;
                    break;
                }

                else if (watcher.Path == parent)
                {
                    toRemove = curr;
                    break;
                }
            }

            if (toRemove != null)
            {
                lstWatchers.Remove(toRemove);
                toRemove.Dispose();
            }
        }

        public void AddWatcher(string absolutePath)
        {
            lock (ManipulateLock)
            {
                FileSystemWatcher watcher = CreateWatcher(absolutePath);
                List<string> folderStack = FolderHelper.GetFoldersStack(watcher.Path);
                lstWatchers.Add(watcher);
                lstTracked.Add(absolutePath); //Could improve using a tree where nodes that are tracked have a flag
                RemoveRedundantWatchers(watcher);
            }
        }

        private FileSystemWatcher CreateWatcher(string absolutePath)
        {
            FileSystemWatcher watcher = new FileSystemWatcher(absolutePath);

            watcher.NotifyFilter = NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.Size;

            watcher.Changed += HandleChanged;
            watcher.Created += HandleCreated;
            watcher.Deleted += HandleDeleted;
            watcher.Renamed += HandleRenamed;

            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;

            return watcher;
        }

        private void HandleRenamed(object sender, RenamedEventArgs e)
        { 
            if (e.FullPath != e.OldFullPath)
            {
                if (FolderHelper.GetFileType(e.FullPath) == FileType.Folder)
                {
                    FolderManager.Instance.RenameFolder(e.OldFullPath, e.FullPath);
                }
                else
                {
                    string oldParentPath = Path.GetDirectoryName(e.FullPath) ?? "";
                    string newParentPath = Path.GetDirectoryName(e.FullPath) ?? "";
   
                    Folder? parent = FolderManager.Instance.GetFolder(oldParentPath);
                    parent?.RemoveFile(Path.GetFileName(e.OldFullPath));

                    parent = FolderManager.Instance.GetFolder(newParentPath);
                    parent?.AddFile(Path.GetFileName(e.FullPath));
                }
            }
        }

        private void HandleDeleted(object sender, FileSystemEventArgs e)
        {
            string parentPath = Path.GetDirectoryName(e.FullPath) ?? string.Empty;
            Folder? folder = FolderManager.Instance.GetFolder(parentPath);
     
            if (folder != null && folder.OwnsFile(Path.GetFileName(e.FullPath)))
            {
                // It's a file
                folder?.RemoveFile(Path.GetFileName(e.FullPath));
            }
            else
            {
                FolderManager.Instance.RemoveFolder(e.FullPath);
                RemoveWatcher(e.FullPath);
            }  
        }

        private void HandleCreated(object sender, FileSystemEventArgs e)
        {
            string parentPath = Path.GetDirectoryName(e.FullPath) ?? string.Empty;
            Folder? parent = FolderManager.Instance.GetFolder(parentPath);

            if (parent != null)
            {
                FileType type = FolderHelper.GetFileType(e.FullPath);
                string folderName = Path.GetFileName(e.FullPath);

                switch (type)
                {
                    case FileType.Folder:
                        FolderManager.Instance.CreateManagedFolder(parent, folderName, 0, 0, e.FullPath);
                        break;

                    case FileType.File:
                        FileInfo info = new FileInfo(e.FullPath);
                        parent.AddFile(info);
                        break;
                }   
            }
        }

        private void HandleChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Renamed)
            {
                return;
            }

            if (FolderHelper.GetFileType(e.FullPath) == FileType.File)
            {
                string parentPath = Path.GetDirectoryName(e.FullPath) ?? string.Empty;
                Folder? parent = FolderManager.Instance.GetFolder(parentPath);
                parent?.FileChanged(Path.GetFileName(e.FullPath));
            }         
        }
    }
}
