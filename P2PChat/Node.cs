using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace P2PChat;

public class Node
{
    //public HttpListener Server;
    public TcpListener Server;
    public int ConnectedPeers => peers.Count;
    //List<(HttpClient http, string ip)> peers = new List<(HttpClient, string)>(32);
    //List<TcpClient> peers = new List<TcpClient>(32);
    List<Peer> peers = new List<Peer>(32);

    public Action<string> OnReceiveMessage;

    public void SendMessage(string message)
    {
        var pack = Encoding.Unicode.GetBytes(message);
        for (int i = 0; i < peers.Count; i++)
        {
            var peer = peers[i];
            var peerClient = peer.Client;
            if (!peerClient.Connected)
            {
                int tries = 0;
            begin:
                tries++;
                if(tries >= 5)
                {
                    peers.RemoveAt(i);
                    continue;
                }
                peers[i] = peer with { Client = new TcpClient((IPEndPoint)peerClient.Client.LocalEndPoint) };

                if (!peerClient.Connected)
                {
                    Thread.Sleep(500);
                    goto begin;
                }
            }

            //peer.GetStream().Write(pack);

            if (peer.CurrentStream == null) peers[i] = peer = peer with { CurrentStream = peer.Client.GetStream() };

            var stream = peer.CurrentStream;

            stream.Write(pack);

            //var (peer, ip) = peers[i];

            //var responseAsync = peer.PostAsync(new Uri(ip), new ByteArrayContent(pack));

            //responseAsync.Wait();
        }
    }

    List<ActiveConnection> connections = new List<ActiveConnection>(32);
    public void DoUpdateStep()
    {
        void processData(NetworkStream stream, int available)
        {
            byte[] buffer = new byte[available];
            while (!stream.CanRead)
                Thread.Sleep(50);
            stream.Read(buffer);
            string message = Encoding.Unicode.GetString(buffer);
            OnReceiveMessage?.Invoke(message);
        }

        foreach(var (client, stream) in connections)
        {
            if (stream.CanRead && stream.DataAvailable)
                processData(stream, client.Available);
        }

        if (Server?.Pending() ?? false)
        {
            Console.WriteLine("Incoming Message Received...");

            var connection = Server.AcceptTcpClient();
            var stream = connection.GetStream();
            processData(stream, connection.Available);

            connections.Add(new ActiveConnection(connection, stream));
        }
        /*var contextAsync = Server.GetContextAsync();
        contextAsync.Wait(TimeSpan.FromSeconds(5));
        if(contextAsync.IsCompletedSuccessfully)
        {
            var context = contextAsync.Result;
            byte[] buffer = new byte[context.Request.ContentLength64];
            context.Request.InputStream.Read(buffer);

            string message = Encoding.Unicode.GetString(buffer);

            OnReceiveMessage?.Invoke(message);
        }*/
    }

    public void Connect(string ip, int port)
    {
        var endPoint = new IPEndPoint(IPAddress.Parse(ip), port);

        TcpClient client = new TcpClient();
        client.Connect(endPoint);
        peers.Add(new Peer(client, endPoint, null));
        //var http = new HttpClient();
        //http.BaseAddress = new Uri($"http://{ip}:{port}/");

        //peers.Add((http, $"http://{ip}:{port}/"));
    }

    public void Shutdown()
    {
        peers.ForEach(peer => peer.Client.Dispose());
        Server?.Stop();
    }

    public Node(IPAddress selfIP, int port, bool dontListen)
    {
        //Server = new HttpListener();
        //Server.Prefixes.Add($"http://*:{port}/");
        //Server.Start();
        if (dontListen)
            return;
        Server = new TcpListener(selfIP, port);
        Server.Start();
    }
}
public record struct Peer(TcpClient Client, IPEndPoint EndPoint, NetworkStream CurrentStream);
public record struct ActiveConnection(TcpClient Client, NetworkStream ActiveStream);