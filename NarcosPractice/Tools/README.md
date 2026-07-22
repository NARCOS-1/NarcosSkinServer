# Seed data pipeline

`Data/SeedMarkers/*.json` (the bundled 429 real lineups) is generated from
`AnnotationSource/local/<map>/<map>.txt` - a copy of the native CS2
annotation files from https://github.com/ReneRebsdorf/CS2-annotations
(MIT licensed, see `AnnotationSource/LICENSE`).

To pull in an update from upstream and regenerate:

```
# from a fresh clone of ReneRebsdorf/CS2-annotations
cp -r <clone>/local NarcosPractice/Tools/AnnotationSource/local
cp <clone>/LICENSE NarcosPractice/Tools/AnnotationSource/LICENSE

cd NarcosPractice/Tools
python3 convert_annotations.py
```

This overwrites `Data/SeedMarkers/*.json` in place. Marker IDs are derived
deterministically from map name + stand position, so re-running against
unchanged source data produces a byte-identical diff (nothing) - if you see
unrelated lineups shifting around in a diff, that's a real upstream data
change, not converter noise.

`convert_annotations.py` also guesses `Technique`/`Strength` from each
lineup's freeform description text via crude keyword matching (there's no
structured field for either in the source format) - it's not always right,
which is why the original text is preserved as-is in each lineup's `Notes`
field rather than discarded.
