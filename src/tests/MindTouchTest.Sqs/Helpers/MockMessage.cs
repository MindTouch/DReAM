using System;
using MindTouch.Sqs;
using MindTouch.Xml;

namespace MindTouchTest.Sqs.Helpers {
    public class MockMessage {
        
        //--- Static fields ---
        private static int NEXT;
        
        //--- Constructors ---
        public static SqsMessage NewMockMessage() {
            var messageId = Guid.NewGuid().ToString();
            var messageReceipt = (++NEXT).ToString();
            return new SqsMessage(new SqsMessageId(messageId), new SqsMessageReceipt(messageReceipt),
                new XDoc("doc").Elem("id", messageId).Elem("receipt-handle", messageReceipt).ToCompactString());
        }

        //--- Methods ---
        public SqsMessage NewMockMessage(int id) {
            var messageId = id.ToString();
            var MessageReceipt = Guid.NewGuid().ToString();
            var body = new XDoc("doc").Elem("id", messageId).Elem("receipt-handle", MessageReceipt).ToCompactString();
            return new SqsMessage(new SqsMessageId(messageId), new SqsMessageReceipt(MessageReceipt), body);
        }
    }
}
