"""Import the retained native projectile snapshot and generate pure scalar profiles.

Usage: python tools/art/build_formation_projectiles.py <native-projectile-json>
No projectile speed is inferred from an FX duration or a legacy fixed-flight adaptation.
"""
from pathlib import Path
import hashlib
import json
import re
import shutil
import sys
import argparse

PROJECT = Path(__file__).resolve().parents[2]
OUTPUT = PROJECT
sys.path.insert(0, str(PROJECT / "tools"))
from package_boss_roster import meta

ROUTES = (
    ("B01", 0, "Default"), ("B05", 0, "Default"),
    ("B07", 0, "Default"), ("B07", 1, "Reborn"),
    ("B08", 0, "Default"), ("B08", 1, "M2"),
    ("B09", 0, "Default"), ("B09", 1, "m1"),
    ("B11", 0, "s1"), ("B11", 1, "s2"),
    ("B12", 0, "s1"), ("B12", 1, "s4"),
    ("B13", 1, "m1attack"), ("B13", 2, "m2attack"),
    ("B15", 0, "P1"), ("B15", 1, "P2"),
    ("B23", 0, "Default"), ("B23", 1, "M2"),
    ("B26", 0, "YinMode"),
)


def write_json(path, data):
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    meta(path, OUTPUT)


def literal(value):
    return format(float(value), ".9g") + "f"


