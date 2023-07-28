using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace application.src.core.disk.notifiable
{
    public interface IFolderDiscoveryNotifiable
    {
        public void DiscoveryFinished(string identifier);
    }
}
