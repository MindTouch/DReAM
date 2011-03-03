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

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace System {

    /// <summary>
    /// Implementation of <see cref="IComparer"/> based on <see cref="CultureInfo.CompareInfo"/>.
    /// </summary>
    public class CultureComparer : IComparer {

        //--- Fields ---
        private CompareInfo _cultureCompare;

        //--- Constructors ---

        /// <summary>
        /// Create comparer based on culture.
        /// </summary>
        /// <param name="culture">Culture to get <see cref="CompareInfo"/> from.</param>
        public CultureComparer(CultureInfo culture) {
            if(culture == null) {
                throw new ArgumentNullException("culture");
            }
            _cultureCompare = culture.CompareInfo;
        }

        //--- Methods ---

        /// <summary>
        /// See <see cref="IComparer.Compare"/>.
        /// </summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The seccond object to compare. </param>
        /// <returns>Value Condition Less than zero x is less than y. Zero x equals y. Greater than zero x is greater than y.</returns>
        public int Compare(object x, object y) {
            return _cultureCompare.Compare((string)x, (string)y);
        }
    }

    /// <summary>
    /// Static utility class containing extension and helper methods for working with strings.
    /// </summary>
    public static class StringUtil {

        //--- Class Fields ---

        /// <summary>
        /// An empty string array. Array counterpart to <see cref="string.Empty"/>.
        /// </summary>
        public static readonly string[] EmptyArray = new string[0];

        private static char[] _alphanum_chars;
        private static RNGCryptoServiceProvider _generator = new RNGCryptoServiceProvider();
        private static Dictionary<string, Sgml.Entity> _literals;
        private static Dictionary<string, string> _entities;
        private static Regex _specialSymbolRegEx = new Regex("[&<>\x22\u0080-\uFFFF]", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static Regex _htmlEntitiesRegEx = new Regex("&(?<value>#(x[a-f0-9]+|[0-9]+)|[a-z0-9]+);", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        //--- Class Constructor ---
        static StringUtil() {
            List<char> chars = new List<char>(128);
            for(char i = '0'; i <= '9'; ++i) {
                chars.Add(i);
            }
            for(char i = 'A'; i < 'Z'; ++i) {
                chars.Add(i);
            }
            for(char i = 'a'; i < 'z'; ++i) {
                chars.Add(i);
            }
            _alphanum_chars = chars.ToArray();
        }

        //--- Class Properties ---
        private static Dictionary<string, Sgml.Entity> LiteralNameLookup {
            get {
                if(_literals == null) {
                    Sgml.SgmlReader sgmlReader = new Sgml.SgmlReader();
                    sgmlReader.DocType = "HTML";
                    _literals = sgmlReader.Dtd.GetEntitiesLiteralNameLookup();
                }
                return _literals;
            }
        }

        private static Dictionary<string, string> EntityNameLookup {
            get {
                if(_entities == null) {
                    Dictionary<string, string> result = new Dictionary<string, string>();
                    foreach(KeyValuePair<string, Sgml.Entity> entry in LiteralNameLookup) {
                        result[entry.Value.Name] = entry.Key;
                    }
                    _entities = result;
                }
                return _entities;
            }
        }

        //--- Extension Methods ---

        /// <summary>
        /// Replace all occurences of a number of strings.
        /// </summary>
        /// <param name="source">Source string.</param>
        /// <param name="replacements">Array of strings to match and their replacements. Each string to be replaced at odd index i must have a replacement value at index i+1.</param>
        /// <returns>String with replacements performed on it.</returns>
        public static string ReplaceAll(this string source, params string[] replacements) {
            if((replacements.Length & 1) != 0) {
                throw new ArgumentException("length of string replacements must be even", "replacements");
            }
            if(string.IsNullOrEmpty(source) || (replacements.Length == 0)) {
                return source;
            }

            // loop over each character in source string
            StringBuilder result = null;
            int currentIndex = 0;
            int lastIndex = 0;
            for(; currentIndex < source.Length; ++currentIndex) {
                
                // loop over all replacement strings
                for(int j = 0; j < replacements.Length; j += 2) {

                    // check if we found a matching replacement at the current position
                    if(string.CompareOrdinal(source, currentIndex, replacements[j], 0, replacements[j].Length) == 0) {
                        result = result ?? new StringBuilder(source.Length * 2);

                        // append any text we've skipped over so far
                        if(lastIndex < currentIndex) {
                            result.Append(source, lastIndex, currentIndex - lastIndex);
                        }

                        // append replacement
                        result.Append(replacements[j + 1]);
                        currentIndex += replacements[j].Length - 1;
                        lastIndex = currentIndex + 1;
                        goto next;
                    }
                }
            next:
                continue;
            }

            // append any text we've skipped over so far
            if(lastIndex < currentIndex) {

                // check if nothing has been replaced; in that case, return the original string
                if(lastIndex == 0) {
                    return source;
                }
                result = result ?? new StringBuilder(source.Length * 2);
                result.Append(source, lastIndex, currentIndex - lastIndex);
            }
            return result.ToString();
        }

        /// <summary>
        /// Replace all occurences of a number of strings.
        /// </summary>
        /// <param name="source">Source string.</param>
        /// <param name="comparison">Type of string comparison to use.</param>
        /// <param name="replacements">Array of strings to match and their replacements. Each string to be replaced at odd index i must have a replacement value at index i+1.</param>
        /// <returns>String with replacements performed on it.</returns>
        public static string ReplaceAll(this string source, StringComparison comparison, params string[] replacements) {
            if((replacements.Length & 1) != 0) {
                throw new ArgumentException("length of string replacements must be even", "replacements");
            }
            if(string.IsNullOrEmpty(source) || (replacements.Length == 0)) {
                return source;
            }

            // loop over each character
            StringBuilder result = null;
            int currentIndex = 0;
            int lastIndex = 0;
            for(; currentIndex < source.Length; ++currentIndex) {

                // loop over all replacement strings
                for(int j = 0; j < replacements.Length; j += 2) {

                    // check if we found a matching replacement at the current position
                    if(string.Compare(source, currentIndex, replacements[j], 0, replacements[j].Length, comparison) == 0) {
                        result = result ?? new StringBuilder(source.Length * 2);

                        // append any text we've skipped over so far
                        if(lastIndex < currentIndex) {
                            result.Append(source, lastIndex, currentIndex - lastIndex);
                        }

                        // append replacement
                        result.Append(replacements[j + 1]);
                        currentIndex += replacements[j].Length - 1;
                        lastIndex = currentIndex + 1;
                        goto next;
                    }
                }
            next:
                continue;
            }

            // append any text we've skipped over so far
            if(lastIndex < currentIndex) {

                // check if nothing has been replaced; in that case, return the original string
                if(lastIndex == 0) {
                    return source;
                }
                result = result ?? new StringBuilder(source.Length * 2);
                result.Append(source, lastIndex, currentIndex - lastIndex);
            }
            return result.ToString();
        }

        /// <summary>
        /// Encode any html entities in a string.
        /// </summary>
        /// <param name="text">String to encode.</param>
        /// <returns>Encoded string.</returns>
        public static string EncodeHtmlEntities(this string text) {
            return EncodeHtmlEntities(text, Encoding.UTF8, true);
        }

        /// <summary>
        /// Encode any html entities in a string.
        /// </summary>
        /// <param name="text">String to encode.</param>
        /// <param name="encoding">Text encoding to use.</param>
        /// <returns>Encoded string.</returns>
        public static string EncodeHtmlEntities(this string text, Encoding encoding) {
            return EncodeHtmlEntities(text, encoding, true);
        }

        /// <summary>
        /// Encode any html entities in a string.
        /// </summary>
        /// <param name="text">String to encode.</param>
        /// <param name="encoding">Text encoding to use.</param>
        /// <param name="useEntityNames">If <see langword="True"/>, encodes html entity using entity name rather than numeric entity code.</param>
        /// <returns>Encoded string.</returns>
        public static string EncodeHtmlEntities(this string text, Encoding encoding, bool useEntityNames) {
            return _specialSymbolRegEx.Replace(text, delegate(Match m) {
                string v = m.Groups[0].Value;
                switch(v) {
                case "&":
                    return "&amp;";
                case "<":
                    return "&lt;";
                case ">":
                    return "&gt;";
                case "\"":
                    return "&quot;";
                }

                // default case
                Sgml.Entity e;
                if(useEntityNames && LiteralNameLookup.TryGetValue(v, out e)) {
                    return "&" + e.Name + ";";
                }
                return (encoding == Encoding.ASCII) ? "&#" + (int)v[0] + ";" : v;
            }, int.MaxValue);
        }

        /// <summary>
        /// Decode Html entities.
        /// </summary>
        /// <param name="text">Html encoded string.</param>
        /// <returns>Decoded string.</returns>
        public static string DecodeHtmlEntities(this string text) {
            return _htmlEntitiesRegEx.Replace(text, delegate(Match m) {
                string v = m.Groups["value"].Value;
                if(v[0] == '#') {
                    if(char.ToLowerInvariant(v[1]) == 'x') {
                        string value = v.Substring(2);
                        return ((char)int.Parse(value, NumberStyles.HexNumber)).ToString();
                    } else {
                        string value = v.Substring(1);
                        return ((char)int.Parse(value)).ToString();
                    }
                } else {
                    string value;
                    if(EntityNameLookup.TryGetValue(v, out value)) {
                        return value;
                    }
                    return m.Groups[0].Value;
                }
            }, int.MaxValue);
        }

        /// <summary>
        /// Escape string.
        /// </summary>
        /// <param name="text">Sources string.</param>
        /// <returns>Escaped string.</returns>
        public static string EscapeString(this string text) {
            if(string.IsNullOrEmpty(text)) {
                return string.Empty;
            }

            // escape any special characters
            StringBuilder result = new StringBuilder(2 * text.Length);
            foreach(char c in text) {
                switch(c) {
                case '\a':
                    result.Append("\\a");
                    break;
                case '\b':
                    result.Append("\\b");
                    break;
                case '\f':
                    result.Append("\\f");
                    break;
                case '\n':
                    result.Append("\\n");
                    break;
                case '\r':
                    result.Append("\\r");
                    break;
                case '\t':
                    result.Append("\\t");
                    break;
                case '\v':
                    result.Append("\\v");
                    break;
                case '"':
                    result.Append("\\\"");
                    break;
                case '\'':
                    result.Append("\\'");
                    break;
                case '\\':
                    result.Append("\\\\");
                    break;
                default:
                    if((c < 32) || (c >= 127)) {
                        result.Append("\\u");
                        result.Append(((int)c).ToString("x4"));
                    } else {
                        result.Append(c);
                    }
                    break;
                }
            }
            return result.ToString();
        }

        /// <summary>
        /// Unescape string.
        /// </summary>
        /// <param name="text">Escaped string.</param>
        /// <returns>Unescaped string.</returns>
        public static string UnescapeString(this string text) {
            StringBuilder result = new StringBuilder(text.Length);
            for(int i = 0; i < text.Length; ++i) {
                char c = text[i];
                if((c == '\\') && (++i < text.Length)) {
                    switch(text[i]) {
                    case 'a':
                        result.Append('\a');
                        break;
                    case 'b':
                        result.Append("\b");
                        break;
                    case 'f':
                        result.Append('\f');
                        break;
                    case 'n':
                        result.Append("\n");
                        break;
                    case 'r':
                        result.Append("\r");
                        break;
                    case 't':
                        result.Append("\t");
                        break;
                    case 'u':
                        string code = text.Substring(i + 1, 4);
                        if(code.Length != 4) {
                            throw new FormatException("illegal \\u escape sequence");
                        }
                        result.Append((char)int.Parse(code, NumberStyles.AllowHexSpecifier));
                        i += 4;
                        break;
                    case 'v':
                        result.Append('\v');
                        break;
                    default:
                        result.Append(text[i]);
                        break;
                    }
                } else {
                    result.Append(c);
                }
            }
            return result.ToString();
        }

        /// <summary>
        /// Escape string and quote the results.
        /// </summary>
        /// <param name="text">Source string.</param>
        /// <returns>Quoted string.</returns>
        public static string QuoteString(this string text) {

            // escape any special characters
            return "\"" + text.EscapeString() + "\"";
        }

        /// <summary>
        /// Replace the contents between delimiters
        /// </summary>
        /// <param name="text">Source text.</param>
        /// <param name="startDelimiter">Delimiter demarking the beginning of the string to be replaced.</param>
        /// <param name="endDelimiter">Delimiter demarking the end of the string to replaced.</param>
        /// <param name="replace">The value to replace the value between the delimters with.</param>
        /// <param name="comparison">The type of string comparison to use to match delimiters.</param>
        /// <returns>String with delimited text replaced.</returns>
        public static string ReplaceWithinDelimiters(this string text, string startDelimiter, string endDelimiter, string replace, StringComparison comparison) {
            return ReplaceWithinDelimiters(text, startDelimiter, endDelimiter, delegate { return replace; }, comparison);
        }

        /// <summary>
        /// Replace the contents between delimiters
        /// </summary>
        /// <param name="text">Source text.</param>
        /// <param name="startDelimiter">Delimiter demarking the beginning of the string to be replaced.</param>
        /// <param name="endDelimiter">Delimiter demarking the end of the string to replaced.</param>
        /// <param name="callback">Callback for converting the text between delimiters into its replacement text.</param>
        /// <param name="comparison">The type of string comparison to use to match delimiters.</param>
        /// <returns>String with delimited text replaced.</returns>
        public static string ReplaceWithinDelimiters(this string text, string startDelimiter, string endDelimiter, Converter<string, string> callback, StringComparison comparison) {
            if(string.IsNullOrEmpty(startDelimiter)) {
                throw new ArgumentNullException("startDelimiter");
            }
            if(string.IsNullOrEmpty(endDelimiter)) {
                throw new ArgumentNullException("endDelimiter");
            }
            if(callback == null) {
                throw new ArgumentNullException("callback");
            }
            if(string.IsNullOrEmpty(text)) {
                return text;
            }
            StringComparer comparer;
            switch(comparison) {
            case StringComparison.CurrentCulture:
                comparer = StringComparer.CurrentCulture;
                break;
            case StringComparison.CurrentCultureIgnoreCase:
                comparer = StringComparer.CurrentCultureIgnoreCase;
                break;
            case StringComparison.InvariantCulture:
                comparer = StringComparer.InvariantCulture;
                break;
            case StringComparison.InvariantCultureIgnoreCase:
                comparer = StringComparer.InvariantCultureIgnoreCase;
                break;
            case StringComparison.Ordinal:
                comparer = StringComparer.Ordinal;
                break;
            case StringComparison.OrdinalIgnoreCase:
                comparer = StringComparer.OrdinalIgnoreCase;
                break;
            default:
                throw new ArgumentException("unknown comparison type", "comparison");
            }
            if(comparer.Equals(startDelimiter, endDelimiter)) {
                StringBuilder result = new StringBuilder();
                int start = 0;
                string delimiter = startDelimiter;
                while(true) {

                    // find first delimiter
                    int foundStart = text.IndexOf(delimiter, start, comparison);
                    if(foundStart < 0) {
                        break;
                    }

                    // find next delimiter
                    int foundEnd = text.IndexOf(delimiter, foundStart + delimiter.Length, comparison);
                    if(foundEnd < 0) {
                        break;
                    }

                    // append part of text we skipped over
                    result.Append(text, start, foundStart - start);

                    // replace contents between delimiters
                    result.Append(callback(text.Substring(foundStart + delimiter.Length, foundEnd - (foundStart + delimiter.Length))));

                    // continue after the found text
                    start = foundEnd + delimiter.Length;
                }
                if(start < text.Length) {
                    result.Append(text, start, text.Length - start);
                }
                return result.ToString();
            } else {
                while(true) {

                    // find end delimiter
                    int foundEnd = text.IndexOf(endDelimiter, comparison);
                    if(foundEnd <= 0) {
                        break;
                    }

                    // find start delimiter immediately preceding the end delimiter
                    int foundStart = text.LastIndexOf(startDelimiter, foundEnd - 1, comparison);
                    if(foundStart < 0) {
                        break;
                    }

                    // replace contents between delimiters
                    StringBuilder buffer = new StringBuilder();
                    buffer.Append(text, 0, foundStart);
                    buffer.Append(callback(text.Substring(foundStart + startDelimiter.Length, foundEnd - (foundStart + startDelimiter.Length))));
                    buffer.Append(text, foundEnd + endDelimiter.Length, text.Length - (foundEnd + endDelimiter.Length));
                    text = buffer.ToString();
                }
            }
            return text;
        }

        /// <summary>
        /// Create a string by repeating a pattern.
        /// </summary>
        /// <param name="pattern">Pattern to repeat.</param>
        /// <param name="count">Repetitions of pattern.</param>
        /// <returns>Pattern string.</returns>
        public static string RepeatPattern(this string pattern, int count) {
            if(pattern == null) {
                throw new ArgumentNullException("pattern");
            }
            StringBuilder result = new StringBuilder();
            for(int i = 0; i < count; ++i) {
                result.Append(pattern);
            }
            return result.ToString();
        }

        /// <summary>
        /// Alternative implementation of <see cref="object.GetHashCode"/>.
        /// </summary>
        /// <param name="token">Token to hash.</param>
        /// <returns>Numeric hashcode.</returns>
        public static int GetAlternativeHashCode(this string token) {
            unchecked {
                int result = 0;
                for(int i = 0; i < token.Length; ++i) {
                    result = (result << 5) - result + token[i];

                }
                return result + token.Length;
            }
        }

         /// <summary>
        /// Alternative implementation of <see cref="object.GetHashCode"/>.
        /// </summary>
        /// <param name="chars">Characters sequence to hash.</param>
        /// <param name="offset">Offset into sequence to start hash consideration.</param>
        /// <param name="length">Number of characters to consider for hash.</param>
        /// <returns>Numeric hashcode.</returns>
        public static int GetAlternativeHashCode(this char[] chars, int offset, int length) {
            unchecked {
                int result = 0;
                for(int i = 0; i < length; ++i) {
                    result = (result << 5) - result + chars[offset + i];

                }
                return result + length;
            }
        }

        /// <summary>
        /// Shortcut for invariant <see cref="string.Equals(string)"/>
        /// </summary>
        /// <param name="left">Left-hand string to compare.</param>
        /// <param name="right">Right-hand string to compare.</param>
        /// <param name="ignoreCase"><see langword="True"/> if case should not be considered in comparison.</param>
        /// <returns><see langword="True"/> if left- and right-hand sides are equal.</returns>
        public static bool EqualsInvariant(this string left, string right, bool ignoreCase) {
            return string.Equals(left, right, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }

        /// <summary>
        /// Shortcut for invariant <see cref="string.Equals(string)"/>
        /// </summary>
        /// <param name="left">Left-hand string to compare.</param>
        /// <param name="right">Right-hand string to compare.</param>
        /// <returns><see langword="True"/> if left- and right-hand sides are equal.</returns>
        public static bool EqualsInvariant(this string left, string right) {
            return string.Equals(left, right, StringComparison.Ordinal);
        }

        /// <summary>
        /// Shortcut for case-insensitive, invariant <see cref="string.Equals(string)"/>
        /// </summary>
        /// <param name="left">Left-hand string to compare.</param>
        /// <param name="right">Right-hand string to compare.</param>
        /// <returns><see langword="True"/> if left- and right-hand sides are equal.</returns>
        public static bool EqualsInvariantIgnoreCase(this string left, string right) {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Shortcut for invariant <see cref="string.Compare(string,string)"/>
        /// </summary>
        /// <param name="left">Left-hand string to compare.</param>
        /// <param name="right">Right-hand string to compare.</param>
        /// <param name="ignoreCase"><see langword="True"/> if case should not be considered in comparison.</param>
        /// <returns>
        /// A 32-bit signed integer indicating the lexical relationship between the two comparands.  Value Condition Less than zero 
        /// left is less than right. Zero left equals right. Greater than zero left is greater than right.
        /// </returns>
        public static int CompareInvariant(this string left, string right, bool ignoreCase) {
            return string.Compare(left, right, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }

        /// <summary>
        /// Shortcut for invariant <see cref="string.Compare(string,string)"/>
        /// </summary>
        /// <param name="left">Left-hand string to compare.</param>
        /// <param name="right">Right-hand string to compare.</param>
        /// <returns>
        /// A 32-bit signed integer indicating the lexical relationship between the two comparands.  Value Condition Less than zero 
        /// left is less than right. Zero left equals right. Greater than zero left is greater than right.
        /// </returns>
        public static int CompareInvariant(this string left, string right) {
            return string.Compare(left, right, StringComparison.Ordinal);
        }

        /// <summary>
        /// Shortcut for case-insensitive, invariant <see cref="string.Compare(string,string)"/>
        /// </summary>
        /// <param name="left">Left-hand string to compare.</param>
        /// <param name="right">Right-hand string to compare.</param>
        /// <returns>
        /// A 32-bit signed integer indicating the lexical relationship between the two comparands.  Value Condition Less than zero 
        /// left is less than right. Zero left equals right. Greater than zero left is greater than right.
        /// </returns>
        public static int CompareInvariantIgnoreCase(this string left, string right) {
            return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Shortcut for invariant <see cref="string.StartsWith(string)"/>
        /// </summary>
        /// <param name="text">Text to examine</param>
        /// <param name="value">The System.String to compare.</param>
        /// <param name="ignoreCase"><see langword="True"/> if case should not be considered in comparison.</param>
        /// <returns><see langword="True"/> if value matches the beginning of the input string; otherwise, <see langword="False"/>.</returns>
        public static bool StartsWithInvariant(this string text, string value, bool ignoreCase) {
            return text.StartsWith(value, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }

        /// <summary>
        /// Shortcut for invariant <see cref="string.StartsWith(string)"/>
        /// </summary>
        /// <param name="text">Text to examine</param>
        /// <param name="value">The System.String to compare.</param>
        /// <returns><see langword="True"/> if value matches the beginning of the input string; otherwise, <see langword="False"/>.</returns>
        public static bool StartsWithInvariant(this string text, string value) {
            return text.StartsWith(value, StringComparison.Ordinal);
        }

        /// <summary>
        /// Shortcut for case-insensitive, invariant <see cref="string.StartsWith(string)"/>
        /// </summary>
        /// <param name="text">Text to examine</param>
        /// <param name="value">The System.String to compare.</param>
        /// <returns><see langword="True"/> if value matches the beginning of the input string; otherwise, <see langword="False"/>.</returns>
        public static bool StartsWithInvariantIgnoreCase(this string text, string value) {
            return text.StartsWith(value, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Shortcut for invariant <see cref="string.EndsWith(string)"/>
        /// </summary>
        /// <param name="text">Text to examine</param>
        /// <param name="value">The System.String to compare.</param>
        /// <param name="ignoreCase"><see langword="True"/> if case should not be considered in comparison.</param>
        /// <returns><see langword="True"/> if value matches the end of the input string; otherwise, <see langword="False"/>.</returns>
        public static bool EndsWithInvariant(this string text, string value, bool ignoreCase) {
            return text.EndsWith(value, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }

        /// <summary>
        /// Shortcut for invariant <see cref="string.EndsWith(string)"/>
        /// </summary>
        /// <param name="text">Text to examine</param>
        /// <param name="value">The System.String to compare.</param>
        /// <returns><see langword="True"/> if value matches the end of the input string; otherwise, <see langword="False"/>.</returns>
        public static bool EndsWithInvariant(this string text, string value) {
            return text.EndsWith(value, StringComparison.Ordinal);
        }

        /// <summary>
        /// Shortcut for case-insensitive, invariant <see cref="string.EndsWith(string)"/>
        /// </summary>
        /// <param name="text">Text to examine</param>
        /// <param name="value">The System.String to compare.</param>
        /// <returns><see langword="True"/> if value matches the end of the input string; otherwise, <see langword="False"/>.</returns>
        public static bool EndsWithInvariantIgnoreCase(this string text, string value) {
            return text.EndsWith(value, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Shortcut for invariant <see cref="string.IndexOf(string)"/>
        /// </summary>
        /// <param name="text">Text to examine</param>
        /// <param name="value">The System.String to find.</param>
        /// <param name="ignoreCase"><see langword="True"/> if case should not be considered in comparison.</param>
        /// <returns>
        /// The zero-based index position of value if that string is found, or -1 if it is not. If value is <see cref="string.Empty"/>, the return value is 0.
        /// </returns>
        public static int IndexOfInvariant(this string text, string value, bool ignoreCase) {
            return text.IndexOf(value, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }

        /// <summary>
        /// Shortcut for invariant <see cref="string.IndexOf(string)"/>
        /// </summary>
        /// <param name="text">Text to examine</param>
        /// <param name="value">The System.String to find.</param>
        /// <returns>
        /// The zero-based index position of value if that string is found, or -1 if it is not. If value is <see cref="string.Empty"/>, the return value is 0.
        /// </returns>
        public static int IndexOfInvariant(this string text, string value) {
            return text.IndexOf(value, StringComparison.Ordinal);
        }

        /// <summary>
        /// Shortcut for case-insensitive, invariant <see cref="string.IndexOf(string)"/>
        /// </summary>
        /// <param name="text">Text to examine</param>
        /// <param name="value">The System.String to find.</param>
        /// <returns>
        /// The zero-based index position of value if that string is found, or -1 if it is not. If value is <see cref="string.Empty"/>, the return value is 0.
        /// </returns>
        public static int IndexOfInvariantIgnoreCase(this string text, string value) {
            return text.IndexOf(value, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Shortcut for invariant <see cref="string.LastIndexOf(string)"/>
        /// </summary>
        /// <param name="text">Text to examine</param>
        /// <param name="value">The System.String to find.</param>
        /// <param name="ignoreCase"><see langword="True"/> if case should not be considered in comparison.</param>
        /// <returns>
        /// The index position of the value parameter if that string is found, or -1 if it is not. If value is <see cref="string.Empty"/>, the return value is the last index position in this instance.
        /// </returns>
        public static int LastIndexOfInvariant(this string text, string value, bool ignoreCase) {
            return text.LastIndexOf(value, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }

        /// <summary>
        /// Shortcut for invariant <see cref="string.LastIndexOf(string)"/>
        /// </summary>
        /// <param name="text">Text to examine</param>
        /// <param name="value">The System.String to find.</param>
        /// <returns>
        /// The index position of the value parameter if that string is found, or -1 if it is not. If value is <see cref="string.Empty"/>, the return value is the last index position in this instance.
        /// </returns>
        public static int LastIndexOfInvariant(this string text, string value) {
            return text.LastIndexOf(value, StringComparison.Ordinal);
        }

        /// <summary>
        /// Shortcut for case-insensitive, invariant <see cref="string.LastIndexOf(string)"/>
        /// </summary>
        /// <param name="text">Text to examine</param>
        /// <param name="value">The System.String to find.</param>
        /// <returns>
        /// The index position of the value parameter if that string is found, or -1 if it is not. If value is <see cref="string.Empty"/>, the return value is the last index position in this instance.
        /// </returns>
        public static int LastIndexOfInvariantIgnoreCase(this string text, string value) {
            return text.LastIndexOf(value, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determine whether a string is contained in another string using invariant comparison.
        /// </summary>
        /// <param name="text">Text to examine</param>
        /// <param name="value">The System.String to find.</param>
        /// <returns><see langword="True"/> if value is found in the input string; otherwise, <see langword="False"/>.</returns>
        public static bool ContainsInvariant(this string text, string value) {
            return IndexOfInvariant(text, value) >= 0;
        }

        /// <summary>
        /// Determine whether a string is contained in another string using case-insensitive, invariant comparison.
        /// </summary>
        /// <param name="text">Text to examine</param>
        /// <param name="value">The System.String to find.</param>
        /// <returns><see langword="True"/> if value is found in the input string; otherwise, <see langword="False"/>.</returns>
        public static bool ContainsInvariantIgnoreCase(this string text, string value) {
            return IndexOfInvariantIgnoreCase(text, value) >= 0;
        }

        /// <summary>
        /// Get Hashcode for a string using the invariant comparer.
        /// </summary>
        /// <param name="text">Text to examine.</param>
        /// <param name="ignoreCase"><see langword="True"/> if case should not be considered in comparison.</param>
        /// <returns>Hashcode for the input string.</returns>
        public static int GetHashCodeInvariant(this string text, bool ignoreCase) {
            return (ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal).GetHashCode(text);
        }

        /// <summary>
        /// Get Hashcode for a string using the invariant comparer.
        /// </summary>
        /// <param name="text">Text to examine.</param>
        /// <returns>Hashcode for the input string.</returns>
        public static int GetHashCodeInvariant(this string text) {
            return StringComparer.Ordinal.GetHashCode(text);
        }

        /// <summary>
        /// Get Hashcode for a string using the case-insensitive, invariant comparer.
        /// </summary>
        /// <param name="text">Text to examine.</param>
        /// <returns>Hashcode for the input string.</returns>
        public static int GetHashCodeInvariantIgnoreCase(this string text) {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(text);
        }

        //--- Class Methods ---

        /// <summary>
        /// Compute a random alphanumeric value.
        /// </summary>
        /// <param name="length">Length of value to compute.</param>
        /// <returns>A random alphanumeric key value.</returns>
        public static string CreateAlphaNumericKey(int length) {
            if(length <= 0) {
                throw new ArgumentException("length");
            }

            // loop until our result is filled
            int index = 0;
            char[] result = new char[length];
            byte[] buffer = new byte[64];
            while(index < result.Length) {

                // request a new set of random bytes
                _generator.GetNonZeroBytes(buffer);
                for(int i = 0; (i < buffer.Length) && (index < result.Length); ++i) {

                    // check if value is in range
                    if(buffer[i] <= _alphanum_chars.Length) {
                        result[index++] = _alphanum_chars[buffer[i] - 1];
                    }
                }
            }
            return new string(result);
        }

        /// <summary>
        /// Compute the MD5 hash.
        /// </summary>
        /// <param name="text">Text to compute hash for.</param>
        /// <returns>MD5 hash.</returns>
        public static byte[] ComputeHash(string text) {
            return ComputeHash(text, Encoding.Unicode);
        }

        /// <summary>
        /// Compute the MD5 hash.
        /// </summary>
        /// <param name="text">Text to compute hash for.</param>
        /// <param name="encoding">Encoding to use to get input string bytes.</param>
        /// <returns>MD5 hash.</returns>
        public static byte[] ComputeHash(string text, Encoding encoding) {
            return MD5.Create().ComputeHash(encoding.GetBytes(text));
        }

        /// <summary>
        /// Compute the MD5 hash.
        /// </summary>
        /// <param name="text">Text to compute hash for.</param>
        /// <returns>MD5 hash string.</returns>
        public static string ComputeHashString(string text) {
            return ComputeHashString(text, Encoding.Unicode);
        }

        /// <summary>
        /// Compute the MD5 hash.
        /// </summary>
        /// <param name="text">Text to compute hash for.</param>
        /// <param name="encoding">Encoding to use to get input string bytes.</param>
        /// <returns>MD5 hash string.</returns>
        public static string ComputeHashString(string text, Encoding encoding) {
            return HexStringFromBytes(ComputeHash(text, encoding));
        }

        /// <summary>
        /// Convert bytes into a hex string.
        /// </summary>
        /// <param name="bytes">Input bytes.</param>
        /// <returns>Sequence of hexadecimal values for input byte array.</returns>
        public static string HexStringFromBytes(byte[] bytes) {
            char[] chars = new char[2 * bytes.Length];
            for(int i = 0, j = 0; i < bytes.Length; ++i) {
                byte value = bytes[i];
                byte low = (byte)(value & 0xF);
                byte high = (byte)(value >> 4);
                chars[j++] = (char)((high >= 10) ? ('a' + (high - 10)) : ('0' + high));
                chars[j++] = (char)((low >= 10) ? ('a' + (low - 10)) : ('0' + low));
            }
            return new string(chars);
        }

        /// <summary>
        /// Convert a hex string to a bytes.
        /// </summary>
        /// <param name="text">A string containing a sequence of hexadecimal values.</param>
        /// <returns>Byte array.</returns>
        public static byte[] BytesFromHexString(string text) {
            if((text == null) || (text.Length % 2 != 0)) {
                throw new ArgumentException("invalid length for a hex string", "text");
            }
            byte[] result = new byte[text.Length / 2];
            for(int i = 0; i < text.Length; i += 2) {

                // decode upper nibble
                int high = char.ToLowerInvariant(text[i]);
                if((high >= 'a') && (high <= 'f')) {
                    high = high - 'a' + 10;
                } else if((high >= '0') && (high <= '9')) {
                    high = high - '0';
                } else {
                    throw new ArgumentException("hex string contains invalid value", "text");
                }

                // decode lower nibble
                int low = char.ToLowerInvariant(text[i + 1]);
                if((low >= 'a') && (low <= 'f')) {
                    low = low - 'a' + 10;
                } else if((low >= '0') && (low <= '9')) {
                    low = low - '0';
                } else {
                    throw new ArgumentException("hex string contains invalid value", "text");
                }

                // reconstitute byte
                result[i >> 1] = (byte)(high * 16 + low);
            }
            return result;
        }

        /// <summary>
        /// Convert an integer value to a hexadecimal value.
        /// </summary>
        /// <param name="n">Integer to convert.</param>
        /// <returns>Hexadecimal character.</returns>
        public static char IntToHexChar(int n) {
            if(n <= 9) {
                return (char)(n + 0x30);
            }
            return (char)((n - 10) + 0x61);
        }

        /// <summary>
        /// Check if string is null or empty.  If so, return the alternative value.
        /// </summary>
        /// <param name="value">String to check.</param>
        /// <param name="alternative">String to return if first string is null or empty.</param>
        /// <returns></returns>
        public static string IfNullOrEmpty(this string value, string alternative) {
            return string.IsNullOrEmpty(value) ? alternative : value;
        }
    }
}
