using VirtualDisk.FileSystem;
using System.Runtime.Caching;
using ClientResult = VirtualDisk.Client.IClient.Result;

namespace VirtualDisk.Client
{
    public class WooZoooFS : IClient
    {
        private readonly VirtualDir directory = new(new VirtualFile() { IsDirectory = true, Name = "root", ID = "-1" });

        private readonly API.WooZooo.Client client = new();

        private readonly object fsLock = new();

        private readonly object synLock = new();

        private readonly ObjectCache fileCache = MemoryCache.Default;

        public string ClientName() => "蓝奏云";

        public ClientResult CreateFile(string fileName, bool isDir)
        {
            ClientResult process()
            {
                var parent = directory.GetParent(fileName);
                var name = VirtualDir.GetName(fileName);

                if (parent == null)
                {
                    return ClientResult.InvalidPath;
                }

                if (isDir)
                {
                    var result = client.CreateFolder(name, parent.ID);

                    if (!result.Success)
                    {
                        return ClientResult.Failure;
                    }

                    parent.Children[name] = new()
                    {
                        IsDirectory = isDir,
                        Name = name,
                        Parent = parent,
                        ID = result.Value ?? ""
                    };

                    return ClientResult.Success;
                }
                else
                {
                    return ClientResult.NotSupport;
                }
            }

            Monitor.Enter(fsLock);

            var result = process();

            Monitor.Exit(fsLock);

            return result;
        }

        public ClientResult DeleteFile(string fileName, bool isDir, bool isTest)
        {
            ClientResult test()
            {
                var node = directory.GetNode(fileName);
                var name = VirtualDir.GetName(fileName);

                if (node == null)
                {
                    return ClientResult.NotFound;
                }

                if (node.Parent == null)
                {
                    return ClientResult.InvalidPath;
                }

                if (node.IsDirectory != isDir)
                {
                    return ClientResult.Rejected;
                }

                if (node.IsDirectory && node.Children.Count > 0)
                {
                    return ClientResult.IsNotEmpty;
                }

                return ClientResult.Success;
            }

            ClientResult process()
            {
                var node = directory.GetNode(fileName);
                var parent = node?.Parent ?? null;

                if (node != null && parent != null)
                {
                    var result = node.IsDirectory ? client.DeleteFolder(node.ID) : client.DeleteFile(node.ID);

                    if (!result)
                    {
                        return ClientResult.Failure;
                    }

                    parent.Children.Remove(node.Name);
                }

                return ClientResult.Success;
            }

            Monitor.Enter(fsLock);

            var result = isTest ? test() : process();

            Monitor.Exit(fsLock);

            return result;
        }

        public ClientResult LockFile(string fileName)
        {
            return ClientResult.NotSupport;
        }

        public ClientResult UnockFile(string fileName)
        {
            return ClientResult.NotSupport;
        }

        public ClientResult MoveFile(string oldName, string newName, bool replace)
        {
            ClientResult process()
            {
                var oldNode = directory.GetNode(oldName);
                var oldParent = directory.GetParent(oldName);
                var newNode = directory.GetNode(newName);
                var newParent = directory.GetParent(newName);

                oldName = VirtualDir.GetName(oldName);
                newName = VirtualDir.GetName(newName);

                if (newNode != null)
                {
                    return ClientResult.FileExists;
                }

                if (oldParent == null || newParent == null)
                {
                    return ClientResult.InvalidPath;
                }

                if (oldNode == null)
                {
                    return ClientResult.NotFound;
                }

                if (oldNode.IsDirectory)
                {
                    return ClientResult.NotSupport;
                }

                if (replace)
                {
                    return ClientResult.NotSupport;
                }

                var result = client.MoveFile(oldNode.ID, newParent.ID);

                if (!result)
                {
                    return ClientResult.Failure;
                }

                oldParent.Children.Remove(oldName);
                oldNode.Name = newName;
                oldNode.Parent = newParent;
                newParent.Children.Add(newName, oldNode);

                return ClientResult.Success;
            }

            Monitor.Enter(fsLock);

            var result = process();

            Monitor.Exit(fsLock);

            return result;
        }

        public ClientResult OpenFile(string fileName, out VirtualFile? file, bool useUpdate = false)
        {
            Monitor.Enter(fsLock);

            file = directory.GetNode(fileName);

            void process(ref VirtualFile file)
            {
                if (file.IsDirectory)
                    return;

                var url = client.GetSharedLink(file.ID);

                if (!url.Success)
                    return;

                var lnk = client.GetFileUrl(url.Value!.Host, url.Value!.FileID, file.ID, url.Value!.Password);

                if (!lnk.Success)
                    return;

                var data = client.GetData(lnk.Value!);

                if (data.Length > 0)
                {
                    file.Size = data.Length;
                    fileCache.Set(file.ID, data, null);
                }
            }

            if (useUpdate && file != null && !file.IsDirectory && !fileCache.Contains(file.ID))
            {
                process(ref file);
            }

            Monitor.Exit(fsLock);

            return ClientResult.Success;
        }

        public ClientResult ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset)
        {
            Monitor.Enter(fsLock);
            var node = directory.GetNode(fileName);
            Monitor.Exit(fsLock);

            bytesRead = 0;

            if (node == null)
            {
                return ClientResult.NotFound;
            }

            if (!fileCache.Contains(node.ID))
            {
                return ClientResult.NotFound;
            }

            var requireSize = buffer.Length;

            var chachData = (byte[])fileCache.Get(node.ID);

            var offsetEnd = chachData.Length - offset;

            var readSize = Math.Min(offsetEnd, buffer.Length);

            for (var i = 0; i < readSize; i++)
            {
                buffer[i] = chachData[offset + i];
            }

            bytesRead = (int)readSize;

            return ClientResult.Success;
        }

        public ClientResult WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset)
        {
            bytesWritten = 0;
            return ClientResult.NotSupport;
        }

        public ClientResult ResizeFile(string fileName, long length)
        {
            return ClientResult.NotSupport;
        }

        public ClientResult SpaceInfo(out long spaceRemain, out long totalBytes, out long bytesRemain)
        {
            Monitor.Enter(fsLock);

            var totalSum = 0L;
            var totalSize = 1024 * 1024 * 1024;

            directory.Traverse((VirtualFile file) =>
            {
                totalSum += file.Size;
            });

            totalBytes = totalSum;
            spaceRemain = totalSize - totalBytes;
            bytesRemain = spaceRemain;

            Monitor.Exit(fsLock);

            return ClientResult.Success;
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

        public ClientResult Synchronize()
        {
            var task = new Task(() =>
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
            });

            task.Start();

            return ClientResult.Success;
        }

        public ClientResult SetCookies(string cookies)
        {
            client.SetCookie(cookies);
            return ClientResult.Success;
        }

    }
}
