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

namespace NetStack.Quantization {
	public struct QuantizedQuaternion {
		public uint m;
		public uint a;
		public uint b;
		public uint c;

		public QuantizedQuaternion(uint m, uint a, uint b, uint c) {
			this.m = m;
			this.a = a;
			this.b = b;
			this.c = c;
		}
	}

	public static class SmallestThree {
		private const float smallestThreeUnpack = 0.70710678118654752440084436210485f + 0.0000001f;
		private const float smallestThreePack = 1f / smallestThreeUnpack;

		public static QuantizedQuaternion Quantize(Quaternion quaternion, int bitsPerElement = 12) {
			float halfRange = (1 << bitsPerElement - 1);
			float packer = smallestThreePack * halfRange;
			float maxValue = float.MinValue;
			bool signMinus = false;
			uint m = 0;
			uint a = 0;
			uint b = 0;
			uint c = 0;

			for (uint i = 0; i <= 3; i++) {
				float element = 0.0f;
				float abs = 0.0f;

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
					signMinus = (element < 0.0f);
					m = i;
					maxValue = abs;
				}
			}

			float af = 0.0f;
			float bf = 0.0f;
			float cf = 0.0f;

			#if ENABLE_MONO || ENABLE_IL2CPP
				switch (m) {
					case 0:
						af = quaternion.y;
						bf = quaternion.z;
						cf = quaternion.w;

						break;
					case 1:
						af = quaternion.x;
						bf = quaternion.z;
						cf = quaternion.w;

						break;
					case 2:
						af = quaternion.x;
						bf = quaternion.y;
						cf = quaternion.w;

						break;
					default:
						af = quaternion.x;
						bf = quaternion.y;
						cf = quaternion.z;

						break;
				}
			#else
				switch (m) {
					case 0:
						af = quaternion.Y;
						bf = quaternion.Z;
						cf = quaternion.W;

						break;
					case 1:
						af = quaternion.X;
						bf = quaternion.Z;
						cf = quaternion.W;

						break;
					case 2:
						af = quaternion.X;
						bf = quaternion.Y;
						cf = quaternion.W;

						break;
					default:
						af = quaternion.X;
						bf = quaternion.Y;
						cf = quaternion.Z;

						break;
				}
			#endif

			if (signMinus) {
				a = (uint)((-af * packer) + halfRange);
				b = (uint)((-bf * packer) + halfRange);
				c = (uint)((-cf * packer) + halfRange);
			} else {
				a = (uint)((af * packer) + halfRange);
				b = (uint)((bf * packer) + halfRange);
				c = (uint)((cf * packer) + halfRange);
			}

			return new QuantizedQuaternion(m, a, b, c);
		}

		public static Quaternion Dequantize(QuantizedQuaternion data, int bitsPerElement = 12) {
   			int halfRange = (1 << bitsPerElement - 1);
			float unpacker = smallestThreeUnpack * (1f / halfRange);
			uint m = data.m;
			int ai = (int)data.a;
			int bi = (int)data.b;
			int ci = (int)data.c;

			ai -= halfRange;
			bi -= halfRange;
			ci -= halfRange;

			float a = ai * unpacker;
			float b = bi * unpacker;
			float c = ci * unpacker;

			float d = (float)Math.Sqrt(1f - ((a * a) + (b * b) + (c * c)));

			switch (m) {
				case 0:
					return new Quaternion(d, a, b, c);

				case 1:
					return new Quaternion(a, d, b, c);

				case 2:
					return new Quaternion(a, b, d, c);

				default:
					return new Quaternion(a, b, c, d);
			}
		}
	}
}
