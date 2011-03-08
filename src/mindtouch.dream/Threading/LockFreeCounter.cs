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

using System.Threading;

namespace MindTouch.Threading {

    /// <summary>
    /// Provides a thread-safe counter with out lock overhead.
    /// </summary>
	public class LockFreeCounter {
	
		//--- Fields ---
		private int _value;
		
		//--- Constructors ---

        /// <summary>
        /// Create a new counter with an initial value.
        /// </summary>
        /// <param name="value">Initial value.</param>
        public LockFreeCounter(int value) {
			_value = value;
		}
		
		//--- Properties ---

        /// <summary>
        /// Current counter value.
        /// </summary>
		public int Value { get { return _value; } }
		
		//--- Methods ---

        /// <summary>
        /// Increment the counter by one.
        /// </summary>
        /// <returns>The counter value after increment.</returns>
		public int Increment() {
			return Interlocked.Increment(ref _value);
		}
		
        /// <summary>
        /// Decrement the counter by one.
        /// </summary>
        /// <returns>The counter value after decrement.</returns>
		public int Decrement() {
			return Interlocked.Decrement(ref _value);
		}

        /// <summary>
        /// Add a value to the counter.
        /// </summary>
        /// <param name="value">Value to add.</param>
        /// <returns>The counter value after the add.</returns>
        public int Add(int value) {
            return Interlocked.Add(ref _value, value);
        }

        /// <summary>
        /// Set the counter to a new value.
        /// </summary>
        /// <param name="value">The value to set the counter to.</param>
        /// <returns>The value of the counter before the new value was set.</returns>
        public int Exchange(int value) {
            return Interlocked.Exchange(ref _value, value);
        }

        /// <summary>
        /// Compares the current value and a comparand for equality and, if they are equal, sets the counter to the new value.
        /// </summary>
        /// <param name="value">The value to try and set the counter to.</param>
        /// <param name="comparand">The expected current value of the counter.</param>
        /// <returns>The original value of the counter. If this value is different from the comparand, the set failed.</returns>
        public int CompareAndSwap(int value, int comparand) {
            return Interlocked.CompareExchange(ref _value, value, comparand);
        }
	}
}
