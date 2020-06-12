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

using System.Diagnostics;
using System.Runtime.CompilerServices;

#if ENABLE_MONO || ENABLE_IL2CPP
	using UnityEngine.Assertions;
#endif

namespace NetStack.Buffers {
	internal static class Utilities {
		[MethodImpl(256)]
		internal static int SelectBucketIndex(int bufferSize) {
			#if ENABLE_MONO || ENABLE_IL2CPP
				Assert.IsTrue(bufferSize > 0);
			#else
				Debug.Assert(bufferSize > 0);
			#endif

			uint bitsRemaining = ((uint)bufferSize - 1) >> 4;
			int poolIndex = 0;

			if (bitsRemaining > 0xFFFF) {
				bitsRemaining >>= 16;
				poolIndex = 16;
			}

			if (bitsRemaining > 0xFF) {
				bitsRemaining >>= 8;
				poolIndex += 8;
			}

			if (bitsRemaining > 0xF) {
				bitsRemaining >>= 4;
				poolIndex += 4;
			}

			if (bitsRemaining > 0x3) {
				bitsRemaining >>= 2;
				poolIndex += 2;
			}

			if (bitsRemaining > 0x1) {
				bitsRemaining >>= 1;
				poolIndex += 1;
			}

			return poolIndex + (int)bitsRemaining;
		}

		[MethodImpl(256)]
		internal static int GetMaxSizeForBucket(int binIndex) {
			int maxSize = 16 << binIndex;

			#if ENABLE_MONO || ENABLE_IL2CPP
				Assert.IsTrue(maxSize >= 0);
			#else
				Debug.Assert(maxSize >= 0);
			#endif

			return maxSize;
		}
	}
}