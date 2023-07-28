using core.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace application.src.core.disk.jobs
{
    public class ExploreJob : IJob
    {
        readonly Folder folder;

        public ExploreJob(Folder folder)
        {
            this.folder = folder;
        }

        public object? Run()
        {
            List<IJob> jobs = new List<IJob>();
            jobs.Add(new AnalyzeJob(this.folder));
            try
            {
                string[] directories = Directory.GetDirectories(folder.GetAbsolutePath());
                foreach (string dir in directories)
                {
                    string? dirName = Path.GetFileName(dir);
                    if (dirName != null)
                    {
                        Folder nextRoot = FolderManager.Instance.CreateManagedFolder(this.folder, dirName, 0, 0, dir);
                        jobs.Add(new ExploreJob(nextRoot));
                    }
                }
            } catch (UnauthorizedAccessException)
            {
                // If you dont have access to the folder, just finalize job returning an empty list
                return new List<IJob>();
            }

            return jobs;
        }
    }
}
