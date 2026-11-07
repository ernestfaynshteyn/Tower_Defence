# Stationary tower contact damage review

No Play Mode or gameplay tests were run, as requested.

## Findings and fixes

- EnemyMovement stopped at a fixed centre distance of 1.5, irrespective of collider geometry. The player box is 2 x 2 at scale 0.7 (half-width 0.7). Ordinary green slime half-width is about 0.57, so a horizontally approaching slime can stop with a gap of about 0.23. Vertical contacts also depend on the slime collider's downward offset. Movement now continues until the actual root colliders overlap.
- EnemyDamage advanced its attack timer exclusively in OnTriggerStay2D. The slime prefabs use dynamic bodies with sleeping enabled. Sleeping contacts need not continue delivering Stay callbacks; rotating the player could wake contact processing. Damage now checks collider overlap in FixedUpdate, independently of sleep and player rotation. It checks only the root player hurtbox, not range/weapon visuals, and ignores dead enemies or disabled colliders.

## Other paths inspected

PlayerHealth applies dodge, defence absorption, and health damage without a weapon-specific immunity branch. AcidSlimeBehaviour and ElectricSlimeBehaviour call those same damage methods; ToxicSlimePoisonTrail uses its own distance/tick check. PrototypeArmoryCombat's lightning damage/stun affects EnemyHealth, not player invulnerability. The shared enemy trigger configuration was inspected across the six slime prefabs; no prefab or hurtbox sizes were changed.

## Validation and remaining checks

Scene/prefab and code inspection only. The external dotnet build is blocked by the existing stale Assembly-CSharp.csproj: 45 missing-source errors reference old script locations. No generated project files were changed to address that unrelated issue.

User playtest: allow a surviving slime to reach the stationary tower from above, below, left and right, with auto-shoot off, then repeat using lightning. Contact damage should recur at each slime's configured rate without rotating. Check defence loss as well as health loss; defence can absorb hits. Lightning can still legitimately kill/stun enemies before they reach the tower. Repeat with acid, electric, toxic, and boss slimes to check their special attacks.
