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

public override void Entry(IModHelper helper)
{
	SteamDew.Instance = this;

	var harmony = new Harmony(this.ModManifest.UniqueID);

	Patches.Patcher[] patchers = new Patches.Patcher[] {
		new Patches.SteamHelper.Initialize(),
		new Patches.SteamHelper.OnEncryptedAppTicketResponse()
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