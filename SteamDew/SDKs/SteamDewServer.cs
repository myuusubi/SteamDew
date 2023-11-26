using StardewValley;
using StardewValley.Network;
using StardewValley.SDKs;
using Steamworks;
using System;
using System.Collections.Generic;

namespace SteamDew.SDKs {

public class SteamDewServer : Server {

private class PeerData {
	public CSteamID SteamID;
	public HSteamNetConnection Conn;

	public PeerData()
	{
		this.SteamID = new CSteamID();
		this.SteamID.Clear();

		this.Conn = HSteamNetConnection.Invalid;
	}

	public override bool Equals(object obj)
	{
		if (obj == null) {
			return false;
		}
		if (!(obj is PeerData)) {
			return false;
		}
		return this.SteamID.Equals((obj as PeerData).SteamID);
	}

	public override int GetHashCode()
	{
		return this.SteamID.GetHashCode();
	}
}

private CallResult<LobbyCreated_t> LobbyCreatedCallResult;

private Callback<PersonaStateChange_t> PersonaStateChangeCallback;
private Callback<SteamNetConnectionStatusChangedCallback_t> SteamNetConnectionStatusChangedCallback;

private CSteamID Lobby;
private HSteamListenSocket Listener;
private HSteamNetPollGroup JoinGroup;
private HSteamNetPollGroup PeerGroup;

private IntPtr[] Messages;

private ServerPrivacy Privacy;

private Dictionary<string, string> LobbyData;

private Bimap<long, PeerData> Peers;

public override int connectionsCount
{
	get {
		if (this.Peers == null) {
			return 0;
		}
		return this.Peers.Count;
	}
}

public SteamDewServer(IGameServer gameServer) : base(gameServer)
{
}

private PeerData FarmerToPeer(long farmerId) {
	if (!this.Peers.ContainsLeft(farmerId)) {
		return null;
	}
	return this.Peers.GetRight(farmerId);
}

private bool GetFarmerFromSteam(ref long farmerId, CSteamID steamID) {
	PeerData peer = new PeerData();
	peer.SteamID = steamID;

	if (!this.Peers.ContainsRight(peer)) {
		return false;
	}

	farmerId = this.Peers.GetLeft(peer);
	return true;
}

private void UpdateLobbyPrivacy()
{
	if (!this.Lobby.IsValid() || !this.Lobby.IsLobby()) {
		return;
	}

	ELobbyType lobbyType = ELobbyType.k_ELobbyTypePrivate;
	switch (this.Privacy) {
	case ServerPrivacy.FriendsOnly:
		lobbyType = ELobbyType.k_ELobbyTypeFriendsOnly;
		break;
	case ServerPrivacy.Public:
		lobbyType = ELobbyType.k_ELobbyTypePublic;
		break;
	}
	SteamMatchmaking.SetLobbyType(this.Lobby, lobbyType);
}

public override void initialize()
{
	this.Lobby = new CSteamID();
	this.Lobby.Clear();

	this.Listener = HSteamListenSocket.Invalid;

	this.JoinGroup = HSteamNetPollGroup.Invalid;
	this.PeerGroup = HSteamNetPollGroup.Invalid;

	this.Messages = new IntPtr[256];

	this.Privacy = Game1.options.serverPrivacy;

	this.LobbyData = new Dictionary<string, string>();

	this.Peers = new Bimap<long, PeerData>();

	SteamDew.Log($"Starting SteamDew Server");

	int maxMembers = 4 * 2;

	Multiplayer multiplayer = SteamDewNetHelper.GetGameMultiplayer();
	if (multiplayer != null) {
		maxMembers = multiplayer.playerLimit * 2;
	}

	this.LobbyCreatedCallResult = CallResult<LobbyCreated_t>.Create(HandleLobbyCreated);

	this.PersonaStateChangeCallback = Callback<PersonaStateChange_t>.Create(HandlePersonaStateChange);
	this.SteamNetConnectionStatusChangedCallback = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(HandleSteamNetConnectionStatusChanged);

	SteamAPICall_t steamAPICall = SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePrivate, maxMembers);
	this.LobbyCreatedCallResult.Set(steamAPICall);
}

private string GetConnectionId(CSteamID steamID)
{
	return "SN_" + steamID.m_SteamID.ToString();
}

private string HandleLobbyCreatedHelper(LobbyCreated_t evt, bool IOFailure)
{
	if (IOFailure) {
		return "IO Failure";
	}

	switch (evt.m_eResult) {
	case EResult.k_EResultOK:
		CSteamID lobby = new CSteamID(evt.m_ulSteamIDLobby);
		if (!lobby.IsValid() || !lobby.IsLobby()) {
			return "Created Lobby is Invalid";
		}
		this.Lobby = lobby;
		return null;
	case EResult.k_EResultTimeout:
		return "Steam Timed Out";
	case EResult.k_EResultLimitExceeded:
		return "Too Many Steam Lobbies";
	case EResult.k_EResultAccessDenied:
		return "Steam Denied Access";
	case EResult.k_EResultNoConnection:
		return "No Steam Connection";			
	}

	return "Unknown Steam Failure";
}

private void HandleLobbyCreated(LobbyCreated_t evt, bool IOFailure)
{
	string lobbyError = HandleLobbyCreatedHelper(evt, IOFailure);
	if (lobbyError == null) {
		this.Listener = SteamNetworkingSockets.CreateListenSocketP2P(0, 0, null);
		this.JoinGroup = SteamNetworkingSockets.CreatePollGroup();
		this.PeerGroup = SteamNetworkingSockets.CreatePollGroup();

		SteamMatchmaking.SetLobbyGameServer(this.Lobby, 0u, 0, SteamUser.GetSteamID());

		foreach (KeyValuePair<string, string> d in this.LobbyData) {
			SteamMatchmaking.SetLobbyData(this.Lobby, d.Key, d.Value);
		}

		SteamMatchmaking.SetLobbyData(this.Lobby, "protocolVersion", SteamDew.PROTOCOL_VERSION);

		SteamMatchmaking.SetLobbyJoinable(this.Lobby, true);

		UpdateLobbyPrivacy();

		SteamDew.Log($"Server successfully created lobby (ID: {this.Lobby.m_SteamID.ToString()})");
		return;
	}
	SteamDew.Log($"Server failed to create lobby ({lobbyError})");	
}

private void HandlePersonaStateChange(PersonaStateChange_t evt)
{
	CSteamID steamID = new CSteamID(evt.m_ulSteamID);
	long farmerId = 0;
	if (!this.GetFarmerFromSteam(ref farmerId, steamID)) {
		return;
	}
	
	string userName = SteamFriends.GetFriendPersonaName(steamID);

	/* Adapted from StardewValley.Multiplayer::broadcastUserName(long farmerId, string userName) */
	foreach (KeyValuePair<long, Farmer> otherFarmer in Game1.otherFarmers) {
		Farmer farmer = otherFarmer.Value;
		if (farmer.UniqueMultiplayerID == farmerId) {
			continue;
		}
		Game1.server.sendMessage(farmer.UniqueMultiplayerID, 16, Game1.serverHost.Value, farmerId, userName);
	}
}

private void HandleConnecting(SteamNetConnectionStatusChangedCallback_t evt, CSteamID steamID)
{
	SteamDew.Log($"{steamID.m_SteamID.ToString()} connecting...");

	if (gameServer.isUserBanned(steamID.m_SteamID.ToString())) {
		SteamDew.Log($"{steamID.m_SteamID.ToString()} is banned");
		SteamDewNetUtils.CloseConnection(evt.m_hConn);
		return;
	}

	SteamNetworkingSockets.AcceptConnection(evt.m_hConn);
}

private void HandleConnected(SteamNetConnectionStatusChangedCallback_t evt, CSteamID steamID)
{
	SteamDew.Log($"{steamID.m_SteamID.ToString()} connected");

	SteamNetworkingSockets.SetConnectionPollGroup(evt.m_hConn, this.JoinGroup);

	this.onConnect(GetConnectionId(steamID));

	gameServer.sendAvailableFarmhands(
		steamID.m_SteamID.ToString(), 
		delegate(OutgoingMessage msg) {
			SteamDewNetUtils.SendMessage(evt.m_hConn, msg, bandwidthLogger);
		}
	);
}

private void HandleDisconnected(SteamNetConnectionStatusChangedCallback_t evt, CSteamID steamID)
{
	SteamDew.Log($"{steamID.m_SteamID.ToString()} disconnected");

	this.onDisconnect(GetConnectionId(steamID));

	long farmerId = 0;
	if (this.GetFarmerFromSteam(ref farmerId, steamID)) {
		this.playerDisconnected(farmerId);
	}

	SteamDewNetUtils.CloseConnection(evt.m_hConn);
}

private void HandleSteamNetConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t evt) {
	if (evt.m_info.m_identityRemote.IsInvalid()) {
		SteamDewNetUtils.CloseConnection(evt.m_hConn);
		return;
	}

	CSteamID steamID = evt.m_info.m_identityRemote.GetSteamID();
	if (!steamID.IsValid()) {
		SteamDewNetUtils.CloseConnection(evt.m_hConn);
		return;
	}

	switch (evt.m_info.m_eState) {
	case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting:
		this.HandleConnecting(evt, steamID);
		return;
	case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
		this.HandleConnected(evt, steamID);
		return;
	case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
	case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
		this.HandleDisconnected(evt, steamID);
		return;
	}
}

