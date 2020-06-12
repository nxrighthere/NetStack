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

using System.Threading;
using System.Runtime.CompilerServices;

namespace NetStack.Buffers {
	public abstract class ArrayPool<T> {
		#if NET_4_6 || NET_STANDARD_2_0
			private static ArrayPool<T> s_sharedInstance = null;
		#else
			private static volatile ArrayPool<T> s_sharedInstance = null;
		#endif

		public static ArrayPool<T> Shared {
			#if NET_4_6 || NET_STANDARD_2_0
				[MethodImpl(256)]
				get {
					return Volatile.Read(ref s_sharedInstance) ?? EnsureSharedCreated();
				}
			#else
				[MethodImpl(256)]
				get {
					return s_sharedInstance ?? EnsureSharedCreated();
				}
			#endif
		}

		#pragma warning disable 420

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static ArrayPool<T> EnsureSharedCreated() {
			Interlocked.CompareExchange(ref s_sharedInstance, Create(), null);

			return s_sharedInstance;
		}

		public static ArrayPool<T> Create() {
			return new DefaultArrayPool<T>();
		}

		public static ArrayPool<T> Create(int maxArrayLength, int maxArraysPerBucket) {
			return new DefaultArrayPool<T>(maxArrayLength, maxArraysPerBucket);
		}

		public abstract T[] Rent(int minimumLength);

		public abstract void Return(T[] array, bool clearArray = false);
	}
}