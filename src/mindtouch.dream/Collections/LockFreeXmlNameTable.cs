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
using System.Threading;
using System.Xml;

namespace MindTouch.Collections {

    /// <summary>
    /// Provides an implementation of <see cref="XmlNameTable"/> that does not incur lock contention for access to its members.
    /// </summary>
    public class LockFreeXmlNameTable : XmlNameTable {

        //--- Constants ---
        private const int DEFAULT_TABLE_SIZE = 0x3FFF + 1; // 2^14

        //--- Types ---
        private struct Entry {

            //--- Fields ---
            public readonly int HashCode;
            public readonly string Token;

            //--- Constructors ---
            public Entry(int hashcode, string token) {
                this.HashCode = hashcode;
                this.Token = token;
            }
        }

        //--- Class Methods ---
        private static int CompareStringToCharArray(string token, char[] chars, int offset, int length) {
            int delta = token.Length - length;
            for(int i = 0; (delta == 0) && (i < length); ++i) {
                delta = token[i] - chars[offset + i];
            }
            return delta;
        }

        private static int CompareStringToString(string token, string other) {
            int delta = token.Length - other.Length;
            for(int i = 0; (delta == 0) && (i < other.Length); ++i) {
                delta = token[i] - other[i];
            }
            return delta;
        }

        //--- Fields ---
        private readonly SingleLinkNode<Entry>[] _buckets;
        private readonly bool _capacityIsPowerOf2;

        //--- Constructors ---

        /// <summary>
        /// Create a new instance.
        /// </summary>
        public LockFreeXmlNameTable() : this(DEFAULT_TABLE_SIZE) { }

        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="capacity">Number of hash buckets to use for the name table.</param>
        public LockFreeXmlNameTable(int capacity) {
            if(capacity <= 0) {
                throw new ArgumentException("capacity must be positive number", "capacity");
            }
            _buckets = new SingleLinkNode<Entry>[capacity];
            _capacityIsPowerOf2 = ((capacity & (capacity - 1)) == 0);
        }

        //--- Methods ---

        /// <summary>
        /// Get a string instance that matches a character sequence, if it exists.
        /// </summary>
        /// <param name="chars">Source character array.</param>
        /// <param name="offset">Offset in chars.</param>
        /// <param name="length">Length of sub-array from chars.</param>
        /// <returns>String instance for character sequence or null, if character sequence was not found.</returns>
        public override string Get(char[] chars, int offset, int length) {

            // check for the empty string and always return the built-in constant for it
            if(length == 0) {
                return string.Empty;
            }

            // locate entry based on hashcode of the supplied token
            int hashcode = chars.GetAlternativeHashCode(offset, length);
            int index = GetIndex(hashcode);
            SingleLinkNode<Entry> current = _buckets[index];

            // check if we're looking for a short string (in that case, we skip the hashcode check)
            if(length < 12) {

                // loop over all nodes until we exhaust them or find a match
                for(; current != null; current = current.Next) {

                    // do only an ordinal string comparison since the token is short
                    if(CompareStringToCharArray(current.Item.Token, chars, offset, length) == 0) {
                        return current.Item.Token;
                    }
                }
            } else {

                // loop over all nodes until we exhaust them or find a match
                for(; current != null; current = current.Next) {

                    // do only an ordinal string comparison since the token is short
                    if((current.Item.HashCode == hashcode) && (CompareStringToCharArray(current.Item.Token, chars, offset, length) == 0)) {
                        return current.Item.Token;
                    }
                }
            }

            // token was not found
            return null;
        }

        /// <summary>
        /// Get a string instance from the table for the provided token, if it exists
        /// </summary>
        /// <param name="token">Token to look up or store in the name table</param>
        /// <returns>String instance for token or null, if the token was not found.</returns>
        public override string Get(string token) {

            // check for the empty string and always return the built-in constant for it
            if(token.Length == 0) {
                return string.Empty;
            }

            // locate entry based on hashcode of the supplied token
            int hashcode = token.GetAlternativeHashCode();
            int index = GetIndex(hashcode);
            SingleLinkNode<Entry> current = _buckets[index];

            // check if we're looking for a short string (in that case, we skip the hashcode check)
            if(token.Length < 12) {

                // loop over all nodes until we exhaust them or find a match
                for(; current != null; current = current.Next) {

                    // do only an ordinal string comparison since the token is short
                    if(CompareStringToString(current.Item.Token, token) == 0) {
                        return current.Item.Token;
                    }
                }
            } else {

                // loop over all nodes until we exhaust them or find a match
                for(; current != null; current = current.Next) {

                    // do only an ordinal string comparison since the token is short
                    if((current.Item.HashCode == hashcode) && (CompareStringToString(current.Item.Token, token) == 0)) {
                        return current.Item.Token;
                    }
                }
            }

            // token was not found
            return null;
        }

