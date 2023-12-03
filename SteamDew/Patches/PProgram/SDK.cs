using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace SteamDew.Patches.PProgram {

public class SDK : Patcher {

public SDK()
{
	MethodInfo m = typeof(StardewValley.Program).GetMethod(
		"get_sdk", 
		BindingFlags.NonPublic | BindingFlags.Static
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
		if (c.DeclaringType.ToString() != "StardewValley.SDKs.SteamHelper") {
			SteamDew.Active = false;
		}
		break;
	}
	return instructions;
}

} /* class SDK */

} /* namespace SteamDew.Patcher.PProgram */