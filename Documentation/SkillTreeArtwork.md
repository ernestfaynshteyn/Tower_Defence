# Skill tree artwork

Generated with the built-in image-generation tool on 2026-10-30.

Assets:
- `Assets/Resources/UI/SkillTreeSlimeEmblem.png` — transparent crowned slime assigned to the existing central Image, preserving its rectangle.
- `Assets/Resources/UI/SkillTreeBackdrop.png` — mossy stone background assigned to the existing tiled tree Image. Point filtering, no compression or mipmaps.

## Emblem prompt

Create a single game UI sprite: a cute green slime wearing a tiny gold crown, centered, front view, simple amateur hand drawn pixel art for a Unity slime tower defense game. Looks designed on a 32 by 32 pixel grid: chunky hard square pixels, dark 1-pixel outline, 5 green colors, two black square eyes and tiny smile, one small pale highlight. Crown three simple points. Flat simple style, not fancy, no gradients, no antialiasing, no text, no border, no extra objects. Transparent background, tightly bounded square composition with generous small clear margin. Output one sprite only.

## Background prompt

Game UI background texture for a slime tower defense upgrade tree. Square composition. Simple beginner hand drawn pixel art, chunky square pixels based on a 128x128 logical pixel canvas. Flat top-down mossy dungeon stone wall/tablet, dark muted forest green stone slabs, scattered tiny cracks, small subdued green slime drips and moss in corners and along outer edges. Center 75 percent is quiet dark desaturated green stone with very low contrast so pale green upgrade buttons and bright connectors will be readable on top. Edge details more character but still dark. Restrained palette 8 flat colors, no gradients, no smooth rendering, no blur, no lighting effects, no text, no letters, no buttons, no paths, no skill tree nodes, no perspective, no characters. Must work repeated across large scrolling UI surface: every edge dark matching muted green. Artwork only.

## Validation

Opened the gameplay scene in Play Mode and opened the upgrades panel. Confirmed the transparent emblem and mossy stone background both render behind the existing UI. No gameplay scripts or node layout changes were made. Existing animation and TMP line-breaking errors remain outside this artwork change.
