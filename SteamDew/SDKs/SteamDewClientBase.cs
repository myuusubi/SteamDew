using Galaxy.Api;
using StardewValley.Network;
using StardewValley.SDKs;
using Steamworks;
using System;
using System.Collections.Generic;

namespace SteamDew.SDKs {

public class SteamDewClientBase : Client {

public override void sendMessage(OutgoingMessage message)
{

}

protected override void receiveMessagesImpl()
{

}

public override string getUserID()
{
	return Convert.ToString(GalaxyInstance.User().GetGalaxyID().ToUint64());
}

protected override string getHostUserName()
{
	return "";
}

public override void disconnect(bool neatly = true)
{

}

protected override void connectImpl()
{

}

}

}