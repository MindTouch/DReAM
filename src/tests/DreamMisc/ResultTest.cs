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
using System.Threading;

using MindTouch.Tasking;
using NUnit.Framework;

namespace MindTouch.Dream.Test {

    public class ResultTestException : Exception {
        public ResultTestException() { }
        public ResultTestException(string message) : base(message) { }
    }

    [TestFixture]
    public class ResultTest {

        #region --- Valid Transitions ---
        [Test]
        public void Return_Wait() {
            Test<int>(
                false,
                result => {
                    result.Return(1);
                },
                result => {
                    /* do nothing */
                });
        }

        [Test]
        public void Wait_Return() {
            Test<int>(
                true,
                result => {
                    result.Return(1);
                },
                result => {
                    /* do nothing */
                });
        }

        [Test]
        public void Throw_Wait() {
            Test<int, ResultTestException>(
                false,
                result => {
                    result.Throw(new ResultTestException());
                },
                result => {
                    /* do nothing */
                },
                false, false, null,
                null
            );
        }

        [Test]
        public void Wait_Throw() {
            Test<int, ResultTestException>(
                true,
                result => {
                    result.Throw(new ResultTestException());
                },
                result => {
                    /* do nothing */
                },
                false, false, null,
                null
            );
        }

        [Test]
        public void Wait_Timeout() {
            Test<int, TimeoutException>(
                false,
                result => {
                    /* do nothing */
                },
                result => {
                    /* do nothing */
                },
                false, false, null,
                null
            );
        }

        [Test]
        public void Wait_Timeout_Return() {
            Test<int, TimeoutException>(
                false,
                result => {
                    /* do nothing */
                },
                result => {
                    result.Return(1);
                },
                false, true, 1,
                null
            );
        }

        [Test]
        public void Wait_Timeout_Throw() {
            Test<int, TimeoutException>(
                false,
                result => {
                    /* do nothing */
                }, result => {
                    result.Throw(new ResultTestException());
                },
                false, true, typeof(ResultTestException),
                null
            );
        }

        [Test]
        public void Wait_Timeout_Cancel() {
            Test<int, TimeoutException>(
                false,
                result => {
                    /* do nothing */
                },
                result => {
                    result.Cancel();
                },
                false, false, null,
                null

            );
        }

        [Test]
        public void Wait_Timeout_ConfirmCancel() {
            Test<int, TimeoutException>(
                false,
                result => {
                    /* do nothing */
                }, result => {
                    result.ConfirmCancel();
                },
                false,true, null,
                null
            );
        }

        [Test]
        public void Wait_Timeout_Cancel_Return() {
            Test<int, TimeoutException>(
                false,
                result => {
                    /* do nothing */
                },
                result => {
                    result.Cancel();
                    result.Return(1);
                },
                false, true, 1,
                null
            );
        }

        [Test]
        public void Wait_Timeout_Cancel_Throw() {
            Test<int, TimeoutException>(
                false,
                result => {
                    /* do nothing */
                }, result => {
                    result.Cancel();
                    result.Throw(new ResultTestException());
                },
                false, true, typeof(ResultTestException),
                null
            );
        }

        [Test]
        public void Wait_Timeout_Cancel_ConfirmCancel() {
            Test<int, TimeoutException>(
                false,
                result => {
                    /* do nothing */
                }, result => {
                    result.Cancel();
                    result.ConfirmCancel();
                },
                false, true, null,
                null
            );
        }

        [Test]
        public void Cancel_Return_Wait() {
            Test<int>(
                false,
                result => {
                    result.Cancel();
                    result.Return(1);
                },
                result => {
                    /* do nothing */
                });
        }

        [Test]
        public void Cancel_Cancel_Return_Wait() {
            Test<int>(
                false,
                result => {
                    result.Cancel();
                    result.Cancel();
                    result.Return(1);
                },
                result => {
                    /* do nothing */
                });
        }

        [Test]
        public void Cancel_HasFinished_Return_Wait() {
            Test<int, CanceledException>(
                false,
                result => {
                    result.Cancel();
                    bool finished = result.HasFinished;
                    result.Return(1);
                },
                result => {
                    /* do nothing */
                },
                false, true, 1,
                null
            );
        }

