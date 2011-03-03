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

namespace System {

    /// <summary>
    /// Provides a common exception for situations that should never occur.
    /// </summary>
    public class ShouldNeverHappenException : Exception {
        
        //--- Constructors ---

        /// <summary>
        /// Create a new instance.
        /// </summary>
        public ShouldNeverHappenException() : base("This exception should never occur, but it did. Please notify the application authors of it and include as much information as posssible, including this message and the stack trace.") { }

        /// <summary>
        /// Create a new instance with a custom message.
        /// </summary>
        /// <param name="message">Custom message to include in exception.</param>
        public ShouldNeverHappenException(string message) : base(string.Format("This exception should never occur, but it did. Please notify the application authors of it and include as much information as posssible, including this message and the stack trace. (reason: {0})", message)) { }
    }
}