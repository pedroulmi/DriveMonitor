using application;
using application.src.core.disk;
using application.src.core.disk.notifiable;
using core.disk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace controller
{
    public class DiskController : IFolderDiscoveryNotifiable, IFolderNotifiable
    {
        private MainWindow view;

        public DiskController(MainWindow view)
        {
            this.view = view;
            DiskManager.Instance.AddFolderDiscoveryObservator(this);
            FolderManager.Instance.AddFolderObserver(this);
        }

        ~DiskController()
        {
            DiskManager.Instance.RemoveFolderDiscoveryObservator(this);
            FolderManager.Instance.RemoveFolderObserver(this);
        }

        public List<String> listDisks()
        {
            List<string> driveNames = new List<string>();
            DriveInfo[] drivesInfo = DiskManager.Instance.GetDrivesInfo();

            foreach (DriveInfo info in drivesInfo)
            {
                driveNames.Add(info.Name);
            }

            return driveNames;
        }

        public void StartDiskAnalysis(string root)
        {
            DiskManager.Instance.StartDiskAnalysis(root);
        }
        
        public void PauseDiskAnalysis(string root)
        {
            DiskManager.Instance.PauseDiskAnalysis(root);
        }
        
        public void ResumeDiskAnalysis(string root)
        {
            DiskManager.Instance.ResumeDiskAnalysis(root);
        }

        public void DiscoveryFinished(string identifier)
        {
            view.window.Dispatcher.Invoke(() =>
            {
                view.DiscoveryFinished(identifier);
            });
        }

        public void FolderChanged(Folder folder)
        {
            view.window.Dispatcher.Invoke(() =>
            {
                view.OnFolderChanged(folder);
            });
        }

        public void FolderDeleted(string absolutePath)
        {
            view.window.Dispatcher.Invoke(() =>
            {
                view.OnFolderDeleted(absolutePath);
            });
        }
    }
}