        [Test]
        public void Cancel_HasFinished_Confirm_Wait() {
            Test<int, CanceledException>(
                false,
                result => {
                    result.Cancel();
                    bool finished = result.HasFinished;
                    result.ConfirmCancel();
                },
                result => {
                    /* do nothing */
                },
                false, true, null,
                null
            );
        }

        [Test]
        public void Wait_Cancel_Return() {
            Test<int, CanceledException>(
                true,
                result => {
                    result.Cancel();
                    result.Return(1);
                },
                result => {
                    /* do nothing */
                },
                false, true, 1,
                null
            );
        }

        [Test]
        public void Wait_Cancel_Cancel_Return() {
            Test<int, CanceledException>(
                true,
            result => {
                result.Cancel();
                result.Cancel();
                result.Return(1);
            },
            result => {
                /* do nothing */
            },
                false, true, 1,
                null
            );
        }

        [Test]
        public void Cancel_Throw_Wait() {
            Test<int, ResultTestException>(
                false,
            result => {
                result.Cancel();
                result.Throw(new ResultTestException());
            },
            result => {
                /* do nothing */
            },
                false, false, null,
                null
            );
        }

        [Test]
        public void Cancel_Cancel_Throw_Wait() {
            Test<int, ResultTestException>(
                false,
            result => {
                result.Cancel();
                result.Cancel();
                result.Throw(new ResultTestException());
            },
            result => {
                /* do nothing */
            },
                false, false, null,
                null
            );
        }

        [Test]
        public void Cancel_HasFinished_Throw_Wait() {
            Test<int, CanceledException>(
                false,
            result => {
                result.Cancel();
                bool finished = result.HasFinished;
                result.Throw(new ResultTestException());
            },
            result => {
                /* do nothing */
            },
                false, true, typeof(ResultTestException),
                null
            );
        }

        [Test]
        public void Wait_Cancel_Throw() {
            Test<int, CanceledException>(
                true,
            result => {
                result.Cancel();
                result.Throw(new ResultTestException());
            },
            result => {
                /* do nothing */
            },
                false, true, typeof(ResultTestException),
                null
            );
        }

        [Test]
        public void Wait_Cancel_Cancel_Throw() {
            Test<int, CanceledException>(
                true,
            result => {
                result.Cancel();
                result.Cancel();
                result.Throw(new ResultTestException());
            },
            result => {
                /* do nothing */
            },
                false, true, typeof(ResultTestException),
                null
            );
        }

        [Test]
        public void Return_Cancel_Wait() {
            Test<int>(
                false,
            result => {
                result.Return(1);
                result.Cancel();
            },
            result => {
                /* do nothing */
            });
        }

        [Test]
        public void Wait_Return_Cancel() {
            Test<int>(
                true,
            result => {
                result.Return(1);
                result.Cancel();
            },
            result => {
                /* do nothing */
            });
        }

        [Test]
        public void Throw_Cancel_Wait() {
            Test<int, ResultTestException>(
                false,
            result => {
                result.Throw(new ResultTestException());
                result.Cancel();
            },
            result => {
                /* do nothing */
            },
                false, false, null,
                null
            );
        }

        [Test]
        public void Wait_Throw_Cancel() {
            Test<int, ResultTestException>(
                true,
            result => {
                result.Throw(new ResultTestException());
                result.Cancel();
            },
            result => {
                /* do nothing */
            },
                false, false, null,
                null
            );
        }

        [Test]
        public void Cancel_ConfirmCancel_Wait() {
            Test<int, CanceledException>(
                false,
            result => {
                result.Cancel();
                result.ConfirmCancel();
            },
            result => {
                /* do nothing */
            },
                false, true, null,
                null
            );
        }

        [Test]
        public void Cancel_ConfirmCancel_Cancel_Wait() {
            Test<int, CanceledException>(
                false,
            result => {
                result.Cancel();
                result.ConfirmCancel();
                result.Cancel();
            },
            result => {
                /* do nothing */
            },
                false, true, null,
                null
            );
        }

