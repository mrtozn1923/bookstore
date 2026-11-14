// Address plan for Bookstore's VNet: checks sizes, overlaps and the addresses Azure keeps in every subnet.
// Run: dotnet run tools/cloud/subnets.cs
using System.Net;

IPNetwork vnet = IPNetwork.Parse("10.20.0.0/16");
// Networks this VNet may be peered or connected with later: they must not overlap
(string Name, IPNetwork Range)[] neighbours =
[
    ("vnet-bookstore-prod-weu", IPNetwork.Parse("10.21.0.0/16")),
    ("office network over VPN", IPNetwork.Parse("10.20.128.0/20")),
];

(string Name, string Cidr, int MinPrefix, string Purpose)[] plan =
[
    ("snet-app", "10.20.1.0/26", 28, "App Service VNet integration (Microsoft.Web/serverFarms)"),
    ("snet-cae", "10.20.2.0/27", 27, "Container Apps environment (Microsoft.App/environments)"),
    ("snet-pe", "10.20.3.0/27", 29, "Private endpoints: SQL, Blob"),
    ("snet-jobs", "10.20.4.0/28", 27, "A mistake: too small for a second environment"),
    ("snet-pe-old", "10.20.3.16/28", 29, "A mistake: overlaps snet-pe"),
];

const int AzureReserved = 5; // network, gateway, 2 x Azure DNS, broadcast
List<(string Name, IPNetwork Range)> accepted = [];

Console.WriteLine($"{"Subnet",-12} {"CIDR",-15} {"First usable",-13} {"Usable",6}  Result");
foreach ((string name, string cidr, int minPrefix, string purpose) in plan)
{
    IPNetwork range = IPNetwork.Parse(cidr);
    int size = 1 << (32 - range.PrefixLength);
    byte[] first = range.BaseAddress.GetAddressBytes();
    first[3] += 4; // .0 network, .1 gateway, .2 and .3 Azure DNS
    string result =
        !vnet.Contains(range.BaseAddress) ? "outside the VNet"
        : range.PrefixLength > minPrefix ? $"too small, needs /{minPrefix} or larger"
        : accepted.FirstOrDefault(a => Overlaps(a.Range, range)) is { Name: not null } hit ? $"overlaps {hit.Name}"
        : "ok";
    if (result == "ok")
    {
        accepted.Add((name, range));
    }
    Console.WriteLine($"{name,-12} {cidr,-15} {new IPAddress(first),-13} {size - AzureReserved,6}  {result}  ({purpose})");
}

foreach ((string name, IPNetwork range) in neighbours)
{
    Console.WriteLine($"VNet {vnet} vs {name} {range}: {(Overlaps(vnet, range) ? "OVERLAP, they can not be connected" : "no overlap")}");
}

static bool Overlaps(IPNetwork a, IPNetwork b) => a.Contains(b.BaseAddress) || b.Contains(a.BaseAddress);
