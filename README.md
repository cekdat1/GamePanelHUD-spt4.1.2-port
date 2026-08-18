# GamePanelHUD — SPT 4.1.2 client port (unofficial)

> ⚠️ **This is an unofficial, AI-assisted port, not an official release.** See the disclaimer below before you install it.

## What this is

[GamePanelHUD](https://github.com/kmyuhkyuk/GamePanelHUD) is a Tarkov HUD expansion (ammo panel, compass, hit markers, kill feed, and more), originally by **[kmyuhkyuk](https://github.com/kmyuhkyuk)**. It depends on [KmyTarkovApi](https://github.com/kmyuhkyuk/KmyTarkovApi).

The official releases target an older EFT client build (`0.16.1.3.35392`). This port retargets it to the client version shipped with **SPT 4.1.2** (`0.16.9.40743`), so it loads and runs on a current SPT install. You also need the matching [KmyTarkovApi port](https://github.com/cekdat1/KmyTarkovApi-spt4.1.2-port) — this won't load without it.

## ⚠️ Disclaimer

This port was **fixed and ported by Claude (Anthropic's AI)**, working from the original open-source code, at the request of a user porting it for their own SPT 4.1.2 install. Please read this in full before using it:

- **Not fully tested.** Every fix here was verified by compiling against the real client DLL, and this build has been run in one real SPT install through a full raid with no load errors. One real runtime bug (ammo HUD not appearing) was found post-install and fixed the same way. That's meaningfully more confidence than a port that only compiles, but it is still one person's playtesting, not exhaustive QA, and not tested by the original author.
- **Not affiliated with kmyuhkyuk.** This is a community/AI patch on top of their work, not an official update.
- **What's included (verified working):** `GamePanelHUDCore`, `GamePanelHUDHit`, `GamePanelHUDKill`, `GamePanelHUDCompass`, `GamePanelHUDWeapon`, `GamePanelHUDGrenade`.
- **What's NOT included / possibly missing:**
  - **`GamePanelHUDHealth`** was never ported — it's excluded entirely from `Release/`. Installing the old, unported DLL alongside these risks a load failure against the new client, so it's just left out. Someone still needs to port this one.
  - **`GamePanelHUDMap` and `GamePanelHUDDebug`** are not included and were not fixable by porting alone — their whole implementation calls `GamePanelHUDCorePlugin.HUDCoreClass`/`HUDClass<T1,T2>`, which don't exist anywhere in the current `GamePanelHUDCore` source at all. That's the original author's own module having drifted out of sync with their own refactored Core (predates this port, unrelated to the client version), not something a rename/reflection fix can solve. Reconstructing them would mean writing new logic from scratch, not a verified port.
  - **Fika multiplayer support removed** from the Hit module (`GamePanelHUDHit/Patches/CoopApplyShot.cs` deleted) — this port is **singleplayer-only**. Ordinary singleplayer hit detection is unaffected.
  - The raw IL byte-patch in `GamePanelHUDHit/Patches/ApplyDamage.cs` had its type rename verified, but the instruction-offset math it relies on could only be confirmed correct by actually testing it in-game (which has now happened once, successfully, but flagging it as the single most fragile piece of code in this port).
  - The `KmyTarkovConfiguration` in-game settings menu (F12) is not part of this port either (see the KmyTarkovApi pack) — all mod settings live in the plain BepInEx `.cfg` files under `BepInEx/config/`, editable by hand.

## Verified fixes (what actually broke and what changed)

All found by reflecting directly against the real client DLL:

| Old (obfuscated-era) type | New (0.16.9.40743) type |
|---|---|
| `DamageInfoStruct` | `EFT.Ballistics.DamageInfo` |
| `SearchableItemItemClass` | `EFT.InventoryLogic.SearchableItem` |
| `MagazineItemClass` | `EFT.InventoryLogic.Magazine` |
| `ThrowWeapItemClass` | `EFT.InventoryLogic.ThrowWeap` |
| `LauncherItemClass` | `EFT.InventoryLogic.Launcher` |

Also fixed: `EFT.IUpdate` now exists in the client and collides with `KmyTarkovUtils.IUpdate` — disambiguated in `GamePanelHUDKill`, `GamePanelHUDCompass`, and `GamePanelHUDWeapon`.

**One real post-install bug found and fixed** (in the `KmyTarkovApi` dependency, not this repo, but worth knowing): ammo HUD display silently broke at runtime because a duck-typed animator lookup started matching two client types instead of one. Full writeup in the [KmyTarkovApi README](https://github.com/cekdat1/KmyTarkovApi-spt4.1.2-port/blob/master/README.md).

## Installation

1. Install the matching [KmyTarkovApi port](https://github.com/cekdat1/KmyTarkovApi-spt4.1.2-port) first.
2. Download `Release/kmyuhkyuk-GamePanelHUD/` from this repo.
3. Copy it into `<your SPT install>\BepInEx\plugins\`.
4. Result: `<SPT install>\BepInEx\plugins\kmyuhkyuk-GamePanelHUD\` (with `GamePanelHUDCore.dll` in its own `core\` subfolder — that's the original layout, BepInEx scans subfolders fine).
5. Launch and check the BepInEx console / `LogOutput.log` for load errors.

## Building from source

Requires the .NET Framework 4.7.2 targeting pack (or the `Microsoft.NETFramework.ReferenceAssemblies.net472` NuGet package, already referenced in the `.csproj` files), a built copy of the [KmyTarkovApi port](https://github.com/cekdat1/KmyTarkovApi-spt4.1.2-port), and your own SPT install's client DLLs.

Each `.csproj`'s `<Reference>` `HintPath`s point at `D:\SPT\...` and `..\..\KmyTarkovApi\...` — **update these to your own paths** before building:
- `D:\SPT\EscapeFromTarkov_Data\Managed\*.dll`, `D:\SPT\BepInEx\core\*.dll`
- The `KmyTarkovApi.dll`/`KmyTarkovReflection.dll`/`KmyTarkovUtils.dll` paths, pointing at your built KmyTarkovApi port

```
dotnet build GamePanelHUDCore\GamePanelHUDCore.csproj
dotnet build GamePanelHUDHit\GamePanelHUDHit.csproj
dotnet build GamePanelHUDKill\GamePanelHUDKill.csproj
dotnet build GamePanelHUDCompass\GamePanelHUDCompass.csproj
dotnet build GamePanelHUDWeapon\GamePanelHUDWeapon.csproj
dotnet build GamePanelHUDGrenade\GamePanelHUDGrenade.csproj
```

## Credits & license

- **Original mod:** [kmyuhkyuk](https://github.com/kmyuhkyuk/GamePanelHUD) — all credit for the design and the vast majority of the code.
- **4.1.2 client port, bug fixes:** done by Claude (Anthropic), at a user's request, working from the original GPL-3.0 source.
- **License:** GPL-3.0, inherited from the original project (see `LICENSE`). This port is distributed under the same terms.