        [Test]
        public void Wait_Cancel_ConfirmCancel() {
            Test<int, CanceledException>(
                true,
            result => {
                result.Cancel();
                result.ConfirmCancel();
            },
            result => {
                /* do nothing */
            },
                false, true, null,
                null
            );
        }

        [Test]
        public void Wait_Cancel_ConfirmCancel_Cancel() {
            Test<int, CanceledException>(
                true,
            result => {
                result.Cancel();
                result.ConfirmCancel();
                result.Cancel();
            },
            result => {
                /* do nothing */
            },
                false, true, null,
                null
            );
        }
        #endregion

        #region --- TryReturn ---
        [Test]
        public void TryReturn() {
            var result = new Result<int>();
            var test = result.TryReturn(2);

            Assert.IsTrue(test, "TryReturn failed");
            Assert.IsTrue(result.HasValue, "result value is missing");
            Assert.AreEqual(2, result.Value, "result value does not match");
        }

        [Test]
        public void Return_TryReturn() {
            var result = new Result<int>();
            result.Return(1);
            var test = result.TryReturn(2);

            Assert.IsFalse(test, "TryReturn succeeded");
            Assert.IsTrue(result.HasValue, "result value is missing");
            Assert.AreEqual(1, result.Value, "result value does not match");
        }

        [Test]
        public void Throw_TryReturn() {
            var result = new Result<int>();
            result.Throw(new ResultTestException());
            var test = result.TryReturn(2);

            Assert.IsFalse(test, "TryReturn succeeded");
            Assert.IsTrue(result.HasException, "result exception is missing");
            Assert.IsInstanceOf<ResultTestException>(result.Exception, "result exception has wrong type");
        }

        [Ignore("Not fully baked yet")]
        [Test]
        public void Cancel_TryReturn() {
            var result = new Result<int>();
            result.Cancel();
            var test = result.TryReturn(2);

            Assert.IsFalse(test, "TryReturn succeeded");
            Assert.IsTrue(result.IsCanceled, "result is not canceled");
            Assert.IsTrue(result.HasException, "result exception is missing");
            Assert.IsInstanceOf<CanceledException>(result.Exception, "result exception has wrong type");
        }
        #endregion

        #region --- Invalid Transitions ---
        [Test]
        public void Return_Return() {
            TestFail<int, InvalidOperationException>(
            result => {
                result.Return(1);
                result.Return(2);
            },
                null,
                result => result.HasValue && (result.Value == 1)
            );
        }

        [Test]
        public void Return_Throw() {
            TestFail<int, InvalidOperationException>(
            result => {
                result.Return(1);
                result.Throw(new ResultTestException());
            },
                null,
                result => result.HasValue && (result.Value == 1)
            );
        }

        [Test]
        public void Throw_Return() {
            TestFail<int, InvalidOperationException>(
            result => {
                result.Throw(new ResultTestException());
                result.Return(1);
            },
                null,
                result => result.HasException && (result.Exception is ResultTestException)
            );
        }

        [Test]
        public void Throw_Throw() {
            TestFail<int, InvalidOperationException>(
            result => {
                result.Throw(new ResultTestException("first"));
                result.Throw(new ResultTestException("second"));
            },
                null,
                result => result.HasException && (result.Exception is ResultTestException)
            );
        }

        [Test]
        public void ConfirmCancel() {
            TestFail<int, InvalidOperationException>(
            result => {
                result.ConfirmCancel();
            },
                null,
                result => result.HasException && (result.Exception is InvalidOperationException)
            );
        }

        [Test]
        public void Return_ConfirmCancel() {
            TestFail<int, InvalidOperationException>(
            result => {
                result.Return(1);
                result.ConfirmCancel();
            },
                null,
                result => result.HasValue && (result.Value == 1)
            );
        }

        [Test]
        public void Throw_ConfirmCancel() {
            TestFail<int, InvalidOperationException>(
            result => {
                result.Throw(new ResultTestException());
                result.ConfirmCancel();
            },
                null,
                result => result.HasException && (result.Exception is ResultTestException)
            );
        }
        #endregion

