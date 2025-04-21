using DokanNet;
using DokanNet.Logging;

namespace VirtualDisk.FileClient
{

    public abstract class ExtendClient
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

    public interface IExtendClient : IDokanOperations
    {
        public string Mount { get; set; }
        public void Synchronize();
        public void SetCookie(string cookies);
    }
}
