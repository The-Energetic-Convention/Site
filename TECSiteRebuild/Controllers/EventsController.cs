using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TECSite.Models;
using TECDataClasses;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace TECSite.Controllers
{
    public class EventsController : Controller
    {
        private readonly ILogger<EventsController> _logger;

        public EventInfo[] events = null;
        public EventInfo[] currentEvents = null;
        public EventInfo? @event = null;
        public string statusString = "BEFORE_CON";

        public EventsController(ILogger<EventsController> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Event(string? id)
        {
            events = null;
            @event = null;

            string domain = Request.Host.Host.Split('.')[0];
            switch (id)
            {
                case "Current":
                    Console.WriteLine($"Current Event page");
                    EventInfo[] eventslist = GetEvents();
                    // redirect to current event
                    return Redirect($"{Program.domain}/Events/Current");

                case null:
                    Console.WriteLine("Event List page");
                    events = GetEvents();

                    if (domain == "api")
                    {
                        return Json(events);
                    }

                    // Show the event list
                    return View();

                default:
                    try
                    {
                        int eventID = int.Parse(id);
                        Console.WriteLine($"Event {eventID} page");
                        @event = GetEvent(eventID);

                        if (domain == "api")
                        {
                            if (@event == null) { return new NotFoundResult(); }
                            return Json(@event);
                        }

                        if (@event == null) { RedirectToAction("Event", null); break; }
                        // Show event info
                        return View();
                    }
                    catch { }
                    // No event found redirect to events list
                    return RedirectToAction("Event", null);

            }
            return View();
        }

        public IActionResult Current(string? id)
        {
            events = null;
            @event = null;
            currentEvents = null;

            Console.WriteLine($"Current Event page");
            EventInfo[] eventslist = GetEvents();
            List<EventInfo> currentEventsList = [];
            EventInfo? lastEvent = null;
            bool inEvent = false;
            int foundEvents = 0; // how many events are running rn
            int i = 0;
            // figure out what events it is based on the current time
            foreach (EventInfo checkEvent in eventslist)
            {
                if (Program.mainNow > checkEvent.EventDate && Program.mainNow < checkEvent.EventEnd) // if we are in this event
                {
                    currentEventsList.Add(checkEvent);
                    foundEvents++;
                    inEvent = true;
                    i++;
                    continue;
                }   // if this isn't the first event to check, we aren't at this event, but we are past the last event
                else if (lastEvent != null && Program.mainNow < checkEvent.EventDate && Program.mainNow > lastEvent.EventEnd)
                {
                    statusString = "DAY_OVER";
                    inEvent = false;
                    break;
                }  // if this is the first event to check, and we aren't at it yet
                else if (lastEvent == null && Program.mainNow < checkEvent.EventDate)
                {
                    statusString = "BEFORE_CON";
                    inEvent = false;
                    break;
                }
                else
                {
                    i++;
                    continue;
                }
            }

            if (!inEvent && i == eventslist.Length) // if we aren't in an event, and we made it through the list
            {
                statusString = "AFTER_CON";
                inEvent = false;
            }

            string domain = Request.Host.Host.Split('.')[0];

            if (inEvent) // if we have found that we are in an event, set it
            { 
                currentEvents = [.. currentEventsList];

                if (domain == "api")
                {
                    if (@event == null) { return new NotFoundResult(); }
                    return new JsonResult(currentEvents);
                }

            }

            if (domain == "api")
            {
                if (@event == null) { return new NotFoundResult(); }
                return new ContentResult() { Content = statusString, ContentType = "text/plain", StatusCode = 200 };
            }

            return View();
        }

        EventInfo? GetEvent(int input)
        {
            var (ss, pipeClient) = Program.ConnectClient();

            // tell the server we are reading
            ss.WriteString("R");
            Program.CheckResponse(ss);

            // tell the server it is an event
            ss.WriteString("E");
            Program.CheckResponse(ss);

            string @event = input.ToString();
            Console.WriteLine($"Sending {@event}");
            ss.WriteString(@event);

            @event = ss.ReadString();
            Console.WriteLine($"Recieved {@event}");
            EventInfo toReturn = JsonConvert.DeserializeObject<EventInfo>(@event)!;
            if (ss.ReadString() != "SUCCESS") { throw new Exception("Server Error"); }
            pipeClient.Close();

            if (toReturn != null) { return toReturn; }
            return null;
        }

        EventInfo[] GetEvents()
        {
            // get events
            var (ss, pipeClient) = Program.ConnectClient();

            // tell the server we are reading
            ss.WriteString("R");
            Program.CheckResponse(ss);

            // tell the server it is an event
            ss.WriteString("E");
            Program.CheckResponse(ss);

            ss.WriteString("ALL");
            EventInfo[] events = JsonConvert.DeserializeObject<EventInfo[]>(ss.ReadString())!;
            if (ss.ReadString() != "SUCCESS") { throw new Exception("Server Error"); }
            pipeClient.Close();

            return events;
        }

        public IActionResult Hosting(string? id) { return View(); }
        public IActionResult Joining(string? id) { return View(); }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
