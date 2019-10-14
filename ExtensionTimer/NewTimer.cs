using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ExtensionTimer
{
    public class NewTimer
    {
        public static Timer CreateTimer(TimerCallback callback, object? state, int value, TimeType type)
        {
            DateTime time = DateTime.Now;
            var mid = GetDiff(type, value, time);
            Timer timer = new Timer(callback, state, mid.Item1, mid.Item2);
            return timer;
        }
        private static Tuple<long, long> GetDiff(TimeType type, int value, DateTime time)
        {
            Func<TimeType, long> func1 = p =>
            {
                switch (p)
                {
                    //case TimeType.Millisecond: return 1000;
                    case TimeType.Second: return 1000 * 60;
                    case TimeType.Minute: return 1000 * 60 * 60;
                    case TimeType.Hours: return 1000 * 60 * 60 * 24;
                    default: throw new Exception("error type,not supported");
                }
            };

            Func<int, int, long> func2 = (p, q) => p > value ? (value + q - p) : (value - p);

            switch (type)
            {
                //case TimeType.Millisecond: return Tuple.Create(func2(time.Millisecond, 1000), func1(type));
                case TimeType.Second: return Tuple.Create(1000 * func2(time.Second, 60), func1(type));
                case TimeType.Minute: return Tuple.Create(1000 * 60 * func2(time.Minute, 60), func1(type));
                case TimeType.Hours: return Tuple.Create(1000 * 60 * 60 * func2(time.Hour, 24), func1(type));
                default: throw new Exception("error type,not supported");
            }
        }
    }
}
