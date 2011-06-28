using MindTouch.Xml;

namespace MindTouch.Aws {
    public class AwsSqsResponse {
        public AwsSqsResponse(XDoc doc) {
            RequestId = doc["sqs:ResponseMetadata/sqs:RequestId"].AsText;
        }
        protected AwsSqsResponse() { }
        public string RequestId { get; protected set; }
    }
}