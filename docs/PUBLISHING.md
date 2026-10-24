# Publishing Hollow Knight 8-Player Co-op v0.11.2

The publication wording was reviewed and approved by the user. The release
and ModLinks PR omit the repeated AI-assisted footer; the README retains the
development disclosure.

After approval, publish the complete source while preserving history, license,
identity and settings/save keys. Exclude game libraries, SDK files, logs, local
reference configuration and compiler intermediates.

GitHub:

- Tag: v0.11.2. Title: Hollow Knight 8-Player Co-op v0.11.2.
- Body: docs/RELEASE_NOTES_v0.11.2.md.
- Asset: HollowKnightLocal8-v0.11.2.zip, containing only HollowKnightLocal8.dll.
- Runtime: 0.11.2. Assembly/file: 0.11.2.0.
- Exact checksums: release/SHA256SUMS.txt. Upload the prepared ZIP unchanged.

Before ModLinks, verify the public uploaded ZIP digest and confirm manual
installation on the target game/API. Native validation limits and the known
Custom Knight cosmetic limitation are documented in validation/compatibility.

ModLinks/Lumafly:

- Recheck upstream before applying release/ModLinks-v0.11.2.patch.
- The prepared entry is release/ModLinks-v0.11.2.xml.
- Reviewed title/body: release/ModLinks-v0.11.2-PR.md.
- Change only version, release ZIP URL and SHA-256.
- Preserve the existing name, other URLs, Custom Knight integration,
  Gameplay/LLM-Assisted tags, author and empty mandatory dependencies.
- Public source/release must exist before submitting the catalog request.

Preparation does not push a branch, publish a release or create a PR.
