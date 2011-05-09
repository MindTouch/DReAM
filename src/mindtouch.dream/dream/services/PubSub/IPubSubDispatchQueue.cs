using System;
using MindTouch.Tasking;

namespace MindTouch.Dream.Services.PubSub {
    public interface IPubSubDispatchQueue {
        void Enqueue(DispatchItem item);
        void SetDequeueHandler(Func<DispatchItem, Result<bool>> dequeueHandler);
    }
}