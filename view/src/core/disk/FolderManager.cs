using application.src.core.disk.jobs;
using application.src.core.disk.notifiable;
using core.disk;
using core.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace application.src.core.disk
{
    public class FolderManager
    {

        private List<IFolderNotifiable> folderObservers;
        private ConcurrentDictionary<string, Folder> dctFolder;

        public static FolderManager Instance { get; private set; }

        private FolderManager()
        {
            this.folderObservers = new List<IFolderNotifiable>();
            this.dctFolder = new ConcurrentDictionary<string, Folder>();
        }

        static FolderManager()
        {
            Instance = new FolderManager();
        }

        public Folder CreateManagedFolder(Folder? parent, string name, int fileCount, float size, string absolutePath)
        {
            Folder folder = new Folder(this, parent, name, fileCount, size, absolutePath);
            this.dctFolder.TryAdd(absolutePath, folder);
            return folder;
        }

        public void AddFolderObserver(IFolderNotifiable folderObserver)
        {
            this.folderObservers.Add(folderObserver);
        }

        public void RemoveFolderObserver(IFolderNotifiable folderObserver)
        {
            this.folderObservers.Remove(folderObserver);
        }

        public void FolderChanged(Folder folder)
        {
            foreach (IFolderNotifiable observer in this.folderObservers)
            {
                observer.FolderChanged(folder);
            }
        }

        public void FolderSynched(Folder folder)
        {
            if (folder.IsTracked())
            {
                WatcherManager.Instance.AddWatcher(folder.GetAbsolutePath());
            }
        }

        public Folder? GetFolder(string absolutePath)
        {
            Folder? folder = null;
            this.dctFolder.TryGetValue(absolutePath, out folder);
            return folder;
        }

        public void RemoveFolder(string absolutePath)
        {
            Folder? folder = null;
            this.dctFolder.TryRemove(absolutePath, out folder);
            folder?.Dispose();
        }

        public void RenameFolder(string oldPath, string newPath)
        {
            Folder? folder = GetFolder(oldPath);
            folder?.Rename(newPath);
        }

        public void NotifyDeletedFolder(string absolutePath)
        {
            foreach (IFolderNotifiable observer in this.folderObservers)
            {
                observer.FolderDeleted(absolutePath);
            }
        }
    }
}
