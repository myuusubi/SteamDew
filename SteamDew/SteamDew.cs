using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using System;
using System.Reflection;

namespace SteamDew {

internal sealed class SteamDew : Mod {

public const string PROTOCOL_VERSION = StardewValley.Multiplayer.protocolVersion;

private static SteamDew Instance;

public static Type SMultiplayerType;
public static Type SGalaxyNetClientType;
public static Type SGalaxyNetServerType;

public static bool Active;

public static SDKs.ClientType LastClientType;

public override void Entry(IModHelper helper)
{
	SteamDew.Instance = this;

	SteamDew.Active = true;

	SteamDew.LastClientType = SDKs.ClientType.Unknown;

	var harmony = new Harmony(this.ModManifest.UniqueID);

	Patches.PProgram.SDK sdkChecker = new Patches.PProgram.SDK();
	sdkChecker.Apply(harmony);

	if (!SteamDew.Active) {
		SteamDew.Log("SteamDew does not work on non-Steam builds. Disabling.", LogLevel.Error);
		return;
	}

	int checkLZ4 = 0;
	try {
		checkLZ4 = LZ4.compressBound(10419);
	} catch (Exception) {

	}
	if (checkLZ4 == 0) {
		SteamDew.Log("LZ4 could not be loaded. Disabling SteamDew.", LogLevel.Error);
		return;
	}

	foreach (Type t in Assembly.GetAssembly(helper.GetType()).GetTypes()) {
		string s = t.ToString();
		switch (s) {
		case "StardewModdingAPI.Framework.SMultiplayer":
			SteamDew.SMultiplayerType = t;
			break;
		case "StardewModdingAPI.Framework.Networking.SGalaxyNetClient":
			SteamDew.SGalaxyNetClientType = t;
			break;
		case "StardewModdingAPI.Framework.Networking.SGalaxyNetServer":
			SteamDew.SGalaxyNetServerType = t;
			break;
		default:
			continue;
		}
		SteamDew.Log($"Found Type: {s}");
	}

	Patches.Patcher[] patchers = new Patches.Patcher[] {
		new Patches.PFarmhandMenu.Update(),
		new Patches.PGalaxySocket.UpdateLobbyPrivacy(),
		new Patches.PGameServer.Ctor(),
		new Patches.PSMultiplayer.InitClient(),
		new Patches.PSMultiplayer.InitServer(),
		new Patches.PSteamHelper.OnGalaxyStateChange()
	};

	foreach (Patches.Patcher patcher in patchers) {
		patcher.Apply(harmony);
	}

	helper.Events.GameLoop.SaveLoaded += HandleSaveLoaded;
	helper.Events.GameLoop.ReturnedToTitle += HandleReturnedToTitle;
}

private void HandleSaveLoaded(object sender, SaveLoadedEventArgs evt)
{
	string steamDewMsg = null;

	switch (SteamDew.LastClientType) {
	case SDKs.ClientType.Galaxy:
		steamDewMsg = "The server is not using SteamDew. The connection may be less stable.";
		break;
	case SDKs.ClientType.SteamDew:
		steamDewMsg = "Connected to a SteamDew server!";
		break;
	}

	if (steamDewMsg != null) {
		StardewValley.Game1.chatBox.addInfoMessage(steamDewMsg);
		SteamDew.Log(steamDewMsg, LogLevel.Info);
	}
}

private void HandleReturnedToTitle(object sender, ReturnedToTitleEventArgs evt)
{
	SteamDew.LastClientType = SDKs.ClientType.Unknown;
}

public static void Log(string s, LogLevel level = LogLevel.Trace)
{
	if (SteamDew.Instance == null) {
		return;
	}
	SteamDew.Instance.Monitor.Log(s, level);
}

public static StardewValley.Multiplayer GetGameMultiplayer() {
	FieldInfo f = typeof(StardewValley.Game1).GetField(
		"multiplayer",
		BindingFlags.NonPublic | BindingFlags.Static
	);
	if (f == null) {
		SteamDew.Log($"Failed to access StardewValley.Game1.multiplayer");
		return null;
	}

	return (StardewValley.Multiplayer) (f.GetValue(null));
}

} /* class SteamDew */

} /* namespace SteamDew */