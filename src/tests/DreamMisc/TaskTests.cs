/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2009 MindTouch, Inc.
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using log4net;
using MindTouch;
using MindTouch.Dream;
using MindTouch.Dream.Test;
using MindTouch.Tasking;
using MindTouch.Threading;
using NUnit.Framework;

namespace MindTouch.Dream.Test {
    using Yield = IEnumerator<IYield>;

    [TestFixture]
    public class TaskTests {

        public class State { }
        public class TaskLifeSpanState : ITaskLifespan {
            public TaskLifeSpanState(string name) { IsDisposed = false; Name = name; Count = 1; }
            public TaskLifeSpanState(string name, int count) { IsDisposed = false; Name = name; Count = count + 1; }
            public object Clone() { return new TaskLifeSpanState(Name, Count); }
            public void Dispose() { IsDisposed = true; }
            public bool IsDisposed { get; private set; }
            public string Name { get; private set; }
            public int Count { get; private set; }
        }

        private static readonly ILog _log = LogUtils.CreateLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [SetUp]
        public void Setup() {
            TaskEnv.Current.Reset();
        }

        [Test]
        public void Can_store_and_retrieve_string_state_on_taskenv_in_coroutine() {
            var env = TaskEnv.Current;
            var state = "bar";
            var key = "foo";
            env.SetState(key, state);
            Assert.IsTrue(Coroutine.Invoke(CheckState, 1, key, state, new Result<bool>()).Wait());
        }


        [Test]
        public void Can_store_and_retrieve_object_state_on_taskenv_in_coroutine() {
            var env = TaskEnv.Current;
            var state = new State();
            Assert.AreNotEqual(new State(), state);
            var key = "foo";
            env.SetState(key, state);
            Assert.IsTrue(Coroutine.Invoke(CheckState, 1, key, state, new Result<bool>()).Wait());
        }

        [Test]
        public void TaskEnv_Current_has_current_state() {
            var state = new State();
            TaskEnv.Current.SetState("foo", state);
            Assert.AreEqual(state, TaskEnv.Current["foo"]);
        }

        [Test]
        public void Copied_TaskEnv_has_current_state() {
            var state = new State();
            TaskEnv.Current.SetState("foo", state);
            Assert.AreEqual(state, TaskEnv.Clone()["foo"]);
        }

        [Test]
        public void New_TaskEnv_does_not_carry_state() {
            var state = new State();
            TaskEnv.Current.SetState("foo", state);
            Assert.IsNull(TaskEnv.New().GetState<State>("foo"));
        }

        [Test]
        public void Forked_thread_has_copy_of_current_state() {
            var state = new State();
            TaskEnv.Current.SetState(state);
            Assert.IsTrue(Async.Fork(() => (state == TaskEnv.Current.GetState<State>()), new Result<bool>()).Wait());
        }

        [Test]
        public void Copied_state_on_another_thread_is_independent_of_original() {
            var state = new State();
            TaskEnv.Current.SetState(state);
            var resetEvent = new AutoResetEvent(true);
            var result = Async.Fork(() => {
                resetEvent.WaitOne();
                TaskEnv.Current.SetState(new State());
                resetEvent.WaitOne();
            }, new Result());
            Assert.AreEqual(state, TaskEnv.Current.GetState<State>());
            resetEvent.Set();
            Thread.Sleep(100);
            Assert.AreEqual(state, TaskEnv.Current.GetState<State>());
            resetEvent.Set();
            result.Wait();
            Assert.AreEqual(state, TaskEnv.Current.GetState<State>());
        }

        [Test]
        public void State_changed_in_coroutine_is_visible_to_subsequent_coroutines() {
            var state = new State();
            TaskEnv.Current.SetState(state);
            Assert.IsTrue(Coroutine.Invoke(ChangeState, 2, state, new Result<bool>()).Wait());
        }

