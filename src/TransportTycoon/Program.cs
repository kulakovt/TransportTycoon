using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Cargo = System.Char;
using Distance = System.UInt32;

// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable BuiltInTypeReferenceStyle

namespace TransportTycoon
{
    enum VehicleType { Truck, Ship }

    enum Location { Factory, Port, A, B }

    [DebuggerDisplay("{VehicleType} —[ {Cargo.ToString()} ]→ {Location}")]
    delegate void WaypointRecord(Location location, VehicleType vehicleType, Cargo cargo);

    [DebuggerDisplay("{Location} ({Distance})")]
    delegate void DestinationRecord(Location location, Distance distance);

    class World
    {
        const Cargo NoCargo = ' ';

        static IReadOnlyDictionary<Waypoint, Destination> Map = new Dictionary<Waypoint, Destination>
        {
            { new Waypoint(Location.Factory, VehicleType.Truck, 'A'), new Destination(Location.Port, distance: 1) },
            { new Waypoint(Location.Port, VehicleType.Truck, NoCargo), new Destination(Location.Factory, distance: 1) },
            { new Waypoint(Location.Port, VehicleType.Ship, 'A'), new Destination(Location.A, distance: 4) },
            { new Waypoint(Location.A, VehicleType.Ship, NoCargo), new Destination(Location.Port, distance: 4) },
            { new Waypoint(Location.Factory, VehicleType.Truck, 'B'), new Destination(Location.B, distance: 5) },
            { new Waypoint(Location.B, VehicleType.Truck, NoCargo), new Destination(Location.Factory, distance: 5) }
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
            new Vehicle(VehicleType.Ship, Location.Port)
        };

        static int InStock => Locations[Location.A].Count + Locations[Location.B].Count;

        public static void Main(string[] args)
        {
            var goods = (args.Length > 0 ? args.First() : "ABBBABAAABBB").ToList();

            var duration = Resolve(goods);

            Console.WriteLine($"Input: {String.Join(String.Empty, goods)}, Output: {duration}");
        }

        static int Resolve(IReadOnlyList<Cargo> goods)
        {
            Locations[Location.Factory].AddRange(goods);

            var duration = 0;
            while (true)
            {
                foreach (var vehicle in Vehicles)
                {
                    vehicle.Run();
                }

                if (InStock == goods.Count)
                {
                    break;
                }

                duration++;
            }

            return duration;
        }

        class Vehicle
        {
            VehicleType type;
            Location location;
            Cargo cargo = NoCargo;
            Distance currentDistance;
            Distance targetDistance;

            public Vehicle(VehicleType vehicleType, Location currentLocation)
            {
                type = vehicleType;
                location = currentLocation;
            }

            bool StillOnTheWay => currentDistance < targetDistance;

            public void Run()
            {
                if (StillOnTheWay)
                {
                    Move();
                    return;
                }

                if (cargo == NoCargo)
                {
                    if (!TryLoad())
                    {
                        return;
                    }
                }
                else
                {
                    Unload();
                }

                GetNewMission();
                Move();
            }

            bool TryLoad()
            {
                var warehouse = Locations[location];
                if (!warehouse.Any())
                {
                    // Wait until cargo is available
                    return false;
                }

                cargo = warehouse.First();
                warehouse.RemoveAt(0);
                return true;
            }

            void Unload()
            {
                var warehouse = Locations[location];
                warehouse.Add(cargo);
                cargo = NoCargo;
            }

            void GetNewMission()
            {
                var currentWaypoint = new Waypoint(location, type, cargo);
                var (newLocation, newDistance) = Map[currentWaypoint];
                location = newLocation;
                currentDistance = 0;
                targetDistance = newDistance;
            }

            void Move()
            {
                currentDistance++;
            }
        }
    }
}
