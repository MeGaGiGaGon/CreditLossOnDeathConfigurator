# CreditLossOnDeathConfigurator

Adds configs for the percentage of scrap lost on both recovered and unrecovered bodies. Also shows the new percentage lost on the round end screen. In the base game, 8% of credits are lost even when a body is recovered. This mod will also now include that on the round end screen. 

The config can be changed mid-game/mid-run and the changes will apply instantly. Required for both hosts and clients since I don't know how to re-sync credit amount.

Now also supports scaling credit loss on lobby size (works with the custom numbers) and disabling credit loss at the company (for those goobers that always end up dead).

Note that the percentage lost shown is exact, so it may be slightly off due to rounding (ie, 19.6% instead of 20%).

If anything breaks, feel free to DM me on discord at `gigagon`

# Changelog
V 1.0.5
- Added rounding on displayed credit loss percentage (can be disabled in config if you liked the old behavior)

V 1.0.4
- Added missing dependancy on HookGenPatcher
- Made mod not crash if another mod gets to the hooks first (Known: `Lategame_Upgrades`)
- Added UNLICENSE
- Updated readme slightly

V 1.0.3
- Fixed an IL hook error breaking the mod in single player
- Added clamping/nan checks to the % lost displayed to ensure nothing breaks
- Made the % lost displayed code always display the actual amount lost, even if other things break
- Added a bunch of `float` casts so the math actually works
- Changed how the credit scaling works to account for the fact party wipes exist, and also make the setting single player compatible

V 1.0.2
- Added `Disable Credit Loss At Company` and `Scale Credit Loss By Lobby Size` config options
- Updated `README.md` to be clearer
- Updated mod description

V 1.0.1
- Updated GitHub link
- Unbroke config path so games can properly end now

V 1.0.0
- Uploaded