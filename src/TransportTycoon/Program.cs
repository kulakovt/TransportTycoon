using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Cargo = System.Char;
using Hour = System.UInt32;

// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable BuiltInTypeReferenceStyle

namespace TransportTycoon
{
    enum VehicleType { Truck, Ship }

    enum Location { Factory, Port, A, B }

    [DebuggerDisplay("{vehicleType} —[ {cargo} ]→ {location}")]
    delegate void WaypointRecord(Location location, VehicleType vehicleType, Cargo cargo);

    [DebuggerDisplay("{location} ({travelDuration})")]
    delegate void DestinationRecord(Location location, Hour travelDuration, Hour loadDuration = 0, Hour unloadDuration = 0);

    class World
    {
        const Cargo NoCargo = ' ';

        static IReadOnlyDictionary<Waypoint, Destination> Map = new Dictionary<Waypoint, Destination>
        {
            { new Waypoint(Location.Factory, VehicleType.Truck, 'A'), new Destination(Location.Port, travelDuration: 1) },
            { new Waypoint(Location.Port, VehicleType.Truck, NoCargo), new Destination(Location.Factory, travelDuration: 1) },
            { new Waypoint(Location.Port, VehicleType.Ship, 'A'), new Destination(Location.A, travelDuration: 6, loadDuration: 1, unloadDuration: 1) },
            { new Waypoint(Location.A, VehicleType.Ship, NoCargo), new Destination(Location.Port, travelDuration: 4) },
            { new Waypoint(Location.Factory, VehicleType.Truck, 'B'), new Destination(Location.B, travelDuration: 5) },
            { new Waypoint(Location.B, VehicleType.Truck, NoCargo), new Destination(Location.Factory, travelDuration: 5) }
        };

        static IReadOnlyDictionary<Location, List<Cargo>> Locations = new Dictionary<Location, List<Cargo>>
        {
            { Location.Factory, new List<Cargo>() },
            { Location.Port, new List<Cargo>() },
            { Location.A, new List<Cargo>() },
            { Location.B, new List<Cargo>() }
        };

        static IReadOnlyList<Vehicle> Vehicles = new List<Vehicle>
        {
            new Vehicle(VehicleType.Truck, Location.Factory),
            new Vehicle(VehicleType.Truck, Location.Factory),
            new Vehicle(VehicleType.Ship, Location.Port, 4)
        };

        static int InStock => Locations[Location.A].Count + Locations[Location.B].Count;

        public static void Main(string[] args)
        {
            var goods = (args.Length > 0 ? args.First() : "ABBBABAAABBB").ToList();

            var duration = Resolve(goods);

            Console.WriteLine($"Input: {String.Join(String.Empty, goods)}, Output: {duration}");
        }

        static Hour Resolve(IReadOnlyList<Cargo> goods)
        {
            Locations[Location.Factory].AddRange(goods);

            var time = 0U;
            while (true)
            {
                foreach (var vehicle in Vehicles)
                {
                    vehicle.Run(time);
                }

                if (InStock == goods.Count)
                {
                    break;
                }

                time++;
            }

            return time;
        }

        class Vehicle
        {
            VehicleType type;
            Location location;
            int capacity;
            List<Cargo> cargo = new List<char>();

            Hour loadEta;
            Hour travelEta;
            Hour unloadEta;

            public Vehicle(VehicleType vehicleType, Location currentLocation, int cargoCapacity = 1)
            {
                type = vehicleType;
                location = currentLocation;
                capacity = cargoCapacity;
            }

            public void Run(Hour time)
            {
                if (loadEta > time)
                {
                    // Loading...
                    return;
                }

                if (travelEta > time)
                {
                    // Still on the way
                    return;
                }

                if (unloadEta > time)
                {
                    // Unloading...
                    return;
                }

                if (HasCargo)
                {
                    Unload(time);
                }
                else
                {
                    Load(time);
                }
            }

            bool HasCargo => cargo.Any();

            void Load(Hour time)
            {
                var warehouse = Locations[location];
                if (!warehouse.Any())
                {
                    // Wait until cargo is available
                    return;
                }

                var newCargo = warehouse.Pop(capacity);
                cargo.AddRange(newCargo);
                var waypointCargo = cargo.First();

                var currentWaypoint = new Waypoint(location, type, waypointCargo);
                var (newLocation, travelDuration, loadDuration, unloadDuration) = Map[currentWaypoint];
                location = newLocation;

                loadEta = time + loadDuration;
                travelEta = loadEta + travelDuration;
                unloadEta = travelEta + unloadDuration;
            }

            void Unload(Hour time)
            {
                var warehouse = Locations[location];
                var newCargo = cargo.PopAll();
                warehouse.AddRange(newCargo);

                var currentWaypoint = new Waypoint(location, type, NoCargo);
                var (newLocation, travelDuration, _, _) = Map[currentWaypoint];
                location = newLocation;

                loadEta = 0;
                travelEta = time + travelDuration;
                unloadEta = 0;
            }
        }
    }
}