public override void setPrivacy(ServerPrivacy privacy)
{
	this.Privacy = privacy;
	UpdateLobbyPrivacy();
}

public override void stopServer()
{
	SteamDew.Log($"Stopping SteamDew server");

	foreach (KeyValuePair<long, PeerData> peer in this.Peers) {
		SteamDewNetUtils.CloseConnection(peer.Value.Conn);
	}

	if (this.Lobby.IsValid() && this.Lobby.IsLobby()) {
		SteamMatchmaking.LeaveLobby(this.Lobby);	
	}
	
	if (this.Listener != HSteamListenSocket.Invalid) {
		SteamNetworkingSockets.CloseListenSocket(this.Listener);
		this.Listener = HSteamListenSocket.Invalid;
	}

	if (this.PeerGroup != HSteamNetPollGroup.Invalid) {
		SteamNetworkingSockets.DestroyPollGroup(this.PeerGroup);
		this.PeerGroup = HSteamNetPollGroup.Invalid;
	}

	if (this.JoinGroup != HSteamNetPollGroup.Invalid) {
		SteamNetworkingSockets.DestroyPollGroup(this.JoinGroup);
		this.JoinGroup = HSteamNetPollGroup.Invalid;
	}
}

private void HandleFarmhandRequest(IncomingMessage msg, HSteamNetConnection msgConn, CSteamID steamID)
{
	Multiplayer multiplayer = SteamDewNetHelper.GetGameMultiplayer();
	if (multiplayer == null) {
		SteamDewNetUtils.CloseConnection(msgConn);
		return;
	}

	NetFarmerRoot farmer = multiplayer.readFarmer(msg.Reader);
	gameServer.checkFarmhandRequest(
		steamID.m_SteamID.ToString(), 
		GetConnectionId(steamID), 
		farmer, 
		delegate(OutgoingMessage msg) {
			SteamDewNetUtils.SendMessage(msgConn, msg, bandwidthLogger);
		},
		delegate {
			long farmerId = farmer.Value.UniqueMultiplayerID;

			SteamNetworkingSockets.SetConnectionUserData(msgConn, farmerId);
			SteamNetworkingSockets.SetConnectionPollGroup(msgConn, this.PeerGroup);

			PeerData peer = new PeerData();
			peer.SteamID = steamID;
			peer.Conn = msgConn;
			this.Peers[farmerId] = peer;
		}
	);
}

