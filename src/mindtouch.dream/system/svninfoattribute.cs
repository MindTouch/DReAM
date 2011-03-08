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
    /// Assembly attribute used by MindTouch build system to track subversion revision number with the Assembly.
    /// </summary>
    public class SvnRevisionAttribute : Attribute {

        //--- Fields ---
        private readonly int _revision;

        //--- Constructors ---

        /// <summary>
        /// Default constructor used by Attribute syntax.
        /// </summary>
        /// <param name="revision">Subversion revision number for the Assembly.</param>
        public SvnRevisionAttribute(int revision) {
            _revision = revision;
        }

        //--- Properties ---
        
        /// <summary>
        /// Accessor for revsion number attached via attribute.
        /// </summary>
        public int Revision { get { return _revision; } }
    }

    /// <summary>
    /// Assembly attribute used by MindTouch build system to track subversion branch name with the Assembly.
    /// </summary>
    public class SvnBranchAttribute : Attribute {

        //--- Fields ---
        private readonly string _branch;

        //--- Constructors ---

        /// <summary>
        /// Default constructor used by Attribute syntax.
        /// </summary>
        /// <param name="branch">Subversion branch name for Assembly.</param>
        public SvnBranchAttribute(string branch) {
            _branch = branch;
        }

        //--- Properties ---

        /// <summary>
        /// Accessor for branch name attached via attribute.
        /// </summary>
        public string Branch { get { return _branch; } }
    }
}
