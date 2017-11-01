using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CalendarQuickstart
{
    class Program
    {
        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/calendar-dotnet-quickstart.json
        //kitos static string[] Scopes = { CalendarService.Scope.CalendarReadonly };
        static string[] Scopes = { CalendarService.Scope.Calendar };
        static string ApplicationName = "Google Calendar API .NET Quickstart";

        static void Main(string[] args)
        {
            DateTime dateFrom = new DateTime(2017, 8, 1, 0, 0, 0);
            DateTime dateTo = new DateTime(2017, 10, 1, 0, 0, 0);
            
            privateMain(dateFrom, dateTo);
        }

        private static void privateMain(DateTime dateFrom, DateTime dateTo)
        {
            TimeSpan ts = dateTo - dateFrom;
            int days = ts.Days;
            
            UserCredential credential;

            using (var stream =
                new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = System.Environment.GetFolderPath(
                    System.Environment.SpecialFolder.Personal);
                credPath = Path.Combine(credPath, ".credentials/calendar-dotnet-quickstart.json");

                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            // Create Google Calendar API service.
            var service = new CalendarService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName,
                });
           
            CalendarListResource.ListRequest requestListCalendar = service.CalendarList.List();

            CalendarList calendars = requestListCalendar.Execute();
            foreach (CalendarListEntry cal in calendars.Items)
            {
                Console.WriteLine("---------------------------");                
                Console.WriteLine("Calendar nombre: " + cal.Summary);
                if (cal.Summary.IndexOf("Kitos") >=0)
                {
                    Events events = ObtenerEventos(dateFrom, service, cal);                    
                    if (events.Items != null && events.Items.Count > 0)
                    {
                        foreach (var eventItem in events.Items)
                        {
                            ModificarEvento(service, cal, eventItem, days);
                        }
                    }
                    else
                    {
                        Console.WriteLine("No upcoming events found.");
                    }
                }
            }

            Console.Read();
        }

        
        private static Events ObtenerEventos(DateTime dateFrom, CalendarService service, CalendarListEntry cal)
        {
            // Define parameters of request.
            //EventsResource.ListRequest request = service.Events.List("primary");
            EventsResource.ListRequest request = service.Events.List(cal.Id);
            request.TimeMin = dateFrom; // DateTime.Now.AddDays(-10);
            request.TimeMax = dateFrom.AddDays(1);
            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.MaxResults = 100;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            // List events.
            Events events = request.Execute();
            return events;
        }

        private static void ModificarEvento(CalendarService service, CalendarListEntry cal, Event eventItem, int days)
        {
            string fechaHoraAntes = eventItem.Start.DateTime.ToString();

            if(eventItem.Start.DateTime.HasValue)
                eventItem.Start.DateTime = eventItem.Start.DateTime.Value.AddDays(days);
            else
            {
                DateTime dt = DateTime.Parse(eventItem.Start.Date);
                eventItem.Start.Date = dt.AddDays(days).Date.ToString();
            }

            if (eventItem.End.DateTime.HasValue)
                eventItem.End.DateTime = eventItem.End.DateTime.Value.AddDays(days);
            else
            {
                DateTime dt = DateTime.Parse(eventItem.End.Date);
                eventItem.End.Date = dt.AddDays(days).Date.ToString();
            }


            eventItem.End.DateTime = eventItem.End.DateTime.Value.AddDays(days);

            EventsResource.PatchRequest patchRequest = service.Events.Patch(eventItem, cal.Id, eventItem.Id);
            Event event2 = patchRequest.Execute();

            Console.WriteLine("{0}: ({1} a {2})", event2.Summary, fechaHoraAntes, event2.Start.DateTime.ToString());
        }

    }
}