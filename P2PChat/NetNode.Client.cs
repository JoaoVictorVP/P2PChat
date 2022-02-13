using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

namespace P2PChat;

public partial class NetNode
{
    public int Receive(Span<byte> buffer)
    {
        var totalBufferArr = ArrayPool<byte>.Shared.Rent(MaxPacketSize + confirmationSize);
        Span<byte> totalBuffer = totalBufferArr;

        EndPoint from = new IPEndPoint(IPAddress.IPv6Any, port);
        server.ReceiveFrom(totalBuffer, ref from);

        PacketType type = (PacketType)totalBuffer[0];
        // Shifts buffer here
        totalBuffer = totalBuffer[1..];

        ushort id = BinaryPrimitives.ReadUInt16LittleEndian(totalBuffer);

        int size = totalBuffer.Length - sizeof(ushort);
        totalBuffer[sizeof(ushort)..].CopyTo(buffer);
        //buffer = totalBuffer[sizeof(ushort)..];

        var proof = confirmationProof(id, true);

        SendTo(proof, ((IPEndPoint)from).Address);

        ArrayPool<byte>.Shared.Return(totalBufferArr);

        switch(type)
        {
            case PacketType.BeginOrder:
                ReceiveFragmented(buffer, id, ref size);
                break;
            case PacketType.Part:
                SavePart(id, buffer);
                break;
            case PacketType.FinishOrder:
                Finish(buffer);
                break;
        }

        return size;
    }
    bool finished;
    int finishedSize;
    byte[] finishedBuffer;
    void ReceiveFragmented(Span<byte> buffer, ushort bigId, ref int size)
    {
        finished = false;

        var rbufferArr = ArrayPool<byte>.Shared.Rent(MaxPacketSize + sizeof(ushort) + 1);
        Span<byte> rbuffer = rbufferArr;

        while(!finished)
            Receive(rbuffer);
        ArrayPool<byte>.Shared.Return(rbufferArr);

        size = finishedSize;
        finishedBuffer.CopyTo(buffer);
    }


    Dictionary<ushort, byte[]> parts = new Dictionary<ushort, byte[]>(32);
    void SavePart(ushort id, Span<byte> part) => parts[id] = part.ToArray();

    void Finish(Span<byte> ordering)
    {
        var mount = new List<byte>(3200);

        int orderCount = ordering.Length / sizeof(ushort);

        for(int i = 0; i < orderCount; i++)
        {
            ushort partId = BinaryPrimitives.ReadUInt16LittleEndian(ordering[(i * sizeof(ushort))..]);
            mount.AddRange(parts[partId]);
        }

        parts.Clear();

        finished = true;
        finishedSize = mount.Count;
        finishedBuffer = mount.ToArray();
    }


    byte[] _conf = new byte[confirmationSize];
    Span<byte> confirmationProof(ushort id, bool suceed)
    {
        Span<byte> conf = _conf;
        BinaryPrimitives.WriteUInt16LittleEndian(conf, id);
        conf[sizeof(ushort)..][0] = (byte)(suceed ? 1 : 0);

        return conf;
    }
}