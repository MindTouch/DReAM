/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2014 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit mindtouch.com;
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

using MindTouch.Dream.Test;
using MindTouch.Tasking;
using NUnit.Framework;

namespace MindTouch.Dream.Storage.Test {
    
    [TestFixture]
    public class DefaultPublicStorageTests {
        [Test]
        public void Default_public_storage_root_cannot_be_read() {
            using(DreamHostInfo hostInfo = DreamTestHelper.CreateRandomPortHost()) {
                MockServiceInfo mock = MockService.CreateMockService(hostInfo);
                mock.Service.CatchAllCallback = delegate(DreamContext context, DreamMessage request, Result<DreamMessage> response2) {
                    DreamMessage r = Plug.New((context.Service as MockService).Storage.Uri.WithoutLastSegment()).Get(new Result<DreamMessage>()).Wait();
                    response2.Return(r);
                };
                DreamMessage response = mock.AtLocalMachine.Get(new Result<DreamMessage>()).Wait();
                Assert.AreEqual(DreamStatus.Forbidden, response.Status);
            }
        }

        [Test]
        public void Default_public_storage_root_cannot_be_written_to() {
            using(DreamHostInfo hostInfo = DreamTestHelper.CreateRandomPortHost()) {
                MockServiceInfo mock = MockService.CreateMockService(hostInfo);
                mock.Service.CatchAllCallback = delegate(DreamContext context, DreamMessage request, Result<DreamMessage> response2) {
                    DreamMessage r = Plug.New((context.Service as MockService).Storage.Uri.WithoutLastSegment())
                        .At("foo.txt")
                        .Put(DreamMessage.Ok(MimeType.TEXT, "bar"), new Result<DreamMessage>())
                        .Wait();
                    response2.Return(r);
                };
                DreamMessage response = mock.AtLocalMachine.Get(new Result<DreamMessage>()).Wait();
                Assert.AreEqual(DreamStatus.Forbidden, response.Status);
            }
        }
    }
}
