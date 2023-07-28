using core.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace application.src.core.disk.jobs
{
    public class JobManager : Manager
    {
        public delegate void AnalyzeCallback(Folder folder);

        public JobManager(string identifier, List<IJob> jobs, int poolSize, FinishedCallback callback) : base(identifier, jobs, poolSize, callback)
        {
  
        }
        public JobManager(string identifier, IJob job, int poolSize, FinishedCallback callback) : this(identifier, new List<IJob> { job }, poolSize, callback)
        {

        }

        public override void JobCompleted(Type type, object? data)
        {
            if (type == typeof(ExploreJob))
            {
                HandleExploreJobFinished(data);
            }
            else if (type == typeof(AnalyzeJob))
            {
                HandleAnalyzeJobFinished(data);
            }      
        }

        private void HandleExploreJobFinished(object? data)
        {
            if (data == null) { return; }

            List<IJob> nextJobs = (List<IJob>) data;
            AddJob(nextJobs);
        }

        private void HandleAnalyzeJobFinished(object? data) { 
            if (data == null) { return; }

            Folder folder = (Folder) data;
            folder.SetSynchronized();
        }
    }
}
