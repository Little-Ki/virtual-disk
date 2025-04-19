using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualDisk
{
    class App
    {
        public static App Instance = new();

        public string Cookies { get; set; } = string.Empty;
    }

}
