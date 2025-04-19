using VirtualDisk.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualDisk.FileSystem
{

    public class VirtualDir(VirtualFile root)
    {

        private VirtualFile root = root;
        public static List<string> GetPath(string fileName) => fileName.Split('\\').Where(x => x.Length > 0).ToList();
        public static string GetName(string fileName) => fileName[(fileName.LastIndexOf('\\') + 1)..];

        public delegate void TraverseFn(VirtualFile file);

        public void SetRoot(VirtualFile root)
        {
            this.root = root;
        }

        public VirtualFile? GetNode(List<string> path)
        {
            var node = root;
            foreach (var it in path)
            {
                if (node == null)
                    return null;

                if (node.Children.TryGetValue(it, out VirtualFile? value))
                {
                    node = value;
                }
                else
                {
                    return null;
                }
            }
            return node;
        }

        public VirtualFile? GetNode(string fileName)
        {
            return GetNode(GetPath(fileName));
        }

        public VirtualFile? GetParent(string fileName)
        {
            var path = GetPath(fileName);
            if (path.Count < 1)
            {
                return null;
            }
            return GetNode(path.GetRange(0, path.Count - 1));
        }

        public void Traverse(TraverseFn callback)
        {
            Stack<VirtualFile> stack = new();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var node = stack.Pop();
                callback(node);

                foreach (var it in node.Children)
                {
                    stack.Push(it.Value);
                }
            }
        }

    }
}
