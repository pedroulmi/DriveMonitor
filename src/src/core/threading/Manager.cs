using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace core.Threading
{
    public abstract class Manager
    {
        private readonly object jobStackLock = new object();
        private readonly object pendingJobsLock = new object();
        
        public delegate void FinishedCallback(string root);

        private List<IJob> jobStack;
        private List<IJob> pendingJobs;

        private Thread core;
        private Boolean halt;
        protected string identifier;

        private int poolSize;
        private FinishedCallback Callback;


        public Manager(string identifier, List<IJob> jobs, int poolSize, FinishedCallback Callback)
        {
            this.jobStack = jobs;
            this.pendingJobs = new List<IJob>();
            this.poolSize = poolSize;
            this.Callback = Callback;
            this.identifier = identifier;

            this.core = new Thread(new ThreadStart(ProcessingLoop));
            this.core.IsBackground = true;
            this.core.Start();
        }

        public List<IJob> Stop()
        {
            this.halt = true;
            List<IJob> state = this.jobStack.Concat(pendingJobs).ToList();
            
            if (state != null)
            {
                return state;
            }

            return new List<IJob>();
        }

        private void ProcessingLoop()
        {
            while (!this.halt && (this.jobStack.Any() || this.pendingJobs.Any()))
            {
                if (this.pendingJobs.Count() == poolSize || (!this.jobStack.Any() && this.pendingJobs.Any()))
                {
                    Thread.Sleep(50);
                }
                else
                {
                    IJob job;
                    lock (jobStackLock)
                    {
                        job = this.jobStack.Last();
                        this.jobStack.RemoveAt(this.jobStack.Count - 1);
                    }

                    lock (pendingJobsLock)
                    {
                        this.pendingJobs.Add(job);
                    }

                    ThreadPool.QueueUserWorkItem(RunJob, Tuple.Create(this, job));
                }
            }

            if (!halt)
            {
                this.Callback(this.identifier);
            }
        }

        public static void RunJob(object? state)
        {
            if (state != null)
            {
                (Manager context, IJob job) = (Tuple <Manager, IJob>) state;
                object? data = job.Run();
                context.JobCompleted(job, data);
            }
        }

        public void JobCompleted(IJob job, object? data)
        {
            if (!this.halt)
            {
                this.JobCompleted(job.GetType(), data);
                
                lock (pendingJobsLock)
                {
                    this.pendingJobs.Remove(job);
                }
            }
        }

        public abstract void JobCompleted(Type type, object? data);

        protected void AddJob(IJob job)
        {
            if (this.halt) { return; }
            
            lock (jobStackLock)
            {
                jobStack.Add(job);
            }
        }

        protected void AddJob(List<IJob> jobs)
        {
            if (this.halt) { return; }

            foreach (IJob job in jobs)
            {
                this.AddJob(job);
            }
        }
        
    }
}
