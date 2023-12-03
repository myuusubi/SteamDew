using Galaxy.Api;
using StardewValley.SDKs;
using Steamworks;

namespace SteamDew.SDKs.Decoys {

public class DSteamDewClient : GalaxyNetClient {

public CSteamID LobbyID;
public CSteamID HostID;
public ClientState State;

public DSteamDewClient(CSteamID lobby, ClientState state, CSteamID host = new CSteamID()) : base(new GalaxyID(lobby.m_SteamID))
{
	SteamDew.Log($"Created Decoy for SteamDew Client");
	this.LobbyID = lobby;
	this.State = state;
	this.HostID = host;

	SteamDew.LastClientType = SDKs.ClientType.SteamDew;
}

~DSteamDewClient()
{
	SteamDew.Log($"Destroyed Decoy for SteamDew Client");
}

} /* class DSteamDewClient */

} /* namespace SteamDew.SDKs.Decoys */