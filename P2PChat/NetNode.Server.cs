using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

namespace P2PChat;

public partial class NetNode
{
    byte[] confirmation = new byte[confirmationSize];
    public void SendTo(Span<byte> data, IPAddress toAddress) => SendTo(data, toAddress, PacketType.None);

    ushort SendTo(Span<byte> data, IPAddress toAddress, PacketType packetType)
    {
        if (data.Length > MaxPacketSize)
        {
            SendFragmented(data, toAddress);
            return 0;
        }

    newId:
        ushort id = getId();
        if (id == 0)
            goto newId;
        begin:
        byte[] packArr = ArrayPool<byte>.Shared.Rent(data.Length + sizeof(ushort) + 1);
        Span<byte> pack = packArr;

        // Sets type of pack
        pack[0] = (byte)packetType;

        // Sets id to pack
        BinaryPrimitives.WriteUInt16LittleEndian(pack[1..], id);

        // Writes data to pack
        data.CopyTo(pack[sizeof(ushort)..]);

        // Send data
        server.SendTo(pack, new IPEndPoint(toAddress, port));

        ArrayPool<byte>.Shared.Return(packArr);

        var waitTime = TimeSpan.FromSeconds(3);

        server.ReceiveTimeout = (int)waitTime.TotalMilliseconds;

        try
        {
            server.Receive(confirmation);
            var conf = decomposeConfirmation(confirmation);
            if (conf.id == id)
            {
                if (!conf.suceed)
                    goto begin;
            }
            else
                goto begin;
        }
        catch
        {
            goto begin;
        }

        return id;
    }

    List<ushort> partIds = new List<ushort>(32);
    void SendFragmented(Span<byte> buffer, IPAddress toAddress)
    {
        ushort bigId = SendTo(new byte[1], toAddress, PacketType.BeginOrder);

        //var maxBuffer = ArrayPool<byte>.Shared.Rent(MaxPacketSize);
        partIds.Clear();

        int size = buffer.Length;
        int steps = size / MaxPacketSize;
        for(int i = 0; i < steps; i++)
        {
            int finalIndex = (i * MaxPacketSize) + MaxPacketSize;
            finalIndex = Math.Min(finalIndex, size);
            var pack = buffer[(i * MaxPacketSize)..finalIndex];

            var id = SendTo(pack, toAddress, PacketType.Part);

            partIds.Add(id);
        }

        var partIdsBufferArr = ArrayPool<byte>.Shared.Rent(sizeof(ushort) + (partIds.Count * sizeof(ushort)));
        BinaryPrimitives.WriteUInt16LittleEndian(partIdsBufferArr, bigId);

        Span<byte> partIdsBuffer = ((Span<byte>)partIdsBufferArr)[sizeof(ushort)..];
        for (int i = 0; i < partIds.Count; i++)
            BinaryPrimitives.WriteUInt16LittleEndian(partIdsBuffer[(i * sizeof(ushort))..], partIds[i]);
        
        SendTo(partIdsBuffer, toAddress, PacketType.FinishOrder);
    }

    ushort getId()
    {
        Span<byte> id = stackalloc byte[sizeof(ushort)];
        random.NextBytes(id);
        return BinaryPrimitives.ReadUInt16LittleEndian(id);
    }

    (ushort id, bool suceed) decomposeConfirmation(Span<byte> conf)
    {
        ushort id = BinaryPrimitives.ReadUInt16LittleEndian(conf);
        byte suceed = conf[sizeof(ushort)..][0];
        return (id, suceed == 0 ? false : true);
    }
}