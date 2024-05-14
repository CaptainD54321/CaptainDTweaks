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
  * Note: Will clean off existing dirt, but not immediately on activating; for perfomance reasons, the boat is cleaned every time the game tries to add dirt, which happens once per gameday.
  * Disabled by default.
* Supply/Demand viewer
  * Shows the values for supply and demand values at distant ports in the trade book (negative supply = demand).
  * This uses the same system Raw Lion uses for the listed prices for goods, so note that the listed value is not the current supply value, but the value the number of days ago listed in the "days ago" column of the trade book.
  * Note: When you first start the game with this mod installed, listed supply values for most ports will be blank; this is not a bug, it simply takes time for the new price reports with the supply values to reach ports; all supply values should populate properly within a few hours of gameplay.
  * Enabled by default.
* Bug Fixes:
  * Fixed bad inventory rotation on the Aestrin compass

All features can be enabled/disabled via config file or [BepinEx Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager).

I plan to update this mod with other tweaks and features I would like to add to this game, although I do not currently have a comprehensive list of everything I would like to add.
