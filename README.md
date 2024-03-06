# CaptainDTweaks
My various tweaks for the game Sailwind.

Current features: 
* Food overflow:
  * Eating food or drinking water that would bring the respective stat above 100% no longer wastes food/water, but instead overflows the bar.
  * When food/water is overflowed, you will be blocked from consuming more, and the food/water value will decrease as normal until it goes below 100% again.
  * Currently, there is no way to see how much a bar is overflowing by, but I plan to implement that in a future update.
  * Overflow values are capped at 200% to prevent glitched food items (I'm looking at you, VOIDFISH) from overflowing food/water values to arbitrarily high values.
  * Enabled by default.
* No dirt:
  * Prevents dirt from building up on boats.
  * Note: Does not clean dirt off already dirty boats; just take your boat to the shipyard and have them clean it, or use your broom.
  * Disabled by default.
 
All features can be enabled/disabled via config file or [BepinEx Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager).

I plan to update this mod with other tweaks and features I would like to add to this game, although I do not currently have a comprehensive list of everything I would like to add.
