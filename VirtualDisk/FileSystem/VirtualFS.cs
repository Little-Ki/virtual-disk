using VirtualDisk.Client;
using DokanNet;
using DokanNet.Logging;
using System.Security.AccessControl;
using ClientResult = VirtualDisk.Client.IClient.Result;

namespace VirtualDisk.FileSystem
{
    public class VirtualFS : IDokanOperations
    {
        private object fsLock = new();

        private IClient? client = null;

        private string mount = @"Z:\";

        private readonly ConsoleLogger logger = new("[Debug]");

        public VirtualFS SetClient<T>(T client) where T : IClient
        {
            this.client = client;
            return this;
        }

        public VirtualFS()
        {

        }
        public VirtualFS SetMount(string mount = @"Z:\")
        {
            this.mount = mount;
            return this;
        }

        public VirtualFS Start()
        {
            var task = new Task(async () =>
            {

                using var logger = new ConsoleLogger("[VirtualFS]");
                using var dokan = new Dokan(logger);
                var builder = new DokanInstanceBuilder(dokan)
                    .ConfigureLogger(() => logger)
                    .ConfigureOptions(options =>
                    {
                        //options.Options = DokanOptions.DebugMode | DokanOptions.EnableNotificationAPI;
                        options.MountPoint = mount;
                    });

                var instance = builder.Build(this);

                await instance.WaitForFileSystemClosedAsync(uint.MaxValue);
            });

            task.Start();

            return this;
        }

        protected NtStatus Trace(string method, IDokanFileInfo info, NtStatus result, params object[] parameters)
        {
#if TRACE
            var extra = parameters != null && parameters.Length > 0
                ? ", " + string.Join(", ", parameters.Select(x => string.Format("{0}", x)))
                : string.Empty;

            logger.Debug(($"{method}({info}{extra}) -> {result}\n"));
#endif

            return result;
        }

        public void Cleanup(string fileName, IDokanFileInfo info)
        {
            if (info.DeleteOnClose)
            {
                client?.DeleteFile(fileName, info.IsDirectory, false);
            }
        }

        public void CloseFile(string fileName, IDokanFileInfo info)
        {

        }
        public NtStatus CreateFile(string fileName, DokanNet.FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, IDokanFileInfo info)
        {
            var result = DokanResult.Unsuccessful;

            if (client == null)
            {
                return DokanResult.NotReady;
            }

            var status = client.OpenFile(fileName, out var file);

            if (info.IsDirectory)
            {
                if (mode == FileMode.Open)
                {
                    if (file?.IsDirectory ?? false)
                    {
                        result = DokanResult.Success;
                    }
                    else
                    {
                        result = DokanResult.NotADirectory;
                    }
                }

                if (mode == FileMode.CreateNew)
                {
                    if (file != null)
                    {
                        if (file.IsDirectory)
                        {
                            result = DokanResult.AlreadyExists;
                        }
                        else
                        {
                            result = DokanResult.FileExists;
                        }
                    }
                    else if (client == null)
                    {
                        result = DokanResult.NotReady;
                    }
                    else
                    {
                        var res = client.CreateFile(fileName, true);

                        if (res == ClientResult.Success)
                        {
                            result = DokanResult.Success;
                        }

                        if (res == ClientResult.InvalidPath)
                        {
                            result = DokanResult.PathNotFound;
                        }

                        if (res == ClientResult.FileExists)
                        {
                            result = DokanResult.AlreadyExists;
                        }
                    }
                }
            }
            else
            {
                if (mode == FileMode.Open)
                {
                    if (file != null)
                    {
                        info.Context = new object();
                        result = DokanResult.Success;
                    }
                    else
                    {
                        result = DokanResult.FileNotFound;
                    }
                }

                if (mode == FileMode.CreateNew)
                {
                    if (file != null)
                    {
                        result = DokanResult.FileExists;
                    }
                    else if (client == null)
                    {
                        result = DokanResult.NotReady;
                    }
                    else
                    {
                        var res = client.CreateFile(fileName, false);

                        if (res == ClientResult.Success)
                        {
                            result = DokanResult.Success;
                        }

                        if (res == ClientResult.InvalidPath)
                        {
                            result = DokanResult.PathNotFound;
                        }

                        if (res == ClientResult.FileExists)
                        {
                            result = DokanResult.FileExists;
                        }
                    }
                }

                if (mode == FileMode.Truncate)
                {
                    if (file != null)
                    {
                        result = DokanResult.FileExists;
                    }
                }
            }

            return Trace(nameof(CreateFile), info, result, fileName, access, share, mode, options, attributes);
        }

        public NtStatus DeleteDirectory(string fileName, IDokanFileInfo info)
        {
            var result = DokanResult.Unsuccessful;

            if (client == null)
            {
                result = DokanResult.NotReady;
            }
            else
            {
                var res = client.DeleteFile(fileName, true, true);

                if (res == ClientResult.Success)
                {
                    result = DokanResult.Success;
                }

                if (res == ClientResult.InvalidPath)
                {
                    result = DokanResult.PathNotFound;
                }

                if (res == ClientResult.NotFound)
                {
                    result = DokanResult.PathNotFound;
                }

                if (res == ClientResult.IsNotEmpty)
                {
                    result = DokanResult.DirectoryNotEmpty;
                }
            }

            return Trace(nameof(DeleteDirectory), info, result, fileName);
        }

        public NtStatus DeleteFile(string fileName, IDokanFileInfo info)
        {
            var result = DokanResult.Unsuccessful;

            if (client == null)
            {
                result = DokanResult.NotReady;
            }
            else
            {
                var res = client.DeleteFile(fileName, false, true);

                if (res == ClientResult.Success)
                {
                    result = DokanResult.Success;
                }

                if (res == ClientResult.InvalidPath)
                {
                    result = DokanResult.PathNotFound;
                }

                if (res == ClientResult.NotFound)
                {
                    result = DokanResult.FileNotFound;
                }
            }

            return Trace(nameof(DeleteFile), info, result, fileName);
        }

        public NtStatus FindFiles(string fileName, out IList<FileInformation> files, IDokanFileInfo info)
        {
            files = [];

            return Trace(nameof(FindFiles), info, DokanResult.Success, fileName);
        }

        public NtStatus FindFilesWithPattern(string fileName, string searchPattern, out IList<FileInformation> files, IDokanFileInfo info)
        {
            if (client == null)
            {
                files = [];
                return DokanResult.NotReady;
            }

            var result = DokanResult.Success;

            var res = client.OpenFile(fileName, out var file);

            if (res == ClientResult.NotReady)
            {
                files = [];
                result = DokanResult.NotReady;
            }
            else if (file != null)
            {
                files = file.Children
                    .Select(x => x.Value)
                    .Where(x => DokanHelper.DokanIsNameInExpression(searchPattern, x.Name, true))
                    .Select(x => new FileInformation
                    {
                        Attributes = x.IsDirectory ? FileAttributes.Directory : FileAttributes.Archive,
                        CreationTime = new DateTime(),
                        LastAccessTime = new DateTime(),
                        LastWriteTime = new DateTime(),
                        Length = x.Size,
                        FileName = x.Name
                    })
                    .ToArray();
            }
            else
            {
                files = [];
            }

            return Trace(nameof(FindFilesWithPattern), info, result, fileName, searchPattern);
        }

        public NtStatus FindStreams(string fileName, out IList<FileInformation> streams, IDokanFileInfo info)
        {
            streams = [];
            return Trace(nameof(FindStreams), info, DokanResult.NotImplemented, fileName);
        }

        public NtStatus FlushFileBuffers(string fileName, IDokanFileInfo info)
        {
            return Trace(nameof(FlushFileBuffers), info, DokanResult.Success, fileName);
        }

        public NtStatus GetDiskFreeSpace(out long freeBytesAvailable, out long totalNumberOfBytes, out long totalNumberOfFreeBytes, IDokanFileInfo info)
        {
            if (client != null)
            {
                client.SpaceInfo(out freeBytesAvailable, out totalNumberOfBytes, out totalNumberOfFreeBytes);
                return DokanResult.Success;
            }

            freeBytesAvailable = 0;
            totalNumberOfBytes = 0;
            totalNumberOfFreeBytes = 0;

            return Trace(nameof(GetDiskFreeSpace), info, DokanResult.Success);
        }

        public NtStatus GetFileInformation(string fileName, out FileInformation fileInfo, IDokanFileInfo info)
        {
            var result = DokanResult.Success;


            if (client == null)
            {
                result = DokanResult.NotReady;
                fileInfo = new FileInformation();
            }
            else
            {
                var res = client.OpenFile(fileName, out var file, true);

                if (res == ClientResult.NotReady)
                {
                    result = DokanResult.NotReady;
                }

                fileInfo = new FileInformation
                {
                    FileName = file?.Name ?? "",
                    Attributes = (file?.IsDirectory ?? false) ? FileAttributes.Directory : FileAttributes.Archive,
                    CreationTime = new DateTime(),
                    LastAccessTime = new DateTime(),
                    LastWriteTime = new DateTime(),
                    Length = file?.Size ?? 0,
                };
            }

            return Trace(nameof(GetFileInformation), info, result, fileInfo);
        }

        public NtStatus GetFileSecurity(string fileName, out FileSystemSecurity? security, AccessControlSections sections, IDokanFileInfo info)
        {
            security = null;
            return Trace(nameof(GetFileSecurity), info, DokanResult.NotImplemented, sections);
        }

        public NtStatus GetVolumeInformation(out string volumeLabel, out FileSystemFeatures features, out string fileSystemName, out uint maximumComponentLength, IDokanFileInfo info)
        {
            if (client != null)
            {
                volumeLabel = client.ClientName();
            }
            else
            {
                volumeLabel = "Cloud";
            }
            fileSystemName = "NTFS";
            maximumComponentLength = 256;

            features = FileSystemFeatures.CasePreservedNames | FileSystemFeatures.CaseSensitiveSearch |
                       FileSystemFeatures.PersistentAcls | FileSystemFeatures.UnicodeOnDisk;


            return Trace(nameof(GetVolumeInformation), info, DokanResult.Success);
        }

        public NtStatus LockFile(string fileName, long offset, long length, IDokanFileInfo info)
        {
            if (client != null)
            {
                client.LockFile(fileName);
            }
            return Trace(nameof(LockFile), info, DokanResult.Success);
        }

        public NtStatus Mounted(string mountPoint, IDokanFileInfo info)
        {
            return DokanResult.Success;
        }

        public NtStatus MoveFile(string oldName, string newName, bool replace, IDokanFileInfo info)
        {
            var result = DokanResult.AccessDenied;

            info.Context = null;

            if (client == null)
            {
                result = DokanResult.NotReady;
            }
            else
            {
                var res = client.MoveFile(oldName, newName, replace);

                if (res == ClientResult.Success)
                {
                    result = DokanResult.Success;
                }

                if (res == ClientResult.FileExists)
                {
                    result = DokanResult.FileExists;
                }

                if (res == ClientResult.InvalidPath)
                {
                    result = DokanResult.PathNotFound;
                }

                if (res == ClientResult.NotFound)
                {
                    if (info.IsDirectory)
                    {
                        result = DokanResult.PathNotFound;
                    }
                    else
                    {
                        result = DokanResult.FileNotFound;
                    }
                }

                if (res == ClientResult.NotSupport)
                {
                    result = DokanResult.NotImplemented;
                }
            }

            return Trace(nameof(MoveFile), info, result);
        }

        public NtStatus ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset, IDokanFileInfo info)
        {
            if (client != null)
            {
                client.ReadFile(fileName, buffer, out bytesRead, offset);
                return Trace(nameof(ReadFile), info, DokanResult.Success);
            }

            bytesRead = 0;
            return Trace(nameof(ReadFile), info, DokanResult.Success);
        }

