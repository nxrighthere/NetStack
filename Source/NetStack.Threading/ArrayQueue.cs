/*
 *  Copyright (c) 2019 Stanislav Denisov
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
using System.Runtime.InteropServices;
using System.Threading;

namespace NetStack.Threading {
	[StructLayout(LayoutKind.Explicit, Size = 192)]
	public sealed class ArrayQueue {
		[FieldOffset(0)]
		private readonly Entry[] _array;
		[FieldOffset(8)]
		private readonly int _arrayMask;
		[FieldOffset(64)]
		private int _enqueuePosition;
		[FieldOffset(128)]
		private int _dequeuePosition;

		public int Count {
			get {
				return _enqueuePosition - _dequeuePosition;
			}
		}

		public ArrayQueue(int capacity) {
			if (capacity < 2)
				throw new ArgumentException("Queue size should be greater than or equal to two");

			if ((capacity & (capacity - 1)) != 0)
				throw new ArgumentException("Queue size should be a power of two");

			_arrayMask = capacity - 1;
			_array = new Entry[capacity];
			_enqueuePosition = 0;
			_dequeuePosition = 0;
		}

		public void Enqueue(object item) {
			while (true) {
				if (TryEnqueue(item))
					break;

				Thread.SpinWait(1);
			}
		}

		public bool TryEnqueue(object item) {
			var array = _array;
			var position = _enqueuePosition;
			var index = position & _arrayMask;

			if (array[index].IsSet != 0)
				return false;

			array[index].element = item;
			array[index].IsSet = 1;

			#if NET_4_6 || NET_STANDARD_2_0
				Volatile.Write(ref _enqueuePosition, position + 1);
			#else
				Thread.MemoryBarrier();
				_enqueuePosition = position + 1;
			#endif

			return true;
		}

		public object Dequeue() {
			while (true) {
				object element;

				if (TryDequeue(out element))
					return element;
			}
		}

		public bool TryDequeue(out object result) {
			var array = _array;
			var position = _dequeuePosition;
			var index = position & _arrayMask;

			if (array[index].IsSet == 0) {
				result = default(object);

				return false;
			}

			result = array[index].element;
			array[index].element = default(object);
			array[index].IsSet = 0;

			#if NET_4_6 || NET_STANDARD_2_0
				Volatile.Write(ref _dequeuePosition, position + 1);
			#else
				Thread.MemoryBarrier();
				_dequeuePosition = position + 1;
			#endif

			return true;
		}

		[StructLayout(LayoutKind.Explicit, Size = 16)]
		private struct Entry {
			[FieldOffset(0)]
			private int isSet;
			[FieldOffset(8)]
			internal object element;

			internal int IsSet {
				get {
					#if NET_4_6 || NET_STANDARD_2_0
						return Volatile.Read(ref isSet);
					#else
						Thread.MemoryBarrier();
						return isSet;
					#endif
				}

				set {
					#if NET_4_6 || NET_STANDARD_2_0
						Volatile.Write(ref isSet, value);
					#else
						Thread.MemoryBarrier();
						isSet = value;
					#endif
				}
			}
		}
	}
}