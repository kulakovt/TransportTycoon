using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TransportTycoon
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var cargo = "ABBBABAAABBB".ToCharArray();
            if (args.Length > 0)
            {
                cargo = args[0].ToCharArray();
            }

            var world = new World(cargo);

            int distance = 0;
            while (!world.IsCargoDelivered())
            {
                distance++;
                world.StepUp();
            }

            var cargoText = String.Join(",", cargo);
            Console.WriteLine("{0} -> {1}", cargoText, distance);
        }
    }

    internal sealed class World
    {
        private const string Factory = "Factory";
        private const string Port = "Port";
        private const string A = "A";
        private const string B = "B";
        private readonly int _totalCargoInWorld;

        private readonly IReadOnlyDictionary<string, List<char>> _warehouses = new Dictionary<string, List<char>>
        {
            { Factory, new List<char>() },
            { Port, new List<char>() },
            { A, new List<char>() },
            { B, new List<char>() }
        };

        private readonly IReadOnlyList<Vehicle> _vehicles;

        public World(char[] cargo)
        {
            _totalCargoInWorld = cargo.Length;
            var wayToPort = new Route(Factory, Port, 1);
            var wayToA = new Route(Port, A, 4);
            var wayToB = new Route(Factory, B, 5);

            _vehicles = new List<Vehicle>
            {
                new Vehicle("Truck-1", new[] { wayToPort, wayToB }, _warehouses),
                new Vehicle("Truck-2", new[] { wayToPort, wayToB }, _warehouses),
                new Vehicle("Ship-1", new[] { wayToA }, _warehouses)
            };

            _warehouses[Factory].AddRange(cargo);
        }

        public void StepUp()
        {
            foreach (var transport in _vehicles)
            {
                transport.Take();
            }

            foreach (var transport in _vehicles)
            {
                transport.Go();
            }

            foreach (var transport in _vehicles)
            {
                transport.Put();
            }
        }

        public bool IsCargoDelivered()
        {
            return
                _warehouses[Factory].Count == 0 &&
                _warehouses[Port].Count == 0 &&
                (_warehouses[A].Count + _warehouses[B].Count == _totalCargoInWorld);
        }

        [DebuggerDisplay("{Name}")]
        private class Vehicle
        {
            public readonly string Name;
            public readonly IReadOnlyList<Route> Routes;
            private readonly IReadOnlyDictionary<string, List<char>> _warehouses;
            private Mission? _mission = null;

            public Vehicle(string name, IReadOnlyList<Route> routes, IReadOnlyDictionary<string, List<char>> warehouses)
            {
                Name = name;
                Routes = routes;
                _warehouses = warehouses;
            }

            public void Take()
            {
                if (_mission == null)
                {
                    _mission = TryLoadCargo();
                }
            }

            public void Go()
            {
                _mission?.MoveOn();
            }

            public void Put()
            {
                switch (_mission)
                {
                    case TransportCargo transportCargo:
                        if (transportCargo.Complete)
                        {
                            UnloadCargo(transportCargo);
                            _mission = ReturnBack.From(transportCargo.Route);
                        }
                        break;
                    case ReturnBack backToFactory:
                        if (backToFactory.Complete)
                        {
                            _mission = null;
                        }
                        break;

                    default:
                        // no cargo, waiting...
                        break;
                }
            }

            private TransportCargo? TryLoadCargo()
            {
                var source = Routes[0].From;
                var warehouse = _warehouses[source];
                if (!warehouse.Any())
                {
                    // Warehouse is empty
                    return null;
                }

                var cargo = warehouse.First();
                var destination = cargo.ToString();

                var newRoute = Routes.SingleOrDefault(route => route.From == source && route.To == destination);

                // Sorry about this hack, but I want to sleep. I promise to refactor this in future versions :(
                if (newRoute == null && source == Factory && destination == A)
                {
                    newRoute = Routes.SingleOrDefault(route => route.From == source && route.To == Port);
                }

                if (newRoute == null)
                {
                    // Can't reach destination
                    return null;
                }

                warehouse.RemoveAt(0);

                return TransportCargo.To(newRoute, cargo);
            }

            private void UnloadCargo(TransportCargo mission)
            {
                var destination = mission.Route.To;
                var cargo = mission.Cargo;
                _warehouses[destination].Add(cargo);
            }
        }

        private abstract class Mission
        {
            public Route Route;
            public int Position;

            public Mission(Route route)
            {
                Route = route;
                Position = 0;
            }

            public bool Complete => Position == Route.Distance;

            public void MoveOn()
            {
                if (Position == Route.Distance)
                {
                    throw new InvalidOperationException("Mission complete");
                }

                Position++;
            }
        }

        private sealed class TransportCargo : Mission
        {
            public readonly char Cargo;

            private TransportCargo(Route route, char cargo)
                : base(route)
            {
                Cargo = cargo;
            }

            public static TransportCargo To(Route route, char cargo)
            {
                return new TransportCargo(route, cargo);
            }
        }

        private sealed class ReturnBack : Mission
        {
            private ReturnBack(Route route)
                : base(route)
            {
            }

            public static ReturnBack From(Route route)
            {
                var backRoute = new Route(route.To, route.From, route.Distance);
                return new ReturnBack(backRoute);
            }
        }

        [DebuggerDisplay("{From} → {To} ({Distance})")]
        private sealed class Route
        {
            public readonly string From;
            public readonly string To;
            public readonly int Distance;

            public Route(string from, string to, int distance)
            {
                From = from;
                To = to;
                Distance = distance;
            }
        }
    }
}
