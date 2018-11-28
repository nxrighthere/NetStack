<p align="center"> 
  <img src="https://i.imgur.com/jD77417.png" alt="alt logo">
</p>

[![PayPal](https://drive.google.com/uc?id=1OQrtNBVJehNVxgPf6T6yX1wIysz1ElLR)](https://www.paypal.me/nxrighthere) [![Bountysource](https://drive.google.com/uc?id=19QRobscL8Ir2RL489IbVjcw3fULfWS_Q)](https://salt.bountysource.com/checkout/amount?team=nxrighthere) [![Discord](https://discordapp.com/api/guilds/515987760281288707/embed.png)](https://discord.gg/ceaWXVw)

NetStack is a lightweight toolset for creating multiplayer games.



Usage
--------
##### Thread-safe buffers pooling:
```c#
// Create a new buffers pool with a maximum size of 1024 bytes per array, 50 arrays per bucket
ArrayPool<byte> buffers = ArrayPool<byte>.Create(1024, 50);

// Rent buffer from the pool with a minimum size of 64 bytes
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

// Release pooled object
messages.Release(message);
```

##### Concurrent objects buffer:
```c#
// Create a new concurrent buffer limited to 8192 cells
ConcurrentBuffer conveyor = new ConcurrentBuffer(8192);

// Enqueue an object
if (!conveyor.TryEnqueue(message))
	Console.WriteLine("Buffer is full!");

// Dequeue all objects
object element;

while (conveyor.TryDequeue(out element)) {
	MessageObject message = (MessageObject)element;
}
```

##### Compress data:
```c#
// 

```

##### Serialize data:
```c#
// 

```

API reference
--------

### Buffers



### Compression



### Serialization



### Threading



### Unsafe

