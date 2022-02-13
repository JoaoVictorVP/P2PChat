using P2PChat;
using System.Net;

Console.WriteLine("P2P Chat");

//var self = ClientUtils.GetSelfIP();
var self = IPAddress.Any;
var selfWAN = ClientUtils.GetSelfWANIPSync();
Console.WriteLine("The Server IP is " + selfWAN);
Console.WriteLine("Listening on... " + self);

const int serverPort = 55000;

var node = new Node(self, serverPort);

Task.Run(() =>
{
    while (true)
    {
        node.DoUpdateStep();
        Thread.Sleep(100);
    }
});

node.OnReceiveMessage += msg => Console.WriteLine(msg);

Console.WriteLine("What do you wanna do? [Connect to peer: C; Send Message: S]");

while (true)
{
    //node.DoUpdateStep();

    if (Console.KeyAvailable)
    {
        var key = Console.ReadKey(true);

        switch (key.Key)
        {
            case ConsoleKey.S:
                if (node.ConnectedPeers == 0)
                {
                    Console.WriteLine("Cannot send any messages because there are no peers connected...");
                    continue;
                }
                Console.WriteLine("Type your message here...");
                Console.Write("> ");
                string message = Console.ReadLine();

                node.SendMessage(message);
                break;
            case ConsoleKey.C:
                Console.WriteLine("Type your peer IP here...");
                Console.Write("> ");
                string ip = Console.ReadLine();

                node.Connect(ip);
                break;
        }
    }

    Thread.Sleep(10);
}