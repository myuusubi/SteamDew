using StardewValley.Network;
using Steamworks;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SteamDew.SDKs {

public class SteamDewNetUtils {

public static void HandleSteamMessage(IntPtr msgPtr, IncomingMessage msg, out HSteamNetConnection msgConn, BandwidthLogger bandwidthLogger)
{
	SteamNetworkingMessage_t msgSteam = Marshal.PtrToStructure<SteamNetworkingMessage_t>(msgPtr);

	msgConn = msgSteam.m_conn;

	byte[] msgBytes = new byte[msgSteam.m_cbSize];
	Marshal.Copy(msgSteam.m_pData, msgBytes, 0, msgBytes.Length);

	MemoryStream msgStream = new MemoryStream(msgBytes);
	msgStream.Position = 0L;

	BinaryReader msgReader = new BinaryReader(msgStream);

	msg.Read(msgReader);

	SteamNetworkingMessage_t.Release(msgPtr);

	if (bandwidthLogger == null) {
		return;
	}

	bandwidthLogger.RecordBytesDown(msgStream.Length);
}

public static void SendMessage(HSteamNetConnection msgConn, OutgoingMessage msg, BandwidthLogger bandwidthLogger)
{
	MemoryStream msgStream = new MemoryStream();
	BinaryWriter msgWriter = new BinaryWriter(msgStream);
	msg.Write(msgWriter);

	msgStream.Seek(0L, SeekOrigin.Begin);
	byte[] msgBytes = msgStream.ToArray();

	int msgSize = Convert.ToInt32(msgBytes.Length);

	/*
	IntPtr msgPtr = SteamNetworkingUtils.AllocateMessage(msgSize);

	SteamNetworkingMessage_t msgSteam = Marshal.PtrToStructure<SteamNetworkingMessage_t>(msgPtr);
	msgSteam.m_conn = msgConn;

	Marshal.Copy(msgBytes, 0, msgSteam.m_pData, msgSize);

	SteamNetworkingSockets.SendMessages(1, new SteamNetworkingMessage_t[] { msgSteam }, null);
	*/

	long pOutMessageNumber;

	IntPtr msgPtr = Marshal.AllocHGlobal(msgSize);
	Marshal.Copy(msgBytes, 0, msgPtr, msgSize);

	SteamNetworkingSockets.SendMessageToConnection(
		msgConn, 
		msgPtr, 
		Convert.ToUInt32(msgBytes.Length), 
		Constants.k_nSteamNetworkingSend_Reliable, 
		out pOutMessageNumber
	);
	
	Marshal.FreeHGlobal(msgPtr);

	if (bandwidthLogger == null) {
		return;
	}

	bandwidthLogger.RecordBytesUp(msgSize);
}

public static void CloseConnection(HSteamNetConnection conn)
{
	if (conn == HSteamNetConnection.Invalid) {
		return;
	}
	SteamNetworkingSockets.SetConnectionPollGroup(conn, HSteamNetPollGroup.Invalid);
	SteamNetworkingSockets.CloseConnection(conn, (int) ESteamNetConnectionEnd.k_ESteamNetConnectionEnd_App_Generic, null, true);
}

} /* class SteamDewNetUtils */

} /* namespace SteamDew.SDKs */