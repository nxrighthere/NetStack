/*
 *  Copyright (c) 2018 Virgile Bello, Stanislav Denisov
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
using System.Threading;

namespace NetStack.Threading {
	public sealed class ConcurrentPool<T> where T : class {
		#if NET_4_6 || NET_STANDARD_2_0
			private SpinLock _lock;
		#else
			private object _lock;
		#endif
		private readonly Func<T> _factory;
		private Segment _head;
		private Segment _tail;

		public ConcurrentPool(int capacity, Func<T> factory) {
			#if NET_4_6 || NET_STANDARD_2_0
				_lock = new SpinLock();
			#else
				_lock = new Object();
			#endif
			_head = _tail = new Segment(capacity);
			_factory = factory;
		}

		public T Acquire() {
			while (true) {
				var localHead = _head;
				var count = localHead.Count;

				if (count == 0) {
					if (localHead.Next != null) {
						#if NET_4_6 || NET_STANDARD_2_0
							bool lockTaken = false;

							try {
								_lock.Enter(ref lockTaken);

								if (_head.Next != null && _head.Count == 0)
									_head = _head.Next;
							}

							finally {
								if (lockTaken)
									_lock.Exit(false);
							}
						#else
							try {
								Monitor.Enter(_lock);

								if (_head.Next != null && _head.Count == 0)
									_head = _head.Next;
							}

							finally {
								Monitor.Exit(_lock);
							}
						#endif
					} else {
						return _factory();
					}
				} else if (Interlocked.CompareExchange(ref localHead.Count, count - 1, count) == count) {
					var localLow = Interlocked.Increment(ref localHead.Low) - 1;
					var index = localLow & localHead.Mask;
					T item;
					#if NET_4_6 || NET_STANDARD_2_0
						var spinWait = new SpinWait();
					#endif

					while ((item = Interlocked.Exchange(ref localHead.Items[index], null)) == null) {
						#if NET_4_6 || NET_STANDARD_2_0
							spinWait.SpinOnce();
						#else
							Thread.SpinWait(1);
						#endif
					}

					return item;
				}
			}
		}

		public void Release(T item) {
			while (true) {
				var localTail = _tail;
				var count = localTail.Count;

				if (count == localTail.Items.Length) {
					#if NET_4_6 || NET_STANDARD_2_0
						bool lockTaken = false;

						try {
							_lock.Enter(ref lockTaken);

							if (_tail.Next == null && count == _tail.Items.Length)
								_tail = _tail.Next = new Segment(_tail.Items.Length << 1);
						}

						finally {
							if (lockTaken)
								_lock.Exit(false);
						}
					#else
						try {
							Monitor.Enter(_lock);

							if (_tail.Next == null && count == _tail.Items.Length)
								_tail = _tail.Next = new Segment(_tail.Items.Length << 1);
						}

						finally {
							Monitor.Exit(_lock);
						}
					#endif
				} else if (Interlocked.CompareExchange(ref localTail.Count, count + 1, count) == count) {
					var localHigh = Interlocked.Increment(ref localTail.High) - 1;
					var index = localHigh & localTail.Mask;
					#if NET_4_6 || NET_STANDARD_2_0
						var spinWait = new SpinWait();
					#endif

					while (Interlocked.CompareExchange(ref localTail.Items[index], item, null) != null) {
						#if NET_4_6 || NET_STANDARD_2_0
							spinWait.SpinOnce();
						#else
							Thread.SpinWait(1);
						#endif
					}

					return;
				}
			}
		}

		private class Segment {
			public readonly T[] Items;
			public readonly int Mask;
			public int Low;
			public int High;
			public int Count;
			public Segment Next;

			public Segment(int size) {
				if (size <= 0 || ((size & (size - 1)) != 0))
					throw new ArgumentOutOfRangeException("Segment size must be power of two");

				Items = new T[size];
				Mask = size - 1;
			}
		}
	}
}