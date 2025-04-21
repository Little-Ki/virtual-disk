using DokanNet.Logging;
using DokanNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualDisk.FileClient
{
    public abstract class ClientTrace
    {
        private readonly ConsoleLogger logger = new("[Debug]");
        protected NtStatus Trace(string method, IDokanFileInfo info, NtStatus result, params object[] parameters)
        {
#if TRACE
            var extra = parameters != null && parameters.Length > 0
                ? ", " + string.Join(", ", parameters.Select(x => string.Format("{0}", x)))
            : string.Empty;

            logger.Debug($"{method}({info}{extra}) -> {result}\n");
#endif

            return result;
        }
    }
}
