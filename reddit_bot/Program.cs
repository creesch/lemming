using System;
using System.Configuration;
using System.Linq;
using RedditSharp;
using ChatSharp;
using System.Net;
using System.Net.NetworkInformation;

namespace reddit_bot
{
    class Program
    {

        static void Main()
        {

            IrcClient client;
            
           client = ircConnection.ConnectToIrc();

            client.RawMessageRecieved += (s, e) =>
            {
                if (e.Message.Contains("ERROR: Closing link"))
                {
                    client.Quit();
                    client = ircConnection.ReconnectToIrc(client);
                }


            };

            client.NetworkError += (s, e) =>
            {
                Console.WriteLine(" ");
                Console.WriteLine("SocketError Received:");
                Console.WriteLine(e.SocketError);
                Console.WriteLine("Attempting to reconnect.");
                Console.WriteLine(" ");


                client = ircConnection.ReconnectToIrc(client);
            };

            client.ChannelMessageRecieved += (s, e) =>
            {
                var channel = client.Channels[e.PrivateMessage.Source];
                Console.WriteLine(channel.Name);
                Console.WriteLine(e.PrivateMessage.Message);
            };

            //Reddit reddit = redditConnection.ConnectToReddit();

            //var subreddit = reddit.GetSubreddit("/r/history");

            //foreach (var post in subreddit.New.Take(1))
            //{
            //    Console.WriteLine("latest message in /r/history: " + post.Title, "#test123");
            //}

            Console.ReadKey();
        }


    }

    // Manage reddit API stuff in here
    class redditConnection
    {
        public static Reddit ConnectToReddit()
        {
            WebAgent.UserAgent = "test experiment (by /u/creesch)";
            WebAgent.RateLimit = WebAgent.RateLimitMode.Burst;

            string ClientId = ConfigurationManager.AppSettings["ClientId"];
            string ClientSecret = ConfigurationManager.AppSettings["ClientSecret"];
            string RediretURI = ConfigurationManager.AppSettings["RediretURI"];
            string UserName = ConfigurationManager.AppSettings["UserName"];
            string UserPassword = ConfigurationManager.AppSettings["UserPassword"];

            AuthProvider ap = new AuthProvider(ClientId, ClientSecret, RediretURI);
            string AccessToken = ap.GetOAuthToken(UserName, UserPassword);

            Reddit reddit = new Reddit(AccessToken);

            return reddit;

        }
    }

    // Deal with irc related stuff in here. 
    class ircConnection
    {

        public static IrcClient ReconnectToIrc(IrcClient client)
        {
            bool networkAvailable = NetworkInterface.GetIsNetworkAvailable();
            bool hostAvailable = false;

            
            // First check if the network is actually alright. 
            while (networkAvailable.Equals(false))
            {
                networkAvailable = NetworkInterface.GetIsNetworkAvailable();

                // Network not available, trying again in 5 seconds. 
                Console.WriteLine("Network not available, trying again in 5 seconds.");
                System.Threading.Thread.Sleep(5000);
            }


            // Then check if the host is available
            while (hostAvailable.Equals(false))
            {
                try
                {
                    var ping = new Ping();
                    var reply = ping.Send("irc.snoonet.org", 60 * 1000); // 1 minute time out (in ms)

                    Console.WriteLine(reply.Status + "test");

                    if (reply.Status.Equals(IPStatus.Success))
                    {
                        Console.WriteLine("Host is back!");
                        hostAvailable = true;
                    }
                    else
                    {
                        Console.WriteLine("Host is not available, trying again in 5 seconds");
                        System.Threading.Thread.Sleep(5000);
                    }


                }
                catch (PingException pe)
                {
                    if (pe.InnerException.Equals("No such host is known"))
                    {
                        Console.WriteLine("Host is unknown, trying again in 5 seconds");
                        System.Threading.Thread.Sleep(5000);
                    }
                }
            }

            // With that out of the way we can attempt to reconnect. 
            client.ConnectAsync();
            return client;
        }
        // Connect to irc and return IrcClient to the method that called it so the functions stay available. 
        public static IrcClient ConnectToIrc()
        {

            string IrcNick = ConfigurationManager.AppSettings["IrcNick"];
            string IrcPass = ConfigurationManager.AppSettings["IrcPass"];

            var client = new IrcClient("irc.snoonet.org", new IrcUser(IrcNick, IrcNick, IrcPass));

            bool nickInUse = false;
            
            client.NickInUse += (s, e) =>
            {
                // e.DoNotHandle = true;
                Console.WriteLine("That is my name!");
                nickInUse = true;


            };
            
            client.RawMessageRecieved += (s, e) =>
            {
                Console.WriteLine(e.Message);

                if (e.Message.Contains("logged in as"))
                {
                    Console.WriteLine(client.Channels);
                    
                    
                    client.JoinChannel("#test123");

                    var channels = client.Channels;
                    Console.WriteLine(channels);

                }

                if (e.Message.Contains("is now your displayed host") && nickInUse == true)
                {
                    client.SendMessage("GHOST " + IrcNick + " " + IrcPass, "NickServ");
                }

            };

            client.ConnectAsync();

            return client;
        }
    }
}
