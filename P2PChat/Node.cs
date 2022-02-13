using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace P2PChat;

public class Node
{
    public NetNode Net;
    public int ConnectedPeers => peers.Count;
    List<Peer> peers = new List<Peer>(32);

    public Action<string> OnReceiveMessage;

    public void SendMessage(string message)
    {
        var pack = Encoding.Unicode.GetBytes(message);
        for (int i = 0; i < peers.Count; i++)
        {
            var peer = peers[i];

            Net.SendTo(pack, peer.Address);
        }
    }

    //List<ActiveConnection> connections = new List<ActiveConnection>(32);
    public void DoUpdateStep()
    {
        if (!Net.Pending)
            return;

        void processData(Span<byte> buffer)
        {
            //byte[] buffer = new byte[available];
            //while (!stream.CanRead)
            //    Thread.Sleep(50);
            //stream.Read(buffer);
            string message = Encoding.Unicode.GetString(buffer);
            OnReceiveMessage?.Invoke(message);
        }

        byte[] buffer = ArrayPool<byte>.Shared.Rent(1_000_000);
        int size = Net.Receive(buffer);
        Console.WriteLine($"Receiving external message of size {size}...");

        processData(((Span<byte>)buffer)[..size]);
    }

    public void Connect(string ip)
    {
        //var endPoint = new IPEndPoint(IPAddress.Parse(ip), port);

        //TcpClient client = new TcpClient();
        //client.Connect(endPoint);
        //peers.Add(new Peer(client, endPoint, null));
        //var http = new HttpClient();
        //http.BaseAddress = new Uri($"http://{ip}:{port}/");

        //peers.Add((http, $"http://{ip}:{port}/"));

        peers.Add(new Peer(IPAddress.Parse(ip)));
    }

    public void Shutdown()
    {
        //peers.ForEach(peer => peer.Client.Dispose());
        //Server?.Stop();
    }

    public Node(IPAddress selfIP, int port)
    {
        Net = new NetNode(selfIP, port);
    }
}
public record struct Peer(IPAddress Address);
//public record struct ActiveConnection(TcpClient Client, NetworkStream ActiveStream);