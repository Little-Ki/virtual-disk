using DokanNet;
using System.Collections.Generic;
using System.Runtime.Caching;
using System.Security.AccessControl;
using VirtualDisk.FileSystem;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace VirtualDisk.FileClient
{
    public class WooZoooClient : ExtendClient, IExtendClient
    {
        private readonly VirtualDir directory = new(new VirtualFile() { IsDirectory = true, Name = "root", ID = "-1" });

        private readonly API.WooZooo.Client client = new();

        private readonly object fsLock = new();

        private readonly object synLock = new();

        private readonly ObjectCache fileCache = new MemoryCache("wz-tasks");

        private readonly ObjectCache taskCache = new MemoryCache("wz-tasks");
        string IExtendClient.Mount { get; set; } = "Z:\\";
        public void Cleanup(string fileName, IDokanFileInfo info)
        {
            void deleteProcess()
            {
                var node = directory.GetNode(fileName);
                var parent = node?.Parent ?? null;

                if (node != null && parent != null)
                {
                    var result = node.IsDirectory ? client.DeleteFolder(node.ID) : client.DeleteFile(node.ID);

                    if (!result)
                    {
                        return;
                    }

                    parent.Children.Remove(node.Name);
                }
            }

            if (info.DeleteOnClose)
            {
                Monitor.Enter(fsLock);
                deleteProcess();
                Monitor.Exit(fsLock);
            }
        }

        public void CloseFile(string fileName, IDokanFileInfo info)
        {

        }

        public NtStatus CreateFile(string fileName, DokanNet.FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, IDokanFileInfo info)
        {
            var result = DokanResult.Unsuccessful;

            Monitor.Enter(fsLock);
            var file = directory.GetNode(fileName);
            Monitor.Exit(fsLock);

            NtStatus process(string fileName, bool isDir)
            {
                var parent = directory.GetParent(fileName);
                var name = VirtualDir.GetName(fileName);

                if (parent == null)
                {
                    return DokanResult.PathNotFound;
                }

                if (isDir)
                {
                    var result = client.CreateFolder(name, parent.ID);

                    if (!result.Success)
                    {
                        return DokanResult.Unsuccessful;
                    }

                    parent.Children[name] = new()
                    {
                        IsDirectory = isDir,
                        Name = name,
                        Parent = parent,
                        ID = result.Value ?? ""
                    };

                    return DokanResult.Success;
                }
                else
                {
                    return DokanResult.NotImplemented;
                }
            }


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
                        result = file.IsDirectory ? DokanResult.AlreadyExists : DokanResult.FileExists;
                    }
                    else
                    {
                        result = process(fileName, true);
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
                    else
                    {
                        result = process(fileName, false);
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
            NtStatus process()
            {
                var node = directory.GetNode(fileName);

                if (node == null)
                {
                    return DokanResult.PathNotFound;
                }

                if (!node.IsDirectory)
                {
                    return DokanResult.NotADirectory;
                }

                if (node.IsDirectory && node.Children.Count > 0)
                {
                    return DokanResult.DirectoryNotEmpty;
                }

                return DokanResult.Success;
            }

            Monitor.Enter(fsLock);
            var result = process();
            Monitor.Exit(fsLock);

            return Trace(nameof(DeleteDirectory), info, result, fileName);
        }

        public NtStatus DeleteFile(string fileName, IDokanFileInfo info)
        {
            NtStatus process()
            {
                var node = directory.GetNode(fileName);

                if (node == null)
                {
                    return DokanResult.FileNotFound;
                }

                if (node.IsDirectory)
                {
                    return DokanResult.InvalidName;
                }

                if (node.IsDirectory && node.Children.Count > 0)
                {
                    return DokanResult.DirectoryNotEmpty;
                }

                return DokanResult.Success;
            }

            Monitor.Enter(fsLock);
            var result = process();
            Monitor.Exit(fsLock);

            return Trace(nameof(DeleteDirectory), info, result, fileName);
        }

        public NtStatus FindFiles(string fileName, out IList<FileInformation> files, IDokanFileInfo info)
        {
            files = [];
            return Trace(nameof(FindFiles), info, DokanResult.Success, fileName);
        }

        public NtStatus FindFilesWithPattern(string fileName, string searchPattern, out IList<FileInformation> files, IDokanFileInfo info)
        {
            var result = DokanResult.Success;

            Monitor.Enter(fsLock);
            var node = directory.GetNode(fileName);
            Monitor.Exit(fsLock);

            if (node != null)
            {
                files = node.Children
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
            Monitor.Enter(fsLock);
            var totalSum = 0L;

            directory.Traverse((VirtualFile file) =>
            {
                totalSum += file.Size;
            });

            freeBytesAvailable = 1024 * 1024 * 1024;
            totalNumberOfBytes = totalSum;
            totalNumberOfFreeBytes = freeBytesAvailable - totalSum;

            Monitor.Exit(fsLock);

            return Trace(nameof(GetDiskFreeSpace), info, DokanResult.Success);
        }

        public NtStatus GetFileInformation(string fileName, out FileInformation fileInfo, IDokanFileInfo info)
        {
            var result = DokanResult.Success;

            Monitor.Enter(fsLock);
            var file = directory.GetNode(fileName);
            Monitor.Exit(fsLock);

            void process(string ID)
            {
                var url = client.GetSharedLink(file.ID);

                if (!url.Success)
                    return;

                var lnk = client.GetFileUrl(url.Value!.Host, url.Value!.FileID, file.ID, url.Value!.Password);

                if (!lnk.Success)
                    return;

                var data = client.GetData(lnk.Value!);

                if (data.Length > 0)
                {
                    fileCache.Add(ID, data, null);
                }
            }

            if (file != null && !file.IsDirectory)
            {
                if (!taskCache.Contains(file.ID) && !fileCache.Contains(file.ID))
                {
                    var task = new Task((id) =>
                    {
                        process(id!.ToString()!);
                        taskCache.Remove(id!.ToString()!);
                    }, file.ID);

                    taskCache.Add(file.ID, task, null);

                    task.Start();
                    task.Wait();
                }

                if (fileCache.Contains(file.ID))
                {
                    var data = (byte[]) fileCache.Get(file.ID);
                    file.Size = data.Length;
                } else
                {
                    result = DokanResult.NotReady;
                }
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

            return Trace(nameof(GetFileInformation), info, result, fileInfo);
        }

        public NtStatus GetFileSecurity(string fileName, out FileSystemSecurity? security, AccessControlSections sections, IDokanFileInfo info)
        {
            security = null;
            return Trace(nameof(GetFileSecurity), info, DokanResult.NotImplemented, sections);
        }

        public NtStatus GetVolumeInformation(out string volumeLabel, out FileSystemFeatures features, out string fileSystemName, out uint maximumComponentLength, IDokanFileInfo info)
        {
            volumeLabel = "蓝奏云";
            fileSystemName = "NTFS";
            maximumComponentLength = 256;

            features = FileSystemFeatures.CasePreservedNames | FileSystemFeatures.CaseSensitiveSearch |
                       FileSystemFeatures.PersistentAcls | FileSystemFeatures.UnicodeOnDisk;


            return Trace(nameof(GetVolumeInformation), info, DokanResult.Success);
        }

        public NtStatus LockFile(string fileName, long offset, long length, IDokanFileInfo info)
        {
            return Trace(nameof(LockFile), info, DokanResult.Success);
        }

        public NtStatus Mounted(string mountPoint, IDokanFileInfo info)
        {
            return Trace(nameof(Mounted), info, DokanResult.Success);
        }

        public NtStatus MoveFile(string oldName, string newName, bool replace, IDokanFileInfo info)
        {
            NtStatus process()
            {
                var oldNode = directory.GetNode(oldName);
                var oldParent = directory.GetParent(oldName);
                var newParent = directory.GetParent(newName);

                oldName = VirtualDir.GetName(oldName);
                newName = VirtualDir.GetName(newName);

                if (oldNode == null)
                {
                    return DokanResult.FileNotFound;
                }

                if (oldParent == null || !oldParent.IsDirectory || newParent == null || !newParent.IsDirectory)
                {
                    return DokanResult.PathNotFound;
                }

                if (oldNode.IsDirectory)
                {
                    return DokanResult.Unsuccessful;
                }

                if (replace)
                {
                    return DokanResult.NotImplemented;
                }

                var result = client.MoveFile(oldNode.ID, newParent.ID);

                if (!result)
                {
                    return DokanResult.AccessDenied;
                }

                oldParent.Children.Remove(oldName);
                oldNode.Name = newName;
                oldNode.Parent = newParent;
                newParent.Children.Add(newName, oldNode);

                return DokanResult.Success;
            }

            Monitor.Enter(fsLock);
            var result = process();
            info.Context = null;
            Monitor.Exit(fsLock);

            return Trace(nameof(MoveFile), info, result);
        }

        public NtStatus ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset, IDokanFileInfo info)
        {
            NtStatus process(out int bytesRead)
            {
                var node = directory.GetNode(fileName);

                bytesRead = 0;

                if (node == null)
                {
                    return DokanResult.FileNotFound;
                }

                if (!fileCache.Contains(node.ID))
                {
                    return DokanResult.NotReady;
                }

                var requireSize = buffer.Length;

                var cacheData = (byte[])fileCache.Get(node.ID);

                var offsetEnd = cacheData.Length - offset;

                var readSize = Math.Min(offsetEnd, buffer.Length);

                for (var i = 0; i < readSize; i++)
                {
                    buffer[i] = cacheData[offset + i];
                }

                bytesRead = (int)readSize;

                return DokanResult.Success;
            }

            Monitor.Enter(fsLock);
            var result = process(out bytesRead);
            Monitor.Exit(fsLock);

            return Trace(nameof(ReadFile), info, DokanResult.Success);
        }

        public NtStatus SetAllocationSize(string fileName, long length, IDokanFileInfo info)
        {
            return Trace(nameof(SetAllocationSize), info, DokanResult.Success);
        }

        public void SetCookie(string cookie)
        {
            client.SetCookie(cookie);
        }

        public NtStatus SetEndOfFile(string fileName, long length, IDokanFileInfo info)
        {
            var result = DokanResult.DiskFull;
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

        private void Synchronize(ref VirtualFile node)
        {
            var page = 1;

            while (true)
            {
                var files = client.GetFiles(node.ID, page, false);

                if (!files.Success || (files.Value?.Count ?? 0) == 0)
                {
                    break;
                }

                foreach (var it in files.Value ?? [])
                {
                    var name = $"[{it.ID}] {it.Name}";
                    node.Children[name] = new()
                    {
                        IsDirectory = false,
                        ID = it.ID,
                        Parent = node,
                        Name = name,
                    };
                }

                page += 1;
            }

            var folders = client.GetFiles(node.ID, 1, true);

            if (!folders.Success)
            {
                return;
            }

            foreach (var it in folders.Value ?? [])
            {
                var name = $"[{it.ID}] {it.Name}";
                VirtualFile folder = new()
                {
                    IsDirectory = true,
                    ID = it.ID,
                    Parent = node,
                    Name = name
                };

                Synchronize(ref folder);
                node.Children.Add(name, folder);
            }
        }

        public void Synchronize()
        {
            new Task(() =>
            {
                if (Monitor.TryEnter(synLock))
                {
                    VirtualFile root = new() { IsDirectory = true, Name = "root", ID = "-1" };

                    Synchronize(ref root);

                    Monitor.Enter(fsLock);

                    directory.SetRoot(root);

                    Monitor.Exit(fsLock);

                    Monitor.Exit(synLock);
                }
            }).Start();
        }

        public NtStatus UnlockFile(string fileName, long offset, long length, IDokanFileInfo info)
        {
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
