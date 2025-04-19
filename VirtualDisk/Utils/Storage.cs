using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Windows.Forms.LinkLabel;

namespace VirtualDisk.Utils
{
    class Storage
    {

        public static T Read<T>(string path) where T : new()
        {
            try
            {
                using StreamReader inFile = new(path);
                return JsonSerializer.Deserialize<T>(inFile.ReadToEnd()) ?? new T();
            }
            catch
            {
                return new T();
            }
        }

        public static void Write<T>(T value, string path)
        {
            try
            {
                using StreamWriter outFile = new StreamWriter(path);
                outFile.Write(JsonSerializer.Serialize(value));
            }
            catch { }
        }


    }
}
