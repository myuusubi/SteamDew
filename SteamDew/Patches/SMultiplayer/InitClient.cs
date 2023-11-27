using Galaxy.Api;
using HarmonyLib;
using StardewValley;
using StardewValley.Network;
using StardewValley.SDKs;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace SteamDew.Patches.SMultiplayer {

public class InitClient : Patcher {

public InitClient()
{
	MethodInfo m = SteamDew.SMultiplayerType.GetMethod(
		"InitClient", 
		BindingFlags.Public | BindingFlags.Instance
	);

	this.DeclaringType = m.DeclaringType;
	this.Name = m.Name;

	this.Transpiler = new HarmonyMethod(
		this.GetType().GetMethod(
			"PatchTranspiler", 
			BindingFlags.NonPublic | BindingFlags.Static
		)
	);
}

private static IEnumerable<CodeInstruction> PatchTranspiler(IEnumerable<CodeInstruction> instructions)
{
	foreach (CodeInstruction instr in instructions) {
		if (instr.opcode != OpCodes.Newobj) {
			continue;
		}
		if (!(instr.operand is ConstructorInfo)) {
			continue;
		}
		ConstructorInfo c = instr.operand as ConstructorInfo;
		if (!c.DeclaringType.Equals(SteamDew.SGalaxyNetClientType)) {
			continue;
		}
		instr.operand = typeof(SDKs.SteamDewClient).GetConstructor(
			new Type[] {
				typeof(GalaxyID),
				typeof(Action<IncomingMessage, Action<OutgoingMessage>, Action>),
				typeof(Action<OutgoingMessage, Action<OutgoingMessage>, Action>)
			}
		);
		break;
	}
	return instructions;
}

} /* class InitClient */

} /* namespace SteamDew.Patcher.SMultiplayer */