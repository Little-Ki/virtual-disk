using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualDisk.Utils
{
    class Module<T> where T : class, new()
    {
        public static T Instance { get; } = new();
    }


}
