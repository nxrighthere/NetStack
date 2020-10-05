/*
 *  Copyright (c) 2018 Stanislav Denisov, Maxim Munnig
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

/*
 *  Copyright (c) 2018 Alexander Shoulson
 *
 *  This software is provided 'as-is', without any express or implied
 *  warranty. In no event will the authors be held liable for any damages
 *  arising from the use of this software.
 *  Permission is granted to anyone to use this software for any purpose,
 *  including commercial applications, and to alter it and redistribute it
 *  freely, subject to the following restrictions:
 *
 *  1. The origin of this software must not be misrepresented; you must not
 *     claim that you wrote the original software. If you use this software
 *     in a product, an acknowledgment in the product documentation would be
 *     appreciated but is not required.
 *  2. Altered source versions must be plainly marked as such, and must not be
 *     misrepresented as being the original software.
 *  3. This notice may not be removed or altered from any source distribution.
 */

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

#if ENABLE_MONO || ENABLE_IL2CPP
	using UnityEngine.Assertions;
#endif

namespace NetStack.Serialization {
	public class BitBuffer {
		private const int defaultCapacity = 375; // 375 * 4 = 1500 bytes
		private const int stringLengthBits = 8;
		private const int stringLengthMax = (1 << stringLengthBits) - 1; // 255
		private const int bitsASCII = 7;
		private const int growFactor = 2;
		private const int minGrow = 1;
		private int readPosition;
		private int nextPosition;
		private uint[] chunks;

		public BitBuffer(int capacity = defaultCapacity) {
			readPosition = 0;
			nextPosition = 0;
			chunks = new uint[capacity];
		}

		public int Length {
			get {
				return ((nextPosition - 1) >> 3) + 1;
			}
		}

		public bool IsFinished {
			get {
				return nextPosition == readPosition;
			}
		}

		[MethodImpl(256)]
		public void Clear() {
			readPosition = 0;
			nextPosition = 0;
		}

		[MethodImpl(256)]
		public BitBuffer Add(int numBits, uint value) {
			#if ENABLE_MONO || ENABLE_IL2CPP
				Assert.IsFalse(numBits < 0); // Pushing negative bits
				Assert.IsFalse(numBits > 32); // Pushing too many bits
			#else
				Debug.Assert(!(numBits < 0));
				Debug.Assert(!(numBits > 32));
			#endif

			int index = nextPosition >> 5;
			int used = nextPosition & 0x0000001F;

			if ((index + 1) >= chunks.Length)
				ExpandArray();

			ulong chunkMask = ((1UL << used) - 1);
			ulong scratch = chunks[index] & chunkMask;
			ulong result = scratch | ((ulong)value << used);

			chunks[index] = (uint)result;
			chunks[index + 1] = (uint)(result >> 32);
			nextPosition += numBits;

			return this;
		}

		[MethodImpl(256)]
		public uint Read(int numBits) {
			uint result = Peek(numBits);

			readPosition += numBits;

			return result;
		}

		[MethodImpl(256)]
		public uint Peek(int numBits) {
			#if ENABLE_MONO || ENABLE_IL2CPP
				Assert.IsFalse(numBits < 0); // Pushing negative bits
				Assert.IsFalse(numBits > 32); // Pushing too many bits
			#else
				Debug.Assert(!(numBits < 0));
				Debug.Assert(!(numBits > 32));
			#endif

			int index = readPosition >> 5;
			int used = readPosition & 0x0000001F;

			ulong chunkMask = ((1UL << numBits) - 1) << used;
			ulong scratch = (ulong)chunks[index];

			if ((index + 1) < chunks.Length)
				scratch |= (ulong)chunks[index + 1] << 32;

			ulong result = (scratch & chunkMask) >> used;

			return (uint)result;
		}

		public int ToArray(byte[] data) {
			Add(1, 1);

			int numChunks = (nextPosition >> 5) + 1;
			int length = data.Length;

			for (int i = 0; i < numChunks; i++) {
				int dataIdx = i * 4;
				uint chunk = chunks[i];

				if (dataIdx < length)
					data[dataIdx] = (byte)(chunk);

				if (dataIdx + 1 < length)
					data[dataIdx + 1] = (byte)(chunk >> 8);

				if (dataIdx + 2 < length)
					data[dataIdx + 2] = (byte)(chunk >> 16);

				if (dataIdx + 3 < length)
					data[dataIdx + 3] = (byte)(chunk >> 24);
			}

			return Length;
		}

