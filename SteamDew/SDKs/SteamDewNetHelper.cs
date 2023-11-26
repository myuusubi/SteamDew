using StardewValley.Network;
using StardewValley.SDKs;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SteamDew.SDKs {

public class SteamDewNetHelper : SDKNetHelper {

private Callback<LobbyDataUpdate_t> LobbyDataUpdateCallback;
private Callback<GameLobbyJoinRequested_t> GameLobbyJoinRequestedCallback;
private Callback<SteamRelayNetworkStatus_t> SteamRelayNetworkStatusCallback;

private List<LobbyUpdateListener> LobbyUpdateListeners;

private CSteamID RequestedLobby;

public SteamDewNetHelper()
{
	this.LobbyUpdateListeners = new List<LobbyUpdateListener>();

	this.GameLobbyJoinRequestedCallback = Callback<GameLobbyJoinRequested_t>.Create(HandleGameLobbyJoinRequested);
	this.LobbyDataUpdateCallback = Callback<LobbyDataUpdate_t>.Create(HandleLobbyDataUpdate);
	this.SteamRelayNetworkStatusCallback = Callback<SteamRelayNetworkStatus_t>.Create(HandleSteamRelayNetworkStatus);

	this.RequestedLobby = new CSteamID();

	bool foundLobby = false;

	string[] args = Environment.GetCommandLineArgs();
	for (int i = 0; i < args.Length - 1; i++) {
		if (args[i] != "+connect_lobby") {
			continue;
		}
		try {
			CSteamID steamID = new CSteamID(Convert.ToUInt64(args[i + 1]));
			this.RequestedLobby = steamID;
			foundLobby = true;
			break;
		} catch (Exception) {
			SteamDew.Log( $"Failed to convert argument for +connect_lobby" );
			continue;
		}
	}

	if (!foundLobby) {
		this.RequestedLobby.Clear();
	} else if (!this.RequestedLobby.IsValid() || !this.RequestedLobby.IsLobby()) {
		string l = this.RequestedLobby.m_SteamID.ToString();
		SteamDew.Log( $"The Lobby ID ({l}) passed to +connect_lobby is invalid" );
		this.RequestedLobby.Clear();
	} else {
		InviteAccepted();
	}

	SteamNetworkingUtils.InitRelayNetworkAccess();
}

public static StardewValley.Multiplayer GetGameMultiplayer() {
	FieldInfo f = typeof(StardewValley.Game1).GetField(
		"multiplayer",
		BindingFlags.NonPublic | BindingFlags.Static
	);
	if (f == null) {
		SteamDew.Log( $"Failed to access StardewValley.Game1.multiplayer" );
		return null;
	}

	return (StardewValley.Multiplayer) (f.GetValue(null));
}

private void InviteAccepted() {
	StardewValley.Multiplayer multiplayer = SteamDewNetHelper.GetGameMultiplayer();
	if (multiplayer == null) {
		SteamDew.Log( $"Could not accept invite: Game1.multiplayer was null" );
		return;
	}
	multiplayer.inviteAccepted();
}

private void HandleGameLobbyJoinRequested(GameLobbyJoinRequested_t evt)
{
	SteamMatchmaking.JoinLobby(evt.m_steamIDLobby);
}

private void HandleLobbyDataUpdate(LobbyDataUpdate_t evt)
{
	ulong lobby = evt.m_ulSteamIDLobby;
	foreach (LobbyUpdateListener l in this.LobbyUpdateListeners) {
		l.OnLobbyUpdate(lobby);
	}
}

private void HandleSteamRelayNetworkStatus(SteamRelayNetworkStatus_t evt) {
	if( evt.m_eAvail == ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_Current ) {
		SteamDew.Log( "Steam Datagram Relay is now available" );
	}
}

public string GetUserID() 
{
	return Convert.ToString(SteamUser.GetSteamID().m_SteamID);
}

private Client CreateClientHelper(CSteamID lobby)
{
	if (!lobby.IsValid() || !lobby.IsLobby()) {
		SteamDew.Log( $"Could not create client: Invalid Lobby ID ({lobby.m_SteamID.ToString()})" );
	}

	StardewValley.Multiplayer multiplayer = SteamDewNetHelper.GetGameMultiplayer();
	if (multiplayer == null) {
		SteamDew.Log( $"Could not create client: Game1.multiplayer was null" );
		return null;
	}
	return multiplayer.InitClient(new SteamDewClient(lobby));
}

public Client CreateClient(object lobby) 
{
	return CreateClientHelper(new CSteamID((ulong) lobby));
}

public Client GetRequestedClient() 
{
	if (this.RequestedLobby.IsValid() && this.RequestedLobby.IsLobby()) {
		return CreateClientHelper(this.RequestedLobby);
	}
	SteamDew.Log( $"Could not GetRequestedClient: invalid requested lobby" );
	return null;
}

public Server CreateServer(IGameServer gameServer) 
{
	StardewValley.Multiplayer multiplayer = SteamDewNetHelper.GetGameMultiplayer();
	if (multiplayer == null) {
		SteamDew.Log( $"Could not create server: Game1.multiplayer was null" );
		return null;
	}
	return multiplayer.InitServer(new SteamDewServer(gameServer));
}

public void AddLobbyUpdateListener(LobbyUpdateListener listener) 
{
	this.LobbyUpdateListeners.Add(listener);
}

public void RemoveLobbyUpdateListener(LobbyUpdateListener listener) 
{
	this.LobbyUpdateListeners.Remove(listener);
}

public void RequestFriendLobbyData() 
{
	EFriendFlags flags = EFriendFlags.k_EFriendFlagImmediate;
	int count = SteamFriends.GetFriendCount(flags);
	for (int i = 0; i < count; i++) {
		FriendGameInfo_t gameInfo;
		SteamFriends.GetFriendGamePlayed(SteamFriends.GetFriendByIndex(i, flags), out gameInfo);
		if(gameInfo.m_gameID.AppID() != SteamUtils.GetAppID()) {
			continue;
		}
		SteamMatchmaking.RequestLobbyData(gameInfo.m_steamIDLobby);
	}
}

public string GetLobbyData(object lobby, string key) 
{
	CSteamID steamLobby = new CSteamID((ulong) lobby);
	if (!steamLobby.IsValid() || !steamLobby.IsLobby()) {
		SteamDew.Log( $"Tried to GetLobbyData for invalid Lobby: {steamLobby.m_SteamID.ToString()}" );
		return "";
	}
	return SteamMatchmaking.GetLobbyData(steamLobby, key);
}

public string GetLobbyOwnerName(object lobby) 
{
	CSteamID steamLobby = new CSteamID((ulong) lobby);
	if (!steamLobby.IsValid() || !steamLobby.IsLobby()) {
		SteamDew.Log( $"Tried to GetLobbyOwnerName for invalid Lobby: {steamLobby.m_SteamID.ToString()}" );
		return "???";
	}
	CSteamID owner = SteamMatchmaking.GetLobbyOwner(steamLobby);
	return SteamFriends.GetFriendPersonaName(owner);
}

public bool SupportsInviteCodes() 
{
	return true;
}

public object GetLobbyFromInviteCode(string inviteCode) 
{
	ulong decoded = 0ul;
	try {
		decoded = Base36.Decode(inviteCode);
	} catch(FormatException) {
		SteamDew.Log( $"Invite is not valid Base36: {inviteCode}" );
		return null;
	}

	CSteamID lobby = new CSteamID(decoded);
	if (lobby.IsValid() && lobby.IsLobby()) {
		return lobby.m_SteamID;
	}

	SteamDew.Log( $"Invite is not a valid Steam Lobby ID: {inviteCode}" );
	return null;
}

public void ShowInviteDialog(object lobby) 
{
	SteamFriends.ActivateGameOverlayInviteDialog(new CSteamID((ulong) lobby));
}

public void MutePlayer(string userId, bool mute) 
{
	if (mute) {
		SteamDew.Log( $"Tried to mute player: {userId}; Not supported on Steam." );
	} else {
		SteamDew.Log( $"Tried to unmute player: {userId}; Not supported on Steam." );
	}
}

public bool IsPlayerMuted(string userId) 
{
	return false;
}

public void ShowProfile(string userId) 
{
	SteamDew.Log( $"Tried to show profile: {userId}; Not supported on Steam." );
}

} /* class SteamDewNetHelper */

} /* namespace SteamDew.SDKs */