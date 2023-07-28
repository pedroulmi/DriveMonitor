using core.disk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace controller
{
    public class DiskController : DiskObservator
    { 

        private DiskManager diskManager;

        public DiskController()
        {
            diskManager = new DiskManager(this);
        }

        public List<String> listDisks()
        {
            return null;
        }

        public List<String> analyzeDisk(string path)
        {
            return null;
        }

        public void FileChanged(string path)
        {
            throw new NotImplementedException();
        }
    }
}
