/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2011 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit wiki.developer.mindtouch.com;
 * please review the licensing section.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.IO;
using System.Text;
using MindTouch.IO;

namespace MindTouch.Dream.Services.PubSub {

    public class DispatchItemSerializer : IQueueItemSerializer<DispatchItem> {

        //--- Constants ---
        public const byte SERIALIZER_VERSION = 1;

        //--- Methods ---
        public Stream ToStream(DispatchItem item) {
            var stream = new MemoryStream();
            stream.WriteByte(SERIALIZER_VERSION);
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
            var version = stream.ReadByte();
            if(version != SERIALIZER_VERSION) {
                throw new InvalidDataException(string.Format("Invalid Version: stream reported version as {0}, expected {1}", version, SERIALIZER_VERSION));
            }
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
            return new DispatchItem(uri, new DispatcherEvent(msg), location);
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
    }
}