def main():
    global OUTPUT
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("native_projectile_json", type=Path)
    parser.add_argument("--ranged-source", type=Path, default=PROJECT / "artifacts/formation-visual-repair/original-ranged-projectiles.json")
    parser.add_argument("--output-root", type=Path, default=PROJECT,
                        help="Stage the same Assets-relative files outside Assets before an integration window.")
    args = parser.parse_args()
    OUTPUT = args.output_root.resolve()
    source_path = args.native_projectile_json.resolve(strict=True)
    original_bytes = source_path.read_bytes()
    original = json.loads(original_bytes)
    source_hash = hashlib.sha256(original_bytes).hexdigest()
    data_root = OUTPUT / "Assets/Data/ArtCatalogs/BossRoster"
    data_root.mkdir(parents=True, exist_ok=True)
    raw_path = data_root / "ORIGINAL-ATTACK-PROJECTILES.json"
    shutil.copyfile(source_path, raw_path)
    meta(raw_path, OUTPUT)
    selected = []
    for task, phase, mode in ROUTES:
        rows = [row for row in original["rows"] if row["task"] == task and row["mode"] == mode
                and row["branch"] == "attack" and row.get("projectileKey")]
        if len(rows) != 1:
            raise ValueError("Ambiguous native attack projectile: " + task + "/" + mode)
        row = rows[0]
        movement = row["movement"]
        if row.get("speed", 0) <= 0 or movement.get("useDynamicSpeed", 0):
            raise ValueError("This original flight needs a different adapter: " + row["projectileKey"])
        selected.append((task, phase, row))
    yin = next(row for task, _, row in selected if task == "B26")
    yang = next(row for row in original["rows"] if row["task"] == "B26" and row["mode"] == "YangMode" and row["branch"] == "attack")
    for field in ("speed", "movementClass"):
        if yin[field] != yang[field]:
            raise ValueError("Yin/Yang need separate scalar selectors")
    for field in ("raiseHeight", "delayToStart", "delayTime", "noRaiseHeightThreshold"):
        if yin["movement"].get(field, 0) != yang["movement"].get(field, 0):
            raise ValueError("Yin/Yang need separate scalar selectors")
    core_path = source_path.parent.parent / "COMPLETE-NATIVE-BINDINGS.json"
    core_bytes = core_path.read_bytes()
    core = json.loads(core_bytes)
    if core["version"] != original["sourceVersion"]:
        raise ValueError("The native core snapshot comes from a different original version")
    core_rows = []
    for role, phase, task, model, mode in (
        ("M14", 0, "B18", "enemy_1510_frstar2", "Default"),
        ("M14", 1, "B18", "enemy_1510_frstar2", "Reborn"),
        ("M32", 1, "B06", "enemy_1523_mandra", "P2"),
    ):
        actor = core["units"][model]
        actor_mode = next(item for item in actor["modes"] if item["name"] == mode)
        attack = actor_mode["attack"]
        if attack["class"] != "RangedAttack":
            raise ValueError("A core source branch is no longer an ordinary ranged attack")
        projectile_key = attack["projectileKey"]
        projectile = core["projectiles"][projectile_key]
        movement = projectile["movement"]
        if movement.get("speed", 0) <= 0 or movement.get("useDynamicSpeed", 0):
            raise ValueError("Unsupported original core movement: " + projectile_key)
        row = {"task": task, "mode": mode, "phase": phase + 1, "branch": "attack",
               "projectileKey": projectile_key, "speed": movement["speed"],
               "movementClass": movement["class"], "movement": movement,
               "source": "units/" + model + "/modes/" + mode + "/attack"}
        selected.append((role, phase, row))
        core_rows.append({"artId": role, "nativePhase": phase, "task": task, "model": model,
                          "mode": mode, "actorAttack": attack, "projectileKey": projectile_key,
                          "projectile": projectile})
    # game#9 changes the selected effect keys, not the base character's ordinary
    # projectile. All three random attack bodies must agree before sharing a profile.
    wisadel = core["units"]["char_1035_wisdel"]
    wisadel_attack = next(item for item in wisadel["modes"] if item["name"] == "Default")["attack"]
    wisadel_variants = wisadel_attack["abilities"]
    if wisadel_attack["class"] != "AbilityRandomGroup" or len(wisadel_variants) != 3:
        raise ValueError("Unexpected original Wisadel ordinary-attack branch")
    if any(item["class"] != "RangedAttack" or item["projectileKey"] != "projectile_chr_wisdel_default"
           for item in wisadel_variants):
        raise ValueError("Wisadel attack variants no longer share the original ordinary projectile")
    projectile_key = "projectile_chr_wisdel_default"
    projectile = core["projectiles"][projectile_key]
    movement = projectile["movement"]
    if movement.get("speed", 0) <= 0 or movement.get("useDynamicSpeed", 0):
        raise ValueError("Unsupported original Wisadel movement")
    row = {"task": "WIS_APRIL_G9", "mode": "Default", "phase": 1, "branch": "attack",
           "projectileKey": projectile_key, "speed": movement["speed"],
           "movementClass": movement["class"], "movement": movement,
           "source": "units/char_1035_wisdel/modes/Default/attack/Attack_A+B+C"}
    selected.append(("WISADEL_APRIL", 0, row))
    core_rows.append({"artId": "WISADEL_APRIL", "nativePhase": 0, "task": "WIS_APRIL_G9",
                      "model": "char_1035_wisdel", "mode": "Default", "actorAttack": wisadel_attack,
                      "projectileKey": projectile_key, "projectile": projectile,
                      "skinPolicy": "game#9 has only effect replacement pairs; no ordinary projectile replacement. See WIS_APRIL_G9/main/ORIGINAL-SKIN-PREFAB.json."})
    ranged_path = args.ranged_source.resolve(strict=True)
    ranged_bytes = ranged_path.read_bytes()
    ranged = json.loads(ranged_bytes)
    if hashlib.sha256(Path(ranged["bundle"]).read_bytes()).hexdigest() != ranged["sha256"]:
        raise ValueError("Retained original ranged-projectile bundle hash differs")
    ranged_target = data_root / "ORIGINAL-LEGACY-RANGED-PROJECTILES.json"
    ranged_target.write_bytes(ranged_bytes)
    meta(ranged_target, OUTPUT)
    ranged_routes = []
    for item in ranged["rows"]:
        movements = [node for node in item["nodes"] if node["type"] == "AdvancedMovement"]
        if len(movements) != 1:
            raise ValueError("Ambiguous original legacy movement: " + item["card"])
        fields = movements[0]["fields"]
        if fields.get("_speed", 0) <= 0 or fields.get("_useDynamicSpeed", 0) or fields.get("_speedKey"):
            raise ValueError("Unsupported original legacy movement: " + item["card"])
        movement = {key.removeprefix("_"): value for key, value in fields.items()}
        movement["class"] = movements[0]["type"]
        for role in item["card"].split("/"):
            row = {"task": role, "mode": "Default", "phase": 1, "branch": "attack",
                   "projectileKey": item["projectile"], "speed": movement["speed"],
                   "movementClass": movement["class"], "movement": movement,
                   "source": "ORIGINAL-LEGACY-RANGED-PROJECTILES/" + item["card"] + "/" + movements[0]["path"]}
            selected.append((role, 0, row))
            ranged_routes.append({"artId": role, "projectileKey": item["projectile"],
                                  "node": movements[0]["path"], "speed": movement["speed"],
                                  "delayToStart": movement.get("delayToStart", 0), "movementClass": movement["class"]})
    if {row["artId"] for row in ranged_routes} != {"M11", "M12", "M16", "M26", "M42", "M36", "M38"}:
        raise ValueError("The original legacy ranged-role set changed")
    write_json(data_root / "ORIGINAL-LEGACY-RANGED-PROJECTILES-SOURCE.json", {
        "version": 1, "sourceSnapshot": str(ranged_path),
        "sourceSnapshotSha256": hashlib.sha256(ranged_bytes).hexdigest(),
        "snapshotPreservedByteForByte": True, "originalBundle": ranged["bundle"],
        "originalBundleSha256": ranged["sha256"], "routes": ranged_routes,
        "meaning": "Previously retained original ordinary ranged-projectile prefab fields; M26/M42 share the same verified projectile. Not the old approved fixed .25-second adaptation.",
        "unityTests": "NOT_RUN_BY_GENERATOR",
    })
    write_json(data_root / "ORIGINAL-CORE-PROJECTILES.json", {
        "version": 1, "sourceVersion": core["version"], "sourceSnapshot": str(core_path),
        "sourceSnapshotSha256": hashlib.sha256(core_bytes).hexdigest(),
        "note": "Verbatim original parsed actor attack and projectile field subsets. No newly inferred speed or projectile field; source snapshot itself is not duplicated here.",
        "rows": core_rows,
    })
    provenance = {
        "version": 1, "sourceVersion": original["sourceVersion"],
        "sourceSnapshot": str(source_path), "sourceSnapshotSha256": source_hash,
        "snapshotPreservedByteForByte": True,
        "coreSnapshotSha256": hashlib.sha256(core_bytes).hexdigest(),
        "legacyRangedSnapshotSha256": hashlib.sha256(ranged_bytes).hexdigest(),
        "legacyRangedOriginalBundleSha256": ranged["sha256"],
        "meaning": "Existing original-prefab extraction, ordinary attack branches only. No client runtime or fresh bundle decode was performed by this generator.",
        "routes": [{"artId": task, "nativePhase": phase, "originalMode": row["mode"],
                    "originalPhase": row["phase"], "projectileKey": row["projectileKey"],
                    "node": row["source"], "speed": row["speed"], "movementClass": row["movementClass"]}
                   for task, phase, row in selected],
        "equivalentScalars": "B26 YinMode and YangMode ordinary attacks both have original speed10, same movement class, raise/delay/threshold; their visual projectile identities remain separate in the raw snapshot.",
        "exclusions": "Unknown/melee/empty branches have no invented speed. Original Talulah fireball is a skill, not an ordinary projectile. April Wisadel uses its verified base ordinary projectile plus game#9 skin hook, never B20/Supernova values.",
        "rights": "Original Arknights resource metadata; original rights retained, no CC0 or public redistribution license claim.",
        "unityTests": "NOT_RUN_BY_GENERATOR",
    }
    write_json(data_root / "ORIGINAL-ATTACK-PROJECTILES-SOURCE.json", provenance)
    adaptations = []
    for role in ("M11", "M12", "M16", "M38", "M24", "M42", "M31", "M36", "WISADEL_APRIL"):
        asset = PROJECT / "Assets/Data/Battles" / ("WisadelVsKroos.asset" if role == "WISADEL_APRIL" else role + ".asset")
        text = asset.read_text(encoding="utf-8-sig")
        seconds = float(re.search(r"(?m)^  projectileTime: (.+)$", text)[1])
        if seconds <= 0:
            raise ValueError("Invalid approved legacy flight: " + role)
        adaptations.append({"artId": role, "fixedSeconds": seconds,
                            "source": asset.relative_to(PROJECT).as_posix(),
                            "kind": "APPROVED_LEGACY_ADAPTATION_NOT_NATIVE_SPEED"})
    write_json(data_root / "APPROVED-LEGACY-FLIGHT.json", {
        "version": 1, "note": "Existing approved remake fixed-flight times. Deliberately separate from original speed profiles; not auto-applied by TryFor.",
        "rows": adaptations,
    })
    lines = [
        "// Generated from retained original-prefab scalar fields; no file or Unity access.",
        "// Sources: BossRoster original attack/core/legacy ranged projectile JSON and SOURCE records.",
        "namespace YesterdayConvoy", "{", "    internal static class FormationProjectileProfiles", "    {",
        "        internal readonly struct Spec", "        {",
        "            internal readonly float Speed, RaiseHeight, Delay, NoRaiseHeightThreshold;",
        "            internal readonly string MovementClass, Source;",
        "            internal Spec(float speed,float raise,float delay,float threshold,string movement,string source)",
        "            {Speed=speed;RaiseHeight=raise;Delay=delay;NoRaiseHeightThreshold=threshold;MovementClass=movement;Source=source;}",
        "        }",
        "        internal static bool TryFor(string artId,int nativePhase,out Spec spec)",
        "        {", "            spec=default;",
        "            if(string.IsNullOrEmpty(artId)||nativePhase<0)return false;",
        "            if(artId.StartsWith(\"CARD_\",System.StringComparison.Ordinal))artId=artId.Substring(5);",
        "            if(artId==\"B18\")artId=\"M14\";else if(artId==\"B06\")artId=\"M32\";",
        "            else if(artId==\"WIS_APRIL_G9\")artId=\"WISADEL_APRIL\";",
        "            switch(artId)", "            {",
    ]
    for task in dict.fromkeys(task for task, _, _ in selected):
        routes = [(phase, row) for t, phase, row in selected if t == task]
        lines.append('                case "' + task + '":')
        if task in ("B13", "M32"):
            lines.append("                    if(nativePhase==0)return false;")
        for phase, row in reversed(routes):
            movement = row["movement"]
            spec = "new Spec(%s,%s,%s,%s,%s,%s)" % (
                literal(row["speed"]), literal(movement.get("raiseHeight", 0)),
                literal(movement.get("delayToStart", movement.get("delayTime", 0))),
                literal(movement.get("noRaiseHeightThreshold", 0)),
                json.dumps(row["movementClass"]),
                json.dumps("B26/YinMode+YangMode/attack/equivalent-scalars" if task == "B26" else task + "/" + row["mode"] + "/attack/" + row["projectileKey"]),
            )
            if len(routes) == 1:
                lines.append("                    spec=" + spec + ";return true;")
            elif phase == routes[0][0]:
                lines.append("                    spec=" + spec + ";return true;")
            else:
                lines.append("                    if(nativePhase>=" + str(phase) + "){spec=" + spec + ";return true;}")
    lines += ["                default:return false;", "            }", "        }", "    }", "}", ""]
    output = OUTPUT / "Assets/Scripts/Runtime/FormationProjectileProfiles.cs"
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text("\n".join(lines), encoding="utf-8")
    meta(output, OUTPUT)
    assert raw_path.read_bytes() == original_bytes
    print("FORMATION_PROJECTILE_PROFILES_PASS: %d original attack routes; %d role IDs; %d separate legacy adaptations" %
          (len(selected), len(set(task for task, _, _ in selected)), len(adaptations)))


if __name__ == "__main__":
    main()
