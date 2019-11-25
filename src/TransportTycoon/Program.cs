﻿using System;
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
            { new Waypoint(Location.Port, VehicleType.Ship, 'A'), new Destination(Location.A, travelDuration: 4) },
            //{ new Waypoint(Location.Port, VehicleType.Ship, 'A'), new Destination(Location.A, travelDuration: 6, loadDuration: 1, unloadDuration: 1) },
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
            new Vehicle(VehicleType.Ship, Location.Port)
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
            Cargo cargo = NoCargo;
            Hour travelEta;

            public Vehicle(VehicleType vehicleType, Location currentLocation)
            {
                type = vehicleType;
                location = currentLocation;
            }

            public void Run(Hour time)
            {
                if (travelEta > time)
                {
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

                GetNewMission(time);
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

            void GetNewMission(Hour time)
            {
                var currentWaypoint = new Waypoint(location, type, cargo);
                var (newLocation, travelDuration, loadDuration, unloadDuration) = Map[currentWaypoint];
                location = newLocation;
                travelEta = time + travelDuration;
            }
        }
    }
}
