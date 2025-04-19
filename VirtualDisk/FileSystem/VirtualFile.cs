using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualDisk.FileSystem
{

    public class VirtualFile
    {
        public string Name = string.Empty;
        public string ID = string.Empty;

        public long Size = 0;
        public bool IsDirectory = false;

        public int useCount = 0;

        public VirtualFile? Parent = null;
        public Dictionary<string, VirtualFile> Children = [];
    }
}
