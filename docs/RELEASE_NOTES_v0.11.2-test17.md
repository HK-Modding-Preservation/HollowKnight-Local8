# v0.11.2-test17

- New saves show the co-op configuration key once, using the rescue hint's
  native font, position and fade. Translated into all ten supported languages.
- Nearby lifeseed health sharing also works when the native Heal callback
  starts after Hit returns, including P1. Repeated callbacks do not add masks.
- An individual player's death keeps the fight's music playing through native
  mixer silence and gradual volume fades. Full defeat and native phase changes
  retain their own audio behavior.

The existing final companion focus, dissolve and essence-stream animation is
unchanged. Runtime 0.11.2-test17, assembly/file 0.11.1.17. Unpublished.
