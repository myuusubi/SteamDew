using Galaxy.Api;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace SteamDew {

internal sealed class SteamDew : Mod {

public const string PROTOCOL_VERSION = "1.5.5";

private static SteamDew Instance;

public static Type SMultiplayerType;
public static Type SGalaxyNetClientType;
public static Type SGalaxyNetServerType;

public override void Entry(IModHelper helper)
{
	SteamDew.Instance = this;

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


	var harmony = new Harmony(this.ModManifest.UniqueID);

	Patches.Patcher[] patchers = new Patches.Patcher[] {
		/* new Patches.SteamHelper.Initialize(), */
		/* new Patches.SteamHelper.OnEncryptedAppTicketResponse() */
		new Patches.SMultiplayer.InitClient(),
		new Patches.SMultiplayer.InitServer(),
		new Patches.SteamHelper.OnGalaxyStateChange()
	};

	foreach (Patches.Patcher patcher in patchers) {
		patcher.Apply(harmony);
	}
}

public static void Log(string s)
{
	if (SteamDew.Instance == null) {
		return;
	}
	SteamDew.Instance.Monitor.Log(s);
}

} /* class SteamDew */

} /* namespace SteamDew */