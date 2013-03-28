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
using MindTouch.Tasking;

using NUnit.Framework;

namespace MindTouch.Dream.Test {
    using Yield = IEnumerator<IYield>;

    [TestFixture]
    public class CoroutineTests {

        //--- Types ---
        public class BubbledException : Exception { }
        public class IntentionalException : Exception { }


        //--- Class Fields ---
        private static readonly log4net.ILog _log = LogUtils.CreateLog();

        //--- Class Methods ---
        #region Coroutines
        private static Yield Ret(int p, Result<int> result) {
            result.Return(p);
            yield break;
        }

        private static Yield Yield_Ret(int p, Result<int> result) {
            yield return AsyncUtil.Sleep(TimeSpan.FromMilliseconds(10));
            result.Return(p);
        }

        private static Yield Throw(int p, Result<int> result) {
            throw new IntentionalException();
        }

        private static Yield Yield_Throw(int p, Result<int> result) {
            yield return AsyncUtil.Sleep(TimeSpan.FromMilliseconds(10));
            throw new IntentionalException();
        }

        private static Yield Ret_Throw(int p, Result<int> result) {
            result.Return(p);
            throw new IntentionalException();
        }

        private static Yield Yield_Ret_Throw(int p, Result<int> result) {
            yield return AsyncUtil.Sleep(TimeSpan.FromMilliseconds(10));
            result.Return(p);
            throw new IntentionalException();
        }

        private static Yield Ret_Yield_Throw(int p, Result<int> result) {
            yield return AsyncUtil.Sleep(TimeSpan.FromMilliseconds(10));
            result.Return(p);
            throw new IntentionalException();
        }

        private static Yield Call(CoroutineHandler<int, Result<int>> coroutine, int p, Result<int> result) {
            Result<int> inner;
            yield return inner = Coroutine.Invoke(coroutine, p, new Result<int>());
            result.Return(inner);
        }

        private Yield Call_Set(CoroutineHandler<int, Result<int>> coroutine, int value, Result<int> result) {
            int? receive = null;
            var res = Coroutine.Invoke(coroutine, value, new Result<int>());
            yield return res.Set(v => receive = v);
            result.Return(receive.Value);
        }

        private Yield Call_Set_Fail(CoroutineHandler<int, Result<int>> coroutine, int value, Result<int> result) {
            int? receive = null;
            yield return Coroutine.Invoke(coroutine, value, new Result<int>()).Set(_ => Assert.Fail());
            result.Return(receive.Value);
        }

        private Yield Call_CatchAndLog(CoroutineHandler<int, Result<int>> coroutine, int value, Result<int> result) {
            Result<int> inner;
            yield return (inner = Coroutine.Invoke(coroutine, value, new Result<int>())).CatchAndLog(_log);
            result.Return(inner);
        }
        #endregion

        //--- Methods ---
        #region Direct invocation tests
        [Test]
        public void Direct_invoke_with_return_in_startup() {
            var result = Coroutine.Invoke(Ret, 11, new Result<int>()).Block();
            Assert.IsTrue(result.HasValue, "result value missing");
            Assert.AreEqual(11, result.Value, "wrong result value");
        }

        [Test]
        public void Direct_invoke_with_return_in_continuation() {
            var result = Coroutine.Invoke(Yield_Ret, 11, new Result<int>()).Block();
            Assert.IsTrue(result.HasValue, "result value missing");
            Assert.AreEqual(11, result.Value, "wrong result value");
        }

        [Test]
        public void Direct_invoke_with_exception_in_startup() {
            var result = Coroutine.Invoke(Throw, 11, new Result<int>()).Block();
            Assert.IsTrue(result.HasException);
            Assert.IsInstanceOfType(typeof(IntentionalException), result.Exception, "wrong exception type");
        }

        [Test]
        public void Direct_invoke_with_exception_in_continuation() {
            var result = Coroutine.Invoke(Yield_Throw, 11, new Result<int>()).Block();
            Assert.IsTrue(result.HasException);
            Assert.IsInstanceOfType(typeof(IntentionalException), result.Exception, "wrong exception type");
        }

