"""Fast repository integrity checks; does not compile or run Unity."""

import json
import re
import subprocess
import sys
from pathlib import Path


def inspect(root, paths):
    errors = []
    tracked = set(paths)
    folded = {}
    guids = {}
    text_extensions = {'.cs', '.md', '.json', '.unity', '.prefab', '.asset',
                       '.meta', '.mat', '.anim', '.controller', '.yml', '.yaml'}
    for name in sorted(tracked):
        collision = folded.setdefault(name.casefold(), name)
        if collision != name:
            errors.append(f'Case-insensitive path collision: {collision} / {name}')
        if name == 'AGENTS.md' or name.split('/')[0].lower() in {
                'library', 'temp', 'obj', 'logs', 'usersettings', 'build', 'builds', 'artifacts'}:
            errors.append(f'Local-only file is tracked: {name}')
        path = root / name
        if not path.is_file():
            errors.append(f'Tracked file is missing: {name}')
            continue
        if path.suffix.lower() in text_extensions:
            content = path.read_text(encoding='utf-8-sig')
            if re.search(r'^(?:<<<<<<< |>>>>>>> |\|\|\|\|\|\|\| )', content, re.M):
                errors.append(f'Unresolved merge conflict: {name}')
        if not name.startswith('Assets/'):
            continue
        if name.endswith('.meta'):
            source = name[:-5]
            if source not in tracked and not any(p.startswith(source + '/') for p in tracked):
                errors.append(f'Orphan .meta: {name}')
            match = re.search(r'^guid: ([0-9a-f]{32})\s*$', content, re.M)
            if not match:
                errors.append(f'Missing or invalid GUID: {name}')
            else:
                previous = guids.setdefault(match[1], name)
                if previous != name:
                    errors.append(f'Duplicate GUID: {previous} / {name}')
        elif name + '.meta' not in tracked:
            errors.append(f'Missing .meta: {name}')
        for parent in Path(name).parents:
            folder = parent.as_posix()
            if folder in {'Assets', '.'}:
                break
            if folder + '.meta' not in tracked:
                errors.append(f'Missing folder .meta: {folder}')

    required = ['Packages/manifest.json', 'Packages/packages-lock.json',
                'ProjectSettings/ProjectVersion.txt', 'ProjectSettings/EditorSettings.asset',
                'ProjectSettings/VersionControlSettings.asset', 'Assets/_Game/Scenes/InitialDemo.unity']
    for name in required:
        if name not in tracked:
            errors.append(f'Missing project file: {name}')
    if all(name in tracked and (root / name).is_file() for name in required):
        version = (root / required[2]).read_text(encoding='utf-8-sig')
        if not re.search(r'^m_EditorVersion: 6000\.6\.5f1\s*$', version, re.M):
            errors.append('Unity version must remain 6000.6.5f1 until the team agrees to change it.')
        editor = (root / required[3]).read_text(encoding='utf-8-sig')
        if not re.search(r'^  m_SerializationMode: 2\s*$', editor, re.M):
            errors.append('Unity asset serialization must use Force Text.')
        vcs = (root / required[4]).read_text(encoding='utf-8-sig')
        if 'm_Mode: Visible Meta Files' not in vcs:
            errors.append('Unity version control must use Visible Meta Files.')
        for name in required[:2]:
            try:
                json.loads((root / name).read_text(encoding='utf-8-sig'))
            except (ValueError, UnicodeError) as error:
                errors.append(f'Invalid JSON: {name}: {error}')
    return sorted(set(errors))


def main():
    root = Path(__file__).resolve().parents[1]
    result = subprocess.run(['git', 'ls-files', '-z'], cwd=root, check=True, stdout=subprocess.PIPE)
    paths = result.stdout.decode('utf-8').rstrip('\0').split('\0')
    errors = inspect(root, paths)
    for error in errors:
        print(f'ERROR: {error}')
    if errors:
        print(f'Repository check failed: {len(errors)} issue(s).')
        return 1
    print(f'REPOSITORY_CHECK_PASS: {len(paths)} tracked files; Unity metadata and project settings checked.')
    print('Unity compilation, EditMode, PlayMode and Windows build were NOT run by this check.')
    return 0


if __name__ == '__main__':
    sys.exit(main())
