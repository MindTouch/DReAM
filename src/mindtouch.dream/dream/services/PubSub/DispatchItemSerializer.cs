using System;
using System.IO;
using System.Text;
using MindTouch.IO;
using MindTouch.Xml;

namespace MindTouch.Dream.Services.PubSub {
    public class DispatchItemSerializer : IQueueItemSerializer<DispatchItem> {
        public Stream ToStream(DispatchItem item) {
            var stream = new MemoryStream();
            WriteString(stream, item.Uri.ToString());
            WriteString(stream, item.Location);
            var msg = item.Event.AsMessage();
            var body = msg.ToBytes();
            WriteString(stream, msg.ContentType.ToString());
            stream.Write(BitConverter.GetBytes(body.Length));
            stream.Write(body);
            foreach(var header in msg.Headers) {
                WriteString(stream, header.Key);
                WriteString(stream, header.Value);
            }
            stream.Position = 0;
            return stream;
        }


        public DispatchItem FromStream(Stream stream) {
            var uri = new XUri(ReadString(stream));
            var location = ReadString(stream);
            var mimetype = new MimeType(ReadString(stream));
            var bodyLength = BitConverter.ToInt32(stream.ReadBytes(4), 0);
            var body = stream.ReadBytes(bodyLength);
            var msg = DreamMessage.Ok(mimetype, body);
            while(stream.Position < stream.Length) {
                var key = ReadString(stream);
                var value = ReadString(stream);
                msg.Headers.Add(key, value);
            }
            return new DispatchItem(uri,new DispatcherEvent(msg), location);
        }

        private string ReadString(Stream stream) {
            var length = BitConverter.ToInt32(stream.ReadBytes(4), 0);
            return Encoding.UTF8.GetString(stream.ReadBytes(length));
        }

        private void WriteString(Stream stream, string s) {
            var bytes = Encoding.UTF8.GetBytes(s);
            var length = BitConverter.GetBytes(bytes.Length);
            stream.Write(length);
            stream.Write(bytes);
        }

        private void WritePair(Stream stream, string key, string value) {
            WriteString(stream,key);
            WriteString(stream,value);
        }
    }
}