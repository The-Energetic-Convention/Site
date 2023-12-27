using System.IO.Pipes;
using System.Net;
using System.Text;
using TECsite;

namespace TECSite
{
    public class Program
    {
        #if DEBUG
        public static string domain = "https://localhost:443";
#else
        public static string domain = "https://thenergeticon.com";
#endif

        public static string authKey = Environment.GetEnvironmentVariable("TECKEY") ?? "NULL";
        public static NamedPipeClientStream pipeClient;

        public static DateTime mainNow = DateTime.Now;
        public static TimeSpan fiveMin = mainNow.AddMinutes(5) - mainNow;

        public static int maxInstances = 5;

        /// <summary>
        /// Used to create a configured <see cref="IWebHostBuilder"/>
        /// </summary>
        /// <param name="args">unused, put in args from main func var</param>
        /// <param name="myIP">The local IP address to listen on</param>
        /// <param name="root">The root directory of the app</param>
        /// <returns>A configured <see cref="IWebHostBuilder"/></returns>
        public static IHostBuilder CreateHostBuilder(string[] args, string myIP, string root)
        {
            //the web host builder to be returned
            var host = new HostBuilder()
                //configure some logging to see whats going on inside, could be commented out later ig
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                    logging.AddEventLog();
                    logging.SetMinimumLevel(LogLevel.Trace); //write EVERYTHING, im trying to get stuff figured out
                })
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder.UseKestrel()
#if DEBUG
                    .UseUrls("https://localhost:443", "http://localhost:80") //set the addresses to listen on from provided IP
#else
                    .UseUrls("https://" + myIP + ":443", "http://" + myIP + ":80") //set the addresses to listen on from provided IP
#endif
                    .CaptureStartupErrors(true) //Capture the startup errors if something happens ig
                    //.UseIISIntegration() //IDK lol
                    .UseStartup<Startup>(); //required ig, use the startup for stuff
                })
                .UseContentRoot(root); //set the content root

            //and return the host builder
            return host;
        }

        public static async Task Main(string[] args)
        {
            var root = Directory.GetCurrentDirectory();
            string hostName = Dns.GetHostName(); // Retrive the Name of HOST computer
            Console.WriteLine(hostName);

            // Get the IPs of the computer, and grab the last one, which seems to be the correct IPv4 for the computer
            IPAddress[] myIPs = Dns.GetHostEntry(hostName).AddressList;
            string myIP = myIPs[myIPs.Length - 1].ToString();
            Console.WriteLine("My IP Address is :" + myIP);

            //var builder = WebApplication.CreateBuilder(args);
            var builder = CreateHostBuilder(args, myIP, root);

            var app = builder.Build();

            //not sure what this is needed for, but keeping just in case ig
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
            }

            try
            {
                app.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex); //catch any exceptions that happen possibly
            }


            int waitime = 0;
            //and finally keep the current time updated every half second
            while (true)
            {
                mainNow = DateTime.Now;
                Console.WriteLine(mainNow.ToString());
                if (waitime < 6) //wait 3 seconds before checking the time, to make sure it doesnt shut down upon startup
                {
                    waitime++;
                }
                else if (mainNow.ToLongTimeString() == "12:00:00 AM") //restart the site every midnight. use windows task scheduler to start a new instance while this on shuts down
                {
                    Console.WriteLine("shutoff");//Environment.Exit(0);
                }
                Thread.Sleep(500);
            }
        }

        public static StreamString ConnectClient()
        {
            try
            {
                pipeClient = new NamedPipeClientStream(".", "TECDatabasePipe", PipeDirection.InOut, PipeOptions.None);
                pipeClient.Connect();
                var ss = new StreamString(pipeClient);
                Console.WriteLine("Authorizing");
                ss.WriteString(authKey);
                if (ss.ReadString() != authKey) { ss.WriteString("Unauthorized server!"); throw new Exception("Unauthorized server connection attemted!"); }

                return ss;
            }
            catch (Exception e) { Console.WriteLine(e.Message); return null; }
        }

        public static void CheckResponse(StreamString ss)
        {
            if (ss.ReadString() != "READY") { throw new Exception("Server Error"); }
        }
    }

    public class StreamString
    {
        private Stream ioStream;
        private UnicodeEncoding streamEncoding;

        public StreamString(Stream ioStream)
        {
            this.ioStream = ioStream;
            streamEncoding = new UnicodeEncoding();
        }

        public string ReadString()
        {
            int i = 0;
        tryread:
            int len;
            len = ioStream.ReadByte() * 256;
            len += ioStream.ReadByte();
            Thread.Sleep(100);
            if (len < 0 && i < 10) { i++; goto tryread; }
            var inBuffer = new byte[len];
            ioStream.Read(inBuffer, 0, len);

            return streamEncoding.GetString(inBuffer);
        }

        public int WriteString(string outString)
        {
            byte[] outBuffer = streamEncoding.GetBytes(outString);
            int len = outBuffer.Length;
            if (len > UInt16.MaxValue)
            {
                len = (int)UInt16.MaxValue;
            }
            ioStream.WriteByte((byte)(len / 256));
            ioStream.WriteByte((byte)(len & 255));
            ioStream.Write(outBuffer, 0, len);
            ioStream.Flush();

            return outBuffer.Length + 2;
        }
    }
}
