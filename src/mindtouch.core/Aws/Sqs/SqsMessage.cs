namespace MindTouch.Dream.Aws.Sqs {
    public class SqsMessage {

        //--- Constructors ---

        /// <summary>
        /// Create a new message instance 
        /// </summary>
        /// <param name="messageId"></param>
        /// <param name="receiptHandle"></param>
        /// <param name="md5OfBody"></param>
        /// <param name="body"></param>
        public SqsMessage(string messageId, string receiptHandle, string md5OfBody, string body) {
            Id = messageId;
            ReceiptHandle = receiptHandle;
            Md5OfBody = md5OfBody;
            Body = body;
        }

        //--- Properties ---
        public string Id { get; protected set; }
        public string ReceiptHandle { get; protected set; }
        public string Md5OfBody { get; protected set; }
        public string Body { get; protected set; }
    }
}