using VirtualDisk.FileSystem;

namespace VirtualDisk.Client
{
    public interface IClient
    {
        public enum Result
        {
            Success,
            Failure,
            NotReady,
            Rejected,
            FileExists,
            NotFound,
            NotSupport,
            InvalidPath,
            IsNotEmpty
        }
        public string ClientName();
        public Result OpenFile(string fileName, out VirtualFile? file, bool useUpdate = false);
        public Result CreateFile(string fileName, bool isDir);
        public Result DeleteFile(string fileName, bool isDir, bool isTest);
        public Result MoveFile(string oldName, string newName, bool replace);
        public Result ResizeFile(string fileName, long length);
        public Result ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset);
        public Result WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset);
        public Result LockFile(string fileName);
        public Result UnockFile(string fileName);
        public Result SpaceInfo(out long spaceRemain, out long totalBytes, out long bytesRemain);
        public Result Synchronize();
        public Result SetCookies(string cookies);
    }
}
