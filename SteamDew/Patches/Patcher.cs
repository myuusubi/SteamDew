using HarmonyLib;
using System;

namespace SteamDew.Patches {

public class Patcher {

public Type DeclaringType = null;
public string Name = null;

public HarmonyMethod Prefix = null;
public HarmonyMethod Postfix = null;
public HarmonyMethod Transpiler = null;

public void Apply(Harmony harmony)
{
	harmony.Patch(
		original: AccessTools.Method(this.DeclaringType, this.Name),
		prefix: this.Prefix,
		postfix: this.Postfix,
		transpiler: this.Transpiler
	);
}

} /* class Patcher */

} /* namespace SteamDew */