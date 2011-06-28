using System;
using MindTouch.Dream;
using MindTouch.Extensions.Time;

namespace MindTouch.Deki.Services.Mtps.Aws.Sqs
{
    /// <summary>
    /// Amazon SQS Client configuration
    /// </summary>
    public class SqsClientConfig { 

        //--- Constants ---
        private const uint DEFAULT_MAX_MESSAGES = 1;
        private const bool DEFAULT_USE_EXPIRES = false;

        //--- Class Fields ---
        private static readonly TimeSpan DEFAULT_TIMEOUT = 30.Seconds();

        //--- Fields ---

        //--- Constructors ---
        public SqsClientConfig() {
            Timeout = DEFAULT_TIMEOUT;
            MaxMessages = DEFAULT_MAX_MESSAGES;
            UseExpires = DEFAULT_USE_EXPIRES;
            VisibilityTimeout = null;
        }

        //--- Properties ---

        /// <summary>
        /// Amazon SQS Queue Uri (WSDL Version 2009-02-01 http://docs.amazonwebservices.com/AWSSimpleQueueService/latest/APIReference)
        /// format: (http|https)://sqs.(region).amazonaws.com/(account)/(queue)
        /// </summary>
        public XUri QueueUri { get; set; }

        /// <summary>
        /// Private Key.
        /// </summary>
        public string PrivateKey { get; set; }

        /// <summary>
        /// Public Key.
        /// </summary>
        public string PublicKey { get; set; }

        /// <summary>
        /// Max messages to fetch per request
        /// constraints: 1 to 10
        /// default: 1
        /// </summary>
        public uint MaxMessages { get; set; }

        /// <summary>
        /// The duration (in seconds) that the received messages are hidden from subsequent retrieve requests after being received
        /// constraints: 0 to 43200
        /// default: visibility timeout for the queue
        /// </summary>
        public uint? VisibilityTimeout { get; set; }

        /// <summary>
        /// Use Expires parameter instead of Timestamp
        /// </summary>
        public bool UseExpires { get; set; }

        /// <summary>
        /// Client call timeout.
        /// </summary>
        public TimeSpan Timeout { get; set; }
    }
}