        [Test]
        public void Direct_invoke_with_exception_after_return_in_startup() {
            var result = Coroutine.Invoke(Ret_Throw, 11, new Result<int>()).Block();
            Assert.IsTrue(result.HasValue, "result value missing");
            Assert.AreEqual(11, result.Value, "wrong result value");
        }

        [Test]
        public void Direct_invoke_with_exception_after_return_in_continuation1() {
            var result = Coroutine.Invoke(Yield_Ret_Throw, 11, new Result<int>()).Block();
            Assert.IsTrue(result.HasValue, "result value missing");
            Assert.AreEqual(11, result.Value, "wrong result value");
        }

        [Test]
        public void Direct_invoke_with_exception_after_return_in_continuation2() {
            var result = Coroutine.Invoke(Ret_Yield_Throw, 11, new Result<int>()).Block();
            Assert.IsTrue(result.HasValue, "result value missing");
            Assert.AreEqual(11, result.Value, "wrong result value");
        }
        #endregion

        #region Nested invocation tests
        public void Nested_invoke_with_return_in_startup() {
            var result = Coroutine.Invoke<CoroutineHandler<int, Result<int>>, int, Result<int>>(Call, Ret, 11, new Result<int>()).Block();
            Assert.IsTrue(result.HasValue, "result value missing");
            Assert.AreEqual(11, result.Value, "wrong result value");
        }

        [Test]
        public void Nested_invoke_with_return_in_continuation() {
            var result = Coroutine.Invoke<CoroutineHandler<int, Result<int>>, int, Result<int>>(Call, Yield_Ret, 11, new Result<int>()).Block();
            Assert.IsTrue(result.HasValue, "result value missing");
            Assert.AreEqual(11, result.Value, "wrong result value");
        }

        [Test]
        public void Nested_invoke_with_exception_in_startup() {
            var result = Coroutine.Invoke<CoroutineHandler<int, Result<int>>, int, Result<int>>(Call, Throw, 11, new Result<int>()).Block();
            Assert.IsTrue(result.HasException);
            Assert.IsInstanceOfType(typeof(IntentionalException), result.Exception, "wrong exception type");
        }

        [Test]
        public void Nested_invoke_with_exception_in_continuation() {
            var result = Coroutine.Invoke<CoroutineHandler<int, Result<int>>, int, Result<int>>(Call, Yield_Throw, 11, new Result<int>()).Block();
            Assert.IsTrue(result.HasException);
            Assert.IsInstanceOfType(typeof(IntentionalException), result.Exception, "wrong exception type");
        }

        [Test]
        public void Nested_invoke_with_exception_after_return_in_startup() {
            var result = Coroutine.Invoke<CoroutineHandler<int, Result<int>>, int, Result<int>>(Call, Ret_Throw, 11, new Result<int>()).Block();
            Assert.IsTrue(result.HasValue, "result value missing");
            Assert.AreEqual(11, result.Value, "wrong result value");
        }

        [Test]
        public void Nested_invoke_with_exception_after_return_in_continuation1() {
            var result = Coroutine.Invoke<CoroutineHandler<int, Result<int>>, int, Result<int>>(Call, Yield_Ret_Throw, 11, new Result<int>()).Block();
            Assert.IsTrue(result.HasValue, "result value missing");
            Assert.AreEqual(11, result.Value, "wrong result value");
        }

        [Test]
        public void Nested_invoke_with_exception_after_return_in_continuation2() {
            var result = Coroutine.Invoke<CoroutineHandler<int, Result<int>>, int, Result<int>>(Call, Ret_Yield_Throw, 11, new Result<int>()).Block();
            Assert.IsTrue(result.HasValue, "result value missing");
            Assert.AreEqual(11, result.Value, "wrong result value");
        }
        #endregion

        #region Set tests
        [Test]
        public void Direct_SetValue_with_return_in_startup() {
            var result = Coroutine.Invoke<CoroutineHandler<int, Result<int>>, int, Result<int>>(Call_Set, Ret, 11, new Result<int>()).Block();
            Assert.IsTrue(result.HasValue, "result value missing");
            Assert.AreEqual(11, result.Value, "wrong result value");
        }

