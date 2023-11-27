using Galaxy.Api;
using StardewValley.Network;
using StardewValley.SDKs;
using Steamworks;
using System;
using System.Collections.Generic;

namespace SteamDew.SDKs {

public class GalaxyFakeClient : GalaxyNetClient {

public GalaxyFakeClient(CSteamID lobby) : base(new GalaxyID(lobby.m_SteamID))
{
	SteamDew.Log($"Created Fake Galaxy Client");
}

~GalaxyFakeClient()
{
	SteamDew.Log($"Destroyed Fake Galaxy Client");
}

}

}