/*
 * MindTouch
 * Copyright (c) 2006-2010 MindTouch Inc.
 * http://mindtouch.com
 *
 * This file and accompanying files are licensed under the 
 * MindTouch Enterprise Master Subscription Agreement (MSA).
 *
 * At any time, you shall not, directly or indirectly: (i) sublicense,
 * resell, rent, lease, distribute, market, commercialize or otherwise
 * transfer rights or usage to: (a) the Software, (b) any modified version
 * or derivative work of the Software created by you or for you, or (c)
 * MindTouch Open Source (which includes all non-supported versions of
 * MindTouch-developed software), for any purpose including timesharing or
 * service bureau purposes; (ii) remove or alter any copyright, trademark
 * or proprietary notice in the Software; (iii) transfer, use or export the
 * Software in violation of any applicable laws or regulations of any
 * government or governmental agency; (iv) use or run on any of your
 * hardware, or have deployed for use, any production version of MindTouch
 * Open Source; (v) use any of the Support Services, Error corrections,
 * Updates or Upgrades, for the MindTouch Open Source software or for any
 * Server for which Support Services are not then purchased as provided
 * hereunder; or (vi) reverse engineer, decompile or modify any encrypted
 * or encoded portion of the Software.
 * 
 * A complete copy of the MSA is available at http://www.mindtouch.com/msa
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