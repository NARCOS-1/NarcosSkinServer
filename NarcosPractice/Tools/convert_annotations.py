"""Converts CS2 native annotation files (KV3 text format) into the JSON
Marker/Lineup schema NarcosPractice bundles as seed data.

Source data: Tools/AnnotationSource/local/<map>/<map>.txt, copied from
https://github.com/ReneRebsdorf/CS2-annotations (MIT licensed - see
Tools/AnnotationSource/LICENSE).

Usage: python3 convert_annotations.py
Writes Data/SeedMarkers/<map>.json for every map found under AnnotationSource.
"""

import json
import re
import uuid
from pathlib import Path

SCRIPT_DIR = Path(__file__).resolve().parent
SRC_DIR = SCRIPT_DIR / "AnnotationSource" / "local"
OUT_DIR = SCRIPT_DIR.parent / "Data" / "SeedMarkers"
OUT_DIR.mkdir(parents=True, exist_ok=True)

NODE_START_RE = re.compile(r'MapAnnotationNode\d+\s*=\s*\n\s*\{')


def find_blocks(text):
    """Yield the raw text of each top-level MapAnnotationNodeN { ... } block,
    using brace-depth counting since Title/Desc are nested { } blocks."""
    blocks = []
    for m in NODE_START_RE.finditer(text):
        start = m.end() - 1  # position of the opening brace
        depth = 0
        i = start
        while i < len(text):
            if text[i] == '{':
                depth += 1
            elif text[i] == '}':
                depth -= 1
                if depth == 0:
                    blocks.append(text[start:i + 1])
                    break
            i += 1
    return blocks


def get_scalar(block, key):
    m = re.search(rf'\n\s*{re.escape(key)}\s*=\s*"([^"]*)"', block)
    if m:
        return m.group(1)
    m = re.search(rf'\n\s*{re.escape(key)}\s*=\s*(true|false)', block)
    if m:
        return m.group(1) == "true"
    return None


def get_vec3(block, key):
    m = re.search(rf'\n\s*{re.escape(key)}\s*=\s*\[\s*([-\d.eE]+)\s*,\s*([-\d.eE]+)\s*,\s*([-\d.eE]+)\s*\]', block)
    if not m:
        return None
    return [float(m.group(1)), float(m.group(2)), float(m.group(3))]


def get_nested_text(block, section, key="Text"):
    m = re.search(rf'{re.escape(section)}\s*=\s*\n?\s*\{{([^}}]*)\}}', block, re.DOTALL)
    if not m:
        return ""
    inner = m.group(1)
    m2 = re.search(rf'{re.escape(key)}\s*=\s*"([^"]*)"', inner)
    return m2.group(1) if m2 else ""


TYPE_MAP = {
    "smoke": "Smoke",
    "flash": "Flash",
    "he": "HE",
    "molotov": "Molotov",
    "incendiary": "Molotov",
}


def guess_technique(jumpthrow, desc_text):
    text = desc_text.lower()
    if jumpthrow:
        if "run" in text or "running" in text:
            return "Runjumpthrow"
        return "Jumpthrow"
    if "run" in text or "running" in text or "walk" in text or "step" in text:
        return "Walkthrow"
    if "crouch" in text or "duck" in text:
        return "Duckthrow"
    return "Normal"


def guess_strength(desc_text):
    text = desc_text.lower()
    if "middle" in text:
        return "Medium"
    if "right" in text or "instant" in text or "no charge" in text:
        return "Short"
    return "Full"


MARKER_ID_NAMESPACE = uuid.UUID("6f1e3a2b-6c0e-4f0a-9b8f-2c8d4e7a9b10")


