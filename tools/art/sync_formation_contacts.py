"""Carry approved legacy attack contacts into read-only runtime art metadata.

These values are existing EngagementDefinition timings, not newly inferred Spine events.
No body frames, duration, combat damage, SP, Unity assets or GUIDs are changed.
"""
from pathlib import Path
import json
import re

PROJECT = Path(__file__).resolve().parents[2]
LEGACY_ATTACKERS = ("M03", "M11", "M17", "M21", "M35", "M12", "M13", "M16", "M27",
                    "M29", "M30", "M38", "M18", "M24", "M42", "M31", "M34", "M36")


def main():
    changes = []
    for role in LEGACY_ATTACKERS:
        asset = PROJECT / "Assets/Data/Battles" / (role + ".asset")
        authored = asset.read_text(encoding="utf-8-sig")
        duration = float(re.search(r"(?m)^  attackDuration: (.+)$", authored)[1])
        windup = float(re.search(r"(?m)^  attackWindup: (.+)$", authored)[1])
        path = PROJECT / "Assets/Data/ArtCatalogs/MonsterCombatArt" / role / "manifest.json"
        data = json.loads(path.read_text(encoding="utf-8"))
        source_duration = sum(clip["duration"] for clip in data["clips"]
                              if clip["key"] in ("attack", "attack_start", "attack_end"))
        if data.get("noAttackClip") or not 0 < windup <= duration or abs(duration - source_duration) > 0.0001:
            raise ValueError("Legacy contact requires manual review: " + role)
        if data.get("attackContact", 0) > 0 and abs(data["attackContact"] - windup) > 0.00001:
            raise ValueError("Existing contact differs: " + role)
        data["attackContact"] = windup
        data["attackContactProvenance"] = {
            "kind": "EXISTING_APPROVED_ENGAGEMENT_WINDUP",
            "asset": asset.relative_to(PROJECT).as_posix(),
            "attackDuration": duration,
            "sourceDuration": source_duration,
            "note": "Reuse the already authored legacy contact. This is not a newly recovered original skeleton event.",
        }
        changes.append((path, data))
    for path, data in changes:
        path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print("FORMATION_LEGACY_CONTACT_SYNC_PASS: %d existing contacts; no frame/duration/stat edits" % len(changes))


if __name__ == "__main__":
    main()