		public void FromArray(byte[] data, int length) {
			int numChunks = (length / 4) + 1;

			if (chunks.Length < numChunks)
				chunks = new uint[numChunks];

			for (int i = 0; i < numChunks; i++) {
				int dataIdx = i * 4;
				uint chunk = 0;

				if (dataIdx < length)
					chunk = (uint)data[dataIdx];

				if (dataIdx + 1 < length)
					chunk = chunk | (uint)data[dataIdx + 1] << 8;

				if (dataIdx + 2 < length)
					chunk = chunk | (uint)data[dataIdx + 2] << 16;

				if (dataIdx + 3 < length)
					chunk = chunk | (uint)data[dataIdx + 3] << 24;

				chunks[i] = chunk;
			}

			int positionInByte = FindHighestBitPosition(data[length - 1]);

			nextPosition = ((length - 1) * 8) + (positionInByte - 1);
			readPosition = 0;
		}

		#if NETSTACK_SPAN
			public int ToSpan(ref Span<byte> data) {
				Add(1, 1);

				int numChunks = (nextPosition >> 5) + 1;
				int length = data.Length;

				for (int i = 0; i < numChunks; i++) {
					int dataIdx = i * 4;
					uint chunk = chunks[i];

					if (dataIdx < length)
						data[dataIdx] = (byte)(chunk);

					if (dataIdx + 1 < length)
						data[dataIdx + 1] = (byte)(chunk >> 8);

					if (dataIdx + 2 < length)
						data[dataIdx + 2] = (byte)(chunk >> 16);

					if (dataIdx + 3 < length)
						data[dataIdx + 3] = (byte)(chunk >> 24);
				}

				return Length;
			}

			public void FromSpan(ref ReadOnlySpan<byte> data, int length) {
				int numChunks = (length / 4) + 1;

				if (chunks.Length < numChunks)
					chunks = new uint[numChunks];

				for (int i = 0; i < numChunks; i++) {
					int dataIdx = i * 4;
					uint chunk = 0;

					if (dataIdx < length)
						chunk = (uint)data[dataIdx];

					if (dataIdx + 1 < length)
 						chunk = chunk | (uint)data[dataIdx + 1] << 8;

					if (dataIdx + 2 < length)
						chunk = chunk | (uint)data[dataIdx + 2] << 16;

					if (dataIdx + 3 < length)
						chunk = chunk | (uint)data[dataIdx + 3] << 24;

					chunks[i] = chunk;
				}

				int positionInByte = FindHighestBitPosition(data[length - 1]);

				nextPosition = ((length - 1) * 8) + (positionInByte - 1);
				readPosition = 0;
			}
		#endif

		[MethodImpl(256)]
		public BitBuffer AddBool(bool value) {
			Add(1, value ? 1U : 0U);

			return this;
		}

		[MethodImpl(256)]
		public bool ReadBool() {
			return Read(1) > 0;
		}

		[MethodImpl(256)]
		public bool PeekBool() {
			return Peek(1) > 0;
		}

		[MethodImpl(256)]
		public BitBuffer AddByte(byte value) {
			Add(8, value);

			return this;
		}

		[MethodImpl(256)]
		public byte ReadByte() {
			return (byte)Read(8);
		}

		[MethodImpl(256)]
		public byte PeekByte() {
			return (byte)Peek(8);
		}

		[MethodImpl(256)]
		public BitBuffer AddShort(short value) {
			AddInt(value);

			return this;
		}

		[MethodImpl(256)]
		public short ReadShort() {
			return (short)ReadInt();
		}

		[MethodImpl(256)]
		public short PeekShort() {
			return (short)PeekInt();
		}

		[MethodImpl(256)]
		public BitBuffer AddUShort(ushort value) {
			AddUInt(value);

			return this;
		}

		[MethodImpl(256)]
		public ushort ReadUShort() {
			return (ushort)ReadUInt();
		}

		[MethodImpl(256)]
		public ushort PeekUShort() {
			return (ushort)PeekUInt();
		}

		[MethodImpl(256)]
		public BitBuffer AddInt(int value) {
			uint zigzag = (uint)((value << 1) ^ (value >> 31));

			AddUInt(zigzag);

			return this;
		}

