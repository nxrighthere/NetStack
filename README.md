<p align="center"> 
  <img src="https://i.imgur.com/jD77417.png" alt="alt logo">
</p>

[![PayPal](https://github.com/Rageware/Shields/blob/master/paypal.svg)](https://www.paypal.me/nxrighthere) [![Bountysource](https://github.com/Rageware/Shields/blob/master/bountysource.svg)](https://salt.bountysource.com/checkout/amount?team=nxrighthere) [![Coinbase](https://github.com/Rageware/Shields/blob/master/coinbase.svg)](https://commerce.coinbase.com/checkout/03e11816-b6fc-4e14-b974-29a1d0886697)

Lightweight toolset for creating concurrent networking systems for multiplayer games.

NetStack is self-contained and has no dependencies.

Modules:

- Buffers
  - Thread-safe [array pool](https://adamsitnik.com/Array-Pool/)
- Quantization
  - [Half precision](https://en.wikipedia.org/wiki/Half-precision_floating-point_format) algorithm
  - [Bounded range](https://gafferongames.com/post/snapshot_compression/#optimizing-position) algorithm
  - [Smallest three](https://gafferongames.com/post/snapshot_compression/#optimizing-orientation) algorithm
- Serialization
  - Lightweight and straightforward
  - Fast processing
  - [Span](https://adamsitnik.com/Span/) support
  - [Fluent builder](http://www.stefanoricciardi.com/2010/04/14/a-fluent-builder-in-c/) support
  - Compact bit-packing
    - [ZigZag](https://developers.google.com/protocol-buffers/docs/encoding#signed-integers) encoding
    - [Variable-length](https://rosettacode.org/wiki/Variable-length_quantity) encoding
- Threading
  - Array queue
    - Single-producer single-consumer first-in-first-out non-blocking queue
  - Concurrent buffer
    - Multi-producer multi-consumer first-in-first-out non-blocking queue
  - Concurrent pool
    - Self-stabilizing semi-lockless circular buffer
- Unsafe
  - Fast memory copying

Building
--------
By default, all scripts are compiled for .NET Framework 3.5. Define `NET_4_6` directive to build the assembly for .NET Framework 4.6 or higher. Define `NET_STANDARD_2_0` to build the assembly for .NET Core 2.1 or higher.

Define `NETSTACK_SPAN` to enable support for Span.

Define `NETSTACK_BUFFERS_LOG` to enable buffers logging.

Usage
--------
##### Thread-safe buffers pool:
```c#
// Create a new array pool with a maximum size of 1024 bytes per array, 50 arrays per bucket
ArrayPool<byte> buffers = ArrayPool<byte>.Create(1024, 50);

// Rent buffer from the pool with a minimum size of 64 bytes, the returned buffer might be larger
byte[] buffer = buffers.Rent(64);

// Do some stuff
byte data = 0;

for (int i = 0; i < buffer.Length; i++) {
	buffer[i] = data++;
}

// Return buffer back to the pool
buffers.Return(buffer);
```

##### Concurrent objects pool:
```c#
// Define a message object
class MessageObject {
	public uint id;
	public byte[] data;
}

// Create a new objects pool with 8 objects in the head
ConcurrentPool messages = new ConcurrentPool<MessageObject>(8, () => new MessageObject());

// Acquire an object in the pool
MessageObject message = messages.Acquire();

// Do some stuff
message.id = 1;
message.data = buffers.Rent(64);

byte data = 0;

for (int i = 0; i < buffer.Length; i++) {
	message.data[i] = data++;
}

buffers.Return(message.data);

// Release pooled object
messages.Release(message);
```

##### Concurrent objects buffer:
```c#
// Create a new concurrent buffer limited to 8192 cells
ConcurrentBuffer conveyor = new ConcurrentBuffer(8192);

// Enqueue an object
conveyor.Enqueue(message);

// Dequeue object
MessageObject message = (MessageObject)conveyor.Dequeue();
```

##### Quantize float:
```c#
ushort quantizedSpeed = HalfPrecision.Quantize(speed);

float speed = HalfPrecision.Dequantize(quantizedSpeed);
```

##### Quantize vector:
```c#
// Create a new BoundedRange array for Vector3 position, each entry has bounds and precision
BoundedRange[] worldBounds = new BoundedRange[3];

worldBounds[0] = new BoundedRange(-50f, 50f, 0.05f); // X axis
worldBounds[1] = new BoundedRange(0f, 25f, 0.05f); // Y axis
worldBounds[2] = new BoundedRange(-50f, 50f, 0.05f); // Z axis

// Quantize position data ready for compact bit-packing 
QuantizedVector3 quantizedPosition = BoundedRange.Quantize(position, worldBounds);

// Read quantized data
Console.WriteLine("Quantized position - X: " + quantizedPosition.x + ", Y:" + quantizedPosition.y + ", Z:" + quantizedPosition.z);

// Dequantize position data ready for reconstruction after bit-packing
Vector3 dequantizedPosition = BoundedRange.Dequantize(quantizedPosition, worldBounds);
```

##### Quantize quaternion:
```c#
// Quantize rotation data ready for compact bit-packing 
QuantizedQuaternion quantizedRotation = SmallestThree.Quantize(rotation);

// Read quantized data
Console.WriteLine("Quantized rotation - M: " + quantizedRotation.m + ", A:" + quantizedRotation.a + ", B:" + quantizedRotation.b + ", C:" + quantizedRotation.c);

// Dequantize rotation data ready for reconstruction after bit-packing
Quaternion rotation = SmallestThree.Dequantize(quantizedRotation);
```

##### Serialize/deserialize data:
```c#
// Create a new bit buffer with 1024 chunks, the buffer can grow automatically if required
BitBuffer data = new BitBuffer(1024);

// Fill bit buffer and serialize data to a byte array
data.AddUInt(peer)
.AddString(name)
.AddBool(accelerated)
.AddUShort(speed)
.AddUInt(quantizedPosition.x)
.AddUInt(quantizedPosition.y)
.AddUInt(quantizedPosition.z)
.AddUInt(quantizedRotation.m)
.AddUInt(quantizedRotation.a)
.AddUInt(quantizedRotation.b)
.AddUInt(quantizedRotation.c)
.ToArray(buffer);

// Get a length of actual data in bit buffer for sending through the network
Console.WriteLine("Data length: " + data.Length);

// Reset bit buffer for further reusing
data.Clear();

// Deserialize data from a byte array
data.FromArray(buffer, length);

// Unload bit buffer in the same order
uint peer = data.ReadUInt();
string name = data.ReadString();
bool accelerated = data.ReadBool();
ushort speed = data.ReadUShort();
QuantizedVector3 position = new QuantizedVector3(data.ReadUInt(), data.ReadUInt(), data.ReadUInt());
QuantizedQuaternion rotation = new QuantizedQuaternion(data.ReadUInt(), data.ReadUInt(), data.ReadUInt(), data.ReadUInt());

// Check if bit buffer is fully unloaded
Console.WriteLine("Bit buffer is empty: " + data.IsFinished);
```

##### Bit-level operations:
```c#
/*
Bits   Min Dec    Max Dec     Max Hex     Bytes Used
0-7    0          127         0x0000007F  1 byte
8-14   128        1023        0x00003FFF  2 bytes
15-21  1024       2097151     0x001FFFFF  3 bytes
22-28  2097152    268435455   0x0FFFFFFF  4 bytes
29-32  268435456  4294967295  0xFFFFFFFF  5 bytes
*/

data.Add(9, 256);

uint value = data.Read(9);
```

##### Abstract data serialization with Span:
```c#
// Create a one-time allocation buffer pool
static class BufferPool {
	[ThreadStatic]
	private static BitBuffer bitBuffer;

	public static BitBuffer GetBitBuffer() {
		if (bitBuffer == null)
			bitBuffer = new BitBuffer(1024);

		return bitBuffer;
	}
}

// Define a networking message
struct MessageObject {
	public const ushort id = 1; // Used to identify the message, can be packed or sent as packet header
	public uint peer;
	public byte race;
	public ushort skin;

	public void Serialize(ref Span<byte> packet) {
		BitBuffer data = BufferPool.GetBitBuffer();

		data.AddUInt(peer)
		.AddByte(race)
		.AddUShort(skin)
		.ToSpan(ref packet);

		data.Clear();
	}

	public void Deserialize(ref ReadOnlySpan<byte> packet, int length) {
		BitBuffer data = BufferPool.GetBitBuffer();

		data.FromSpan(ref packet, length);

		peer = data.ReadUInt();
		race = data.ReadByte();
		skin = data.ReadUShort();

		data.Clear();
	}
}
```
