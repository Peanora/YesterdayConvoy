"""Prepare the existing native baker for char_1035_wisdel@game#9 only.

Reads locally verified source/capture caches. Does not fetch media, edit the game
Assets, or modify original bundles. Run with Python 3.13 used by acquire_actors.
"""
from pathlib import Path
import argparse, hashlib, json, sys

parser = argparse.ArgumentParser()
parser.add_argument('--source', type=Path, required=True)
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args()
sys.stdout.reconfigure(encoding='utf8')
source, output = args.source.resolve(), args.output.resolve()
output.mkdir(parents=True, exist_ok=True)
raw = json.loads((source/'COMPLETE-NATIVE-BINDINGS.json').read_text(encoding='utf8'))
unit = raw['units']['char_1035_wisdel']
replacements = {k: k+'_game#9' for k in (
    'wisdel_skill_03_buff_02_f', 'wisdel_skill_03_hit',
    'wisdel_skill_03_hit_02', 'wisdel_skill_03_hit_03',
    'wisdel_skill_03_start', 'wisdel_skill_03_trail')}
replacements.update(wisdel_attack_a_start='wisdel_attack_c_start',
                    wisdel_attack_b_start='wisdel_attack_c_start')
keys = sorted({replacements.get(k, k) for k in unit['refs']})
# SwitchableThreeFaceEffect requires these original directional companions.
keys.append('wisdel_skill_03_up_start')
metadata = {p['key']: p for p in json.loads(
    (source/'native-cache/metadata.json').read_text(encoding='utf8'))['prefabs']}
assert all(k in metadata for k in keys)
cache = output/'native-cache'
cache.mkdir(exist_ok=True)
(cache/'metadata.json').write_text(json.dumps({'prefabs': [metadata[k] for k in keys]},
    ensure_ascii=False, indent=2), encoding='utf8')
# Keep the successful shader/texture dependency closure of the existing baker.
# Bundles are referenced in place rather than copied or platform-converted again.
bundles = json.loads((source/'native-cache/bundles.json').read_text(encoding='utf8'))['bundles']
# The cache was already dependency-closed for wisdel.ab. Retain only its exact
# native dependency graph; there is no need to load other boss effect prefabs.
legacy = source.parents[1]/'2026-11-05_泥岩原作技能-v1'
index = json.loads((legacy/'bundle-index.json').read_text(encoding='utf8'))
lookup = {r['name']: i for i, r in enumerate(index)}
closure = set()
def visit(i):
    if i in closure:
        return
    closure.add(i)
    for dependency in index[i]['deps']:
        visit(dependency)
visit(lookup['battle/prefabs/effects/wisdel.ab'])
required = {index[i]['name'] for i in closure}
selected = [r for r in bundles if Path(r['source']).as_posix().split('/source-bundles/')[-1] in required]
assert len(selected) == len(required), (len(selected), len(required))
assert all(Path(r['file']).is_file() for r in selected)
(cache/'bundle-paths.txt').write_text('\n'.join(r['file'] for r in selected), encoding='utf8')
(cache/'bundles.json').write_text(json.dumps({'bundles': selected}, ensure_ascii=False, indent=2), encoding='utf8')
jobs = []
for key in keys:
    # Source native world units. Body/native scale adaptation is done by the
    # runtime PPU, not by changing the native prefab or capture renderer scale.
    ortho = 6 if ('skill_03' in key or 'birth' in key) else 4
    job = dict(key='', output=key, duration=3, fps=30, size=512,
        orthoSize=ortho, hdrRange=256, probeEvery=15,
        camera=dict(x=0,y=12,z=-6.9282), target=dict(x=0,y=.25,z=0),
        anchor=dict(x=0,y=0,z=0), instances=[dict(key=key,start=0)])
    if 'trail' in key:
        # Track the projectile with the camera: image pivot remains at the
        # projectile, rather than baking a second movement into its atlas.
        movement=dict(x=10,y=0,z=0)
        job['instances'][0].update(movementPerSecond=movement,replayTrailAges=True)
        job['cameraMovementPerSecond']=movement
        job.update(duration=1, size=1024, orthoSize=12)
    jobs.append(job)
(output/'jobs.json').write_text(json.dumps({'jobs': jobs}, ensure_ascii=False, indent=2), encoding='utf8')
representatives = {'wisdel_attack_c_start','wisdel_skill_03_start_game#9',
                   'wisdel_skill_03_hit_02_game#9','wisdel_skill_03_trail_game#9'}
(output/'probe-jobs.json').write_text(json.dumps({'jobs': [j for j in jobs if j['output'] in representatives]},
    ensure_ascii=False, indent=2), encoding='utf8')
(output/'SOURCE-AUDIT.json').write_text(json.dumps(dict(
    task='WIS_APRIL_G9', version=raw['version'], unit=unit,
    projectiles={k:v for k,v in raw['projectiles'].items() if k.startswith('projectile_chr_wisdel_')},
    replacements=replacements, keys=keys, bundles=selected,
    sourceBundleSha256=hashlib.sha256((source/'source-bundles/battle/prefabs/effects/wisdel.ab').read_bytes()).hexdigest(),
    note='Original game#9 SkinHooker: six S3 replacements; A/B cast uses original C. S1/S2 retain default. No sale#14 media.'
    ), ensure_ascii=False, indent=2), encoding='utf8')
print('READY', len(keys), 'clips',len(selected),'existing dependency bundles', output)
