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
using System.Threading;

namespace MindTouch.Threading {

    /// <summary>
    /// Provides extension methods to invoke lambdas in the context of a <see cref="ReaderWriterLockSlim"/> instance.
    /// </summary>
    public static class ReaderWriterLockSlimEx {

        /// <summary>
        /// Wrap's a lambda with an upgradeable read lock
        /// </summary>
        /// <typeparam name="T">Return Type of the lambda executed in the lock context.</typeparam>
        /// <param name="lockSlim">The lock to use for the lambda exectuion.</param>
        /// <param name="func">Lambda function to execute.</param>
        /// <returns>Return value of the lambda executed in the lock's context.</returns>
        public static T ExecuteWithUpgradeableReadLock<T>(this ReaderWriterLockSlim lockSlim, Func<T> func) {
            lockSlim.EnterUpgradeableReadLock();
            try {
                return func();
            } finally {
                lockSlim.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        /// Wrap's a lambda with an upgradeable lock
        /// </summary>
        /// <param name="lockSlim">The lock to use for the lambda exectuion.</param>
        /// <param name="action">Lambda action to execute.</param>
        public static void ExecuteWithUpgradeableReadLock(this ReaderWriterLockSlim lockSlim, Action action) {
            lockSlim.EnterUpgradeableReadLock();
            try {
                action();
            } finally {
                lockSlim.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        /// Wrap's a lambda with a read lock
        /// </summary>
        /// <typeparam name="T">Return Type of the lambda executed in the lock context.</typeparam>
        /// <param name="lockSlim">The lock to use for the lambda exectuion.</param>
        /// <param name="func">Lambda function to execute.</param>
        /// <returns>Return value of the lambda executed in the lock's context.</returns>
        public static T ExecuteWithReadLock<T>(this ReaderWriterLockSlim lockSlim, Func<T> func) {
            lockSlim.EnterReadLock();
            try {
                return func();
            } finally {
                lockSlim.ExitReadLock();
            }
        }

        /// <summary>
        /// Wrap's a lambda with a read lock
        /// </summary>
        /// <param name="lockSlim">The lock to use for the lambda exectuion.</param>
        /// <param name="action">Lambda action to execute.</param>
        public static void ExecuteWithReadLock(this ReaderWriterLockSlim lockSlim, Action action) {
            lockSlim.EnterReadLock();
            try {
                action();
            } finally {
                lockSlim.ExitReadLock();
            }
        }

        /// <summary>
        /// Wrap's a lambda with a write lock
        /// </summary>
        /// <typeparam name="T">Return Type of the lambda executed in the lock context.</typeparam>
        /// <param name="lockSlim">The lock to use for the lambda exectuion.</param>
        /// <param name="func">Lambda function to execute.</param>
        /// <returns>Return value of the lambda executed in the lock's context.</returns>
        public static T ExecuteWithWriteLock<T>(this ReaderWriterLockSlim lockSlim, Func<T> func) {
            lockSlim.EnterWriteLock();
            try {
                return func();
            } finally {
                lockSlim.ExitWriteLock();
            }
        }

        /// <summary>
        /// Wrap's a lambda with a write lock
        /// </summary>
        /// <param name="lockSlim">The lock to use for the lambda exectuion.</param>
        /// <param name="action">Lambda action to execute.</param>
        public static void ExecuteWithWriteLock(this ReaderWriterLockSlim lockSlim, Action action) {
            lockSlim.EnterWriteLock();
            try {
                action();
            } finally {
                lockSlim.ExitWriteLock();
            }
        }
    }
}
