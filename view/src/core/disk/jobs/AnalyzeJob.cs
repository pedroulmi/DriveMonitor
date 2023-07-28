using core.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace application.src.core.disk.jobs
{
    public class AnalyzeJob : IJob
    {
        Folder folder;
        private static readonly int largeThreshold = 10;

        public AnalyzeJob(Folder folder)
        {
            this.folder = folder;
        }

        public object? Run()
        {
            try
            {
                string[] files = Directory.GetFiles(folder.GetAbsolutePath());
                foreach (string file in files)
                {
                    try
                    {
                        FileInfo fi = new FileInfo(file);
                        folder.AddFile(fi);
                    }
                    catch
                    {
                        // Skip File
                    }
                }
            }
            catch
            {
                // Ignore this folder since we can't read its data
                // Can improve by adding info on screen
                return null;
            }
            
            return folder;
        }
    }
}
