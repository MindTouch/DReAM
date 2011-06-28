using System;
using MindTouch.Dream;

namespace MindTouch.Aws {
    public class AwsSqsRequestException : Exception {
        public readonly DreamMessage Request;

        public AwsSqsRequestException(string message, DreamMessage request)
            : base(message) {
            Request = request;
        }
    }
}