private void PollFarmhandRequests()
{
	int msgCount = SteamNetworkingSockets.ReceiveMessagesOnPollGroup(this.JoinGroup, this.Messages, this.Messages.Length);
	for (int m = 0; m < msgCount; ++m) {
		IncomingMessage msg = new IncomingMessage();
		HSteamNetConnection msgConn = HSteamNetConnection.Invalid;

		SteamDewNetUtils.HandleSteamMessage(this.Messages[m], msg, out msgConn, bandwidthLogger);

		if (msg.MessageType != 2) {
			continue;
		}

		SteamNetConnectionInfo_t info;
		SteamNetworkingSockets.GetConnectionInfo(msgConn, out info);

		if (info.m_identityRemote.IsInvalid()) {
			SteamDewNetUtils.CloseConnection(msgConn);
			continue;
		}

		CSteamID steamID = info.m_identityRemote.GetSteamID();
		if (!steamID.IsValid()) {
			SteamDewNetUtils.CloseConnection(msgConn);
			continue;
		}

		this.HandleFarmhandRequest(msg, msgConn, steamID);
	}
}

private void PollPeers()
{
	int msgCount = SteamNetworkingSockets.ReceiveMessagesOnPollGroup(this.PeerGroup, this.Messages, this.Messages.Length);
	for (int m = 0; m < msgCount; ++m) {
		IncomingMessage msg = new IncomingMessage();
		HSteamNetConnection msgConn = HSteamNetConnection.Invalid;

		SteamDewNetUtils.HandleSteamMessage(this.Messages[m], msg, out msgConn, bandwidthLogger);

		long farmerId = SteamNetworkingSockets.GetConnectionUserData(msgConn);
		PeerData peer = this.FarmerToPeer(farmerId);

		if (peer == null || peer.Conn != msgConn) {
			SteamDewNetUtils.CloseConnection(msgConn);
			continue;
		}

		this.gameServer.processIncomingMessage(msg);
	}
}

