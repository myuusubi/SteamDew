using Galaxy.Api;
using HarmonyLib;
using StardewValley;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace SteamDew.Patches.SteamHelper {

public class Initialize : Patcher {

public Initialize()
{
	this.DeclaringType = typeof(StardewValley.SDKs.SteamHelper);
	this.Name = nameof(StardewValley.SDKs.SteamHelper.Initialize);

	this.Transpiler = new HarmonyMethod(
		this.GetType().GetMethod(
			"PatchTranspiler", 
			BindingFlags.NonPublic | BindingFlags.Static
		)
	);
}

private static IEnumerable<CodeInstruction> PatchTranspiler(IEnumerable<CodeInstruction> instructions)
{
	int state = 0;
	foreach (CodeInstruction instr in instructions) {
		string s;

		switch (state) {
		case 0:
			if (instr.opcode != OpCodes.Ldstr) {
				continue;
			}
			if (!(instr.operand is string)) {
				continue;
			}
			s = instr.operand as string;
			if (s == "Initializing GalaxySDK") {
				state = 1;
			}
			break;
		case 1:
			if (instr.opcode != OpCodes.Call) {
				break;
			}
			if (!(instr.operand is MethodInfo)) {
				break;
			}
			MethodInfo m = instr.operand as MethodInfo;
			if (m.DeclaringType != typeof(Galaxy.Api.GalaxyInstance)) {
				break;
			}
			if (m.Name != "Init") {
				break;
			}
			state = 2;
			break;
		case 2:
			state = 3;
			break;
		case 3:
			if (instr.opcode != OpCodes.Stfld) {
				break;
			}
			if (!(instr.operand is FieldInfo)) {
				break;
			}
			FieldInfo f = instr.operand as FieldInfo;
			if (f.DeclaringType != typeof(StardewValley.SDKs.SteamHelper)) {
				break;
			}
			if (f.Name != "encryptedAppTicketResponse" ) {
				break;
			}
			state = 4;
			break;
		case 4:
			state = 5;
			break;
		case 5:
			if (instr.opcode != OpCodes.Ldstr) {
				break;
			}
			if (!(instr.operand is string)) {
				break;
			}
			s = instr.operand as string;
			if (s == "Requesting Steam app ticket") {
				state = 6;
			}
			break;
		}

		switch( state ) {
		case 1:
		case 2:
		case 5:
			instr.opcode = OpCodes.Nop;
			instr.operand = null;
			break;
		}
	}
	return instructions;
}

} /* class Initialize */

} /* namespace SteamDew.Patcher.SteamHelper */