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

namespace NetStack.Buffers {
	internal sealed partial class DefaultArrayPool<T> : ArrayPool<T> {
		private const int DefaultMaxArrayLength = 1024 * 1024;
		private const int DefaultMaxNumberOfArraysPerBucket = 50;
		private static T[] s_emptyArray;
		private readonly Bucket[] _buckets;

		internal DefaultArrayPool() : this(DefaultMaxArrayLength, DefaultMaxNumberOfArraysPerBucket) { }

		internal DefaultArrayPool(int maxArrayLength, int maxArraysPerBucket) {
			if (maxArrayLength <= 0)
				throw new ArgumentOutOfRangeException("maxArrayLength");

			if (maxArraysPerBucket <= 0)
				throw new ArgumentOutOfRangeException("maxArraysPerBucket");

			const int MinimumArrayLength = 0x10, MaximumArrayLength = 0x40000000;

			if (maxArrayLength > MaximumArrayLength)
				maxArrayLength = MaximumArrayLength;
			else if (maxArrayLength < MinimumArrayLength)
				maxArrayLength = MinimumArrayLength;

			int poolId = Id;
			int maxBuckets = Utilities.SelectBucketIndex(maxArrayLength);
			var buckets = new Bucket[maxBuckets + 1];

			for (int i = 0; i < buckets.Length; i++) {
				buckets[i] = new Bucket(Utilities.GetMaxSizeForBucket(i), maxArraysPerBucket, poolId);
			}

			_buckets = buckets;
		}

		private int Id {
			get {
				return GetHashCode();
			}
		}

		public override T[] Rent(int minimumLength) {
			if (minimumLength < 0)
				throw new ArgumentOutOfRangeException("minimumLength");
			else if (minimumLength == 0)
				return s_emptyArray ?? (s_emptyArray = new T[0]);

			#if NETSTACK_BUFFERS_LOG
				var log = ArrayPoolEventSource.EventLog;
			#endif

			T[] buffer = null;
			int index = Utilities.SelectBucketIndex(minimumLength);

			if (index < _buckets.Length) {
				const int MaxBucketsToTry = 2;

				int i = index;

				do {
					buffer = _buckets[i].Rent();

					if (buffer != null) {
						#if NETSTACK_BUFFERS_LOG
							log.BufferRented(buffer.GetHashCode(), buffer.Length, Id, _buckets[i].Id);
						#endif

						return buffer;
					}
				}

				while (++i < _buckets.Length && i != index + MaxBucketsToTry);

				buffer = new T[_buckets[index]._bufferLength];
			} else {
				buffer = new T[minimumLength];
			}

			#if NETSTACK_BUFFERS_LOG
				int bufferId = buffer.GetHashCode(), bucketId = -1;

				log.BufferRented(bufferId, buffer.Length, Id, bucketId);
				log.BufferAllocated(bufferId, buffer.Length, Id, bucketId, index >= _buckets.Length ? ArrayPoolEventSource.BufferAllocatedReason.OverMaximumSize : ArrayPoolEventSource.BufferAllocatedReason.PoolExhausted);
			#endif

			return buffer;
		}

		public override void Return(T[] array, bool clearArray = false) {
			if (array == null)
				throw new ArgumentNullException("array");
			else if (array.Length == 0)
				return;

			int bucket = Utilities.SelectBucketIndex(array.Length);

			if (bucket < _buckets.Length) {
				if (clearArray)
					Array.Clear(array, 0, array.Length);

				_buckets[bucket].Return(array);
			}

			#if NETSTACK_BUFFERS_LOG
				var log = ArrayPoolEventSource.EventLog;

				log.BufferReturned(array.GetHashCode(), array.Length, Id);
			#endif
		}
	}
}