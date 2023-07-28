using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace application.src.core.disk
{
    public static class FolderHelper
    {
        public enum FileType
        {
            Folder, File
        }

        public static List<string> GetFoldersStack(string folder) 
        {
            string self = Path.GetFileName(folder);
            string? parent = Path.GetDirectoryName(folder);
            List<string> stack;
            
            if (parent == null)
            {
               stack  = new List<string>();
            }
            else
            {
               stack = GetFoldersStack(parent);
            }

            stack.Add(self);

            return stack;
        }

        public static string GetParentPath(string pathA, string pathB)
        {
            if (pathB.StartsWith(pathA))
            {
                return pathA;
            }

            if (pathA.StartsWith(pathB))
            {
                return pathB;
            }

            return "";
        }

        public static FileType GetFileType(string absolutePath)
        {
            FileAttributes attr = File.GetAttributes(absolutePath);

            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                return FileType.Folder;
            }
            else
            {
                return FileType.File;
            }
        }
    }
}
