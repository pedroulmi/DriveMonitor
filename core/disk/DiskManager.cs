using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace core.disk
{
    public class DiskManager
    {
        private List<DiskObservator> observators;

        public DiskManager(DiskObservator diskController)
        {
            this.observators = new List<DiskObservator>
            {
                diskController
            };
        }
        
        public void onDiskEvent()
        {
            // for observator in observators
            // send update
        }
    }
}
