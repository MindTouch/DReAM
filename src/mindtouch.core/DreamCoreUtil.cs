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
using System.Collections.Generic;
using System.Reflection;

using MindTouch.Xml;

namespace MindTouch.Dream {
    internal static class CoreUtil {

        //--- Constants ---
        internal const byte PADDING_BYTE = 32;

        //--- Methods ---
        internal static XDoc ExecuteCommand(Plug env, DreamHeaders headers, XDoc cmd) {
            try {
                switch(cmd.Name.ToLowerInvariant()) {
                case "script":
                    return ExecuteScript(env, headers, cmd);
                case "fork":
                    return ExecuteFork(env, headers, cmd);
                case "action":
                    return ExecuteAction(env, headers, cmd);
                case "pipe":
                    return ExecutePipe(env, headers, cmd);
                default:
                    throw new DreamException(string.Format("unregonized script command: " + cmd.Name.ToString()));
                }
            } catch(Exception e) {
                return new XException(e);
            }
        }

        internal static XDoc ExecuteScript(Plug env, DreamHeaders headers, XDoc script) {

            // execute script commands
            XDoc reply = new XDoc("results");
            string ID = script["@ID"].Contents;
            if(!string.IsNullOrEmpty(ID)) {
                reply.Attr("ID", ID);
            }
            foreach(XDoc cmd in script.Elements) {
                reply.Add(ExecuteCommand(env, headers, cmd));
            }
            return reply;
        }

        internal static XDoc ExecuteFork(Plug env, DreamHeaders headers, XDoc fork) {

            // execute script commands
            XDoc reply = new XDoc("results");
            string ID = fork["@ID"].Contents;
            if(!string.IsNullOrEmpty(ID)) {
                reply.Attr("ID", ID);
            }

            // TODO (steveb): we should use a 'fixed capacity' cue which marks itself as done when 'count' is reached

            XDoc forks = fork.Elements;
            foreach(XDoc cmd in forks) {

                // TODO (steveb): we should be doing this in parallel, not sequentially!

                try {
                    reply.Add(ExecuteCommand(env, headers, cmd));
                } catch(Exception e) {
                    reply.Add(new XException(e));
                }
            }
            return reply;
        }

#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking.  Please avoid using it if possible.")]
#endif
        /// <summary>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </summary>
        internal static XDoc ExecuteAction(Plug env, DreamHeaders headers, XDoc action) {
            string verb = action["@verb"].Contents;
            string path = action["@path"].Contents;
            if((path.Length > 0) && (path[0] == '/')) {
                path = path.Substring(1);
            }
            XUri uri;
            if(!XUri.TryParse(path, out uri)) {
                uri = env.Uri.AtAbsolutePath(path);
            }

            // create message
            DreamMessage message = DreamMessage.Ok(GetActionBody(action));
            message.Headers.AddRange(headers);

            // apply headers
            foreach(XDoc header in action["header"]) {
                message.Headers[header["@name"].Contents] = header.Contents;
            }

            // BUG #814: we need to support events

            // execute action
            DreamMessage reply = Plug.New(uri).Invoke(verb, message);

            // prepare response
            XDoc result = new XMessage(reply);
            string ID = action["@ID"].Contents;
            if(!string.IsNullOrEmpty(ID)) {
                result.Root.Attr("ID", ID);
            }
            return result;
        }

#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking.  Please avoid using it if possible.")]
#endif
        /// <summary>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </summary>
        internal static XDoc ExecutePipe(Plug env, DreamHeaders headers, XDoc pipe) {
            DreamMessage message = null;
            foreach(XDoc action in pipe["action"]) {
                string verb = action["@verb"].Contents;
                string path = action["@path"].Contents;
                XUri uri;
                if(!XUri.TryParse(path, out uri)) {
                    uri = env.Uri.AtPath(path);
                }

                // create first message
                if(message == null) {
                    message = DreamMessage.Ok(GetActionBody(action));
                }
                message.Headers.AddRange(headers);

                // apply headers
                foreach(XDoc header in action["header"]) {
                    message.Headers[header["@name"].Contents] = header.Contents;
                }

                // execute action
                message = Plug.New(uri).Invoke(verb, message);
                if(!message.IsSuccessful) {
                    break;
                }
            }

            // prepare response
            if(message == null) {
                return XDoc.Empty;
            }
            XDoc result = new XMessage(message);
            string ID = pipe["@ID"].Contents;
            if(!string.IsNullOrEmpty(ID)) {
                result.Root.Attr("ID", ID);
            }
            return result;
        }

        internal static XDoc GetActionBody(XDoc action) {
            XDoc result = action["body"];
            if(!result.IsEmpty) {

                // check if the body tag contains a nested element
                if(!result.Elements.IsEmpty) {
                    result = result[0];
                }
            } else {

                // select the last child element of the <action> element
                result = action["*[last()]"];
            }
            return result;
        }

        internal static Type FindBuiltInTypeBySID(XUri sid) {
            Assembly assembly = Assembly.GetCallingAssembly();
            foreach(Type type in assembly.GetTypes()) {
                DreamServiceAttribute attr = (DreamServiceAttribute)Attribute.GetCustomAttribute(type, typeof(DreamServiceAttribute), false);
                XUri[] sids = (attr != null) ? attr.GetSIDAsUris() : new XUri[0];
                if(sids.Length > 0) {
                    foreach(XUri tmp in sids) {
                        if(tmp == sid) {
                            return type;
                        }
                    }
                }
            }
            return null;
        }
    }
}
