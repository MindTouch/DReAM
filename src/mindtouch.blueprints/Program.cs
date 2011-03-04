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
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Web;
using MindTouch.Xml;

namespace MindTouch.Tools {
    internal static class Blueprints {

        //--- Constants ---
        private const string URL_OPTION = "-url=";
        private const string DESTINATION_OPTION = "-dest=";
        private const string LOGIN_OPTION = "-login=";
        private const string PASSWORD_OPTION = "-password=";
        private const string SERVICE_OPTION = "-service=";
        private static readonly string OPTION_PATTERN = String.Format(@"(^{0}|^{1}|^{2}|^{3}|^{4})", URL_OPTION, DESTINATION_OPTION, LOGIN_OPTION, PASSWORD_OPTION, SERVICE_OPTION);
        private static readonly Regex OPTION_REGEX = new Regex(OPTION_PATTERN, RegexOptions.Compiled | RegexOptions.CultureInvariant);

        //--- Class Methdos ---
        private static void Main(string[] args) {
            if(args.Length == 0) {
                PrintUsage();
                return;
            }
            string path = null;
            string login = String.Empty;
            string password = String.Empty;
            string service = String.Empty;
            XUri uri = null;

            // extract command line options
            Match option = OPTION_REGEX.Match(args[0].ToLower());
            while(option != null && option.Success) {
                switch(option.Value) {
                case URL_OPTION:
                    uri = XUri.TryParse(args[0].Substring(URL_OPTION.Length));
                    break;
                case DESTINATION_OPTION:
                    path = args[0].Substring(DESTINATION_OPTION.Length);
                    break;
                case LOGIN_OPTION:
                    login = args[0].Substring(LOGIN_OPTION.Length);
                    break;
                case PASSWORD_OPTION:
                    password = args[0].Substring(PASSWORD_OPTION.Length);
                    break;
                case SERVICE_OPTION:
                    service = args[0].Substring(SERVICE_OPTION.Length);
                    break;
                }
                args = ArrayUtil.SubArray(args, 1);
                if(args.Length > 0) {
                    option = OPTION_REGEX.Match(args[0].ToLower());
                } else {
                    option = null;
                }
            }

            // output the blueprint
            foreach(string arg in args) {
                if(null != uri) {
                    PublishBlueprint(uri, login, password, Assembly.LoadFrom(arg), service);
                } else {
                    if(path != null) {
                        path = Path.GetFullPath(path);
                    }
                    PrintBlueprint(path ?? Directory.GetCurrentDirectory(), Assembly.LoadFrom(arg));
                }
            }
        }

        private static void PublishBlueprint(XUri uri, String login, String password, Assembly assembly, string service) {

            // retrieve the authentication cookie
            List<DreamCookie> cookies = null;
            Plug p = Plug.New(uri).AtPath("users/authenticate").WithCredentials(login, password);
            DreamMessage msg = DreamMessage.Ok();
            msg = p.Get(msg, new Result<DreamMessage>()).Wait();
            if (DreamStatus.Ok == msg.Status) {
                cookies = msg.Cookies;
            } else {
                throw new Exception("An error occurred during user authentication:  " + msg.Status);
            }

            // extract all blueprints from the specified assembly and publish it to the wiki
            foreach (Type type in assembly.GetTypes()) {
                if(!string.IsNullOrEmpty(service) && !service.EqualsInvariantIgnoreCase(type.ToString())) {
                    continue;
                }
                object[] dsa = type.GetCustomAttributes(typeof(DreamServiceAttribute), false);
                if (!ArrayUtil.IsNullOrEmpty(dsa)) {
                    XDoc blueprint = DreamService.CreateServiceBlueprint(type);
                    string info = blueprint["info"].Contents;
                    string destination;
                    try {
                        destination = new XUri(info).Path.Trim('/');
                        if(string.IsNullOrEmpty(destination)) {
                            throw new Exception();
                        }
                    } catch {
                        Console.WriteLine("Warning: Cannot pusblish service '{0}' because it does not have a valid Info Uri: {1}",type,info);
                        continue;
                    }
                    PublishService(uri, login, password, cookies, destination, blueprint);
                    foreach(XDoc feature in blueprint["//feature"]) {
                        PublishFeature(uri, login, password, cookies, destination, feature);
                    }
                }
            }
        }

        private static void PublishService(XUri uri, String login, String password, List<DreamCookie> cookies, String destination, XDoc service) {

            // create a page on the wiki corresponding to a service
            Publish(uri, login, password, cookies, destination, GetServiceOverview(service), GetServiceAdditionalInfo(service));
        }

        private static void PublishFeature(XUri uri, String login, String password, List<DreamCookie> cookies, String destination, XDoc feature) {

            // create a page on the wiki corresponding to a  feature
            if(!IsExcludedFeature(feature["pattern"].Contents)) {
                Publish(uri, login, password, cookies, destination + "/" + feature["pattern"].Contents.Replace("/", "//"), GetFeatureOverview(feature), GetFeatureAdditionalInfo(feature));
            }
        }

