namespace MindTouch.Dream.Services.PubSub {
    public interface IPersistentPubSubDispatchQueueFactory {
        IPubSubDispatchQueue Create(string location);
    }
}