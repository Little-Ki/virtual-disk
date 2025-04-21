using DokanNet.Logging;
using DokanNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualDisk.FileClient
{
    public class ClientHolder
    {
        private Task? task = null;

        private delegate void SuspendEvent();

        private event SuspendEvent? Suspend = null;
        public IExtendClient? Client { get; private set; } = null;

        public void Start<T>(string mount = @"Z:\") where T : IExtendClient, new()
        {
            if (task != null)
            {
                return;
            }

            Client = new T()
            {
                Mount = mount
            };

            task = new Task(async () =>
            {

                using var logger = new ConsoleLogger("[VirtualFS]");
                using var dokan = new Dokan(logger);
                var builder = new DokanInstanceBuilder(dokan)
                    .ConfigureLogger(() => logger)
                    .ConfigureOptions(options =>
                    {
                        //options.Options = DokanOptions.DebugMode | DokanOptions.EnableNotificationAPI;
                        options.MountPoint = Client.Mount;
                    });

                var instance = builder.Build(Client);

                Suspend = () =>
                {
                    dokan.RemoveMountPoint(Client.Mount);
                    Client = null;
                };

                await instance.WaitForFileSystemClosedAsync(uint.MaxValue);
            });

            task.Start();
        }

        public void Stop()
        {
            Suspend?.Invoke();
            Suspend = null;

            task?.Wait();
            task?.Dispose();    
            task = null;
        }
    }
}
