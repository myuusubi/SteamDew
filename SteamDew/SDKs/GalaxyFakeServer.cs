using Galaxy.Api;
using StardewValley.Network;
using StardewValley.SDKs;
using Steamworks;
using System;
using System.Collections.Generic;

namespace SteamDew.SDKs {

public class GalaxyFakeServer : GalaxyNetServer {

public GalaxyFakeServer(IGameServer gameServer) : base(gameServer)
{
	SteamDew.Log($"Created Fake Galaxy Server");
}

~GalaxyFakeServer()
{
	SteamDew.Log($"Destroyed Fake Galaxy Server");
}

} /* class GalaxyFakeServer */

} /* namespace SteamDew.SDKs */