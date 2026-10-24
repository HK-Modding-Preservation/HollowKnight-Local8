# v0.11.2-test14

- Hornet's first conversation keeps its original actor until the native
  control handoff finishes. Movement no longer triggers orphan-control
  recovery during that conversation.
- A guarded fallback resumes a stalled corpse position decision. Native
  departure still controls cloak activation and gates, without direct grants.
- Terrain recovery ignores trigger, unsimulated and kinematic hero bodies.
  Local hazard/terrain recovery cannot request a scripted group teleport.

Runtime 0.11.2-test14, assembly/file 0.11.1.14. Unpublished. Camera,
enemy/Baldur sensing, controllers, rewards and assets are unchanged from test13.
Automated fixtures pass; a full native gameplay trial is still required.
