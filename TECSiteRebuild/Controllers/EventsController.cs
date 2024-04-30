using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TECSite.Models;
using TECDataClasses;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
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

                case "":
                case null:
                    Console.WriteLine("\nEvent List page\n");
                    events = GetEvents();

                    if (domain == "api")
                    {
                        return Json(events);
                    }

                    // construct the html tables for each day

                    /* example table building
                    StringBuilder sb = new StringBuilder();
                    using (Html.Table table = new Html.Table(sb, id: "some-id"))
                    {
                        table.StartHead();
                        using (var thead = table.AddRow())
                        {
                            thead.AddCell("Category Description");
                            thead.AddCell("Item Description");
                            thead.AddCell("Due Date");
                            thead.AddCell("Amount Budgeted");
                            thead.AddCell("Amount Remaining");
                        }
                        table.EndHead();
                        table.StartBody();
                        foreach (var alert in alertsForUser)
                        {
                            using (var tr = table.AddRow(classAttributes: "someattributes"))
                            {
                                tr.AddCell(alert.ExtendedInfo.CategoryDescription);
                                tr.AddCell(alert.ExtendedInfo.ItemDescription);
                                tr.AddCell(alert.ExtendedInfo.DueDate.ToShortDateString());
                                tr.AddCell(alert.ExtendedInfo.AmountBudgeted.ToString("C"));
                                tr.AddCell(alert.ExtendedInfo.ItemRemaining.ToString("C"));
                            }
                        }
                        table.EndBody();
                    } */

                    // list for the events of each day, with event id, name, start, end, type, location, and rating
                    List<EventInfo> day1 = [];
                    List<EventInfo> day2 = [];
                    List<EventInfo> day3 = [];

                    // populate the dicts with the events
                    bool dayChanged = false;
                    int i = 0;
                    DateTime? day1Start = events[i].EventDate;
                    while (!dayChanged)
                    {
                        try
                        {
                            dayChanged = day1[i - 1].EventDate.Day != events[i].EventDate.Day;
                            if (dayChanged) { break; }
                        }
                        catch { }

                        day1.Add(events[i]);
                        i++;
                    }
                    DateTime? day1End = events[i-1].EventEnd;

                    dayChanged = false;
                    int j = i; // offset into events list
                    i = 0;
                    DateTime? day2Start = events[i+j].EventDate;
                    while (!dayChanged)
                    {
                        try
                        {
                            dayChanged = day2[i - 1].EventDate.Day != events[i + j].EventDate.Day;
                            if (dayChanged) { break; }
                        }
                        catch { }

                        day2.Add(events[i + j]);
                        i++;
                    }
                    DateTime? day2End = events[(i - 1) + j].EventEnd;

                    dayChanged = false;
                    j += i; // offset into events list
                    i = 0;
                    DateTime? day3Start = events[i + j].EventDate;
                    while (!dayChanged)
                    {
                        if ((i+j) == events.Length) { break; }
                        try
                        {
                            dayChanged = day3[i - 1].EventDate.Day != events[i + j].EventDate.Day;
                            if (dayChanged) { break; }
                        }
                        catch { }

                        day3.Add(events[i + j]);
                        i++;
                    }
                    DateTime? day3End = events[(i - 1) + j].EventEnd;

                    // build the html of each day
                    string day1Table = DayTableBuilder(day1, day1Start, day1End, true);
                    string day2Table = DayTableBuilder(day2, day2Start, day2End, false);
                    string day3Table = DayTableBuilder(day3, day3Start, day3End, false);

                    // Show the event list
                    ViewData.Add("day1Table", day1Table);
                    ViewData.Add("day2Table", day2Table);
                    ViewData.Add("day3Table", day3Table);
                    ViewData.Add("day1Date", day1Start.Value.ToString("dddd, MMMM dd:"));
                    ViewData.Add("day2Date", day2Start.Value.ToString("dddd, MMMM dd:"));
                    ViewData.Add("day3Date", day3Start.Value.ToString("dddd, MMMM dd:"));
                    ViewData.Add("events", events);
                    ViewData.Add("event", @event);
                    ViewData.Add("status", statusString);
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
                        ViewData.Add("events", events);
                        ViewData.Add("event", @event);
                        ViewData.Add("status", statusString);
                        return View();
                    }
                    catch { }
                    // No event found redirect to events list
                    return RedirectToAction("Event", null);

            }
            return View();
        }

        string DayTableBuilder(List<EventInfo> day, DateTime? dayStart, DateTime? dayEnd, bool firstday)
        {
            StringBuilder daybuilder = new StringBuilder();
            using (Html.Table table = new(daybuilder, "tg", "", "undefined;table-layout: fixed;"))
            {
                int columns = 0;
                table.StartHead();
                using (var thead = table.AddRow())
                {
                    // add cell for each 30 min of the day
                    int cells = (day[day.Count - 1].EventEnd.Hour - day[0].EventDate.Hour) * 2;
                    thead.AddCell("LOCATION", "tableLocation", "", "", "", $"font-size: {(cells < 15 ? 12 : 10)}px");
                    DateTime start = day[0].EventDate;
                    for (int i = 0; i < cells; i++)
                    {
                        thead.AddCell(start.AddMinutes(30 * i).ToString("hh:mm tt"), "tableHeader", "", "", "", $"font-size: {(firstday ? 12 : 8)}px");
                        columns++;
                    }
                }
                table.EndHead();

                table.StartBody();
                // construct the main part
                // somehow detect when a cell should take multiple rows, such as opening and closing, when in multiple places adjacent on the schedule to each other
                // also detecting how many cells each should take up in a row, 1 for half hour, 2 for hour, etc.
                List<(int id, string name, int slots, int rowsTogether, int[] rows, EventType type, EventRating rating, DateTime start, DateTime end)> day2Slots = [];
                foreach (var @event in day)
                {
                    int slots = (int)((@event.EventEnd - @event.EventDate).TotalMinutes / 30);
                    string[] locations = @event.EventLocation.Split("+");
                    int rowsTogether = 1;
                    List<int> rows = [];
                    foreach (string location in locations)
                    {
                        switch (location)
                        {
                            case "VRC":
                                rows.Add(0);
                                break;
                            case "VC":
                                rows.Add(1);
                                if (rows.Contains(0)) { rowsTogether++; }
                                break;
                            case "OTHER":
                                rows.Add(2);
                                if (rows.Contains(1)) { rowsTogether++; }
                                break;
                            case "GAME":
                                rows.Add(3);
                                if (rows.Contains(2)) { rowsTogether++; }
                                break;
                            default:
                                break;
                        }
                    }
                    day2Slots.Add((@event.EventNumber, @event.EventName, slots, rowsTogether, rows.ToArray(), @event.EventType, @event.EventRating, @event.EventDate, @event.EventEnd));
                }

                using (var vrcRow = table.AddRow("", "", $"line-height: {(firstday ? 50 : 30)}px"))
                {
                    vrcRow.AddCell("VRCHAT", "sidebarOdd", "", "", "", $"font-size: {(firstday ? 16 : 12)}px");
                    List<(int id, string name, int slots, int rowsTogether, int[] rows, EventType type, EventRating rating, DateTime start, DateTime end)> VRCCells = [];
                    day2Slots.ForEach((cell) => { if (cell.rows.Contains(0)) { VRCCells.Add(cell); } });
                    (int id, string name, int slots, int rowsTogether, int[] rows, EventType type, EventRating rating, DateTime start, DateTime end)? prevcell = null;
                    foreach (var cell in VRCCells)
                    {
                        if (prevcell != null)
                        {
                            if ((cell.start - prevcell.Value.end).TotalMinutes > 5)
                            {
                                vrcRow.AddCell("", "rowOdd", "", $"{(int)((cell.start - prevcell.Value.end).TotalMinutes / 30)}");
                            }
                        }
                        else if (prevcell == null && (cell.start - dayStart).Value.TotalMinutes > 5)
                        {
                            vrcRow.AddCell("", "rowOdd", "", $"{(int)((cell.start - dayStart).Value.TotalMinutes / 30)}");
                        }
                        vrcRow.AddCell(AltName(cell.name), cell.type.ToString() + " " + cell.rating.ToString(), "", $"{cell.slots}", $"{cell.rowsTogether}", $"font-size: {CalcSize(cell.name, firstday)}px; line-height: {(firstday ? 1.6 : 2)}", $"{cell.id}");
                        prevcell = cell;
                    }
                    if (!prevcell.HasValue) { vrcRow.AddCell("", "rowOdd", "", $"{(int)((dayEnd - dayStart).Value.TotalMinutes / 30)}"); }
                    else if ((dayEnd - prevcell!.Value.end).Value.TotalMinutes > 5)
                    {
                        double cellsFloat = (dayEnd - prevcell!.Value.end).Value.TotalMinutes / 30;
                        int cellsInt = (int)cellsFloat;
                        int cells = cellsFloat > cellsInt + 0.5 ? cellsInt + 1 : cellsInt;
                        vrcRow.AddCell("", "rowOdd", "", $"{cells}");
                    }
                }

                using (var discordRow = table.AddRow("", "", $"line-height: {(firstday ? 50 : 30)}px"))
                {
                    discordRow.AddCell("DISCORD", "sidebarEven", "", "", "", $"font-size: {(firstday ? 16 : 12)}px");
                    List<(int id, string name, int slots, int rowsTogether, int[] rows, EventType type, EventRating rating, DateTime start, DateTime end)> DiscordCells = [];
                    day2Slots.ForEach((cell) => { if (cell.rows.Contains(1)) { DiscordCells.Add(cell); } });
                    (int id, string name, int slots, int rowsTogether, int[] rows, EventType type, EventRating rating, DateTime start, DateTime end)? prevcell = null;
                    foreach (var cell in DiscordCells)
                    {
                        if (prevcell != null)
                        {
                            if ((cell.start - prevcell.Value.end).TotalMinutes > 5)
                            {
                                discordRow.AddCell("", "rowEven", "", $"{(int)((cell.start - prevcell.Value.end).TotalMinutes / 30)}");
                            }
                        }
                        else if (prevcell == null && (cell.start - dayStart).Value.TotalMinutes > 5)
                        {
                            discordRow.AddCell("", "rowEven", "", $"{(int)((cell.start - dayStart).Value.TotalMinutes / 30)}");
                        }
                        if (!cell.rows.Contains(0)) { discordRow.AddCell(cell.name, cell.type.ToString() + " " + cell.rating.ToString(), "", $"{cell.slots}", $"{cell.rowsTogether}", $"font-size: {CalcSize(cell.name, firstday)}px; line-height: {(firstday ? 1.6 : 2)}", $"{cell.id}"); }
                        prevcell = cell;
                    }
                    if (!prevcell.HasValue) { discordRow.AddCell("", "rowEven", "", $"{(int)((dayEnd - dayStart).Value.TotalMinutes / 30)}"); }
                    else if ((dayEnd - prevcell!.Value.end).Value.TotalMinutes > 5)
                    {
                        double cellsFloat = (dayEnd - prevcell!.Value.end).Value.TotalMinutes / 30;
                        int cellsInt = (int)cellsFloat;
                        int cells = cellsFloat > cellsInt + 0.5 ? cellsInt + 1 : cellsInt;
                        discordRow.AddCell("", "rowEven", "", $"{cells}");
                    }
                }

                using (var otherRow = table.AddRow("", "", $"line-height: {(firstday ? 50 : 30)}px"))
                {
                    otherRow.AddCell("OTHER", "sidebarOdd", "", "", "", $"font-size: {(firstday ? 16 : 12)}px");
                    List<(int id, string name, int slots, int rowsTogether, int[] rows, EventType type, EventRating rating, DateTime start, DateTime end)> OtherCells = [];
                    day2Slots.ForEach((cell) => { if (cell.rows.Contains(2)) { OtherCells.Add(cell); } });
                    (int id, string name, int slots, int rowsTogether, int[] rows, EventType type, EventRating rating, DateTime start, DateTime end)? prevcell = null;
                    foreach (var cell in OtherCells)
                    {
                        if (prevcell != null)
                        {
                            if ((cell.start - prevcell.Value.end).TotalMinutes > 5)
                            {
                                otherRow.AddCell("", "rowOdd", "", $"{(int)((cell.start - prevcell.Value.end).TotalMinutes / 30)}");
                            }
                        }
                        else if (prevcell == null && (cell.start - dayStart).Value.TotalMinutes > 5)
                        {
                            otherRow.AddCell("", "rowOdd", "", $"{(int)((cell.start - dayStart).Value.TotalMinutes / 30)}");
                        }
                        if (!cell.rows.Contains(1)) { otherRow.AddCell(AltName(cell.name), cell.type.ToString() + " " + cell.rating.ToString(), "", $"{cell.slots}", $"{cell.rowsTogether}", $"font-size: {CalcSize(cell.name, firstday)}px; line-height: {(firstday ? 1.6 : 2)}", $"{cell.id}"); }
                        prevcell = cell;
                    }
                    if (!prevcell.HasValue) { otherRow.AddCell("", "rowOdd", "", $"{(int)((dayEnd - dayStart).Value.TotalMinutes / 30)}"); }
                    else if ((dayEnd - prevcell!.Value.end).Value.TotalMinutes > 5)
                    {
                        double cellsFloat = (dayEnd - prevcell!.Value.end).Value.TotalMinutes / 30;
                        int cellsInt = (int)cellsFloat;
                        int cells = cellsFloat > cellsInt + 0.5 ? cellsInt + 1 : cellsInt;
                        otherRow.AddCell("", "rowOdd", "", $"{cells}");
                    }
                }

                using (var otherGamesRow = table.AddRow("", "", $"line-height: {(firstday ? 25 : 15)}px"))
                {
                    otherGamesRow.AddCell("OTHER GAMES", "sidebarEven", "", "", "", $"font-size: {(firstday ? 16 : 12)}px");
                    List<(int id, string name, int slots, int rowsTogether, int[] rows, EventType type, EventRating rating, DateTime start, DateTime end)> OtherGamesCells = [];
                    day2Slots.ForEach((cell) => { if (cell.rows.Contains(3)) { OtherGamesCells.Add(cell); } });
                    (int id, string name, int slots, int rowsTogether, int[] rows, EventType type, EventRating rating, DateTime start, DateTime end)? prevcell = null;
                    foreach (var cell in OtherGamesCells)
                    {
                        if (prevcell != null)
                        {
                            if ((cell.start - prevcell.Value.end).TotalMinutes > 5)
                            {
                                otherGamesRow.AddCell("", "rowEven", "", $"{(int)((cell.start - prevcell.Value.end).TotalMinutes / 30)}");
                            }
                        }
                        else if (prevcell == null && (cell.start - dayStart).Value.TotalMinutes > 5)
                        {
                            otherGamesRow.AddCell("", "rowEven", "", $"{(int)((cell.start - dayStart).Value.TotalMinutes / 30)}");
                        }
                        if (!cell.rows.Contains(2)) { otherGamesRow.AddCell(cell.name, cell.type.ToString() + " " + cell.rating.ToString(), "", $"{cell.slots}", $"{cell.rowsTogether}", $"font-size: {CalcSize(cell.name, firstday)}px; line-height: {(firstday ? 1.6 : 2)}", $"{cell.id}"); }
                        prevcell = cell;
                    }
                    if (!prevcell.HasValue) { otherGamesRow.AddCell("", "rowEven", "", $"{(int)((dayEnd - dayStart).Value.TotalMinutes / 30)}"); }
                    else if ((dayEnd - prevcell!.Value.end).Value.TotalMinutes > 5)
                    {
                        double cellsFloat = (dayEnd - prevcell!.Value.end).Value.TotalMinutes / 30;
                        int cellsInt = (int)cellsFloat;
                        int cells = cellsFloat > cellsInt + 0.5 ? cellsInt + 1 : cellsInt;
                        otherGamesRow.AddCell("", "rowEven", "", $"{cells}");
                    }
                }

                table.EndBody();
                table.AddColgroup(columns - 1, !firstday);
            }
            return daybuilder.ToString();
        }

        string AltName(string eventName) => (eventName.StartsWith("Opening") ? "Opening" : eventName.StartsWith("Closing") ? "Closing" : eventName.StartsWith("open time slot") ? "Open Slot" : eventName);
        float CalcMargin(string eventName) => ((0.5f / (AltName(eventName).Length)) * 100);
        float CalcSize(string eventName, bool firstday) 
        {
            int x = AltName(eventName).Length;

            if (x > 14) { return (firstday ? 13 : 9); }
            else if (x < 10) { return (firstday ? 20 : 16); }
            return (firstday ? 16 : 12);

            /*
            float fontSize = -((x + (-8)) * (x + (-8))) / (1 * (((x + (-8)) * (x + (-8))) - (x + (-8))) + 8) * 6.9f + 20;
            Console.WriteLine($"\nName: {AltName(eventName)}, Length: {AltName(eventName).Length}, with Font Size: {fontSize}\n");
            return fontSize > 20 ? 20 : fontSize; */
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
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
                    lastEvent = checkEvent;
                    i++;
                    continue;
                }   // if this isn't the first event to check, we aren't at this event, but we are past the last event
                else if (lastEvent != null && Program.mainNow < checkEvent.EventDate && Program.mainNow > lastEvent.EventEnd)
                {
                    statusString = "DAY_OVER";
                    inEvent = false;
                    lastEvent = checkEvent;
                    break;
                }  // if this is the first event to check, and we aren't at it yet
                else if (lastEvent == null && Program.mainNow < checkEvent.EventDate)
                {
                    statusString = "BEFORE_CON";
                    inEvent = false;
                    lastEvent = checkEvent;
                    break;
                }
                else
                {
                    i++;
                    lastEvent = checkEvent;
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

            ViewData.Add("currentevents", currentEvents);
            ViewData.Add("status", statusString);
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
            ss.WriteString(@event);

            @event = ss.ReadString();
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Hosting(string? id) { return View(); }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Joining(string? id) { return View(); }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
