using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SteamKitDota2.More;

public static class Events
{
    public static readonly EventId NewSession = new(1);
    public static readonly EventId DeadSession = new(2);
    public static readonly EventId Ready = new(3);
    public static readonly EventId NotReady = new(4);

    public static readonly EventId Welcome = new(5);
    public static readonly EventId ConnectionStatus = new(6);

    public static readonly EventId Hello = new(7);
}
