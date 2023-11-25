using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace SteamDew {

internal sealed class ModEntry : Mod {

public override void Entry(IModHelper helper)
{
	helper.Events.Input.ButtonPressed += this.OnButtonPressed;
}


private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
{
	if (!Context.IsWorldReady)
	return;

	this.Monitor.Log($"{Game1.player.Name} pressed {e.Button}.", LogLevel.Debug);
}

} /* class ModEntry */

} /* namespace SteamDew */