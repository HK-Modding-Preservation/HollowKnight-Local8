# v0.11.2 release candidate validation

Prepared from the complete test17 checkpoint for Hollow Knight 1.5.78.11833
and the matching Modding API. Runtime: 0.11.2; assembly/file/ModLinks: 0.11.2.0.
The GitHub and ModLinks wording was reviewed and approved by the user.

All gameplay source files and embedded assets are byte-identical to test17.
Only ReleaseInfo changes metadata. The 37-suite test17 evidence applies to
that identical code; this does not claim another live gameplay run. See the
individual test READMEs and VALIDATION_v0.11.2-test17.md for controlled IO.

The final review read current public issues #1/#2. Controller conflicts and
party enemy/Baldur sensors have the existing regression coverage. An added
Moss Charger detached Attack Range case uses the exact native CheckAlertRange.Apply
IL: world_interactions passes 1,875 checks, including each actor in 2/4/8 parties,
missing initial Enter, a disabled boss body, one actor leaving while another
stays, and last-actor exit. This exercises the Hidden-state range primitive;
it does not execute the full Godhome intro/CANCEL route, fight or animations.
Native in-game confirmation remains necessary for those scenes.

Known cosmetic limitation: Custom Knight body/HUD textures are independent,
but some attack sprites and particles can still follow its global skin. This
is documented, not described as fixed. Pale Court/content mods remain experimental.

Release checks cover game/API compilation, emitted versions/resources and
hook wiring, a deterministic DLL-only ZIP, archive integrity/extraction-byte
comparison, SHA-256 and the updated manifest's upstream ManifestType schema.
The catalog patch changes only Version, ZIP URL and ZIP SHA-256. Names,
description, URLs, author, integrations, dependencies and tags remain intact.
The current public base/catalog were verified read-only. A clean public-base
plus full-checkpoint reconstruction is rebuilt and byte-compared before delivery.

No game libraries, SDK files, user logs or local reference settings are public
source inputs. Native Unity campaigns/audio/rendering, physical controllers,
Steam Remote Play sessions and all mod combinations were not run here. This
is not a zero-bug or whole-game-completion guarantee. At validation time, publication had not yet been performed.
