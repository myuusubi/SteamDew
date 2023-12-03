using StardewValley.Network;
using StardewValley.SDKs;

namespace SteamDew.SDKs.Decoys {

public class DGalaxyNetServer : GalaxyNetServer {

public IGameServer iGameServer;

public DGalaxyNetServer(IGameServer gameServer) : base(gameServer)
{
	SteamDew.Log($"Created Decoy for Galaxy Server");
	this.iGameServer = gameServer;

	SteamDew.LastClientType = SDKs.ClientType.Unknown;
}

~DGalaxyNetServer()
{
	SteamDew.Log($"Destroyed Decoy for Galaxy Server");
}

} /* class DGalaxyNetServer */

} /* namespace SteamDew.SDKs.Decoys */