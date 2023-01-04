This project is used to generate a lot of code. The code is mostly struct declarations—one for each type of packet that Garland sends between client and server.

Basically a glorified macro, if C# had macros.

See [`Input.cs`](Input.cs) for the string that contains info on every packet.

See [`Generate.cs`](Generate.cs) for the code that processes that string and generates [`Packets.generated.cs`](../common/Packets.generated.cs) from it.
