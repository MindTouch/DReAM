using System.Collections.Generic;
using MindTouch.Dream;

namespace MindTouch.Aws {
    public class AwsEndpoint {

        //--- Class Fields ---
        public static readonly AwsEndpoint USEast = new AwsEndpoint(null, "http://s3.amazonaws.com", "http://sqs.us-east-1.amazonaws.com/");
        public static readonly AwsEndpoint USWest = new AwsEndpoint("us-west-1", "http://s3-us-west-1.amazonaws.com", "http://sqs.us-west-1.amazonaws.com/");
        public static readonly AwsEndpoint EU = new AwsEndpoint("EU", "http://s3-eu-west-1.amazonaws.com", "http://sqs.eu-west-1.amazonaws.com/");
        public static readonly AwsEndpoint AsiaPacificSingapore = new AwsEndpoint("ap-southeast-1", "http://s3-ap-southeast-1.amazonaws.com", "http://sqs.ap-southeast-1.amazonaws.com/");
        public static readonly AwsEndpoint AsiaPacificJapan = new AwsEndpoint("ap-northeast-1", "http://s3-ap-northeast-1.amazonaws.com", "http://sqs.ap-northeast-1.amazonaws.com/");
        public static readonly AwsEndpoint Default = USEast;
        private static readonly IDictionary<string, AwsEndpoint> _endpoints = new Dictionary<string, AwsEndpoint>();

        //--- Class Constructor ----
        static AwsEndpoint() {
            _endpoints.Add("default", Default);
            _endpoints.Add("us-east-1", USEast);
            _endpoints.Add(USWest.Name, USWest);
            _endpoints.Add(EU.Name, EU);
            _endpoints.Add(AsiaPacificSingapore.Name, AsiaPacificSingapore);
            _endpoints.Add(AsiaPacificJapan.Name, AsiaPacificJapan);
        }

        //--- Class Methods ---
        public static AwsEndpoint GetEndpoint(string name) {
            AwsEndpoint endpoint;
            _endpoints.TryGetValue(name, out endpoint);
            return endpoint;
        }

        public static void AddEndpoint(AwsEndpoint endpoint) {
            _endpoints[endpoint.Name] = endpoint;
        }

        //--- Fields ---
        public readonly XUri S3Uri;
        public readonly XUri SqsUri;
        public readonly string LocationConstraint;
        public readonly string Name;

        //--- Constructors ---
        public AwsEndpoint(string locationConstraint, string s3Uri, string sqsUri) {
            S3Uri = new XUri(s3Uri);
            SqsUri = new XUri(sqsUri);
            LocationConstraint = locationConstraint;
            Name = LocationConstraint ?? "default";
        }
    }
}