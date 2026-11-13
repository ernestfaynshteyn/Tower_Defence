# Game audit — 10 November 2026

Baseline: `45e176d` (`build it, submit`). Read-only review of first-party gameplay, upgrades, waves, abilities, throwables, records, settings, scene routing, and serialized UI. No Play Mode, scene saves, fixes, package changes, or build performed. This report is the only added project file.

## Prioritized findings

### A01 — Medium, confirmed: menu clicks can consume throwables; pause does not block nuke input

`Assets/Scripts/Combat/Grenades/UseGrenade.cs:82–100,176` checks the left mouse button without checking `EventSystem.IsPointerOverGameObject()` or `Time.timeScale`. With a grenade equipped, clicking a shop or milestone button can consume inventory and begin a throw. `Assets/Scripts/Abilities/NukeAbility.cs:48–54` likewise accepts N during a paused milestone menu. The grenade's delay pauses, but inventory has already been deducted; a nuke can be instantiated and its cooldown assigned while paused. The newer `TowerAbilityController.CanActivate` already checks pause.

Fix: gate combat input consistently on pause and UI focus, before spending inventory or starting cooldowns. Validate clicking shop/milestone controls with a grenade equipped, and N during milestones.

### A02 — Medium, confirmed: huge waves can spawn before their warning ends

`Assets/Scripts/Waves/WaveManager.cs:159–168` resets spawn counts and prepares the next wave before waiting through the huge-wave warning. `Assets/Scripts/Waves/SpawnPoint.cs:33–50` only checks the counts, not whether spawning is allowed. Therefore the spawner can create enemies during the warning, before `StartWave()` updates the label and raises the milestone event.

Fix: expose and enforce a spawning-allowed state. Validate no enemies appear until the warning completes, including huge waves on milestone boundaries.

### A03 — Medium, confirmed: one smoke cloud can cancel another cloud's slow

`Assets/Scripts/Enemies/EnemyHealth.cs:206–221` stores one slow multiplier. `Assets/Scripts/Combat/Grenades/UseGrenade.cs:508–533` resets every affected enemy to normal speed when a cloud expires, even if another cloud or effect still applies. It also holds a slow until the entire cloud ends rather than removing it when the enemy leaves.

Fix: track slow sources and expiry/area membership independently; use the strongest active slow. Validate overlapping clouds with staggered expiry and an enemy moving out of smoke.

### A04 — Medium, confirmed: visible shot paths and hit detection disagree

`Assets/Scripts/PrototypeArmory/PrototypeArmoryCombat.cs:384–391,421–443,496–500` draws shots from `GetWeaponMuzzlePosition()` but raycasts from the tower center. A target between the center and barrel can be hit without the visible shot passing through it; spread paths can also differ from the shown muzzle-to-end lines.

Fix: share the firing origin and trajectory between visuals and damage, explicitly preserving the intended tower-centered maximum range. Validate close targets, spread, multishot, and range-edge targets for all weapons.

### A05 — Medium, likely balance problem: lifesteal is based on enemy maximum health

`Assets/Scripts/PrototypeArmory/PrototypeArmoryCombat.cs:1660–1663` heals `enemy.MaxHealth * lifestealAmount` on a successful roll, regardless of actual damage dealt. The skill definitions allow 185% lifesteal amount and 95% chance when fully upgraded. Even a weak hit on a high-health boss can therefore refill the tower, making later combat nearly harmless. This is confirmed math; whether the formula is intended is a design decision.

Fix: define a base lifesteal fraction and apply healing upgrades to it, or explicitly cap healing. Prefer actual damage dealt if conventional lifesteal is intended. Validate weak rapid hits against a high-health boss.

### A06 — Medium, confirmed limit; crowd impact needs testing: Sewer Pulse only queries 128 colliders

`Assets/Scripts/Abilities/TowerAbilityController.cs:44–45,92–96` uses a fixed 128-collider buffer without overflow handling. The gameplay scene permits 200 enemies per wave, and boss splits can add more. In a dense crowd the pulse can miss enemies inside its radius, and irrelevant colliders can consume buffer capacity.

Fix: use a filtered expandable query or detect saturation and retry with a larger buffer. Validate more than 128 nearby enemy colliders.

### A07 — Medium, likely: nuke screen-clear leaves boss split children alive

`Assets/Scripts/Abilities/NukeExplosion.cs:94–107` clears a snapshot of existing enemies. `Assets/Scripts/Enemies/MegaSlimeBehaviour.cs:41–81` spawns children synchronously from the parent's death event. Those children were not in the nuke snapshot, so they survive. If the nuke promises a complete screen-clear, this is inconsistent; retaining split enemies could instead be intentional.

