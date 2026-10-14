# Native event destination search

Run `python tests/event_search/run.py --dotnet /absolute/path/to/dotnet --managed /absolute/path/to/Managed`.

Tests production FsmRegistryIndex, NativeEventSearch.Patch and ImpactMetrics.
The patch is applied to the actual supplied PlayMaker method and checked for
instruction preservation, stack signature and safe refusal of an incompatible
pattern. Recipient traces compare the supplied native IL's translated control
flow against the indexed lookup, including mutation during callbacks, nested
events, registration order, snapshots, live children and sender/data identity.

These are controlled .NET 8 tests, not execution of the Unity game or a FPS
benchmark. The actual runtime IL-hook installation still needs the user test.