def convert_map(txt_path, map_name):
    text = txt_path.read_text(encoding="utf-8", errors="replace")
    blocks = find_blocks(text)

    nodes = {}
    for b in blocks:
        node_id = get_scalar(b, "Id")
        if not node_id:
            continue
        nodes[node_id] = {
            "sub_type": get_scalar(b, "SubType"),
            "position": get_vec3(b, "Position"),
            "angles": get_vec3(b, "Angles"),
            "grenade_type": get_scalar(b, "GrenadeType"),
            "jumpthrow": get_scalar(b, "JumpThrow"),
            "master_id": get_scalar(b, "MasterNodeId"),
            "title": get_nested_text(b, "Title"),
            "desc": get_nested_text(b, "Desc"),
        }

    mains = {nid: n for nid, n in nodes.items() if n["sub_type"] == "main"}
    children_by_master = {}
    for nid, n in nodes.items():
        if n["master_id"]:
            children_by_master.setdefault(n["master_id"], []).append(n)

    lineups = []
    for main_id, main in mains.items():
        if not main["position"] or not main["angles"] or not main["grenade_type"]:
            continue

        nade_type = TYPE_MAP.get(main["grenade_type"])
        if not nade_type:
            continue

        children = children_by_master.get(main_id, [])
        aim_target = next((c for c in children if c["sub_type"] == "aim_target"), None)
        destination = next((c for c in children if c["sub_type"] == "destination"), None)

        # Use the more precise aim_target angle/position pair if present, else
        # fall back to the main node's own angle (still points roughly right).
        throw_pos = main["position"]
        throw_angles = (aim_target or main)["angles"]

        detonate_pos = destination["position"] if destination and destination["position"] else throw_pos

        desc_text = (aim_target or main)["desc"] or main["desc"] or ""
        title = main["title"] or "Lineup"

        # aim_target's own Position is the actual authored "aim your crosshair
        # here" point (e.g. a spot 150 units up in the air on a wall) - it's a
        # completely separate field from its Angles. Keep this real target
        # point instead of making PracticeService fake one by projecting a
        # generic distance out along the angle vector.
        aim_pos = aim_target["position"] if aim_target and aim_target["position"] else None

        lineup = {
            "Name": title,
            "Type": nade_type,
            "Technique": guess_technique(bool(main["jumpthrow"]), desc_text),
            "Strength": guess_strength(desc_text),
            "ThrowPosX": throw_pos[0],
            "ThrowPosY": throw_pos[1],
            "ThrowPosZ": throw_pos[2],
            "ThrowAngPitch": throw_angles[0],
            "ThrowAngYaw": throw_angles[1],
            "DetonatePosX": detonate_pos[0],
            "DetonatePosY": detonate_pos[1],
            "DetonatePosZ": detonate_pos[2],
            # The original author's real freeform instruction, preserved as-is
            # so players can see actual ground truth even where our guessed
            # Technique/Strength above is wrong.
            "Notes": desc_text,
        }

        if aim_pos:
            lineup["AimPosX"] = aim_pos[0]
            lineup["AimPosY"] = aim_pos[1]
            lineup["AimPosZ"] = aim_pos[2]

        lineups.append(lineup)

    # Group lineups into markers by stand position (same "stacking" radius logic
    # as MarkerService.SameMarkerRadius = 48 units).
    markers = []
    for lu in lineups:
        placed = False
        for m in markers:
            dx = m["PosX"] - lu["ThrowPosX"]
            dy = m["PosY"] - lu["ThrowPosY"]
            dz = m["PosZ"] - lu["ThrowPosZ"]
            if (dx * dx + dy * dy + dz * dz) ** 0.5 <= 48.0:
                m["Lineups"].append(lu)
                placed = True
                break
        if not placed:
            # Deterministic, not random - re-running the converter on unchanged
            # source data should produce byte-identical output, not a noisy
            # diff of nothing but reshuffled IDs.
            marker_id = uuid.uuid5(
                MARKER_ID_NAMESPACE,
                f"{map_name}:{lu['ThrowPosX']}:{lu['ThrowPosY']}:{lu['ThrowPosZ']}"
            ).hex
            markers.append({
                "Id": marker_id,
                "PosX": lu["ThrowPosX"],
                "PosY": lu["ThrowPosY"],
                "PosZ": lu["ThrowPosZ"],
                "Lineups": [lu],
            })

    return markers


def main():
    total_lineups = 0
    for map_dir in sorted(SRC_DIR.iterdir()):
        txt_files = list(map_dir.glob("*.txt"))
        if not txt_files:
            continue
        map_name = map_dir.name
        markers = convert_map(txt_files[0], map_name)
        out_path = OUT_DIR / f"{map_name}.json"
        out_path.write_text(json.dumps(markers, indent=2))
        count = sum(len(m["Lineups"]) for m in markers)
        total_lineups += count
        print(f"{map_name}: {len(markers)} markers, {count} lineups")

    print(f"TOTAL: {total_lineups} lineups")


if __name__ == "__main__":
    main()
