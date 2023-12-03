using StardewValley.Network;
using StardewValley.SDKs;

namespace SteamDew.SDKs.Decoys {

public class DSteamDewServer : GalaxyNetServer {

public IGameServer iGameServer;

public DSteamDewServer(IGameServer gameServer) : base(gameServer)
{
	SteamDew.Log($"Created Decoy for SteamDew Server");
	this.iGameServer = gameServer;

	SteamDew.LastClientType = SDKs.ClientType.Unknown;
}

~DSteamDewServer()
{
	SteamDew.Log($"Destroyed Decoy for SteamDew Server");
}

} /* class DSteamDewServer */

} /* namespace SteamDew.SDKs.Decoys */