        /// <summary>
        /// Add character sequence to nametable and get the string instance it represents.
        /// </summary>
        /// <remarks>
        /// If the sequence already exists, the existing string instance is returned.
        /// </remarks>
        /// <param name="chars">Source character array.</param>
        /// <param name="offset">Offset in chars.</param>
        /// <param name="length">Length of sub-array from chars.</param>
        /// <returns>String instance for character sequence.</returns>
        public override string Add(char[] chars, int offset, int length) {

            // check for the empty string and always return the built-in constant for it
            if(length == 0) {
                return string.Empty;
            }

            // locate entry based on hashcode of the supplied token
            int hashcode = chars.GetAlternativeHashCode(offset, length);
            int index = GetIndex(hashcode);
            SingleLinkNode<Entry> current = _buckets[index];
            SingleLinkNode<Entry> entry = null;

            // check if a head node exists for the given hashcode
            if(current == null) {
                entry = new SingleLinkNode<Entry>(new Entry(hashcode, new string(chars, offset, length)));

                // try to update the head node with the new entry
                current = Interlocked.CompareExchange(ref _buckets[index], entry, null);

                // check if we succeeded, which means the provided token is now the reference token
                if(current == null) {
                    return entry.Item.Token;
                }

                // otherwise, continue on since 'current' now has the updated head node
            }

            // loop until we successfully find or append the token
            SingleLinkNode<Entry> previous = null;
            while(true) {

                // check if we're looking for a short string (in that case, we skip the hashcode check)
                if(length < 12) {

                    // loop over all entries until we exhaust them or find a match
                    for(; current != null; current = current.Next) {

                        // do only an ordinal string comparison since the token is short
                        if(CompareStringToCharArray(current.Item.Token, chars, offset, length) == 0) {
                            return current.Item.Token;
                        }
                        previous = current;
                    }
                } else {

                    // loop over all entries until we exhaust them or find a match
                    for(; current != null; current = current.Next) {

                        // do only an ordinal string comparison since the token is short
                        if((current.Item.HashCode == hashcode) && (CompareStringToCharArray(current.Item.Token, chars, offset, length) == 0)) {
                            return current.Item.Token;
                        }
                        previous = current;
                    }
                }

                // NOTE: it's possible that an earlier attempt already initialized 'entry'
                entry = entry ?? new SingleLinkNode<Entry>(new Entry(hashcode, new string(chars, offset, length)));

                // try to update the previous node with the new entry
                current = Interlocked.CompareExchange(ref previous.Next, entry, null);

                // check if we succeeded, which means the provided token is now the reference token
                if(current == null) {

                    // provided token was added
                    return entry.Item.Token;
                }

                // otherwise, continue on since 'current' now has the updated next node
            }
        }

