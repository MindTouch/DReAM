using System;
using MindTouch.Tasking;

namespace MindTouch.Dream.Services.PubSub {
    public class PersistentPubSubDispatchQueueFactory : IPersistentPubSubDispatchQueueFactory {
        private readonly TaskTimerFactory _taskTimerFactory;
        private readonly TimeSpan _checkTime;
        private readonly TimeSpan _retryTime;

        public PersistentPubSubDispatchQueueFactory(TaskTimerFactory taskTimerFactory, TimeSpan checkTime, TimeSpan retryTime) {
            _taskTimerFactory = taskTimerFactory;
            _checkTime = checkTime;
            _retryTime = retryTime;
        }

        public IPubSubDispatchQueue Create(string location) {
            return new MemoryPubSubDispatchQueue(_taskTimerFactory, _checkTime, _retryTime);
        }
    }
}