        #region --- Helpers ---
        private static void Test<T>(bool waitFirst, Action<Result<T>> before, Action<Result<T>> after) {
            int success = 0;
            int error = 0;
            int canceled = 0;
            Result<T> canceldResult = null;

            AutoResetEvent wait = new AutoResetEvent(false);
            Result<T> result = new Result<T>(TimeSpan.FromSeconds(0.1), TaskEnv.Instantaneous);

            if(!waitFirst) {
                if(before != null) {
                    before(result);
                }
            }

            result.WithCleanup(r => { ++canceled; canceldResult = r; wait.Set(); });
            result.WhenDone(
                v => { ++success; wait.Set(); },
                e => { ++error; wait.Set(); }
            );

            if(waitFirst) {
                if(before != null) {
                    before(result);
                }
            }

            bool waited = wait.WaitOne(TimeSpan.FromSeconds(1), false);
            Assert.IsTrue(waited, "result failed to time out");
            if(after != null) {
                after(result);
            }

            int expected_success = 1;
            int expected_error = 0;
            Assert.AreEqual(expected_success, success, string.Format("success was {1}, but expected {0}", expected_success, success));
            Assert.AreEqual(expected_error, error, string.Format("error was {1}, but expected {0}", expected_error, error));
            Assert.IsNull(result.Exception, "exception is set");
        }

        private static void Test<T, TException>(bool waitFirst, Action<Result<T>> before, Action<Result<T>> after, bool isSuccess, bool isCancel, object cancelOutcome, Action<Result<T>> check) where TException : Exception {
            int success = 0;
            int error = 0;
            int canceled = 0;
            Result<T> canceldResult = null;

            AutoResetEvent wait = new AutoResetEvent(false);
            Result<T> result = new Result<T>(TimeSpan.FromSeconds(0.1), TaskEnv.Instantaneous);

            if(!waitFirst) {
                if(before != null) {
                    before(result);
                }
            }

            result.WithCleanup(r => { ++canceled; canceldResult = r; wait.Set(); });
            result.WhenDone(
                v => { ++success; wait.Set(); },
                e => { ++error; wait.Set(); }
            );

            if(waitFirst) {
                if(before != null) {
                    before(result);
                }
            }

            bool waited = wait.WaitOne(TimeSpan.FromSeconds(1), false);
            Assert.IsTrue(waited, "result failed to time out");
            if(after != null) {
                after(result);
            }

            int expected_success = isSuccess ? 1 : 0;
            int expected_error = 1;
            int expected_cancel = isCancel ? 1 : 0;
            Assert.AreEqual(expected_success, success, string.Format("success was {1}, but expected {0}", expected_success, success));
            Assert.AreEqual(expected_error, error, string.Format("error was {1}, but expected {0}", expected_error, error));
            Assert.AreEqual(expected_cancel, canceled, string.Format("canceled was {1}, but expected {0}", expected_cancel, canceled));
            Assert.IsInstanceOf<TException>(result.Exception, "exception has wrong type");
            if(isCancel) {
                if(cancelOutcome == null) {
                    Assert.IsNull(canceldResult, "canceled result was not null");
                } else if(cancelOutcome is Type) {
#pragma warning disable 612
                    Assert.IsInstanceOfType((Type)cancelOutcome, canceldResult.Exception, "canceled result exception did not match");
#pragma warning restore 612
                } else {
                    Assert.AreEqual(cancelOutcome, canceldResult.Value, "canceled result value did not match");
                }
            }
            if(check != null) {
                check(result);
            }
        }

        private static void TestFail<T, TException>(Action<Result<T>> callback, string exceptionMessage, Predicate<Result<T>> validate) where TException : Exception {
            Exception ex = null;
            var result = new Result<T>();
            try {
                callback(result);
            } catch(Exception e) {
                ex = e;
            }
            Assert.IsInstanceOf<TException>(ex, "unexpected exception on failed operation");
            if(exceptionMessage != null) {
                Assert.AreEqual(exceptionMessage, ex.Message);
            }
            if(validate != null) {
                Assert.IsTrue(validate(result), "result validation failed");
            }
        }
        #endregion
    }
}