        [Test]
        public void TaskEnv_invokenow_sets_task_state() {
            var currentState = new State();
            State newState = null;
            TaskEnv.Current.SetState(currentState);
            var currentEnv = TaskEnv.Current;
            var newEnv = TaskEnv.New();
            bool? hasState = null;

            // Note: have to over acquire otherwise env is wiped after invokenow
            currentEnv.Acquire();
            currentEnv.Acquire();
            currentEnv.InvokeNow(() => {
                hasState = currentState == TaskEnv.Current.GetState<State>();
            });
            Assert.IsTrue(hasState.HasValue);
            Assert.IsTrue(hasState.Value);
            hasState = null;

            // Note: have to over acquire otherwise env is wiped after invokenow
            newEnv.Acquire();
            newEnv.Acquire();
            newEnv.Acquire();
            newEnv.InvokeNow(() => {
                hasState = currentState == TaskEnv.Current.GetState<State>();
                newState = new State();
                TaskEnv.Current.SetState(newState);
            });
            Assert.IsTrue(hasState.HasValue);
            Assert.IsFalse(hasState.Value);
            Assert.IsNotNull(newState);
            Assert.AreEqual(currentState, TaskEnv.Current.GetState<State>());
            Assert.AreNotEqual(currentState, newState);
            hasState = null;
            newEnv.InvokeNow(() => {
                hasState = newState == TaskEnv.Current.GetState<State>();
                newEnv = TaskEnv.Current;
            });
            Assert.IsTrue(hasState.HasValue);
            Assert.IsTrue(hasState.Value);
        }

        [Test]
        public void TaskEnv_invoke_sets_task_state() {
            _log.Debug("setting up envs");
            var currentState = new State();
            State newState = null;
            TaskEnv.Current.SetState(currentState);
            var currentEnv = TaskEnv.Current;
            var newEnv = TaskEnv.New();
            var resetEvent = new AutoResetEvent(false);
            bool? hasState = null;

            // Note: have to over acquire otherwise env is wiped after invokenow
            currentEnv.Acquire();
            currentEnv.Acquire();
            currentEnv.Invoke(() => {
                _log.Debug("current env invoke");
                hasState = currentState == TaskEnv.Current.GetState<State>();
                resetEvent.Set();
            });
            resetEvent.WaitOne();
            Assert.IsTrue(hasState.HasValue);
            Assert.IsTrue(hasState.Value);
            hasState = null;

            // Note: have to over acquire otherwise env is wiped after invokenow
            newEnv.Acquire();
            newEnv.Acquire();
            newEnv.Acquire();
            newEnv.Invoke(() => {
                _log.Debug("new env invoke");
                hasState = currentState == TaskEnv.Current.GetState<State>();
                newState = new State();
                TaskEnv.Current.SetState(newState);
                resetEvent.Set();
            });
            resetEvent.WaitOne();
            Assert.IsTrue(hasState.HasValue);
            Assert.IsFalse(hasState.Value);
            Assert.IsNotNull(newState);
            Assert.AreEqual(currentState, TaskEnv.Current.GetState<State>());
            Assert.AreNotEqual(currentState, newState);
            hasState = null;
            newEnv.Invoke(() => {
                _log.Debug("new env invoke 2");
                hasState = newState == TaskEnv.Current.GetState<State>();
                newEnv = TaskEnv.Current;
                resetEvent.Set();
            });
            resetEvent.WaitOne();
            Assert.IsTrue(hasState.HasValue);
            Assert.IsTrue(hasState.Value);
        }