		[MethodImpl(256)]
		public int ReadInt() {
			uint value = ReadUInt();
			int zagzig = (int)((value >> 1) ^ (-(int)(value & 1)));

			return zagzig;
		}

		[MethodImpl(256)]
		public int PeekInt() {
			uint value = PeekUInt();
			int zagzig = (int)((value >> 1) ^ (-(int)(value & 1)));

			return zagzig;
		}

		[MethodImpl(256)]
		public BitBuffer AddUInt(uint value) {
			uint buffer = 0x0u;

			do {
				buffer = value & 0x7Fu;
				value >>= 7;

				if (value > 0)
					buffer |= 0x80u;

				Add(8, buffer);
			}

			while (value > 0);

			return this;
		}

		[MethodImpl(256)]
		public uint ReadUInt() {
			uint buffer = 0x0u;
			uint value = 0x0u;
			int shift = 0;

			do {
				buffer = Read(8);

				value |= (buffer & 0x7Fu) << shift;
				shift += 7;
			}

			while ((buffer & 0x80u) > 0);

			return value;
		}

		[MethodImpl(256)]
		public uint PeekUInt() {
			int tempPosition = readPosition;
			uint value = ReadUInt();

			readPosition = tempPosition;

			return value;
		}

		[MethodImpl(256)]
		public BitBuffer AddLong(long value) {
			AddInt((int)(value & uint.MaxValue));
			AddInt((int)(value >> 32));

			return this;
		}

		[MethodImpl(256)]
		public long ReadLong() {
			int low = ReadInt();
			int high = ReadInt();
			long value = high;

			return value << 32 | (uint)low;
		}

		[MethodImpl(256)]
		public long PeekLong() {
			int tempPosition = readPosition;
			long value = ReadLong();

			readPosition = tempPosition;

			return value;
		}

		[MethodImpl(256)]
		public BitBuffer AddULong(ulong value) {
			AddUInt((uint)(value & uint.MaxValue));
			AddUInt((uint)(value >> 32));

			return this;
		}

		[MethodImpl(256)]
		public ulong ReadULong() {
			uint low = ReadUInt();
			uint high = ReadUInt();

			return (ulong)high << 32 | low;
		}

		[MethodImpl(256)]
		public ulong PeekULong() {
			int tempPosition = readPosition;
			ulong value = ReadULong();

			readPosition = tempPosition;

			return value;
		}

		[MethodImpl(256)]
		public BitBuffer AddString(string value) {
			if (value == null)
				throw new ArgumentNullException("value");

			uint length = (uint)value.Length;

			if (length > stringLengthMax) {
				length = (uint)stringLengthMax;

				throw new ArgumentOutOfRangeException("value length exceeded");
			}

			Add(stringLengthBits, length);

			for (int i = 0; i < length; i++) {
				Add(bitsASCII, ToASCII(value[i]));
			}

			return this;
		}

		[MethodImpl(256)]
		public string ReadString() {
			StringBuilder builder = new StringBuilder();
			uint length = Read(stringLengthBits);

			for (int i = 0; i < length; i++) {
				builder.Append((char)Read(bitsASCII));
			}

			return builder.ToString();
		}

		public override string ToString() {
			StringBuilder builder = new StringBuilder();

			for (int i = chunks.Length - 1; i >= 0; i--) {
				builder.Append(Convert.ToString(chunks[i], 2).PadLeft(32, '0'));
			}

			StringBuilder spaced = new StringBuilder();

			for (int i = 0; i < builder.Length; i++) {
				spaced.Append(builder[i]);

				if (((i + 1) % 8) == 0)
					spaced.Append(" ");
			}

			return spaced.ToString();
		}

		private void ExpandArray() {
			int newCapacity = (chunks.Length * growFactor) + minGrow;
			uint[] newChunks = new uint[newCapacity];

			Array.Copy(chunks, newChunks, chunks.Length);
			chunks = newChunks;
		}

		[MethodImpl(256)]
		private static int FindHighestBitPosition(byte data) {
			int shiftCount = 0;

			while (data > 0) {
				data >>= 1;
				shiftCount++;
			}

			return shiftCount;
		}

		private static byte ToASCII(char character) {
			byte value = 0;

			try {
				value = Convert.ToByte(character);
			}

			catch (OverflowException) {
				throw new Exception("Cannot convert to ASCII: " + character);
			}

			if (value > 127)
				throw new Exception("Cannot convert to ASCII: " + character);

			return value;
		}
	}
}
