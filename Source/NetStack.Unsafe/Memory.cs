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
using System.Runtime.CompilerServices;

namespace NetStack.Unsafe {
	#if NET_4_6 || NET_STANDARD_2_0
		public static class Memory {
			#if NETSTACK_INLINING
				[MethodImpl(256)]
			#endif
			public static unsafe void Copy(IntPtr source, int sourceOffset, byte[] destination, int destinationOffset, int length) {
				if (length > 0) {
					fixed (byte* destinationPointer = &destination[destinationOffset]) {
						byte* sourcePointer = (byte*)source + sourceOffset;

						Buffer.MemoryCopy(sourcePointer, destinationPointer, length, length);
					}
				}
			}

			#if NETSTACK_INLINING
				[MethodImpl(256)]
			#endif
			public static unsafe void Copy(byte[] source, int sourceOffset, IntPtr destination, int destinationOffset, int length) {
				if (length > 0) {
					fixed (byte* sourcePointer = &source[sourceOffset]) {
						byte* destinationPointer = (byte*)destination + destinationOffset;

						Buffer.MemoryCopy(sourcePointer, destinationPointer, length, length);
					}
				}
			}

			#if NETSTACK_INLINING
				[MethodImpl(256)]
			#endif
			public static unsafe void Copy(byte[] source, int sourceOffset, byte[] destination, int destinationOffset, int length) {
				if (length > 0) {
					fixed (byte* sourcePointer = &source[sourceOffset]) {
						fixed (byte* destinationPointer = &destination[destinationOffset]) {
							Buffer.MemoryCopy(sourcePointer, destinationPointer, length, length);
						}
					}
				}
			}
		}
	#endif
}