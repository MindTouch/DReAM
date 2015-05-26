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

using System;
using System.Text.RegularExpressions;

namespace MindTouch.Data {
    public class VersionInfo {

        //--- Class Fields ---
        private static readonly Regex _versionRegex = new Regex(@"(?<major>\d+)\.(?<minor>\d+).(?<revision>\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        //--- Fields ---
        public readonly bool IsValid;
        public readonly string Raw;
        public readonly int Major = -1;
        public readonly int Minor;
        public readonly int Revision;

        //--- Constructors ---
        public VersionInfo(string versionString) {
            Raw = versionString;
            var m = _versionRegex.Match(versionString);
            if(!m.Success) {
                return;
            }
            IsValid = true;
            Major = Convert.ToInt32(m.Groups["major"].Value);
            Minor = Convert.ToInt32(m.Groups["minor"].Value);
            Revision = Convert.ToInt32(m.Groups["revision"].Value);
        }

        //--- Methods ---
        public VersionDiff CompareTo(VersionInfo other) {
            if(!IsValid || !other.IsValid) {
                return Raw.CompareTo(other.Raw) == 0
                    ? new VersionDiff(VersionChange.None, VersionSeverity.None)
                    : new VersionDiff(VersionChange.Incompatible, VersionSeverity.Unknown);
            }
            var change = VersionChange.None;
            var severity = VersionSeverity.None;

            if(Major > other.Major) {
                change = VersionChange.Upgrade;
                severity = VersionSeverity.Major;
            } else if(Major < other.Major) {
                change = VersionChange.Downgrade;
                severity = VersionSeverity.Major;
            } else if(Minor > other.Minor) {
                change = VersionChange.Upgrade;
                severity = VersionSeverity.Minor;
            } else if(Minor < other.Minor) {
                change = VersionChange.Downgrade;
                severity = VersionSeverity.Minor;
            } else if(Revision > other.Revision) {
                change = VersionChange.Upgrade;
                severity = VersionSeverity.Revision;
            } else if(Revision < other.Revision) {
                change = VersionChange.Downgrade;
                severity = VersionSeverity.Revision;
            }
            return new VersionDiff(change, severity);
        }
    }

    public struct VersionDiff {

        //--- Fields ---
        public readonly VersionChange Change;
        public readonly VersionSeverity Severity;

        //--- Constructors ----
        public VersionDiff(VersionChange change, VersionSeverity severity) {
            Change = change;
            Severity = severity;
        }
    }

    public enum VersionChange {
        None,
        Upgrade,
        Downgrade,
        Incompatible
    }

    public enum VersionSeverity {
        None = 0,
        Revision = 1,
        Minor = 2,
        Major = 3,
        Unknown = 4
    }
}
