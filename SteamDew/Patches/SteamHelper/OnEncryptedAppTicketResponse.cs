using Galaxy.Api;
using HarmonyLib;
using StardewValley;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace SteamDew.Patches.SteamHelper {

public class OnEncryptedAppTicketResponse : Patcher {

public OnEncryptedAppTicketResponse()
{
	MethodInfo m = typeof(StardewValley.SDKs.SteamHelper).GetMethod(
		"onEncryptedAppTicketResponse", 
		BindingFlags.NonPublic | BindingFlags.Instance
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

private static void InjectNetHelper(StardewValley.SDKs.SteamHelper helper)
{
	int connectionProgress = helper.ConnectionProgress;

	MethodInfo scp = helper.GetType().GetMethod(
		"set_ConnectionProgress", 
		BindingFlags.NonPublic | BindingFlags.Instance
	);

	MethodInfo scf = helper.GetType().GetMethod(
		"set_ConnectionFinished", 
		BindingFlags.NonPublic | BindingFlags.Instance
	);

	FieldInfo net = helper.GetType().GetField(
		"networking",
		BindingFlags.NonPublic | BindingFlags.Instance
	);

	scp.Invoke(helper, new object[] { connectionProgress + 4 });
	scf.Invoke(helper, new object[] { true });

	net.SetValue(helper, new SDKs.SteamDewNetHelper());

	SteamDew.Log($"ConnectionProgress: {helper.ConnectionProgress}");
	SteamDew.Log($"ConnectionFinished: {helper.ConnectionFinished}");
	SteamDew.Log($"Networking: {helper.Networking.GetType().ToString()}");
}

private static IEnumerable<CodeInstruction> PatchTranspiler(IEnumerable<CodeInstruction> instructions)
{
	int state = 0;
	foreach (CodeInstruction instr in instructions) {
		switch (state) {
		case 0:
			if (instr.opcode != OpCodes.Ldc_I4) {
				break;
			}
			if (!(instr.operand is Int32)) {
				break;
			}
			Int32 i = (Int32) instr.operand;
			if (i == 1024) {
				state = 1;
			}
			break;
		case 1:
			if (instr.opcode != OpCodes.Ldloc_0) {
				break;
			}
			instr.opcode = OpCodes.Ldarg_0;
			instr.operand = null;
			state = 2;
			break;
		case 2:
			state = 3;
			break;
		case 3:
			if (instr.opcode != OpCodes.Call) {
				break;
			}
			if (!(instr.operand is MethodInfo)) {
				break;
			}
			MethodInfo m = instr.operand as MethodInfo;
			if (m.DeclaringType != typeof(StardewValley.SDKs.SteamHelper)) {
				break;
			}
			if (m.Name != "set_ConnectionProgress") {
				break;
			}

			m = typeof(OnEncryptedAppTicketResponse).GetMethod(
				"InjectNetHelper", 
				BindingFlags.NonPublic | BindingFlags.Static
			);

			instr.opcode = OpCodes.Call;
			instr.operand = m;

			state = 4;
			break;
		}

		switch (state) {
		case 1:
		case 3:
			instr.opcode = OpCodes.Nop;
			instr.operand = null;
			break;
		default:
			break;
		}
	}
	return instructions;
}

} /* class OnEncryptedAppTicketResponse */

} /* namespace SteamDew.Patcher.SteamHelper */