        [Test]
        public void TaskEnv_invoke_with_custom_dispatchqueue_sets_task_state() {
            var dispatchQueue = new TestDispatchQueue();
            _log.Debug("setting up envs");
            var currentState = new State();
            State newState = null;
            TaskEnv.Current.SetState(currentState);
            var copiedEnv = TaskEnv.Clone(dispatchQueue);
            var newEnv = TaskEnv.New(dispatchQueue);
            var resetEvent = new AutoResetEvent(false);
            bool? hasState = null;

            // Note: have to over acquire otherwise env is wiped after invokenow
            copiedEnv.Acquire();
            copiedEnv.Acquire();
            copiedEnv.Invoke(() => {
                _log.Debug("copied env invoke");
                hasState = currentState == TaskEnv.Current.GetState<State>();
                resetEvent.Set();
            });
            resetEvent.WaitOne();
            dispatchQueue.LastItem.Wait();
            Assert.IsTrue(hasState.HasValue);
            Assert.IsTrue(hasState.Value);
            hasState = null;

            // Note: have to over acquire otherwise env is wiped after invokenow
            newEnv.Acquire();
            newEnv.Acquire();
            newEnv.Acquire();
            newEnv.Invoke(() => {
                _log.Debug("new env invoke");
                hasState = currentState == TaskEnv.Current.GetState<State>();
                newState = new State();
                TaskEnv.Current.SetState(newState);
                resetEvent.Set();
            });
            resetEvent.WaitOne();
            dispatchQueue.LastItem.Wait();
            Assert.IsTrue(hasState.HasValue);
            Assert.IsFalse(hasState.Value);
            Assert.IsNotNull(newState);
            Assert.AreEqual(currentState, TaskEnv.Current.GetState<State>());
            Assert.AreNotEqual(currentState, newState);
            hasState = null;
            newEnv.Invoke(() => {
                _log.Debug("new env invoke 2");
                hasState = newState == TaskEnv.Current.GetState<State>();
                newEnv = TaskEnv.Current;
                resetEvent.Set();
            });
            resetEvent.WaitOne();
            dispatchQueue.LastItem.Wait();
            Assert.IsTrue(hasState.HasValue);
            Assert.IsTrue(hasState.Value);
        }

        [Test]
        public void ITaskCloneable_is_cloned_on_task_copy() {
            var state = new TaskLifeSpanState("baz");
            TaskEnv.Current.SetState("foo", state);
            bool? hasState = null;
            bool stateExists = false;
            var env = TaskEnv.Clone();
            env.Acquire();
            env.InvokeNow(() => {
                stateExists = TaskEnv.Current.ContainsKey("foo");
                hasState = state == TaskEnv.Current.GetState<TaskLifeSpanState>("foo");
            });
            Assert.IsTrue(hasState.HasValue);
            Assert.IsFalse(hasState.Value);
        }

        [Test]
        public void ITaskCloneable_is_not_cloned_on_task_current_instantiation() {
            var state = new TaskLifeSpanState("baz");
            TaskEnv.Current.SetState("foo", state);
            bool? hasState = null;
            TaskEnv.Current.Acquire();
            TaskEnv.Current.InvokeNow(() => {
                hasState = state == TaskEnv.Current.GetState<TaskLifeSpanState>("foo");
            });
            Assert.IsTrue(hasState.HasValue);
            Assert.IsTrue(hasState.Value);
        }

        [Test]
        public void Async_Result_WhenDone_does_not_get_executed_task_state() {
            var state = new TaskLifeSpanState("baz");
            var allgood = false;
            var resetEvent = new ManualResetEvent(false);
            Async.Fork(() => {
                _log.Debug("setting inner state");
                TaskEnv.Current.SetState("foo", state);
            }, new Result()).WhenDone(r => {
                _log.Debug("executing whendone");
                allgood = !r.HasException && state != TaskEnv.Current.GetState<TaskLifeSpanState>("foo");
                resetEvent.Set();
            });
            _log.Debug("waiting for fork");
            resetEvent.WaitOne();
            _log.Debug("done");
            Assert.IsTrue(allgood);
        }

        [Test]
        public void Result_with_current_state_should_get_original_state_on_whendone_with_return_before_whendone() {
            var state = new TaskLifeSpanState("baz");
            TaskEnv.Current.SetState(state);
            var allgood = false;
            var resetEvent = new ManualResetEvent(false);
            var r = new Result(TaskEnv.Current);
            r.Return();
            r.WhenDone(r2 => {
                allgood = !r2.HasException && state == TaskEnv.Current.GetState<TaskLifeSpanState>();
                resetEvent.Set();
            });
            resetEvent.WaitOne();
            Assert.IsTrue(allgood);
        }

