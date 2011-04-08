using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

namespace Naggy
{
    public class DelayedRequestExecutor<T>
    {
        Timer delayTimer;
        Dictionary<T, Action> idRequestMap = new Dictionary<T, Action>();

        public DelayedRequestExecutor(int timerIntervalInMilliseconds)
        {
            delayTimer = new Timer(timerIntervalInMilliseconds);
            delayTimer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
        }

        object idRequestMapLock = new object();
        public void Add(T id, Action request)
        {
            lock (idRequestMapLock)
            {
                idRequestMap[id] = request;
                RestartTimer();
            }
        }

        private void RestartTimer()
        {
            delayTimer.Stop();
            delayTimer.Start();
        }
        
        object reentrancyBlockerLock = new object();
        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (reentrancyBlockerLock)
            {
                delayTimer.Stop();
                ExecutePendingRequests();
            }
        }

        private void ExecutePendingRequests()
        {
            KeyValuePair<T, Action> [] requests;

            lock (idRequestMapLock)
            {
                requests = idRequestMap.ToArray();
                idRequestMap.Clear();
            }

            foreach (var request in requests)
            {
                request.Value();
            }
        }
    }
}
