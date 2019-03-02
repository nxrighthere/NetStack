/*
 *  Copyright (c) 2018 Stanislav Denisov
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in all
 *  copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *  SOFTWARE.
 */

using System;
using System.Diagnostics;
using System.Threading;

namespace NetStack.Buffers {
	internal sealed partial class DefaultArrayPool<T> : ArrayPool<T> {
		private sealed class Bucket {
			internal readonly int _bufferLength;
			private readonly T[][] _buffers;
			#if NETSTACK_BUFFERS_LOG
				private readonly int _poolId;
			#endif
			#if NET_4_6 || NET_STANDARD_2_0
				private SpinLock _lock;
			#else
				private object _lock;
			#endif
			private int _index;

			internal Bucket(int bufferLength, int numberOfBuffers, int poolId) {
				#if NET_4_6 || NET_STANDARD_2_0
					_lock = new SpinLock();
				#else
					_lock = new Object();
				#endif
				_buffers = new T[numberOfBuffers][];
				_bufferLength = bufferLength;

				#if NETSTACK_BUFFERS_LOG
					_poolId = poolId;
				#endif
			}

			#if NET_4_6 || NET_STANDARD_2_0
				internal int Id => GetHashCode();
			#else
				internal int Id {
					get {
						return GetHashCode();
					}
				}
			#endif

			internal T[] Rent() {
				T[][] buffers = _buffers;
				T[] buffer = null;
				bool allocateBuffer = false;

				#if NET_4_6 || NET_STANDARD_2_0
					bool lockTaken = false;

					try {
						_lock.Enter(ref lockTaken);

						if (_index < buffers.Length) {
							buffer = buffers[_index];
							buffers[_index++] = null;
							allocateBuffer = buffer == null;
						}
					}

					finally {
						if (lockTaken)
							_lock.Exit(false);
					}
				#else
					try {
						Monitor.Enter(_lock);

						if (_index < buffers.Length) {
							buffer = buffers[_index];
							buffers[_index++] = null;
							allocateBuffer = buffer == null;
						}
					}

					finally {
						Monitor.Exit(_lock);
					}
				#endif

				if (allocateBuffer) {
					buffer = new T[_bufferLength];

					#if NETSTACK_BUFFERS_LOG
						var log = ArrayPoolEventSource.EventLog;

						log.BufferAllocated(buffer.GetHashCode(), _bufferLength, _poolId, Id, ArrayPoolEventSource.BufferAllocatedReason.Pooled);
					#endif
				}

				return buffer;
			}

			internal void Return(T[] array) {
				if (array.Length != _bufferLength)
					throw new ArgumentException("BufferNotFromPool", "array");

				#if NET_4_6 || NET_STANDARD_2_0
					bool lockTaken = false;

					try {
						_lock.Enter(ref lockTaken);

						if (_index != 0)
							_buffers[--_index] = array;

					}

					finally	{
						if (lockTaken)
							_lock.Exit(false);
					}
				#else
					try {
						Monitor.Enter(_lock);

						if (_index != 0)
							_buffers[--_index] = array;
					}

					finally {
						Monitor.Exit(_lock);
					}
				#endif
			}
		}
	}
}