        [Test]
        public void Direct_SetValue_with_return_in_continuation() {
            var result = Coroutine.Invoke<CoroutineHandler<int, Result<int>>, int, Result<int>>(Call_Set, Yield_Ret, 11, new Result<int>()).Block();
            Assert.IsTrue(result.HasValue, "result value missing");
            Assert.AreEqual(11, result.Value, "wrong result value");
        }

        [Test]
        public void Direct_SetValue_with_exception_in_startup() {
            var result = Coroutine.Invoke<CoroutineHandler<int, Result<int>>, int, Result<int>>(Call_Set_Fail, Throw, 11, new Result<int>()).Block();
            Assert.IsTrue(result.HasException);
            Assert.IsInstanceOfType(typeof(IntentionalException), result.Exception);
        }

        [Test]
        public void Direct_SetValue_with_exception_in_continuation() {
            var result = Coroutine.Invoke<CoroutineHandler<int, Result<int>>, int, Result<int>>(Call_Set_Fail, Yield_Throw, 11, new Result<int>()).Block();
            Assert.IsTrue(result.HasException);
            Assert.IsInstanceOfType(typeof(IntentionalException), result.Exception);
        }
        #endregion

        #region CatchAndLog tests
        [Test]
        public void Direct_CatchAndLog_with_return_in_startup() {
            var result = Coroutine.Invoke<CoroutineHandler<int, Result<int>>, int, Result<int>>(Call_CatchAndLog, Ret, 11, new Result<int>()).Block();
            Assert.IsTrue(result.HasValue, "result value missing");
            Assert.AreEqual(11, result.Value, "wrong result value");
        }

        [Test]
        public void Direct_CatchAndLog_with_return_in_continuation() {
            var result = Coroutine.Invoke<CoroutineHandler<int, Result<int>>, int, Result<int>>(Call_CatchAndLog, Yield_Ret, 11, new Result<int>()).Block();
            Assert.IsTrue(result.HasValue, "result value missing");
            Assert.AreEqual(11, result.Value, "wrong result value");
        }

        [Test]
        public void Direct_CatchAndLog_with_exception_in_startup() {
            var result = Coroutine.Invoke<CoroutineHandler<int, Result<int>>, int, Result<int>>(Call_CatchAndLog, Throw, 11, new Result<int>()).Block();
            Assert.IsTrue(result.HasException);
            Assert.IsInstanceOfType(typeof(IntentionalException), result.Exception);
        }

        [Test]
        public void Direct_CatchAndLog_with_exception_in_continuation() {
            var result = Coroutine.Invoke<CoroutineHandler<int, Result<int>>, int, Result<int>>(Call_CatchAndLog, Yield_Throw, 11, new Result<int>()).Block();
            Assert.IsTrue(result.HasException);
            Assert.IsInstanceOfType(typeof(IntentionalException), result.Exception);
        }
        #endregion

        #region Misc tests
        [Test]
        public void Nested_Catches_bubble_up_exception_from_innermost_coroutine() {
            _log.Debug("invoking coroutine");
            Result r = Coroutine.Invoke(BubbleCoroutine, 5, new Result());
            r.Block();
            Assert.IsTrue(r.HasException);
            _log.Debug("exception", r.Exception);
            Assert.AreEqual(typeof(BubbledException), r.Exception.GetType());
        }

        private Yield BubbleCoroutine(int depth, Result result) {
            _log.DebugFormat("{0} levels remaining", depth);
            yield return AsyncUtil.Sleep(TimeSpan.FromMilliseconds(10));
            if(depth > 0) {
                _log.Debug("invoking BubbleCoroutine again");
                yield return Coroutine.Invoke(BubbleCoroutine, depth - 1, new Result());
                result.Return();
                yield break;
            }
            _log.Debug("throwing");
            throw new BubbledException();
        }
        #endregion
    }
}
