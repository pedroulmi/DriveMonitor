using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace application.src.core.disk.notifiable
{
    public interface IFolderNotifiable
    {
        public void FolderChanged(Folder folder);
        public void FolderDeleted(string absolutePath);

    }
}
