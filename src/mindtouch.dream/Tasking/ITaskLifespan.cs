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

namespace MindTouch.Tasking {

    /// <summary>
    /// Interface for marking objects whose lifespan is governed by a single <see cref="TaskEnv"/>
    /// </summary>
    public interface ITaskLifespan {

        //--- Methods --- 

        /// <summary>
        /// Appropriately duplicate the instance for attachment to another <see cref="TaskEnv"/>
        /// </summary>
        /// <returns></returns>
        object Clone();

        /// <summary>
        /// Clean-up the instance at the conclusion of the owning <see cref="TaskEnv"/>
        /// </summary>
        void Dispose();
    }
}
