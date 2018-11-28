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

#if !(ENABLE_MONO || ENABLE_IL2CPP)
	using System.Numerics;
#else
	using UnityEngine;
#endif

namespace NetStack.Compression {
	public struct CompressedQuaternion {
		public byte m;
		public short a;
		public short b;
		public short c;

		public CompressedQuaternion(byte m, short a, short b, short c) {
			this.m = m;
			this.a = a;
			this.b = b;
			this.c = c;
		}
	}

	public static class SmallestThree {
		private const float floatPrecision = 10000f;

		public static CompressedQuaternion Compress(Quaternion quaternion) {
			CompressedQuaternion data = default(CompressedQuaternion);
			byte m = 0;
			float maxValue = float.MinValue;
			float sign = 1f;

			for (int i = 0; i < 3; i++) {
				float element = 0f;
				float abs = 0f;

				switch (i) {
					#if ENABLE_MONO || ENABLE_IL2CPP
						case 0:
							element = quaternion.x;

							break;

						case 1:
							element = quaternion.y;

							break;

						case 2:
							element = quaternion.z;

							break;

						case 3:
							element = quaternion.w;

							break;
					#else
						case 0:
							element = quaternion.X;

							break;

						case 1:
							element = quaternion.Y;

							break;

						case 2:
							element = quaternion.Z;

							break;

						case 3:
							element = quaternion.W;

							break;
					#endif
				}

				abs = Math.Abs(element);

				if (abs > maxValue) {
					sign = (element < 0) ? -1 : 1;
					m = (byte)i;
					maxValue = abs;
				}
			}

			if (Math.Abs(1f - maxValue) < Math.Max(0.000001f * Math.Max(Math.Abs(maxValue), Math.Abs(1f)), Single.Epsilon * 8)) {
				data.m = (byte)(m + 4);

				return data;
			}

			short a = 0;
			short b = 0;
			short c = 0;

			#if ENABLE_MONO || ENABLE_IL2CPP
				if (m == 0) {
					a = (short)(quaternion.y * sign * floatPrecision);
					b = (short)(quaternion.z * sign * floatPrecision);
					c = (short)(quaternion.w * sign * floatPrecision);
				} else if (m == 1) {
					a = (short)(quaternion.x * sign * floatPrecision);
					b = (short)(quaternion.z * sign * floatPrecision);
					c = (short)(quaternion.w * sign * floatPrecision);
				} else if (m == 2) {
					a = (short)(quaternion.x * sign * floatPrecision);
					b = (short)(quaternion.y * sign * floatPrecision);
					c = (short)(quaternion.w * sign * floatPrecision);
				} else {
					a = (short)(quaternion.x * sign * floatPrecision);
					b = (short)(quaternion.y * sign * floatPrecision);
					c = (short)(quaternion.z * sign * floatPrecision);
				}
			#else
				if (m == 0) {
					a = (short)(quaternion.Y * sign * floatPrecision);
					b = (short)(quaternion.Z * sign * floatPrecision);
					c = (short)(quaternion.W * sign * floatPrecision);
				} else if (m == 1) {
					a = (short)(quaternion.X * sign * floatPrecision);
					b = (short)(quaternion.Z * sign * floatPrecision);
					c = (short)(quaternion.W * sign * floatPrecision);
				} else if (m == 2) {
					a = (short)(quaternion.X * sign * floatPrecision);
					b = (short)(quaternion.Y * sign * floatPrecision);
					c = (short)(quaternion.W * sign * floatPrecision);
				} else {
					a = (short)(quaternion.X * sign * floatPrecision);
					b = (short)(quaternion.Y * sign * floatPrecision);
					c = (short)(quaternion.Z * sign * floatPrecision);
				}
			#endif

			data.m = m;
			data.a = a;
			data.b = b;
			data.c = c;

			return data;
		}

		public static Quaternion Decompress(CompressedQuaternion data) {
			byte m = data.m;

			if (m >= 4 && m <= 7) {
				float x = (m == 4) ? 1f : 0f;
				float y = (m == 5) ? 1f : 0f;
				float z = (m == 6) ? 1f : 0f;
				float w = (m == 7) ? 1f : 0f;

				return new Quaternion(x, y, z, w);
			}

			float a = (float)data.a / floatPrecision;
			float b = (float)data.b / floatPrecision;
			float c = (float)data.c / floatPrecision;
			float d = (float)Math.Sqrt(1f - ((a * a) + (b * b) + (c * c)));

			if (m == 0)
				return new Quaternion(d, a, b, c);
			else if (m == 1)
				return new Quaternion(a, d, b, c);
			else if (m == 2)
				return new Quaternion(a, b, d, c);

			return new Quaternion(a, b, c, d);
		}
	}
}