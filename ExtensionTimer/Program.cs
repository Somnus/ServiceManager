using System;
using System.Threading;

namespace ExtensionTimer
{
    class Program
    {
        static void Main(string[] args)
        {
            TimerCallback callback = state =>
            {
                Console.WriteLine("每秒执行一次的定时任务,当前线程Id:{0}", Thread.CurrentThread.ManagedThreadId);
            };
            var timer = NewTimer.CreateTimer(callback, null, 40, TimeType.Second);
            Console.ReadKey();
        }
    }
  
}
