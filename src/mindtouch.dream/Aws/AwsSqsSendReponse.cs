using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Aws {
    public class AwsSqsSendReponse : AwsSqsResponse {

        public AwsSqsSendReponse(XDoc doc) {
            MessageId = doc["sqs:SendMessageResult/sqs:MessageId"].AsText;
            RequestId = doc["sqs:ResponseMetadata/sqs:RequestId"].AsText;
            MD5OfMessageBody = doc["sqs:SendMessageResult/sqs:MD5OfBody"].AsText;
        }
        public string MD5OfMessageBody { get; protected set; }
        public string MessageId { get; protected set; }
    }
}