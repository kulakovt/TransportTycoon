using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TransportTycoon
{
    static class Logger
    {
        static string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory ?? Assembly.GetCallingAssembly().Location, "Logs");
        static string LogFile = Path.Combine(LogDirectory, DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".log");
        static JsonSerializerOptions Options;

        static Logger()
        {
            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }

            Options = new JsonSerializerOptions
            {
                IgnoreNullValues = true
            };
        }

        public static void Write(string messageType, int time, int transportId, TransportType type, Location location, Location? destination, int? duration, IReadOnlyList<Cargo> store)
        {
            var departEvent = new TransportEvent
            {
                Type = messageType,
                Time = time,
                TransportId = transportId,
                Kind = type.ToString().ToUpper(),
                Location = location.ToString().ToUpper(),
                Destination = destination?.ToString().ToUpper(),
                Duration = duration
            };

            foreach (var cargo in store)
            {
                departEvent.Cargo.Add(new TransportCargo
                {
                    Id = cargo.Id,
                    Origin = cargo.Origin.ToString().ToUpper(),
                    Destination = cargo.Destination.ToString().ToUpper()
                });
            }

            Write(departEvent);
        }

        static void Write(TransportEvent message)
        {
            var entry = JsonSerializer.Serialize(message, Options);
            File.AppendAllText(LogFile, entry + Environment.NewLine);
        }
    }

    class TransportEvent
    {
        public TransportEvent()
        {
            Cargo = new List<TransportCargo>();
        }

        /// <summary>
        /// Type of log entry: DEPART, ARRIVE, LOAD or UNLOAD
        /// </summary>
        [JsonPropertyName("event")]
        public string? Type { get; set; }

        /// <summary>
        /// Time in hours
        /// </summary>
        [JsonPropertyName("time")]
        public int? Time { get; set; }

        /// <summary>
        /// Unique transport id
        /// </summary>
        [JsonPropertyName("transport_id")]
        public int? TransportId { get; set; }

        /// <summary>
        /// Transport kind: TRUCK or SHIP
        /// </summary>
        [JsonPropertyName("kind")]
        public string? Kind { get; set; }

        /// <summary>
        /// Current location
        /// </summary>
        [JsonPropertyName("location")]
        public string? Location { get; set; }

        /// <summary>
        /// Destination (only for DEPART events)
        /// </summary>
        [JsonPropertyName("destination")]
        public string? Destination { get; set; }

        [JsonPropertyName("duration")]
        public int? Duration { get; set; }

        /// <summary>
        /// Array of cargo being carried
        /// </summary>
        [JsonPropertyName("cargo")]
        public List<TransportCargo> Cargo { get; private set; }
    }

    class TransportCargo
    {
        /// <summary>
        /// Unique cargo id
        /// </summary>
        [JsonPropertyName("cargo_id")]
        public int? Id { get; set; }

        /// <summary>
        /// Where should the cargo be delivered
        /// </summary>
        [JsonPropertyName("destination")]
        public string? Destination { get; set; }

        /// <summary>
        /// Where it is originally from
        /// </summary>
        [JsonPropertyName("origin")]
        public string? Origin { get; set; }
    }
}