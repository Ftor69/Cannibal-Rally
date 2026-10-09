"""Create and validate a portable web prototype ZIP; never a Melty package."""
from pathlib import Path, PurePosixPath
import hashlib
import re
import zipfile

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT / 'releases' / 'cannibal-rally-v0.1.zip'
BASE = ['.gitignore', 'README.md', 'package.json', 'package-lock.json', 'index.html']
FOLDERS = ['src', 'tests', 'scripts', 'docs', 'dist']
PATTERNS = [
    r'gh[pousr]_[A-Za-z0-9]{20,}',
    r'github_pat_[A-Za-z0-9_]{30,}',
    r'sk-(?:proj-)?[A-Za-z0-9_-]{24,}',
    r'AKIA[0-9A-Z]{16}',
    r'xox[baprs]-[A-Za-z0-9-]{20,}',
    r'-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----',
    r'(?i)(?:password|passwd|api_key|apiKey|access_token|auth_token|client_secret)\s*[=:]\s*[\x22\x27][^\x22\x27\s]{8,}[\x22\x27]',
    r'https?://[^/\s:@]+:[^/\s@]+@',
]
FORBIDDEN = {'.git', 'node_modules', 'work', 'melty-local', '.aws', '.codex', '.agents'}


def check_content(name, data):
    text = data.decode('utf-8')
    for pattern in PATTERNS:
        if re.search(pattern, text):
            raise ValueError(f'Potential credential in {name}; inspect locally, do not publish.')


def build():
    if not (ROOT / 'dist/index.html').is_file():
        raise ValueError('Missing dist/index.html: run npm run build first.')
    files = [ROOT / name for name in BASE]
    for folder in FOLDERS:
        files.extend(p for p in (ROOT / folder).rglob('*') if p.is_file())
    files.sort()
    payload = {}
    for path in files:
        rel = path.relative_to(ROOT)
        if path.is_symlink() or any(part in FORBIDDEN for part in rel.parts):
            raise ValueError(f'Forbidden path: {rel}')
        if path.name.startswith('.env') or path.suffix in {'.pem', '.key'}:
            raise ValueError(f'Local configuration or private key: {rel}')
        data = path.read_bytes()
        check_content(str(rel), data)
        payload['cannibal-rally/' + rel.as_posix()] = data
    DEST.parent.mkdir(exist_ok=True)
    with zipfile.ZipFile(DEST, 'w', zipfile.ZIP_DEFLATED) as archive:
        for name, data in payload.items():
            info = zipfile.ZipInfo(name, (2026, 10, 9, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            info.external_attr = 0o100644 << 16
            archive.writestr(info, data)
    with zipfile.ZipFile(DEST) as archive:
        if archive.testzip() is not None:
            raise ValueError('ZIP CRC validation failed.')
        names = archive.namelist()
        if len(names) != len(set(names)) or set(names) != set(payload):
            raise ValueError('Duplicate or unexpected ZIP members.')
        for name in names:
            path = PurePosixPath(name)
            if path.is_absolute() or '..' in path.parts or path.parts[0] != 'cannibal-rally':
                raise ValueError('Unsafe ZIP member path.')
            data = archive.read(name)
            if data != payload[name]:
                raise ValueError('ZIP content differs from source.')
            check_content(name, data)
        html = archive.read('cannibal-rally/dist/index.html').decode()
        for asset in re.findall(r'(?:src|href)="(/assets/[^\"]+)"', html):
            if 'cannibal-rally/dist' + asset not in names:
                raise ValueError(f'Missing built asset: {asset}')
    digest = hashlib.sha256(DEST.read_bytes()).hexdigest()
    DEST.with_suffix('.zip.sha256').write_text(f'{digest}  {DEST.name}\n')
    print(f'PASS: {len(payload)} files, one root, safe paths, matching bytes, CRC, assets and credential-pattern checks.')
    print(f'{DEST.relative_to(ROOT)}: {DEST.stat().st_size} bytes; SHA256 {digest}')


if __name__ == '__main__':
    build()
