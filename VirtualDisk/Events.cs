using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualDisk
{
    enum Events
    {

    }

    abstract class Event
    {
        abstract public void Run();

        private static readonly ConcurrentQueue<Event> EventQueue = new();

        static public Event? Peek()
        {
            EventQueue.TryPeek(out Event? e);
            return e;
        }

        static public void Push(Event e)
        {
            EventQueue.Enqueue(e);
        }

        static public void Pop()
        {
            EventQueue.TryDequeue(out Event? _);
        }
    }

}