        private static void Publish(XUri uri, String login, String password, List<DreamCookie> cookies, string destination, XDoc overviewDoc, XDoc additionalInfoDoc) {
            string encodedPageName = XUri.DoubleEncodeSegment(destination);
            string plugPath = "pages/=" + encodedPageName + "/contents?edittime=" + DateTime.Now.ToUniversalTime().ToString("yyyyMMddHHmmss");
            XDoc pageContents = null;

            // attempt to retrieve the page to determine if it already exists
            Plug p = Plug.New(uri).AtPath(plugPath);
            DreamMessage msg = DreamMessage.Ok();
            msg.Cookies.AddRange(cookies);
            msg = p.Get(msg, new Result<DreamMessage>()).Wait();

            switch(msg.Status) {
            case DreamStatus.Ok:

                // the page exists - update the auto-generated overview section
                plugPath += "&section=1";
                pageContents = overviewDoc;
                break;
            case DreamStatus.NotFound:

                // the page does not yet exist - create it
                pageContents = overviewDoc;
                pageContents.AddNodes(additionalInfoDoc);
                break;
            default:
                throw new Exception("An error occurred during publishing:  " + msg.Status);
            }

            // save the page contents
            p = Plug.New(uri).With("redirects", "0").AtPath(plugPath).WithCredentials(login, password);
            msg = DreamMessage.Ok(MimeType.TEXT, pageContents.ToInnerXHtml());
            msg.Cookies.AddRange(cookies);
            msg = p.Post(msg, new Result<DreamMessage>()).Wait();
            if(DreamStatus.Ok != msg.Status) {
                throw new Exception("An error occurred during publishing:  " + msg.Status);
            }
        }

        private static XDoc GetServiceOverview(XDoc service) {

            // generate a document containing the overview information for a service
            XDoc overviewDoc = new XDoc("html");
            overviewDoc.Start("h2").Value("Overview").End();
            overviewDoc.Add(GetWarning());
            overviewDoc.Start("strong").Value("Assembly:  ").End().Value(service["assembly"].Contents);
            overviewDoc.Add(new XDoc("br"));
            overviewDoc.Start("strong").Value("Class:  ").End().Value(service["class"].Contents);
            overviewDoc.Add(new XDoc("br"));
            overviewDoc.Start("strong").Value("SID:  ").End().Value(service["sid"].Contents);
            overviewDoc.Start("h5").Value("Configuration").End();
            overviewDoc.Add(GetTable(new string[] { "Name", "Type", "Description" }, service["configuration/entry"]));
            overviewDoc.Start("h5").Value("Features").End();
            XDoc featuresDoc = new XDoc("features");
            foreach(XDoc featureDoc in service["features/feature"]) {
                string name = featureDoc["pattern"].Contents;
                if(!IsExcludedFeature(name)) {
                    featuresDoc.Start("feature").Start("name").Value(String.Format("[[./{0}|{1}]]", name.Replace("/", "//"), name)).End().Start("description").Value(featureDoc["description"].Contents).End().End();
                }
            }
            overviewDoc.Add(GetTable(new string[] { "Name", "Description" }, featuresDoc["feature"]));
            return overviewDoc;
        }

        private static XDoc GetServiceAdditionalInfo(XDoc feature) {

            // generate a document containing the additional information for a service
            XDoc additionalInfoDoc = new XDoc("html");
            additionalInfoDoc.Start("h2").Value("Implementation Notes").End();
            additionalInfoDoc.Start("p").Start("em").Value("(include implementation notes information)").End().End();
            return additionalInfoDoc;
        }

        private static XDoc GetFeatureOverview(XDoc feature) {

            // generate a document containing the overview information for a feature
            XDoc overviewDoc = new XDoc("html");
            overviewDoc.Start("h2").Value("Overview").End();
            overviewDoc.Add(GetWarning());
            overviewDoc.Start("p").Start("strong").Value(feature["access"].Contents + ".  ").End().Value(feature["description"].Contents).End();
            XDoc uriParams = new XDoc("uriParams");
            XDoc queryParams = new XDoc("queryParams");
            foreach(XDoc param in feature["param"]) {
                string paramName = param["name"].Contents;
                if(paramName.StartsWith("{")) {
                    if(feature["pattern"].Contents.Contains(paramName)) {
                        param["name"].ReplaceValue(paramName.Trim(new char[] { '{', '}' }));
                        uriParams.Add(param);
                    }
                } else {
                    queryParams.Add(param);
                }
            }
            overviewDoc.Start("h5").Value("Uri Parameters").End();
            overviewDoc.Add(GetTable(new string[] { "Name", "Type", "Description" }, uriParams["param"]));
            overviewDoc.Start("h5").Value("Query Parameters").End();
            overviewDoc.Add(GetTable(new string[] { "Name", "Type", "Description" }, queryParams["param"]));
            overviewDoc.Start("h5").Value("Return Codes").End();
            XDoc statusesDoc = new XDoc("statuses");
            foreach(XDoc statusDoc in feature["status"]) {
                DreamStatus status = (DreamStatus)statusDoc["@value"].AsInt;
                statusesDoc.Start("status").Elem("name", status.ToString()).Elem("value", (int)status).Elem("description", statusDoc.Contents).End();
            }
            overviewDoc.Add(GetTable(new string[] { "Name", "Value", "Description" }, statusesDoc["status"]));
            return overviewDoc;
        }

