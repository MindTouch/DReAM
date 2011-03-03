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
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

using MindTouch.Tasking;
using MindTouch.Text;

namespace MindTouch.IO {
    using Yield = IEnumerator<IYield>;

    /// <summary>
    /// A set of static and extension methods to simplify common stream opreations and add <see cref="Result"/> based asynchronous operations
    /// </summary>
    public static class StreamUtil {

        //--- Constants ---

        /// <summary>
        /// Common size for internal byte buffer used for Stream operations
        /// </summary>
        public const int BUFFER_SIZE = 16 * 1024;

        private static readonly TimeSpan READ_TIMEOUT = TimeSpan.FromSeconds(30);

        //--- Class Fields ---
        private static readonly Random Randomizer = new Random();
        private static log4net.ILog _log = log4net.LogManager.GetLogger(typeof(StreamUtil));

        //--- Extension Methods ---

        /// <summary>
        /// Write a string to <see cref="Stream"/>
        /// </summary>
        /// <param name="stream">Target <see cref="Stream"/></param>
        /// <param name="encoding">Encoding to use to convert the string to bytes</param>
        /// <param name="text">Regular string or composite format string to write to the <see cref="Stream"/></param>
        /// <param name="args">An System.Object array containing zero or more objects to format.</param>
        public static void Write(this Stream stream, Encoding encoding, string text, params object[] args) {
            const int bufferSize = BUFFER_SIZE / sizeof(char);
            if(text.Length > bufferSize) {

                // to avoid a allocating a byte array of greater than 64k, we chunk our string writing here
                if(args.Length != 0) {
                    text = string.Format(text, args);
                }
                var length = text.Length;
                var idx = 0;
                var buffer = new char[bufferSize];
                while(true) {
                    var size = Math.Min(bufferSize, length - idx);
                    if(size == 0) {
                        break;
                    }
                    text.CopyTo(idx, buffer, 0, size);
                    stream.Write(encoding.GetBytes(buffer, 0, size));
                    idx += size;
                }
            } else {
                if(args.Length == 0) {
                    stream.Write(encoding.GetBytes(text));
                } else {
                    stream.Write(encoding.GetBytes(string.Format(text, args)));
                }
            }
        }

