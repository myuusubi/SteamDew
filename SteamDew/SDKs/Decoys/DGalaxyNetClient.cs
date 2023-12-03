using Galaxy.Api;
using StardewValley.SDKs;

namespace SteamDew.SDKs.Decoys {

public class DGalaxyNetClient : GalaxyNetClient {

public GalaxyID LobbyID;

public DGalaxyNetClient(GalaxyID lobby) : base(lobby)
{
	SteamDew.Log($"Created Decoy for Galaxy Client");
	this.LobbyID = lobby;

	SteamDew.LastClientType = SDKs.ClientType.Galaxy;
}

~DGalaxyNetClient()
{
	SteamDew.Log($"Destroyed Decoy for Galaxy Client");
}

} /* class DGalaxyNetClient */

} /* namespace SteamDew.SDKs.Decoys */