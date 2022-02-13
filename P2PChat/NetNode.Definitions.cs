using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

namespace P2PChat;

public partial class NetNode
{
    const ushort MaxPacketSize = 65_527 - confirmationSize - /* id */ sizeof(ushort) - /* type */ 1;

    readonly int port;
    Socket server;

    const int confirmationSize = sizeof(ushort) + sizeof(byte);

    Random random = new Random();
    public NetNode(IPAddress selfIP, int port)
    {
        this.port = port;

        server = new Socket(SocketType.Dgram, ProtocolType.Udp);

        server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, 0);

        /*IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());*/
        IPEndPoint endPoint = new IPEndPoint(selfIP, port);
        server.Bind(endPoint);
    }

    public enum PacketType : byte
    {
        None = 0, // A normal packet here
        Part = 1, // A packet part, identify that this is a part of a more complete big packet
        BeginOrder = 2, // Starts a big packet order
        
        FinishOrder = 3 // A big packet finish order, identify to receiver that now the data can be mounted and returned to caller
    }
}