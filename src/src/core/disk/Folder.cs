using application.src.core.disk.notifiable;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace application.src.core.disk
{
    public class Folder: IDisposable
    {
        private struct MetaFile
        {
            public string FullName { get; set;}
            public string Name { get; set;}
            public float SizeMB { get; set;}
        }

        private static readonly float SIZE_THRESHOLD = 10.0f;
        private static readonly float BASE_SIZE = (1024 * 1024);
        private readonly object AddFileLock = new object();
        private readonly object LargeCountLock = new object();
        private readonly object ChildrenLock = new object();
        private readonly object FilesLock = new object();
        private readonly object RenameLock = new object();

        private Folder? parent;
        
        private string name;
        private string absolutePath;
        private Boolean synchronized;
        private int fileCount;
        private float size;
        private float largeThresholdMB;
        private int largeCount;
        private FolderManager? owner;

        private ConcurrentDictionary<string, Folder> children;
        private ConcurrentDictionary<string, MetaFile> files;

        public Folder(FolderManager? owner, Folder? parent, string name, int fileCount, float size, string absolutePath)
        {
            this.owner = owner;
            this.parent = parent;
            this.name = name;
            this.synchronized = false;
            this.fileCount = fileCount;
            this.size = size;
            this.absolutePath = absolutePath;
            this.largeCount = 0;
            this.children = new ConcurrentDictionary<string, Folder>();
            this.files = new ConcurrentDictionary<string, MetaFile>();
            this.parent?.AddChild(this);
        }

        public void AddChild(Folder child)
        {
            lock (ChildrenLock)
            {
                this.children.TryAdd(child.GetAbsolutePath(), child);
                lock (AddFileLock)
                {
                    this.size += child.GetSize();
                    this.fileCount += child.GetFileCount();
                }
            }
        }

        public void RemoveChild(Folder folder)
        {
            lock (ChildrenLock)
            {
                Folder? child;
                this.children.TryRemove(folder.GetAbsolutePath(), out child);

                if (child != null)
                {
                    lock (AddFileLock)
                    {
                        this.size -= child.GetSize();
                        this.fileCount -= child.GetFileCount();
                    }
                }  
            }
        }

        private void FolderChangedEvent()
        {
            if (this.IsTracked() && this.IsSynchronized())
            {
                this.owner?.FolderChanged(this);
            }
        }

        public void AddMetaFile(float size)
        {
            lock (AddFileLock)
            {   
                this.fileCount++;
                this.size += size;          
            }

            this.parent?.AddMetaFile(size);
            FolderChangedEvent();
        }

        public void RemoveMetaFile(float size)
        {
            lock (AddFileLock)
            {
                this.fileCount--;
                this.size -= size;
            }

            this.parent?.RemoveMetaFile(size);
            FolderChangedEvent();
        }

        public void AddFile(FileInfo fileInfo)
        {
            AddFile(GetMetaFile(fileInfo));
        }

        private MetaFile GetMetaFile(FileInfo fileInfo)
        {
            MetaFile metaFile = new MetaFile();
            metaFile.Name = fileInfo.Name;
            metaFile.FullName = fileInfo.FullName;
            metaFile.SizeMB = (float) fileInfo.Length / (1024 * 1024);

            return metaFile;
        }

        private void AddFile(MetaFile metaFile)
        {
            if (this.files.TryAdd(metaFile.Name, metaFile))
            {
                if (metaFile.SizeMB > SIZE_THRESHOLD)
                {
                    RegisterLargeFile();
                }
                this.AddMetaFile(metaFile.SizeMB);
            }
        }

        public bool OwnsFile(string fileName)
        {
            return this.files.ContainsKey(fileName);
        }

        public void AddFile(string fileName)
        {
            string path = Path.Join(this.GetAbsolutePath(), fileName);
            FileInfo info = new FileInfo(path);
            this.AddFile(info);
        }

        public void RemoveFile(string fileName)
        {
            MetaFile metaFile;

            if (this.files.TryRemove(fileName, out metaFile))
            {
                string file = metaFile.Name;

                if (metaFile.SizeMB > SIZE_THRESHOLD)
                {
                    UnregisterLargeFile();
                }

                this.RemoveMetaFile(metaFile.SizeMB);
            }
        }

        public bool IsTracked()
        {
            return this.largeCount > 0;
        }

        private void RegisterLargeFile()
        {
            lock (LargeCountLock)
            {
                largeCount += 1;

                if (largeCount == 1)
                {
                    FolderChangedEvent();
                }
            }            
        }

        private void UnregisterLargeFile()
        {
            lock (LargeCountLock)
            {
                largeCount -= 1;

                if (largeCount == 0)
                {
                    FolderChangedEvent();
                }
            }
        }

        public string GetAbsolutePath()
        {
            lock (RenameLock)
            {
                return absolutePath;
            }
        }

        public Folder? GetParent()
        {
            lock (RenameLock)
            {
                return this.parent;
            }
        }

        public string GetFolderName()
        {
            lock (RenameLock)
            {
                return this.name;
            }
        }

        public Boolean IsSynchronized()
        {
            return this.synchronized;
        }
        
        public void SetSynchronized()
        {
            this.synchronized = true;
            this.FolderChangedEvent();
            this.owner?.FolderSynched(this);
        }

        public float GetSize()
        {
            return this.size;
        }

        public int GetFileCount()
        {
            return this.fileCount;
        }

        public void Rename(string absolutePath)
        {
            lock(AddFileLock)
            {
                lock (RenameLock) {
                    string newParentPath = Path.GetDirectoryName(absolutePath) ?? "";
                    this.absolutePath = absolutePath;
                    this.name = Path.GetFileName(absolutePath);

                    if (newParentPath != this.parent?.GetAbsolutePath())
                    {
                        parent?.RemoveChild(this);
                        Folder? newParent = FolderManager.Instance.GetFolder(newParentPath);

                        newParent?.AddChild(this);
                        this.parent = newParent;
                    }
                }
            }
        }

        public void FileChanged(string absolutePath)
        {
            if (this.files.TryGetValue(absolutePath, out MetaFile oldFile)) {
                MetaFile newFile = GetMetaFile(new FileInfo(absolutePath));

                if (newFile.SizeMB != oldFile.SizeMB)
                {
                    // Could be enhanced
                    this.RemoveMetaFile(oldFile.SizeMB);
                    this.AddMetaFile(newFile.SizeMB);
                }
            }
        }

        public void Dispose()
        {
            foreach (Folder child in ImmutableList.ToImmutableList<Folder>(children.Values))
            {
                child.Dispose();
            }


            foreach (MetaFile file in ImmutableList.ToImmutableList<MetaFile>(files.Values))
            {
                this.RemoveFile(file.Name);
            }

            FolderManager.Instance.NotifyDeletedFolder(GetAbsolutePath());
        }
    }
}
