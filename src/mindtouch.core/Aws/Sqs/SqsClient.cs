using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using log4net;
using MindTouch.Deki.Services.Mtps.Aws.Sqs;

namespace MindTouch.Dream.Aws.Sqs {
    public class SqsClient : IDisposable {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly SqsClientConfig _config;
        private readonly uint _maxMessages;
        private readonly uint? _visibilityTimeout;
        private readonly bool _useExpires;

        //--- Constructors ---
        public SqsClient(SqsClientConfig config) {
            _config = config;
            _maxMessages = config.MaxMessages;
            if (config.VisibilityTimeout != null) {
                _visibilityTimeout = config.VisibilityTimeout;
            }
            _useExpires = config.UseExpires;
            QueueUri = config.QueueUri;
        }

        //--- Properties ---
        public XUri QueueUri { get; private set; }

        //--- Methods ---
        public List<SqsMessage> GetMessages() {
            var parameters = new Dictionary<string, string> {
                {"MaxNumberOfMessages", _maxMessages.ToString()}
            };
            if(_visibilityTimeout != null) {
                parameters.Add("VisibilityTimeout", _visibilityTimeout.ToString()); 
            }
            var response = GetResponse("GET", "ReceiveMessage", parameters);
            if(!response.IsSuccessful) {
                throw new DreamResponseException(response);
            }
            var results = response.ToDocument()
                .UsePrefix("sqs", "http://queue.amazonaws.com/doc/2009-02-01/")
                ["//sqs:ReceiveMessageResult/sqs:Message"];
            var messages = new List<SqsMessage>();
            foreach(var result in results) {
                var receiptHandle = result["ReceiptHandle"].AsText;
                var message = new SqsMessage(
                    result["MessageId"].AsText,
                    receiptHandle,
                    result["MD5OfBody"].AsText,
                    result["Body"].AsText
                    );
                messages.Add(message);
                DeleteMessage(receiptHandle);
            }
            return messages;
        }

        public void PostMessage(string message) {
            var parameters = new Dictionary<string, string> {
                                                                {"MessageBody", message}
                                                            };
            var response = GetResponse("POST", "SendMessage", parameters);
            if(response.IsSuccessful) {
                return;
            }
            throw new DreamResponseException(response);
        }

        public void DeleteMessage(string receiptHandle) {
            var parameters = new Dictionary<string, string> {
                                                                {"ReceiptHandle", receiptHandle},
                                                            };
            var response = GetResponse("POST", "DeleteMessage", parameters);
            if(response.IsSuccessful) {
                return;
            }
            throw new DreamResponseException(response);
        }

        public void Dispose() {
        }

        protected DreamMessage GetResponse(string verb, string action, Dictionary<string, string> parameters) {
            verb = verb.ToUpperInvariant();
            var time = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
            parameters.Add("AWSAccessKeyId", _config.PublicKey);
            parameters.Add("Action", action);
            parameters.Add("SignatureMethod", AwsSignature.HashMethod);
            parameters.Add("SignatureVersion", "2");
            parameters.Add("Version", "2009-02-01");
            parameters.Add(_useExpires ? "Expires" : "Timestamp", time);
            foreach(var key in parameters.Keys.ToArray()) {
                parameters[key] = XUri.Encode(parameters[key]);
            }
            parameters = parameters
                .OrderBy(param => param.Key, StringComparer.Ordinal)
                .ToDictionary(param => param.Key, param => param.Value);
            var query = string.Join("&", parameters.Keys.Select(param => param + "=" + parameters[param]).ToArray());
            var p = Plug.New(_config.QueueUri).WithTimeout(_config.Timeout);
            p = p.WithQuery(query);
            var request = string.Format("{0}\n{1}\n{2}\n{3}", verb, p.Uri.Host, p.Uri.Path, query);
            var signature = new AwsSignature(_config.PrivateKey).GetSignature(request);

            switch(verb) {
            case "GET":
                break;
            case "POST":
                p = p.WithHeader(DreamHeaders.CONTENT_TYPE, "application/x-www-form-urlencoded");
                break;
            default:
                break;
            }
            return p.With("Signature", signature).Invoke(verb, DreamMessage.Ok());
        }
    }
}