        /// <summary>
        /// Add a token to the name table and get the common string instance it represents.
        /// </summary>
        /// <remarks>
        /// If the token already exists in the name table, the existing instance is returned.
        /// </remarks>
        /// <param name="token">The token to add.</param>
        /// <returns>String instance for the token.</returns>
        public override string Add(string token) {

            // check for the empty string and always return the built-in constant for it
            if(token.Length == 0) {
                return string.Empty;
            }

            // locate entry based on hashcode of the supplied token
            int hashcode = token.GetAlternativeHashCode();
            int index = GetIndex(hashcode);
            SingleLinkNode<Entry> current = _buckets[index];
            SingleLinkNode<Entry> entry = null;

            // check if a head node exists for the given hashcode
            if(current == null) {
                entry = new SingleLinkNode<Entry>(new Entry(hashcode, token));

                // try to update the head node with the new entry
                current = Interlocked.CompareExchange(ref _buckets[index], entry, null);

                // check if we succeeded, which means the provided token is now the reference token
                if(current == null) {
                    return token;
                }

                // otherwise, continue on since 'current' now has the updated head node
            }

            // loop until we successfully find or append the token
            SingleLinkNode<Entry> previous = null;
            while(true) {

                // check if we're looking for a short string (in that case, we skip the hashcode check)
                if(token.Length < 12) {

                    // loop over all entries until we exhaust them or find a match
                    for(; current != null; current = current.Next) {

                        // do only an ordinal string comparison since the token is short
                        if(CompareStringToString(current.Item.Token, token) == 0) {
                            return current.Item.Token;
                        }
                        previous = current;
                    }
                } else {

                    // loop over all entries until we exhaust them or find a match
                    for(; current != null; current = current.Next) {

                        // do only an ordinal string comparison since the token is short
                        if((current.Item.HashCode == hashcode) && (CompareStringToString(current.Item.Token, token) == 0)) {
                            return current.Item.Token;
                        }
                        previous = current;
                    }
                }

                // NOTE: it's possible that an earlier attempt already initialized 'entry'
                entry = entry ?? new SingleLinkNode<Entry>(new Entry(hashcode, token));

                // try to update the previous node with the new entry
                current = Interlocked.CompareExchange(ref previous.Next, entry, null);

                // check if we succeeded, which means the provided token is now the reference token
                if(current == null) {

                    // provided token was added
                    return token;
                }

                // otherwise, continue on since 'current' now has the updated next node
            }
        }

        /// <summary>
        /// Get statistics for the name table.
        /// </summary>
        /// <param name="capacity">Number of buckets used by the name table.</param>
        /// <param name="entries">Number of entries stored in the name table.</param>
        /// <param name="bytes">Number of bytes used by the name table storage.</param>
        /// <param name="distribution">Distribution of hash keys in buckets.</param>
        /// <param name="expectedComparisonsPerLookup">Expected number of comparisons to retrieve a value.</param>
        public void GetStats(out int capacity, out int entries, out long bytes, out int[] distribution, out double expectedComparisonsPerLookup) {
            capacity = _buckets.Length;
            entries = 0;
            bytes = 0;

            // loop over all entries
            var maxDepth = 0;
            var depthFrequency = new Dictionary<int, int>();
            for(int i = 0; i < capacity; ++i) {

                // count how deep this entry goes
                int depth = 0;
                for(var entry = _buckets[i]; entry != null; entry = entry.Next) {

                    // increase depth, entries, and bytes counted
                    ++depth;
                    ++entries;
                    bytes += entry.Item.Token.Length * sizeof(char);
                }

                // record depth
                int previous;
                depthFrequency.TryGetValue(depth, out previous);
                depthFrequency[depth] = previous + 1;

                // update the distribution data structure
                maxDepth = Math.Max(maxDepth, depth);
            }

            // convert dictionary into an array, and compute expected number of comparisons per lookup
            expectedComparisonsPerLookup = 0.0;
            distribution = new int[maxDepth];
            for(int i = maxDepth; i > 0; --i) {

                // number of entries at current depth is equal to those counted, plus all those at the next depth level
                int count;
                depthFrequency.TryGetValue(i, out count);
                if(i < maxDepth) {
                    count += distribution[i];
                }
                expectedComparisonsPerLookup += i * count;
                distribution[i - 1] = count;
            }
            expectedComparisonsPerLookup /= entries;
        }

        /// <summary>
        /// Get all string instances stored in the name table.
        /// </summary>
        /// <returns></returns>
        public string[] GetEntries() {
            var result = new List<string>();
            for(int i = 0; i < _buckets.Length; ++i) {

                // count how deep this entry goes
                for(var entry = _buckets[i]; entry != null; entry = entry.Next) {
                    result.Add(entry.Item.Token);
                }
            }
            result.Sort();
            return result.ToArray();
        }

        /// <summary>
        /// Get a string instance for a spcific hash code.
        /// </summary>
        /// <param name="hashcode"></param>
        /// <returns></returns>
        private int GetIndex(int hashcode) {
            int result;
            int capacity = _buckets.Length;
            if(_capacityIsPowerOf2) {
                result = hashcode & (capacity - 1);
            } else {
                result = hashcode % capacity;
                if(result < 0) {
                    result += capacity;
                }
            }
            return result;
        }
    }
}