        /// <summary>
        /// Write an entire buffer to a <see cref="Stream"/>
        /// </summary>
        /// <param name="stream">Target <see cref="Stream"/></param>
        /// <param name="buffer">An array of bytes to write to the <see cref="Stream"/></param>
        public static void Write(this Stream stream, byte[] buffer) {
            stream.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Determine whether a <see cref="Stream"/> contents are in memory
        /// </summary>
        /// <param name="stream">Target <see cref="Stream"/></param>
        /// <returns><see langword="true"/> If the <see cref="Stream"/> contents are in memory</returns>
        public static bool IsStreamMemorized(this Stream stream) {
            return (stream is ChunkedMemoryStream) || (stream is MemoryStream);
        }

        /// <summary>
        /// Asynchronously read from a <see cref="Stream"/>
        /// </summary>
        /// <param name="stream">Source <see cref="Stream"/></param>
        /// <param name="buffer">Byte array to fill from the source</param>
        /// <param name="offset">Position in buffer to start writing to</param>
        /// <param name="count">Number of bytes to read from the <see cref="Stream"/></param>
        /// <param name="result">The <see cref="Result"/> instance to be returned by the call.</param>
        /// <returns>Synchronization handle for the number of bytes read.</returns>
        public static Result<int> Read(this Stream stream, byte[] buffer, int offset, int count, Result<int> result) {
            if(SysUtil.UseAsyncIO) {
                return Async.From(stream.BeginRead, stream.EndRead, buffer, offset, count, null, result);
            }
            return Async.Fork(() => SyncRead_Helper(stream, buffer, offset, count), result);
        }

        private static int SyncRead_Helper(Stream stream, byte[] buffer, int offset, int count) {
            var readTotal = 0;
            while(count != 0) {
                var read = stream.Read(buffer, offset, count);
                if(read <= 0) {
                    return readTotal;
                }
                readTotal += read;
                offset += read;
                count -= read;
            }
            return readTotal;
        }

        /// <summary>
        /// Asynchronously write to a <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">Target <see cref="Stream"/>.</param>
        /// <param name="buffer">Byte array to write to the target.</param>
        /// <param name="offset">Position in buffer to start reading from.</param>
        /// <param name="count">Number of bytes to read from buffer.</param>
        /// <param name="result">The <see cref="Result"/> instance to be returned by the call.</param>
        /// <returns>Synchronization handle for the number of bytes read.</returns>
        public static Result Write(this Stream stream, byte[] buffer, int offset, int count, Result result) {
            if(SysUtil.UseAsyncIO) {
                return Async.From(stream.BeginWrite, stream.EndWrite, buffer, offset, count, null, result);
            }
            return Async.Fork(() => stream.Write(buffer, offset, count), result);
        }

        /// <summary>
        /// Synchronous copying of one stream to another.
        /// </summary>
        /// <param name="source">Source <see cref="Stream"/>.</param>
        /// <param name="target">Target <see cref="Stream"/>.</param>
        /// <param name="length">Number of bytes to copy from source to target.</param>
        /// <returns>Actual bytes copied.</returns>
        public static long CopyTo(this Stream source, Stream target, long length) {
            var bufferLength = length >= 0 ? length : long.MaxValue;
            var buffer = new byte[Math.Min(bufferLength, BUFFER_SIZE)];
            long total = 0;
            while(length != 0) {
                var count = (length >= 0) ? Math.Min(length, buffer.LongLength) : buffer.LongLength;
                count = source.Read(buffer, 0, (int)count);
                if(count == 0) {
                    break;
                }
                target.Write(buffer, 0, (int)count);
                total += count;
                length -= count;
            }
            return total;
        }

        /// <summary>
        /// Asynchronous copying of one stream to another.
        /// </summary>
        /// <param name="source">Source <see cref="Stream"/>.</param>
        /// <param name="target">Target <see cref="Stream"/>.</param>
        /// <param name="length">Number of bytes to copy from source to target.</param>
        /// <param name="result">The <see cref="Result"/> instance to be returned by the call.</param>
        /// <returns>Synchronization handle for the number of bytes copied.</returns>
        public static Result<long> CopyTo(this Stream source, Stream target, long length, Result<long> result) {

            if(!SysUtil.UseAsyncIO) {
                return Async.Fork(() => CopyTo(source, target, length), result);
            }

            // NOTE (steveb): intermediary copy steps already have a timeout operation, no need to limit the duration of the entire copy operation

            if((source == Stream.Null) || (length == 0)) {
                result.Return(0);
            } else if(source.IsStreamMemorized() && target.IsStreamMemorized()) {

                // source & target are memory streams; let's do the copy inline as fast as we can
                result.Return(CopyTo(source, target, length));
            } else {

                // use new task environment so we don't copy the task state over and over again
                TaskEnv.ExecuteNew(() => Coroutine.Invoke(CopyTo_Helper, source, target, length, result));
            }
            return result;
        }

        private static Yield CopyTo_Helper(Stream source, Stream target, long length, Result<long> result) {
            byte[] readBuffer = new byte[BUFFER_SIZE];
            byte[] writeBuffer = new byte[BUFFER_SIZE];
            long total = 0;
            int zero_read_counter = 0;
            Result write = null;

            // NOTE (steveb): we stop when we've read the expected number of bytes and the length was non-negative, 
            //                otherwise we stop when we can't read anymore bytes.

            while(length != 0) {

                // read first
                long count = (length >= 0) ? Math.Min(length, readBuffer.LongLength) : readBuffer.LongLength;
                if(source.IsStreamMemorized()) {
                    count = source.Read(readBuffer, 0, (int)count);

                    // check if we failed to read
                    if(count == 0) {
                        break;
                    }
                } else {
                    yield return Read(source, readBuffer, 0, (int)count, new Result<int>(READ_TIMEOUT)).Set(v => count = v);

                    // check if we failed to read
                    if(count == 0) {

                        // let's abort after 10 tries to read more data
                        if(++zero_read_counter > 10) {
                            break;
                        }
                        continue;
                    }
                    zero_read_counter = 0;
                }
                total += count;
                length -= count;

                // swap buffers
                byte[] tmp = writeBuffer;
                writeBuffer = readBuffer;
                readBuffer = tmp;

                // write second
                if((target == Stream.Null) || target.IsStreamMemorized()) {
                    target.Write(writeBuffer, 0, (int)count);
                } else {
                    if(write != null) {
                        yield return write;
                    }
                    write = Write(target, writeBuffer, 0, (int)count, new Result());
                }
            }
            if(write != null) {
                yield return write;
            }

            // return result
            result.Return(total);
        }

        /// <summary>
        /// Asynchrounously copy a <see cref="Stream"/> to several targets
        /// </summary>
        /// <param name="source">Source <see cref="Stream"/></param>
        /// <param name="targets">Array of target <see cref="Stream"/> objects</param>
        /// <param name="length">Number of bytes to copy from source to targets</param>
        /// <param name="result">The <see cref="Result"/> instance to be returned by the call.</param>
        /// <returns>Synchronization handle for the number of bytes copied to each target.</returns>
        public static Result<long?[]> CopyTo(this Stream source, Stream[] targets, long length, Result<long?[]> result) {

            // NOTE (steveb): intermediary copy steps already have a timeout operation, no need to limit the duration of the entire copy operation

            if((source == Stream.Null) || (length == 0)) {
                long?[] totals = new long?[targets.Length];
                for(int i = 0; i < totals.Length; ++i) {
                    totals[i] = 0;
                }
                result.Return(totals);
            } else {

                // use new task environment so we don't copy the task state over and over again
                TaskEnv.ExecuteNew(() => Coroutine.Invoke(CopyTo_Helper, source, targets, length, result));
            }
            return result;
        }

        private static Yield CopyTo_Helper(Stream source, Stream[] targets, long length, Result<long?[]> result) {
            byte[] readBuffer = new byte[BUFFER_SIZE];
            byte[] writeBuffer = new byte[BUFFER_SIZE];
            int zero_read_counter = 0;
            Result[] writes = new Result[targets.Length];

            // initialize totals
            long?[] totals = new long?[targets.Length];
            for(int i = 0; i < totals.Length; ++i) {
                totals[i] = 0;
            }

            // NOTE (steveb): we stop when we've read the expected number of bytes when the length was non-negative, 
            //                otherwise we stop when we can't read anymore bytes.

            while(length != 0) {

                // read first
                long count = (length >= 0) ? Math.Min(length, readBuffer.LongLength) : readBuffer.LongLength;
                if(source.IsStreamMemorized()) {
                    count = source.Read(readBuffer, 0, (int)count);

                    // check if we failed to read
                    if(count == 0) {
                        break;
                    }
                } else {
                    yield return Read(source, readBuffer, 0, (int)count, new Result<int>(READ_TIMEOUT)).Set(v => count = v);

                    // check if we failed to read
                    if(count == 0) {

                        // let's abort after 10 tries to read more data
                        if(++zero_read_counter > 10) {
                            break;
                        }
                        continue;
                    }
                    zero_read_counter = 0;
                }
                length -= count;

                // swap buffers
                byte[] tmp = writeBuffer;
                writeBuffer = readBuffer;
                readBuffer = tmp;

                // wait for pending writes to complete
                for(int i = 0; i < writes.Length; ++i) {
                    if(writes[i] != null) {
                        yield return writes[i].Catch();
                        if(writes[i].HasException) {
                            totals[i] = null;
                        }
                        writes[i] = null;
                    }
                }

                // write second
                for(int i = 0; i < targets.Length; ++i) {

                    // check that write hasn't had an error yet
                    if(totals[i] != null) {
                        totals[i] += count;
                        Stream target = targets[i];
                        if((target == Stream.Null) || target.IsStreamMemorized()) {
                            target.Write(writeBuffer, 0, (int)count);
                        } else {
                            writes[i] = Write(target, writeBuffer, 0, (int)count, new Result());
                        }
                    }
                }
            }

            // wait for pending writes to complete
            for(int i = 0; i < writes.Length; ++i) {
                if(writes[i] != null) {
                    yield return writes[i].Catch();
                    if(writes[i].HasException) {
                        totals[i] = null;
                    }
                }
            }

            // return result
            result.Return(totals);
        }

#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking.  Please avoid using it if possible.")]
#endif
        /// <summary>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </summary>
        public static void CopyToFile(this Stream stream, string filename, long length) {
            FileStream file = null;
            try {
                using(file = File.Create(filename)) {
                    CopyTo(stream, file, length);
                }
            } catch {

                // check if we created a file, if so delete it
                if(file != null) {
                    try {
                        File.Delete(filename);
                    } catch { }
                }
                throw;
            } finally {

                // make sure the source stream is closed
                try {
                    stream.Close();
                } catch { }
            }
        }

        /// <summary>
        /// Asychronously Pad a stream with a sequence of bytes
        /// </summary>
        /// <param name="stream">Target <see cref="Stream"/></param>
        /// <param name="count">Number of bytes to pad</param>
        /// <param name="value">Byte value to use for padding</param>
        /// <param name="result">The <see cref="Result"/> instance to be returned by the call.</param>
        /// <returns>Synchronization handle for completion of padding action.</returns>
        public static Result Pad(this Stream stream, long count, byte value, Result result) {
            if(count < 0) {
                throw new ArgumentException("count must be non-negative");
            }

            // NOTE (steveb): intermediary copy steps already have a timeout operation, no need to limit the duration of the entire copy operation

            if(count == 0) {
                result.Return();
            } else {

                // initialize buffer so we can write in large chunks if need be
                byte[] bytes = new byte[(int)Math.Min(4096L, count)];
                for(int i = 0; i < bytes.Length; ++i) {
                    bytes[i] = value;
                }

                // use new task environment so we don't copy the task state over and over again
                TaskEnv.ExecuteNew(() => Coroutine.Invoke(Pad_Helper, stream, count, bytes, result));
            }
            return result;
        }

        private static Yield Pad_Helper(Stream stream, long count, byte[] bytes, Result result) {

            // write until we reach zero
            while(count > 0) {
                var byteCount = (int)Math.Min(count, bytes.LongLength);
                yield return Write(stream, bytes, 0, byteCount, new Result());
                count -= byteCount;
            }
            result.Return();
        }

        /// <summary>
        /// Compute the MD5 hash.
        /// </summary>
        /// <param name="stream">Stream to hash.</param>
        /// <returns>MD5 hash.</returns>
        public static byte[] ComputeHash(this Stream stream) {
            return MD5.Create().ComputeHash(stream);
        }

        /// <summary>
        /// Compute the MD5 hash string.
        /// </summary>
        /// <param name="stream">Stream to hash.</param>
        /// <returns>MD5 hash string.</returns>
        public static string ComputeHashString(this Stream stream) {
            return StringUtil.HexStringFromBytes(ComputeHash(stream));
        }

#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking.  Please avoid using it if possible.")]
#endif
        /// <summary>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </summary>
        public static byte[] ReadBytes(this Stream source, long length) {
            var result = new MemoryStream();
            CopyTo(source, result, length);
            return result.ToArray();
        }

#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking.  Please avoid using it if possible.")]
#endif
        /// <summary>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </summary>
        public static Result<MemoryStream> ToMemoryStream(this Stream stream, long length, Result<MemoryStream> result) {
            MemoryStream copy;
            if(stream is MemoryStream) {
                var mem = (MemoryStream)stream;
                copy = new MemoryStream(mem.GetBuffer(), 0, (int)mem.Length, false, true);
                result.Return(copy);
            } else {
                copy = new MemoryStream();
                stream.CopyTo(copy, length, new Result<long>(TimeSpan.MaxValue)).WhenDone(
                    v => {
                        copy.Position = 0;
                        result.Return(copy);
                    },
                    result.Throw
                );
            }
            return result;
        }

#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking.  Please avoid using it if possible.")]
#endif
        /// <summary>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </summary>
        public static Result<ChunkedMemoryStream> ToChunkedMemoryStream(this Stream stream, long length, Result<ChunkedMemoryStream> result) {
            var copy = new ChunkedMemoryStream();
            stream.CopyTo(copy, length, new Result<long>(TimeSpan.MaxValue)).WhenDone(
                v => {
                    copy.Position = 0;
                    result.Return(copy);
                },
                result.Throw
            );
            return result;
        }

        /// <summary>
        /// Detect stream encoding.
        /// </summary>
        /// <param name="stream">Stream to examine</param>
        /// <returns>Encoding type detected or null</returns>
        public static Encoding DetectEncoding(this Stream stream) {
            return new BOMEncodingDetector().Detect(stream)

                // TODO (steveb): add <meta> tag detector here
                ?? new CharacterEncodingDetector().Detect(stream);
        }

        //--- Class Methods ---

#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking.  Please avoid using it if possible.")]
#endif
        /// <summary>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </summary>
        public static Stream[] DupStream(Stream stream, long length, int copies) {
            if(copies < 2) {
                throw new ArgumentException("copies");
            }
            Stream[] result = new Stream[copies];

            // make master stream
            MemoryStream master = stream.ToMemoryStream(length, new Result<MemoryStream>()).Wait();
            result[0] = master;

            for(int i = 1; i < copies; i++) {
                result[i] = new MemoryStream(master.GetBuffer(), 0, (int)master.Length, false, true);
            }
            return result;
        }

        /// <summary>
        /// Try to open a file for exclusive read/write access
        /// </summary>
        /// <param name="filename">Path to file</param>
        /// <returns>A <see cref="Stream"/> for the opened file, or <see langword="null"/> on failure to open the file.</returns>
        public static Stream FileOpenExclusive(string filename) {
            for(int attempts = 0; attempts < 10; ++attempts) {
                try {
                    return File.Open(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                } catch(IOException e) {
                    _log.TraceExceptionMethodCall(e, "FileOpenExclusive", filename, attempts);
                } catch(UnauthorizedAccessException e) {
                    _log.TraceExceptionMethodCall(e, "FileOpenExclusive", filename, attempts);
                }
                Thread.Sleep((attempts + 1) * Randomizer.Next(100));
            }
            return null;
        }

        /// <summary>
        /// Create a pipe
        /// </summary>
        /// <param name="writer">The writer endpoint of the pipe</param>
        /// <param name="reader">The reader endpoint of the pipe</param>
        public static void CreatePipe(out Stream writer, out Stream reader) {
            PipeStreamBuffer buffer = new PipeStreamBuffer();
            writer = new PipeStreamWriter(buffer);
            reader = new PipeStreamReader(buffer);
        }

        /// <summary>
        /// Create a pipe
        /// </summary>
        /// <param name="size">The size of the pipe buffer</param>
        /// <param name="writer">The writer endpoint of the pipe</param>
        /// <param name="reader">The reader endpoint of the pipe</param>
        public static void CreatePipe(int size, out Stream writer, out Stream reader) {
            PipeStreamBuffer buffer = new PipeStreamBuffer(size);
            writer = new PipeStreamWriter(buffer);
            reader = new PipeStreamReader(buffer);
        }
    }
}