        private static XDoc GetFeatureAdditionalInfo(XDoc feature) {

            // generate a document containing the additional information for a feature
            XDoc additionalInfoDoc = new XDoc("html");
            additionalInfoDoc.Start("h2").Value("Message Format").End();
            additionalInfoDoc.Start("p").Start("em").Value("(placeholder for message format)").End().End();
            additionalInfoDoc.Start("h2").Value("Implementation Notes").End();
            additionalInfoDoc.Start("p").Start("em").Value("(placeholder for implementation notes)").End().End();
            additionalInfoDoc.Start("h2").Value("Code Samples").End();
            additionalInfoDoc.Start("p").Start("em").Value("(placeholder for code samples)").End().End();
            return additionalInfoDoc;
        }

        private static bool IsExcludedFeature(string name) {
            int prefixIndex = name.IndexOf(':');
            if(0 <= prefixIndex) {
                return name.Substring(prefixIndex).StartsWith(":@");
            }
            return false;
        }

        private static XDoc GetWarning() {

            // generate a document contianing a warning that this documentation is auto-generated and changes will be overwritten
            return XDocFactory.From("<span class=\"comment\"><hr /><strong><font color=\"#9e0b0e\">WARNING: </font></strong><font color=\"#000000\">The 'Overview' section is auto-generated. Any changes applied to it will be overwritten when the documentation is updated.</font><hr/></span>", MimeType.XML);
        }

        private static XDoc GetTable(string[] columnNames, XDoc tableElements) {
            int numRows = tableElements.ListLength;
            string[] keys = new string[numRows];
            XDoc[] sortedTableElements = new XDoc[numRows];
            for(int i = 0; i < numRows; i++) {
                keys[i] = tableElements[0].Contents;
                sortedTableElements[i] = tableElements;
                tableElements = tableElements.Next;
            }
            Array.Sort(keys, sortedTableElements, StringComparer.Ordinal);

            // generate a document containing a table with the specified names and values
            XDoc tableDoc = null;
            if(0 == sortedTableElements.Length) {
                tableDoc = (new XDoc("p")).Start("em").Value("None").End();
            } else {
                tableDoc = XDocFactory.From("<table cellspacing=\"0\" cellpadding=\"1\" border=\"1\" style=\"width: 100%;\"><tbody><tr style=\"text-align: left; vertical-align: top; background-image: none; background-color: #e1e1e1;\"></tr></tbody></table>", MimeType.XML);
                foreach(string columnName in columnNames) {
                    tableDoc["tbody/tr"].Start("td").Start("strong").Value(columnName).End().End();
                }
                foreach(XDoc tableElement in sortedTableElements) {
                    XDoc rowDoc = new XDoc("tr").Attr("style", "text-align: left; vertical-align: top; background-image: none;");
                    foreach(XDoc column in tableElement.Elements) {
                        rowDoc.Start("td").Value(column.Contents).End();
                    }
                    tableDoc["tbody"].Add(rowDoc);
                }
            }
            return tableDoc;
        }

        private static void PrintBlueprint(string path, Assembly assembly) {
            foreach(Type type in assembly.GetTypes()) {
                object[] dsa = type.GetCustomAttributes(typeof(DreamServiceAttribute), false);
                if(!ArrayUtil.IsNullOrEmpty(dsa)) {
                    XDoc blueprint = DreamService.CreateServiceBlueprint(type);
                    foreach(XDoc sid in blueprint["sid"]) {
                        SaveBlueprint(path, sid.AsUri, blueprint);
                    }
                }
            }
        }

        private static void SaveBlueprint(string path, XUri uri, XDoc blueprint) {

            // check if uri is valid
            if(uri == null) {
                Console.WriteLine("ERROR: invalid SID ({0}) for {1}", uri, blueprint["class"].AsText);
                return;
            }

            // combine uri segments into a path and create folders
            for(int i = 0; i < uri.Segments.Length; ++i) {
                path = Path.Combine(path, uri.Segments[i]);
            }
            if(!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }
            path = Path.Combine(path, "blueprint.xml");
            blueprint.Save(path);
            Console.WriteLine("Create blueprint for {0}", blueprint["class"].AsText);
        }

        private static void CreateDestinationDirectory(string path) {
        }

        private static void PrintUsage() {
            Console.WriteLine("MindTouch Blueprints, Copyright (c) 2006-2011 MindTouch, Inc.");
            Console.WriteLine("USAGE: mindtouch.blueprints.exe <arg1> ... <argN>");
            Console.WriteLine("    -url=<url>              url for publishing to a wiki");
            Console.WriteLine("    -login=<login>          login for publishing to a wiki");
            Console.WriteLine("    -password=<password>    password for publishing to a wiki");
            Console.WriteLine("    -dest=<path>            destination path for blueprints");
            Console.WriteLine("    <assembly-name>         assembly file to load and process");
        }
    }
}
