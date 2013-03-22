/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2013 MindTouch, Inc.
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
using MindTouch.Xml;

namespace MindTouch.Dream {
    internal class DreamFeatureDirectory {

        //--- Fields ---
        internal Dictionary<string, DreamFeatureDirectory> Subfeatures = new Dictionary<string, DreamFeatureDirectory>(StringComparer.Ordinal);
        internal List<DreamFeature> SignatureMap = new List<DreamFeature>();

        //--- Methods ---
        internal void Add(string[] path, int level, DreamFeature feature) {

            // check if we reached the last part of the feature path
            if(level == path.Length) {

                // add feature to signature map
                SignatureMap.Add(feature);
            } else {
                string key = path[level];
                DreamFeatureDirectory entry = null;

                // check if sub-feature already exists
                if(Subfeatures.TryGetValue(key, out entry)) {
                    entry.Add(path, level + 1, feature);
                } else {
                    DreamFeatureDirectory tree = new DreamFeatureDirectory();
                    tree.Add(path, level + 1, feature);
                    Subfeatures.Add(key, tree);
                }
            }
        }

        internal void Add(string[] path, int level, DreamFeatureDirectory features) {

            // check if we reached the last part of the feature path
            if(level == path.Length) {
                throw new ArgumentException(string.Format("feature path is already in use ({0})", string.Join("/", path)));
            } else {
                string key = path[level];
                DreamFeatureDirectory entry = null;
                Subfeatures.TryGetValue(key, out entry);

                // check if added feature directory is a leaf node
                if(level == (path.Length - 1)) {

                    // check if there was an existing sub-feature
                    if(entry != null) {
                        features.CopyAndMergeFrom(entry);
                        Subfeatures[key] = features;
                    } else {
                        Subfeatures.Add(key, features);
                    }
                } else {

                    // check if a sub-feature needs to be created
                    if(entry == null) {
                        entry = new DreamFeatureDirectory();
                        Subfeatures.Add(key, entry);
                    }
                    entry.Add(path, level + 1, features);
                }
            }
        }

        internal void CopyAndMergeFrom(DreamFeatureDirectory features) {
            SignatureMap.AddRange(features.SignatureMap);
            foreach(KeyValuePair<string, DreamFeatureDirectory> pair in features.Subfeatures) {
                DreamFeatureDirectory entry;
                if(Subfeatures.TryGetValue(pair.Key, out entry)) {
                    entry.CopyAndMergeFrom(pair.Value);
                } else {
                    Subfeatures.Add(pair.Key, pair.Value);
                }
            }
        }

        internal void Remove(XUri uri) {
            Remove(uri.GetSegments(UriPathFormat.Normalized), 0);
        }

        internal void Remove(string[] path, int level) {
            string key = path[level];
            DreamFeatureDirectory entry;
            if(Subfeatures.TryGetValue(key, out entry)) {
                if(level == (path.Length - 1)) {
                    Subfeatures.Remove(key);
                } else {
                    entry.Remove(path, level + 1);
                }
            }
        }

        internal List<DreamFeature> Find(XUri uri) {
            return Find(uri.GetSegments(UriPathFormat.Normalized), 0);
        }

        internal List<DreamFeature> Find(string[] path, int level) {

            // check if we reached the last part of the path
            if(level == path.Length) {
                return FindMatchingSignature(0);
            }

            // search for path in sub-features
            List<DreamFeature> result = null;
            DreamFeatureDirectory entry;
            string key = path[level];

            // find sub-feature by name
            if(Subfeatures.TryGetValue(key, out entry)) {
                result = entry.Find(path, level + 1);
                if(result != null) {
                    return result;
                }
            }

            // find sub-feature by wildcard
            if(Subfeatures.TryGetValue("*", out entry)) {
                result = entry.Find(path, level + 1);
                if(result != null) {
                    return result;
                }
            }
            return FindMatchingSignature(path.Length - level);
        }

        internal List<DreamFeature> FindMatchingSignature(int argCount) {
            List<DreamFeature> result = null;
            foreach(DreamFeature entry in SignatureMap) {
                if(entry.OptionalSegments >= argCount) {
                    if(result == null) {
                        result = new List<DreamFeature>();
                    }
                    result.Add(entry);
                }
            }
            return result;
        }

        internal XDoc ListAll() {
            XDoc result = new XDoc("features");
            int count = 0;
            int hits = 0;
            RecurseListAll(string.Empty, result, ref count, ref hits);
            result.Attr("count", count);
            result.Attr("hits", hits);
            return result;
        }

        private void RecurseListAll(string prefix, XDoc doc, ref int count, ref int hits) {
            foreach(DreamFeature feature in SignatureMap) {
                if(feature.OptionalSegments != int.MaxValue) {
                    string optional = string.Empty;
                    for(int i = 0; i < feature.OptionalSegments; ++i) {
                        optional += "?/";
                    }
                    doc.Start("feature");
                    doc.Attr("path", feature.Verb + ":" + prefix + "/" + optional);
                    doc.Attr("hits", feature.HitCounter);
                    doc.End();
                    ++count;
                    hits += feature.HitCounter;
                } else {
                    doc.Start("feature");
                    doc.Attr("path", feature.Verb + ":" + prefix + "//*");
                    doc.Attr("hits", feature.HitCounter);
                    doc.End();
                    ++count;
                    hits += feature.HitCounter;
                }
            }
            foreach(KeyValuePair<string,DreamFeatureDirectory> subdir in Subfeatures) {
                subdir.Value.RecurseListAll(prefix + "/" + subdir.Key, doc, ref count, ref hits);
            }
        }
    }
}