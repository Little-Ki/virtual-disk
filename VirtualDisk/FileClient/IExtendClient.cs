using DokanNet;
using DokanNet.Logging;

namespace VirtualDisk.FileClient
{
    public interface IExtendClient : IDokanOperations
    {
        public string Mount { get; set; }
        public void Synchronize();
        public void SetCookie(string cookies);
    }
}
