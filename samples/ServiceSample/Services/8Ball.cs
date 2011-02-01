/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006, 2007 MindTouch, Inc.
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

using System.Collections.Generic;
using MindTouch.Dream;
using MindTouch.Tasking;

namespace MindTouch.Sample.Services {
    [DreamService("Dream Tutorial 8-Ball", "Copyright (c) 2006, 2007 MindTouch, Inc.",
        Info = "http://doc.opengarden.org/Dream/Samples/8-Ball",
        SID = new[] { "http://services.mindtouch.com/dream/tutorial/2007/03/8ball" }
    )]
    public class EightBallService : DreamService {

        //--- Class Fields ---

        // instance for generating random numbers
        private static System.Random _random = new System.Random();

        // list of random responses for the 8-ball
        private static string[] _answers = new string[] {
                                                            "Reply hazy, try again", "Without a doubt.", "My sources say no", "As I see it, yes" 
                                                        };

        //--- Features ---
        [DreamFeature("GET:", "Returns a random 8-ball message")]
        public DreamMessage GetAnswer() {

            // compute a random number between 0 and the number of responses we have
            int index = _random.Next(_answers.Length);

            // send our response back to the requestor
            return DreamMessage.Ok(MimeType.TEXT, "The 8-ball sez: " + _answers[index]);
        }
    }
}