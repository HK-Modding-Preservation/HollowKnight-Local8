# v0.11.2-test13

- Soul statues share their normal SOUL units with every eligible living player
  within 12 units. Native orb count, hit cooldown and remaining statue uses
  are retained. The visual disperses outward and fades around the statue;
  it does not fly to P1 or play the native P1 collection effect.
- Automatic terrain recovery now requires substantial penetration with the
  body's center inside blocking terrain. Foot/edge overlaps, effector surfaces,
  ignored collider pairs/layers and the post-recovery protection window do
  not trigger it. Actual falling-vine hazard recovery remains unchanged.

Enemy detection/Baldur, camera framing, controllers and all embedded UI assets
are unchanged from test12. Unpublished runtime 0.11.2-test13, assembly/file
0.11.1.13. Native Unity/GPU playtesting remains outside automated validation.
