using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Hour = System.Int32;
using CargoId = System.Int32;
using TransportId = System.Int32;

// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable BuiltInTypeReferenceStyle

namespace TransportTycoon
{
    enum TransportType { Truck, Ship }

    enum Location { Factory, Port, A, B }

    [DebuggerDisplay("{transportType} —[ {from} ]→ {to}")]
    delegate void TrackRecord(Location from, TransportType transportType, Location? to);

    [DebuggerDisplay("Travel = {travelDuration} (L = {loadDuration}, U = {unloadDuration})")]
    delegate void DestinationRecord(Location location, Hour travelDuration, Hour loadDuration = 0, Hour unloadDuration = 0);

    [DebuggerDisplay("{id} ({origin} → {destination})")]
    delegate void CargoRecord(CargoId id, Location origin, Location destination);

    class World
    {
        static Location? Home = null;

        static IReadOnlyDictionary<Track, Destination> Map = new Dictionary<Track, Destination>
        {
            { new Track(Location.Factory, TransportType.Truck, Location.A), new Destination(Location.Port, travelDuration: 1) },
            { new Track(Location.Port, TransportType.Truck, Home), new Destination(Location.Factory, travelDuration: 1) },
            { new Track(Location.Port, TransportType.Ship, Location.A), new Destination(Location.A, travelDuration: 6, loadDuration: 1, unloadDuration: 1) },
            { new Track(Location.A, TransportType.Ship, Home), new Destination(Location.Port, travelDuration: 6) },
            { new Track(Location.Factory, TransportType.Truck, Location.B), new Destination(Location.B, travelDuration: 5) },
            { new Track(Location.B, TransportType.Truck, Home), new Destination(Location.Factory, travelDuration: 5) }
        };

        static IReadOnlyDictionary<Location, List<Cargo>> Warehouses = new Dictionary<Location, List<Cargo>>
        {
            { Location.Factory, new List<Cargo>() },
            { Location.Port, new List<Cargo>() },
            { Location.A, new List<Cargo>() },
            { Location.B, new List<Cargo>() }
        };

        static IReadOnlyList<Transport> Transports = new List<Transport>
        {
            new Transport(TransportType.Truck, Location.Factory),
            new Transport(TransportType.Truck, Location.Factory),
            new Transport(TransportType.Ship, Location.Port, 4)
        };

        static int InStock => Warehouses[Location.A].Count + Warehouses[Location.B].Count;
        static int CargoIdGenerator;

        public static void Main(string[] args)
        {
            Cargo NewCargo(char name) => new Cargo(CargoIdGenerator++, Location.Factory, Enum.Parse<Location>(name.ToString(), true));

            var store = (args.Length > 0 ? args.First() : "ABBBABAAABBB").ToList();

            var duration = Resolve(store.Select(NewCargo).ToList());

            Console.WriteLine($"Input: {String.Join(String.Empty, store)}, Output: {duration}");
        }

        static Hour Resolve(IReadOnlyList<Cargo> goods)
        {
            Warehouses[Location.Factory].AddRange(goods);

            var time = 0;
            while (true)
            {
                foreach (var transport in Transports)
                {
                    transport.Run(time);
                }

                if (InStock == goods.Count)
                {
                    break;
                }

                time++;
            }

            return time;
        }

        [DebuggerDisplay("{type} №{id} ({from} → {to}) [{store.Count}]")]
        class Transport
        {
            static TransportId IdGenerator;
            const Hour NoEta = -1;

            TransportId id;
            TransportType type;
            Location from;
            Location to;
            int capacity;
            List<Cargo> store = new List<Cargo>();

            Hour loadEta = NoEta;
            Hour travelEta = NoEta;
            Hour unloadEta = NoEta;

            public Transport(TransportType transportType, Location currentLocation, int cargoCapacity = 1)
            {
                id = IdGenerator++;
                type = transportType;
                from = currentLocation;
                to = currentLocation;
                capacity = cargoCapacity;
            }

            public void Run(Hour time)
            {
                if (loadEta > time)
                {
                    // Loading...
                    return;
                }

                if (loadEta == time)
                {
                    // Load complete
                    OnDepart(time);
                }

                if (travelEta > time)
                {
                    // Still on the way...
                    return;
                }

                if (travelEta == time)
                {
                    // Travel complete
                    Park();
                    OnArrive(time);

                    if (unloadEta != NoEta)
                    {
                        OnBeginUnload(time);
                    }
                }

                if (unloadEta > time)
                {
                    // Unloading...
                    return;
                }

                if (HasCargo)
                {
                    Unload(time);

                    // Back home
                    OnDepart(time);
                }
                else
                {
                    Load(time);

                    if (loadEta != NoEta)
                    {
                        OnBeginLoad(time);
                    }

                    if (loadEta == time)
                    {
                        // Load complete
                        OnDepart(time);
                    }
                }
            }

            bool HasCargo => store.Any();

            void Park()
            {
                from = to;
            }

            void Load(Hour time)
            {
                var warehouse = Warehouses[from];
                if (!warehouse.Any())
                {
                    // Wait until cargo is available
                    return;
                }

                var cargo = warehouse.Pop(capacity);
                store.AddRange(cargo);
                var destination = store.First().Destination;

                var currentWaypoint = new Track(from, type, destination);
                var (newLocation, travelDuration, loadDuration, unloadDuration) = Map[currentWaypoint];
                to = newLocation;

                loadEta = time + loadDuration;
                travelEta = loadEta + travelDuration;
                unloadEta = travelEta + unloadDuration;
            }

            void Unload(Hour time)
            {
                var warehouse = Warehouses[to];
                var cargo = store.PopAll();
                warehouse.AddRange(cargo);

                var currentWaypoint = new Track(to, type, Home);
                var (newLocation, travelDuration, _, _) = Map[currentWaypoint];
                from = to;
                to = newLocation;

                loadEta = NoEta;
                travelEta = time + travelDuration;
                unloadEta = NoEta;
            }

            void OnBeginLoad(Hour time) => Log("LOAD", time, from, duration: unloadEta - travelEta);
            void OnDepart(Hour time) => Log("DEPART", time, from, to);
            void OnBeginUnload(Hour time) => Log("UNLOAD", time, to, duration: unloadEta - travelEta);
            void OnArrive(Hour time) => Log("ARRIVE", time, to);

            void Log(string messageType, Hour time, Location location, Location? destination = null, int? duration = null)
            {
                if (duration == 0)
                {
                    // Skip zero-duration operations
                    return;
                }

                Logger.Write(messageType, time, id, type, location, destination, duration, store);
            }
        }
    }
}
