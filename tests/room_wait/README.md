# Party room waiting

Run `python3 tests/room_wait/run.py --dotnet /path/to/dotnet`.

The complete production TransitionVote and RoomWaitVisibility classes run in a
native .NET test host. Doorways' destination resolution, proximity, lock checks,
wait identity and complete Tick method are extracted without edits, together
with InteractionRouter's targeted room-entry release and End method. Unity
rendering/physics, prompts, animation completion and the final scene-loading
boundary are controlled doubles; this is not a Unity playthrough.

The frozen test6 vote is a negative control: its native door stays black and
keeps the interaction reservation until timeout. Positive cases cover 2/4/8
players with every player entering first, consumed native prompts, late fades,
exact door/entry identity, gates and FSM doors, locks, prerequisites, range,
actor eligibility, normal fade at commit, winning actor context, five-second
timeout, majority/ties, pauses, settings, cancellation, disconnects and unrelated
dialogues. Death/hazard/story fades and scripted/boss/dream/stag routes remain
outside ordinary room-wait recovery. A disconnected voter after a long pause
must not make the other votes expire from stale timestamps.