        [Test]
        public void Result_with_current_state_should_get_original_state_on_whendone_with_return_after_whendone() {
            var state = new TaskLifeSpanState("baz");
            TaskEnv.Current.SetState(state);
            var allgood = false;
            var resetEvent = new ManualResetEvent(false);
            var r = new Result(TaskEnv.Current);
            r.WhenDone(r2 => {
                allgood = !r2.HasException && state == TaskEnv.Current.GetState<TaskLifeSpanState>();
                resetEvent.Set();
            });
            r.Return();
            resetEvent.WaitOne();
            Assert.IsTrue(allgood);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void InvokeNow_an_unacquired_TaskEnv_throws() {
            TaskEnv.Current.InvokeNow(() => _log.Debug("invoke now"));
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Invoke_an_unacquired_TaskEnv_throws() {
            TaskEnv.Current.Invoke(() => _log.Debug("invoke now"));
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Invoke1arg_an_unacquired_TaskEnv_throws() {
            TaskEnv.Current.Invoke(x => _log.Debug("invoke now"), 1);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Invoke2arg_an_unacquired_TaskEnv_throws() {
            TaskEnv.Current.Invoke((x, y) => _log.Debug("invoke now"), 1, 2);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Invoke3arg_an_unacquired_TaskEnv_throws() {
            TaskEnv.Current.Invoke((x, y, z) => _log.Debug("invoke now"), 1, 2, 3);
        }

        [Test]
        public void Invokenow_does_not_release() {
            var state = new TaskLifeSpanState("baz");
            TaskEnv.Current.SetState(state);
            Assert.IsFalse(state.IsDisposed);
            TaskEnv.Current.Acquire();
            Assert.IsFalse(state.IsDisposed);
            TaskEnv.Current.InvokeNow(() => _log.Debug("invoke now"));
            Assert.IsFalse(state.IsDisposed);
            TaskEnv.Current.Release();
            Assert.IsTrue(state.IsDisposed);
        }

        [Test]
        public void Each_invokeNoArg_counteracts_one_acquire() {
            var state = new TaskLifeSpanState("baz");
            TaskEnv.Current.SetState(state);
            Assert.IsFalse(state.IsDisposed);
            TaskEnv.Current.Acquire();
            TaskEnv.Current.Acquire();
            Assert.IsFalse(state.IsDisposed);
            var resetEvent = new ManualResetEvent(false);
            TaskEnv.Current.Invoke(() => resetEvent.Set());
            resetEvent.WaitOne();
            resetEvent.Reset();
            // Note (arnec): got a race condition since the set happens before the release
            Thread.Sleep(100);
            Assert.IsFalse(state.IsDisposed);
            TaskEnv.Current.Invoke(() => resetEvent.Set());
            resetEvent.WaitOne();
            resetEvent.Reset();
            Thread.Sleep(100);
            Assert.IsTrue(state.IsDisposed);
        }

        [Test]
        public void WhenDone_triggers_state_disposal() {
            var state = new TaskLifeSpanState("baz");
            Assert.IsFalse(state.IsDisposed);
            TaskEnv.Current.SetState(state);
            var resetEvent = new ManualResetEvent(false);
            var result = new Result(TaskEnv.Current);
            _log.Debug("setting up whendone");
            result.WhenDone(r => {
                _log.Debug("whendone called");
                resetEvent.Set();
            });
            result.Return();
            resetEvent.WaitOne();
            Assert.IsTrue(Wait.For(() => state.IsDisposed, TimeSpan.FromSeconds(10)));
        }

        private Yield ChangeState(int depth, State state, Result<bool> result) {
            if(state != TaskEnv.Current.GetState<State>()) {
                _log.DebugFormat("{0} - passed in state does not match current state", depth);
                result.Return(false);
                yield break;
            }
            if(depth <= 0) {
                result.Return(true);
                yield break;
            }
            state = new State();
            TaskEnv.Current.SetState(state);
            Result<bool> r;
            yield return r = Coroutine.Invoke(ChangeState, depth - 1, state, new Result<bool>());
            result.Return(r);
        }

        private IEnumerator<IYield> CheckState(int depth, string key, object state, Result<bool> result) {
            if(depth > 0) {
                Result<bool> r;
                yield return r = Coroutine.Invoke(CheckState, depth - 1, key, state, new Result<bool>());
                result.Return(r.Value);
            } else {
                result.Return(state == TaskEnv.Current[key]);
            }
        }
    }

    public class TestDispatchQueue : IDispatchQueue {

        public Result LastItem;
        public void QueueWorkItem(Action callback) {
            LastItem = Async.ForkThread(callback, new Result());
        }

        public bool TryQueueWorkItem(Action callback) {
            throw new NotImplementedException();
        }
    }
}
