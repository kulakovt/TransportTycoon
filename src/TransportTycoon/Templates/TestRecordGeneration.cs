using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace TransportTycoon.Templates2
{
    internal delegate void TrackRecord(decimal age);
}

namespace TransportTycoon.Templates
{
    [DebuggerDisplay("{Name} → {Position}")]
    delegate void ShipRecord(string? name, IReadOnlyList<char> cargo, int? position = 0);

    public delegate Assembly TruckRecord(AssemblyName name);

    public delegate void No(int length);
}