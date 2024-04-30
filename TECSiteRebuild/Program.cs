using Newtonsoft.Json;
using System.IO.Pipes;
using System.Net;
using System.Text;
using TECsite;

namespace TECSite
{
    public class Program
    {
        #if DEBUG
        public static string domain = "https://localhost:727";
#else
        public static string domain = "https://thenergeticon.com";
#endif

        public static string authKey = Environment.GetEnvironmentVariable("TECKEY") ?? "NULL";

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
                    .UseUrls("https://localhost:727", "http://localhost:929") //set the addresses to listen on from provided IP
#else
                    .UseUrls("https://" + myIP + ":420", "http://" + myIP + ":6969") //set the addresses to listen on from provided IP
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

        public static (StreamString, NamedPipeClientStream) ConnectClient()
        {
            try
            {
                NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "TECDatabasePipe", PipeDirection.InOut, PipeOptions.WriteThrough);
                pipeClient.Connect();
                var ss = new StreamString(pipeClient);
                Console.WriteLine("Authorizing");
                ss.WriteString(authKey);
                if (ss.ReadString() != authKey) { ss.WriteString("Unauthorized server!"); throw new Exception("Unauthorized server connection attemted!"); }

                return (ss, pipeClient);
            }
            catch (Exception e) { Console.WriteLine($"\n{JsonConvert.SerializeObject(e)}\n"); return (null,null); }
        }

        public static void CheckResponse(StreamString ss)
        {
            if (ss.ReadString() != "READY") { throw new Exception("Server Error"); }
        }

        public static HttpContext AddNoCacheHeaders(HttpContext context)
        {
            context.Response.Headers.Add("Cache-Control", "no-cache, no-store");
            context.Response.Headers.Add("Expires", "-1");
            return context;
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

    public static class Html
    {
        public class Table : HtmlBase, IDisposable
        {
            public Table(StringBuilder sb, string classAttributes = "", string id = "", string style = "") : base(sb)
            {
                Append("<table");
                AddOptionalAttributes(classAttributes, id, "", "", style);
            }

            public void StartHead(string classAttributes = "", string id = "")
            {
                Append("<thead");
                AddOptionalAttributes(classAttributes, id);
            }

            public void EndHead()
            {
                Append("</thead>");
            }

            public void StartFoot(string classAttributes = "", string id = "")
            {
                Append("<tfoot");
                AddOptionalAttributes(classAttributes, id);
            }

            public void EndFoot()
            {
                Append("</tfoot>");
            }

            public void StartBody(string classAttributes = "", string id = "")
            {
                Append("<tbody");
                AddOptionalAttributes(classAttributes, id);
            }

            public void EndBody()
            {
                Append("</tbody>");
            }

            public void AddColgroup(int cols, bool smaller)
            {
                Append("<colgroup>");
                Append($"<col style=\"width: {(smaller ? 100 : 115)}px\">");
                for (int i = 0; i < cols; i++)
                { Append($"<col style=\"width: {(smaller ? 60 : 70)}px\">"); }
                Append("</colgroup>");
            }

            public void Dispose()
            {
                Append("</table>");
            }

            public Row AddRow(string classAttributes = "", string id = "", string style = "")
            {
                return new Row(GetBuilder(), classAttributes, id, style);
            }
        }

        public class Row : HtmlBase, IDisposable
        {

            public Row(StringBuilder sb, string classAttributes = "", string id = "", string style = "") : base(sb)
            {
                Append("<tr");
                AddOptionalAttributes(classAttributes, id, "", "", style);
            }
            public void Dispose()
            {
                Append("</tr>");
            }
            public void AddCell(string innerText, string classAttributes = "", string id = "", string colSpan = "", string rowSpan = "", string style = "", string eventID = "")
            {
                Append("<td");
                AddOptionalAttributes(classAttributes, id, colSpan, rowSpan, style); 
                if (!string.IsNullOrEmpty(eventID))
                    { Append($"<a class=\"{classAttributes}\" href=\"{Program.domain}/Events/Event/{eventID}\"/>"); }
                Append(innerText);
                Append("</td>");
            }
        }

        public abstract class HtmlBase
        {
            private StringBuilder _sb;

            protected HtmlBase(StringBuilder sb)
            {
                _sb = sb;
            }

            public StringBuilder GetBuilder()
            {
                return _sb;
            }

            protected void Append(string toAppend)
            {
                _sb.Append(toAppend);
            }

            protected void AddOptionalAttributes(string className = "", string id = "", string colSpan = "", string rowSpan = "", string style = "")
            {

                if (!string.IsNullOrEmpty(id))
                {
                    _sb.Append($" id=\"{id}\"");
                }
                if (!string.IsNullOrEmpty(className))
                {
                    _sb.Append($" class=\"{className}\"");
                }
                if (!string.IsNullOrEmpty(colSpan))
                {
                    _sb.Append($" colspan=\"{colSpan}\"");
                }
                if (!string.IsNullOrEmpty(rowSpan))
                {
                    _sb.Append($" rowspan=\"{rowSpan}\"");
                }
                if (!string.IsNullOrEmpty(style))
                {
                    _sb.Append($" style=\"{style}\"");
                }
                _sb.Append(">");
            }
        }
    }
}
