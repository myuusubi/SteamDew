using Galaxy.Api;
using StardewValley.Network;
using StardewValley.SDKs;
using Steamworks;
using System;
using System.Collections.Generic;

namespace SteamDew.SDKs {

public class SteamDewClient : Client {

private CallResult<LobbyEnter_t> LobbyEnterCallResult;

private Callback<SteamNetConnectionStatusChangedCallback_t> SteamNetConnectionStatusChangedCallback;

private CSteamID Lobby;
private CSteamID HostID;

private HSteamNetConnection Conn;

private IntPtr[] Messages;

public SteamDewClient(CSteamID lobby)
{
	this.LobbyEnterCallResult = CallResult<LobbyEnter_t>.Create(HandleLobbyEnter);

	this.SteamNetConnectionStatusChangedCallback = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(HandleSteamNetConnectionStatusChanged);

	this.Lobby = lobby;
	this.HostID = new CSteamID();
	this.HostID.Clear();
	this.Conn = HSteamNetConnection.Invalid;

	this.Messages = new IntPtr[256];
}

~SteamDewClient()
{
	this.SteamNetConnectionStatusChangedCallback.Unregister();
}

private string HandleLobbyEnterHelper(LobbyEnter_t evt, bool IOFailure)
{
	if (IOFailure) {
		return "IO Failure";
	}

	CSteamID lobby = new CSteamID(evt.m_ulSteamIDLobby);
	if (!lobby.IsValid() || !lobby.IsLobby()) {
		SteamMatchmaking.LeaveLobby(lobby);
		return $"Invalid Lobby ID: {lobby.m_SteamID.ToString()}";
	}

	if (this.Lobby.m_SteamID != evt.m_ulSteamIDLobby) {
		SteamMatchmaking.LeaveLobby(lobby);
		return $"Wrong Lobby (ID: {lobby.m_SteamID.ToString()}";
	}

	string lobbyVersion = SteamMatchmaking.GetLobbyData(lobby, "protocolVersion");

	if (lobbyVersion == "") {
		return "Missing Protocol Version";
	}

	if (lobbyVersion != SteamDew.PROTOCOL_VERSION) {
		return $"Protocol Mismatch (Local: {SteamDew.PROTOCOL_VERSION}, Remote: {lobbyVersion})";
	}

	uint ip = 0u;
	ushort port = 0;
	CSteamID hostID = new CSteamID();
	hostID.Clear();

	if (SteamMatchmaking.GetLobbyGameServer(lobby, out ip, out port, out hostID)) {
		SteamMatchmaking.LeaveLobby(lobby);
		if (hostID.IsValid()) {
			this.HostID = hostID;

			SteamNetworkingIdentity identity = new SteamNetworkingIdentity();
			identity.Clear();
			identity.SetSteamID(hostID);
			this.Conn = SteamNetworkingSockets.ConnectP2P(ref identity, 0, 0, null);

			return null;
		}
	}

	return $"Invalid Server ID: {hostID.m_SteamID.ToString()}";
}

private void HandleLobbyEnter(LobbyEnter_t evt, bool IOFailure)
{
	string lobbyError = HandleLobbyEnterHelper(evt, IOFailure);
	if (lobbyError == null) {
		return;
	}

	string failMsg = StardewValley.Game1.content.LoadString("Strings\\UI:CoopMenu_Failed");
	this.connectionMessage = $"{failMsg} ({lobbyError})";

	SteamDew.Log($"Error joining lobby ({lobbyError})");
}

private void HandleConnecting(SteamNetConnectionStatusChangedCallback_t evt) {
	SteamDew.Log($"Client connecting to server (ID: {this.HostID.m_SteamID.ToString()})...");
}

private void HandleConnected(SteamNetConnectionStatusChangedCallback_t evt) {
	SteamDew.Log($"Client connected to server (ID: {this.HostID.m_SteamID.ToString()})");
}

private void HandleDisconnected(SteamNetConnectionStatusChangedCallback_t evt) {
	SteamDew.Log($"Client disconnected from server (ID: {this.HostID.m_SteamID.ToString()})");

	this.timedOut = true;
	this.pendingDisconnect = StardewValley.Multiplayer.DisconnectType.HostLeft;
}

private void HandleSteamNetConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t evt) {
	if (evt.m_hConn != this.Conn) {
		return;
	}
	switch (evt.m_info.m_eState) {
	case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting:
		this.HandleConnecting(evt);
		return;
	case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
		this.HandleConnected(evt);
		return;
	case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
	case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
		this.HandleDisconnected(evt);
		return;
	}
}

protected override void connectImpl()
{
	SteamDew.Log($"Client connecting to lobby (ID: {this.Lobby.m_SteamID.ToString()})...");

	SteamAPICall_t steamAPICall = SteamMatchmaking.JoinLobby(this.Lobby);
	this.LobbyEnterCallResult.Set(steamAPICall);
}

public override void disconnect(bool neatly = true)
{
	if (this.Conn == HSteamNetConnection.Invalid) {
		return;
	}

	SteamDew.Log($"Client disconnecting from server (ID: {this.HostID.m_SteamID.ToString()})...");
	SteamDewNetUtils.CloseConnection(this.Conn);

	this.connectionMessage = null;
}

protected override void receiveMessagesImpl()
{
	if (this.Conn == HSteamNetConnection.Invalid) {
		return;
	}

	int msgCount = SteamNetworkingSockets.ReceiveMessagesOnConnection(this.Conn, this.Messages, this.Messages.Length);
	for (int m = 0; m < msgCount; ++m) {
		IncomingMessage msg = new IncomingMessage();
		HSteamNetConnection msgConn = HSteamNetConnection.Invalid;

		SteamDewNetUtils.HandleSteamMessage(this.Messages[m], msg, out msgConn, bandwidthLogger);

		this.processIncomingMessage(msg);
	}
}

public override void sendMessage(OutgoingMessage message)
{
	if (this.Conn == HSteamNetConnection.Invalid) {
		return;
	}
	SteamDewNetUtils.SendMessage(this.Conn, message, bandwidthLogger);
}

public override string getUserID()
{
	/* return Convert.ToString(SteamUser.GetSteamID().m_SteamID); */
	return Convert.ToString(GalaxyInstance.User().GetGalaxyID().ToUint64());
}

protected override string getHostUserName()
{
	if (this.HostID.IsValid()) {
		return SteamFriends.GetFriendPersonaName(this.HostID);
	}
	return "???";
}

public override float GetPingToHost()
{
	SteamNetworkingQuickConnectionStatus status;
	SteamNetworkingSockets.GetQuickConnectionStatus(this.Conn, out status);
	return status.m_nPing;
}

} /* class SteamDewClient */

} /* namespace SteamDew.SDKs */