Fix: decide whether splitting should be suppressed during screen-clear damage or whether newly created children should also be cleared. Validate a nuke killing the MEGA boss.

### A08 — Medium, confirmed configuration: loss-screen records are too large and low contrast

`Assets/Scripts/UI/Menus/GameOverUI.cs:87–96` creates a 280-unit-high label at font size 96, with multiple lines, no autosizing and black text. `Assets/Scenes/You lost - Armory Prototype.unity:1161` leaves the records reference empty, so this creation path is used. The newly installed background is dark. The result is a strong overflow/readability risk, independent of the upcoming overall sizing pass.

Fix: author a dedicated records area with a fitting body font, explicit wrapping/overflow behavior, and readable contrast. Validate the longest formatted records at 1080p and 4K.

### A09 — Low/medium, confirmed implementation: poison loses damage under long frames

`Assets/Scripts/PrototypeArmory/PrototypePoisonStatus.cs:26–36` applies only one half-second tick per Update and resets the timer to 0.5 instead of carrying overdue time. During long frames it drops ticks, so nominal DPS decreases under load. A duration shorter than 0.5 seconds still receives a full half-second tick immediately.

Fix: accumulate elapsed time with bounded catch-up or integrate damage over elapsed active duration. Validate the same poison duration at different frame rates.

### A10 — Medium workflow limitation, confirmed: generated C# project is stale

`Assembly-CSharp.csproj` contains 65 compile inputs, 45 of which reference missing old paths such as `Assets/Scripts/UseGrenade.cs`. The actual scripts now live in categorized folders. This prevents a meaningful external compile with the generated project; it is not evidence that Unity's own compiler fails.

Fix: regenerate IDE project files from Unity. Then check Unity compilation and Console without needing Play Mode.

## UI sizing inventory for the next pass

- Title and loss scene Canvas reference sizes: **800 × 600**.
- Selection and gameplay reference sizes: **3840 × 2160**.
- Milestone runtime Canvas reference: **1920 × 1080** (`MilestoneManager.cs:358`).
- Gameplay contains **21 distinct authored TMP font sizes**, from **11 to 52**, before ancestor scaling or runtime overrides.
- Gameplay includes RectTransforms scaled **7 × 11** and **8 × 10**, which stretch visuals and text differently by axis. Loss includes scale **7 × 7**; selection includes **5 × 5**.
- Runtime scripts override typography and layout: `PrototypeLoadoutSelection`, `PrototypeArmoryBootstrap`, `GameSettingsPopup`, `GameOverUI`, `HealthBar`, `MilestoneManager`, and `skills`. Editing scene font sizes alone will not give a consistent result.
- `PrototypeArmoryBootstrap.ConfigureShopForCurrentResolution` additionally scales the shop frame between 0.25 and 1, changing the apparent sizes of its children.

Recommended sizing work: choose a shared screen reference and a small hierarchy of title, heading, button, body, and value sizes; normalize unintended nonuniform scales; use consistent button heights, padding, and gaps; then reconcile runtime overrides. Preserve deliberate differences between headings and descriptions rather than setting every label to one size.

## Healthy checks and excluded false positives

- All four production scenes are enabled and the centralized routes match them. Retry and Reselect use different destinations.
- Early-wave health/damage multipliers remain 50% for waves 1–2, 75% for 3–4, and 100% thereafter; movement speed is separate.
- Enemy contact damage checks actual root-collider overlap on physics ticks and does not depend on rotation callbacks.
- Current critical-damage skill totals are exactly 100%; the chance-style cap is not currently discarding those authored upgrades. An earlier suspicion was rejected after checking the definitions.
- The gameplay skill tree explicitly disables PlayerPrefs save/load. Delayed restoration of persistent tree levels is therefore not reported as a current bug.
- Package-owned GUID references are not missing scripts merely because they are absent under Assets. The initial Assets-only reference scan cannot establish package-reference failures.

## Coverage and limits

Inspected the four production scene files, first-party scripts across the systems above, shared slime prefab collider configuration, build-scene routes, package manifest, and authored UI values. Vendor/demo scenes were excluded from gameplay findings. No first-party automated test suite was identified in the reviewed source inventory.

No Play Mode, visual screen capture, full build, profiler, target-device test, or successful compile was performed. A full package-inclusive GUID scan was stopped because it was taking too long; exhaustive missing-reference validation remains incomplete. The live Editor log could not be read consistently due to its file lock, so this report makes no claim of a clean Console. Findings marked likely require a focused user playtest.

Existing rendering/project-setting edits were left untouched. Recommended order: fix input/wave timing and combat inconsistencies first, then normalize UI canvases/scales and typography together.
