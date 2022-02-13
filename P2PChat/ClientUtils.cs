using System.Net;
using System.Net.Sockets;

public static class ClientUtils
{
    static IPAddress? selfIPCache;
    public static IPAddress GetSelfIP()
    {
        if (selfIPCache != null) return selfIPCache;
        using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
        {
            socket.Connect("8.8.8.8", 65530);
            IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
            return selfIPCache = endPoint.Address;
        }
    }

    public static IPAddress GetSelfWANIPSync()
    {
        var wait = GetSelfWANIP();
        wait.Wait();
        return wait.Result;
    }

    static IPAddress selfWANIP;
    public static async Task<IPAddress> GetSelfWANIP()
    {
        if (selfWANIP != null) return selfWANIP;

        using HttpClient web = new HttpClient();
        string selfIP = await web.GetStringAsync("https://ipinfo.io/ip");
        selfWANIP = IPAddress.Parse(selfIP.Trim());

        return selfWANIP;
    }
}
