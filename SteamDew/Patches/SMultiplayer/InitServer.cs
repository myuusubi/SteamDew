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

public class InitServer : Patcher {

public InitServer()
{
	MethodInfo m = SteamDew.SMultiplayerType.GetMethod(
		"InitServer", 
		BindingFlags.Public |BindingFlags.Instance
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
		if (!c.DeclaringType.Equals(SteamDew.SGalaxyNetServerType)) {
			continue;
		}
		instr.operand = typeof(SDKs.SteamDewServer).GetConstructor(
			new Type[] {
				typeof(IGameServer),
				typeof(object),
				typeof(Action<IncomingMessage, Action<OutgoingMessage>, Action>),
			}
		);
		break;
	}
	return instructions;
}

} /* class InitServer */

} /* namespace SteamDew.Patcher.SMultiplayer */