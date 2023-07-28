using core.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace application.src.core.threading
{
    public abstract class Pooler<T>
    {
        private readonly object lstAddRemoveLock = new object();

        private Thread core;
        private bool halt;
        private int poolingCooldown;
        private List<T> lstPooling;
        private List<T> lstCooldown;
        private List<T> lstAdd;
        private List<T> lstRemove;

        public Pooler(int poolingCooldown)
        {
            this.halt = false;
            this.poolingCooldown = poolingCooldown;
            this.core = new Thread(new ThreadStart(ProcessingLoop));
            this.core.IsBackground = true;
            this.core.Start();

            this.lstCooldown = new List<T>();
            this.lstPooling = new List<T>();
            this.lstAdd = new List<T>();
            this.lstRemove = new List<T>();           
        }

        public void Stop()
        {
            this.halt = true;
        }

        protected abstract void Pool(T item);

        public void Add(T item)
        {
            lock(lstAddRemoveLock)
            {
                if (lstRemove.Contains(item))
                {
                    lstRemove.Remove(item);
                }

                lstAdd.Add(item);
            }
        }

        public void Remove(T item)
        {
            lock (lstAddRemoveLock)
            {
                if (lstAdd.Contains(item))
                {
                    lstAdd.Remove(item);
                }
                lstRemove.Add(item);
            }
        }

        private void ProcessingLoop()
        {
            while (!this.halt)
            {
                lstPooling = lstCooldown;
                lstCooldown = new List<T>();
                
                lock (lstAddRemoveLock)
                {
                    lstPooling.AddRange(lstAdd);
                    lstAdd = new List<T>();
                
                    foreach (T item in lstRemove)
                    {
                        lstPooling.Remove(item);
                    }

                    lstRemove = new List<T>();
                }
                
                foreach (T item in lstPooling)
                {
                    Pool(item);
                }
            }

            Thread.Sleep(poolingCooldown);
        }
    }
}
