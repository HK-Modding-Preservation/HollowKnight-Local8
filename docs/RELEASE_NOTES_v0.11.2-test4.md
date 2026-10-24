# v0.11.2-test4 (unpublished)

- Hazards use the brief native Take Hit effect. The spike/acid death prefabs
  containing a second animated Knight are no longer spawned by co-op feedback.
- P1 can select keyboard or any connected controller in the pregame lobby.
  Changing an unoccupied device keeps the same player count; choosing an
  occupied pad exchanges devices while keeping the lobby slot objects.
- Existing in-game P1 keyboard/controller controls remain available in F8.
  Selecting the current device preserves the existing action set and bindings.
- Keep test3's pregame settings, individual mask feedback and reused DLC icon.
  New controls reuse the existing translations in all ten UI languages.

Delivered as HollowKnightLocal8.dll only. Runtime label: `0.11.2-test4`;
assembly/file version: `0.11.1.4`. No release or ModLinks update was submitted.

Steam Remote Play Together is a Steam application setting, not enabled by this
DLL. As checked on 23 October 2026, Hollow Knight's store lists Remote Play on
Phone/Tablet/TV, but not Together. The mod consumes separately exposed virtual
gamepads through InControl; tests of that input contract are not a real Steam
streaming session. No official Together invitation support is claimed.
