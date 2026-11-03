# Stone tower and mounted cannons

The approved first tower from `MainTowerConcepts.png` is installed as `Assets/Resources/WeaponIcons/StoneTower.png` on the existing Player SpriteRenderer. It uses Point filtering, no compression, 16 pixels per unit and the existing player scale, matching the collider footprint. Player color is white so the stone artwork is not darkened.

All six existing `Assets/Resources/WeaponIcons/*.png` weapon assets were replaced in place, preserving their GUIDs so the loadout, icon prefabs and equipped-weapon renderer use the same new artwork. Sprites are cropped to their main connected silhouette and fitted to 32x32. The equipped visual is offset 0.25 world units from the tower center to sit on its mounting plate.

## Generation

Built-in image-generation tool. One call per cannon using the following complete prompt template, with `{design}` substituted from the list below:

One isolated mounted tower cannon game sprite: {design}. Simple hand-drawn beginner pixel art designed on a 32x32 logical grid, chunky crisp square pixels and dark outlines, 5-7 flat colors. Top-down plan view, barrel pointing horizontally RIGHT, small round mounting pivot at LEFT rear, no grip, no trigger, no handheld gun silhouette. Fits on a stone defense tower. Complete single connected cannon silhouette, centered with clear transparent margin. No tower base underneath, no faces, no eyes, no text, no lightning flying in front, no muzzle flash, no flames, no smoke, no particles, no loose pixels, no glow, no shadows, no gradients, no antialiasing. Genuine transparent background. ONE cannon only, not a sprite sheet.

- RapidBullets: olive green automatic cannon with a short thick barrel and chunky rear block
- PiercingLaser: blue steel energy cannon with a narrow long barrel and small cyan energy core
- ShotgunBurst: rust brown wide stubby scatter cannon with a visibly broad dark muzzle
- Flamethrower: orange steel flamethrower cannon with thick nozzle and small red fuel tank attached at rear
- LightningArc: purple and gold coil cannon with two small coils along its barrel, no bolts or sparks outside the gun
- ToxicSpitter: dark green chunky poison launcher cannon with small bright green tank attached at rear

## Validation

Inspected the six resulting 32x32 sprite files and checked the edited mounting code for whitespace errors. Unity Play Mode validation pending Editor compilation.

