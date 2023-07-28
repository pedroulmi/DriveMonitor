using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace core.disk
{
    public interface DiskObservator
    {
        void FileChanged(string path);
    }
}