public override void receiveMessages()
{
	if (!this.connected()) {
		return;
	}

	this.PollFarmhandRequests();
	this.PollPeers();
}

private void sendMessage(PeerData peer, OutgoingMessage message)
{
	if (!this.connected()) {
		return;
	}
	if (peer.Conn == HSteamNetConnection.Invalid) {
		return;
	}
	SteamDewNetUtils.SendMessage(peer.Conn, message, bandwidthLogger);
}

public override void sendMessage(long peerId, OutgoingMessage message)
{
	PeerData peer = this.FarmerToPeer(peerId);
	if (peer == null) {
		return;
	}
	this.sendMessage(peer, message);
}

public override bool connected()
{
	if (this.Listener == HSteamListenSocket.Invalid) {
		return false;
	}
	if (this.PeerGroup == HSteamNetPollGroup.Invalid) {
		return false;
	}
	return true;
}

public override bool canOfferInvite()
{
	return this.connected();
}

public override void offerInvite()
{
	if (!this.connected()) {
		return;
	}
	if (!this.Lobby.IsValid() || !this.Lobby.IsLobby()) {
		return;
	}
	SteamFriends.ActivateGameOverlayInviteDialog(this.Lobby);
}

public override string getInviteCode()
{
	return Base36.Encode(this.Lobby.m_SteamID);
}

public override string getUserId(long farmerId)
{
	PeerData peer = this.FarmerToPeer(farmerId);
	if (peer == null) {
		return null;
	}
	return peer.SteamID.m_SteamID.ToString();
}

public override bool hasUserId(string userId)
{
	foreach (PeerData p in this.Peers.RightValues) {
		if (p.SteamID.m_SteamID.ToString().Equals(userId)) {
			return true;
		}
	}
	return false;
}

public override float getPingToClient(long farmerId)
{
	PeerData peer = this.FarmerToPeer(farmerId);
	if (peer == null) {
		return -1.0f;
	}
	SteamNetworkingQuickConnectionStatus status;
	SteamNetworkingSockets.GetQuickConnectionStatus(
		peer.Conn,
		out status
	);
	return status.m_nPing;
}

public override bool isConnectionActive(string connection_id)
{
	foreach (PeerData p in this.Peers.RightValues) {
		if (GetConnectionId(p.SteamID) == connection_id) {
			return true;
		}
	}
	return false;
}

public override string getUserName(long farmerId)
{
	PeerData peer = this.FarmerToPeer(farmerId);
	if (peer == null) {
		return null;
	}
	return SteamFriends.GetFriendPersonaName(peer.SteamID);
}

public override void setLobbyData(string key, string value)
{
	if (this.LobbyData == null) {
		return;
	}
	this.LobbyData[key] = value;
	if (!this.Lobby.IsValid() || !this.Lobby.IsLobby()) {
		return;
	}
	SteamMatchmaking.SetLobbyData(this.Lobby, key, value);
}

public override void kick(long disconnectee)
{
	base.kick(disconnectee);
	PeerData peer = this.FarmerToPeer(disconnectee);
	if (peer == null) {
		return;
	}
	Farmer player = Game1.player;
	StardewValley.Object[] data = new StardewValley.Object[0];
	sendMessage(peer, new OutgoingMessage(23, player, data));
}

public override void playerDisconnected(long disconnectee)
{
	if (this.FarmerToPeer(disconnectee) == null) {
		return;
	}
	base.playerDisconnected(disconnectee);
	this.Peers.RemoveLeft(disconnectee);
}

} /* class SteamDewServer */

} /* namespace SteamDew.SDKs */