using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using application.src.core.disk;
using application.src.core.disk.jobs;
using application.src.core.disk.notifiable;
using core.Threading;

namespace core.disk
{
    public class DiskManager
    {
        private List<IFolderDiscoveryNotifiable> folderDiscoveryObservators;
        private Dictionary<string, List<IJob>> scanStateDictionary;
        private Dictionary<string, JobManager> jobManagers;

        public static DiskManager Instance { get; private set; }

        private DiskManager()
        {
            this.folderDiscoveryObservators = new List<IFolderDiscoveryNotifiable>();
            this.scanStateDictionary = new Dictionary<string, List<IJob>>();
            this.jobManagers = new Dictionary<string, JobManager>();
        }

        static DiskManager() 
        { 
            Instance = new DiskManager(); 
        }

        public void AddFolderDiscoveryObservator(IFolderDiscoveryNotifiable observator)
        {
            this.folderDiscoveryObservators.Add(observator);
        }

        public void RemoveFolderDiscoveryObservator(IFolderDiscoveryNotifiable observator)
        {
            this.folderDiscoveryObservators.Remove(observator);
        }

        public void NotifyDiscoveryFinished(string identifier)
        {
            foreach (IFolderDiscoveryNotifiable observator in this.folderDiscoveryObservators)
            {
                observator.DiscoveryFinished(identifier);
            }
        }

        public void StartDiskAnalysis(string root)
        {
            Folder folder = FolderManager.Instance.CreateManagedFolder(null, root, 0, 0, root);
            ExploreJob job = new ExploreJob(folder);
            JobManager manager = new JobManager(root, job, 10, NotifyDiscoveryFinished);
            jobManagers.Add(root, manager);
        }

        public void ResumeDiskAnalysis(string root)
        {
            if (scanStateDictionary.ContainsKey(root)) {
                List<IJob> state = scanStateDictionary.GetValueOrDefault(root, new List<IJob>());
                scanStateDictionary.Remove(root);
                JobManager manager = new JobManager(root, state, 24, NotifyDiscoveryFinished);
                jobManagers.Add(root, manager);
            }
            else
            {
                this.StartDiskAnalysis(root);
            }
        }

        public void PauseDiskAnalysis(string root)
        {
            List<IJob> state = jobManagers[root].Stop();
            scanStateDictionary.Add(root, state);
            jobManagers.Remove(root);
        }

        public DriveInfo[] GetDrivesInfo() 
        {
            return DriveInfo.GetDrives();  
        }
    }
}