        public NtStatus SetAllocationSize(string fileName, long length, IDokanFileInfo info)
        {
            return Trace(nameof(SetAllocationSize), info, DokanResult.Success);
        }

        public NtStatus SetEndOfFile(string fileName, long length, IDokanFileInfo info)
        {
            var result = DokanResult.DiskFull;

            if (client == null)
            {
                result = DokanResult.NotReady;
            }
            else
            {
                var res = client.ResizeFile(fileName, length);

                if (res == ClientResult.NotSupport)
                {
                    result = DokanResult.NotImplemented;
                }
            }

            return Trace(nameof(SetEndOfFile), info, result, length);
        }

        public NtStatus SetFileAttributes(string fileName, FileAttributes attributes, IDokanFileInfo info)
        {
            return Trace(nameof(SetFileAttributes), info, DokanResult.Success, attributes);
        }

        public NtStatus SetFileSecurity(string fileName, FileSystemSecurity security, AccessControlSections sections, IDokanFileInfo info)
        {
            return Trace(nameof(SetFileSecurity), info, DokanResult.Success, security, sections);
        }

        public NtStatus SetFileTime(string fileName, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime, IDokanFileInfo info)
        {
            return Trace(nameof(SetFileTime), info, DokanResult.Success);
        }

        public NtStatus UnlockFile(string fileName, long offset, long length, IDokanFileInfo info)
        {
            if (client != null)
            {
                client.UnockFile(fileName);
            }
            return Trace(nameof(UnlockFile), info, DokanResult.Success, offset, length);
        }

        public NtStatus Unmounted(IDokanFileInfo info)
        {
            return Trace(nameof(Unmounted), info, DokanResult.Success);
        }

        public NtStatus WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset, IDokanFileInfo info)
        {
            bytesWritten = 0;
            return Trace(nameof(WriteFile), info, DokanResult.NotReady, offset);
        }
    }


}
