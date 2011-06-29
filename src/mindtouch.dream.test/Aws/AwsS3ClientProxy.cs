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
using MindTouch.Aws;

namespace MindTouch.Dream.Test.Aws {
    public class AwsS3ClientProxy : IAwsS3Client {

        public IAwsS3Client Client;

        public void Dispose() {
        }

        public AwsS3DataInfo GetDataInfo(string path, bool head) {
            return Client.GetDataInfo(path, head);
        }

        public void PutFile(string path, AwsS3FileHandle fileInfo) {
            Client.PutFile(path, fileInfo);
        }

        public void Delete(string path) {
            Client.Delete(path);
        }
    }
}