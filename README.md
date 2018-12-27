<p align="center"> 
  <img src="https://i.imgur.com/jD77417.png" alt="alt logo">
</p>

[![PayPal](https://drive.google.com/uc?id=1OQrtNBVJehNVxgPf6T6yX1wIysz1ElLR)](https://www.paypal.me/nxrighthere) [![Bountysource](https://drive.google.com/uc?id=19QRobscL8Ir2RL489IbVjcw3fULfWS_Q)](https://salt.bountysource.com/checkout/amount?team=nxrighthere) [![Coinbase](https://drive.google.com/uc?id=1LckuF-IAod6xmO9yF-jhTjq1m-4f7cgF)](https://commerce.coinbase.com/checkout/03e11816-b6fc-4e14-b974-29a1d0886697) [![Discord](https://discordapp.com/api/guilds/515987760281288707/embed.png)](https://discord.gg/ceaWXVw)

Lightweight toolset for creating concurrent networking systems for multiplayer games.

NetStack is self-contained and has no dependencies.

Modules:

- Buffers
  - Thread-safe [array pool](https://adamsitnik.com/Array-Pool/)
- Compression
  - [Half precision](https://en.wikipedia.org/wiki/Half-precision_floating-point_format) algorithm
  - [Bounded range](https://gafferongames.com/post/snapshot_compression/#optimizing-position) algorithm
  - [Smallest three](https://gafferongames.com/post/snapshot_compression/#optimizing-orientation) algorithm
- Serialization
  - Lightweight and straightforward
  - Fast processing
  - [Span](https://msdn.microsoft.com/en-us/magazine/mt814808.aspx) support
  - [Fluent builder](http://www.stefanoricciardi.com/2010/04/14/a-fluent-builder-in-c/) support
  - Compact bit-packing
    - [ZigZag](https://developers.google.com/protocol-buffers/docs/encoding#signed-integers) encoding
    - Elastic encoding
- Threading
  - Concurrent objects buffer
    - Multi-producer multi-consumer first-in-first-out non-blocking queue
  - Concurrent objects pool
    - Self-stabilizing semi-lockless circular buffer
- Unsafe
  - Fast memory copying

Building
--------
By default, all scripts are compiled for .NET Framework 3.5. Define `NET_4_6` directive to build the assembly for .NET Framework 4.6 or higher. Define `NET_STANDARD_2_0` to build the assembly for .NET Core 2.1 or higher.

Define `NETSTACK_INLINING` to enable aggressive inlining for performance critical functionality.

Define `NETSTACK_SPAN` to enable support for Span.

Define `NETSTACK_BUFFERS_LOG` to enable buffers logging.

Usage
--------
##### Thread-safe buffers pooling:
```c#
// Create a new buffers pool with a maximum size of 1024 bytes per array, 50 arrays per bucket
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

##### Concurrent objects pooling:
```c#
// Define a message object
class MessageObject {
	public uint id { get; set; }
	public byte[] data { get; set; }
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
if (!conveyor.TryEnqueue(message))
	Console.WriteLine("Conveyor is full!");

// Dequeue all objects
object element;

while (conveyor.TryDequeue(out element)) {
	MessageObject message = (MessageObject)element;
}
```

##### Compress float:
```c#
// Compress data
ushort compressedSpeed = HalfPrecision.Compress(speed);

// Decompress data
float speed = HalfPrecision.Decompress(compressedSpeed);
```

##### Compress vector:
```c#
// Create a new BoundedRange array for Vector3 position, each entry has bounds and precision
BoundedRange[] worldBounds = new BoundedRange[3];

worldBounds[0] = new BoundedRange(-50f, 50f, 0.05f); // X axis
worldBounds[1] = new BoundedRange(0f, 25f, 0.05f); // Y axis
worldBounds[2] = new BoundedRange(-50f, 50f, 0.05f); // Z axis

// Compress position data
CompressedVector3 compressedPosition = BoundedRange.Compress(position, worldBounds);

// Read compressed data
Console.WriteLine("Compressed position - X: " + compressedPosition.x + ", Y:" + compressedPosition.y + ", Z:" + compressedPosition.z);

// Decompress position data
Vector3 decompressedPosition = BoundedRange.Decompress(compressedPosition, worldBounds);
```

##### Compress quaternion:
```c#
// Compress rotation data
CompressedQuaternion compressedRotation = SmallestThree.Compress(rotation);

// Read compressed data
Console.WriteLine("Compressed rotation - M: " + compressedRotation.m + ", A:" + compressedRotation.a + ", B:" + compressedRotation.b + ", C:" + compressedRotation.c);

// Decompress rotation data
Quaternion rotation = SmallestThree.Decompress(compressedRotation);
```

##### Serialize/deserialize data:
```c#
// Create a new bit buffer with 128 chunks
BitBuffer data = new BitBuffer(128);

// Fill bit buffer and serialize data to a byte array
data.AddUInt(peer)
.AddString(name)
.AddBool(accelerated)
.AddUInt(speed)
.AddUInt(compressedPosition.x)
.AddUInt(compressedPosition.y)
.AddUInt(compressedPosition.z)
.AddByte(compressedRotation.m)
.AddInt(compressedRotation.a)
.AddInt(compressedRotation.b)
.AddInt(compressedRotation.c)
.ToArray(buffer); // The length of the byte array should be at least data.Length + 4 bytes for conversion

// Get data length in bit buffer for sending through the network
Console.WriteLine("Bit buffer length: " + data.Length);

// Reset bit buffer for further reusing
data.Clear();

// Deserialize data from a byte array
data.FromArray(buffer, length);

// Unload bit buffer in the same order
uint peer = data.ReadUInt();
string name = data.ReadString();
bool accelerated = data.ReadBool();
ushort speed = (ushort)data.ReadUInt();
CompressedVector3 position = new CompressedVector3(data.ReadUInt(), data.ReadUInt(), data.ReadUInt());
CompressedQuaternion rotation = new CompressedQuaternion(data.ReadByte(), (short)data.ReadInt(), (short)data.ReadInt(), (short)data.ReadInt());

// Check if bit buffer is fully unloaded
Console.WriteLine("Bit buffer is empty: " + data.IsFinished);
```
