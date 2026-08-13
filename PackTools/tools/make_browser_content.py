#!/usr/bin/env python3
"""Build the browser VM content bundle: a trimmed TSO tree + FSO overlay packed
into one content.tar.gz that BrowserContentBoot fetches and extracts into MEMFS.

The TSO subset is browser-content-files.txt — the exact file set a LotHostLite
smoke client opens during a full join/pie-menu/interaction run (strace-derived,
2026-08-13), minus the HIT audio archives (tso.content Audio tolerates their
absence). The FSO overlay ships whole EXCEPT Content/Objects: only the compiled
pack .iffs go there, because every lockstep participant must resolve the same
GUID set from the same bytes — the host runs with --bare-objects to match.

Usage:
  make_browser_content.py --tso-dir /home/user/tso/game/TSOClient \
      --repo /home/user/FreeSO --packs /home/user/packs-out \
      --out /home/user/browser-content

Outputs under --out:
  tso/...           trimmed TSO tree (also usable as a native --tso-dir)
  work/Content/...  overlay the same way LotHostLite lays out its workdir
  content.tar.gz    single blob of tso/ + work/ for the browser
  content-manifest.json  {files: [{path, size, sha256}], totals}
"""
import argparse, gzip, hashlib, json, os, shutil, sys, tarfile

def sha256(path):
    h = hashlib.sha256()
    with open(path, 'rb') as f:
        for chunk in iter(lambda: f.read(1 << 20), b''):
            h.update(chunk)
    return h.hexdigest()

def copy_into(src, dst):
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    shutil.copy2(src, dst)

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--tso-dir', required=True)
    ap.add_argument('--repo', required=True)
    ap.add_argument('--packs', required=True)
    ap.add_argument('--out', required=True)
    ap.add_argument('--file-list', default=os.path.join(os.path.dirname(__file__), 'browser-content-files.txt'))
    ap.add_argument('--dir-list', default=os.path.join(os.path.dirname(__file__), 'browser-content-dirs.txt'))
    args = ap.parse_args()

    out_tso = os.path.join(args.out, 'tso')
    out_work = os.path.join(args.out, 'work')
    for d in (out_tso, out_work):
        if os.path.isdir(d): shutil.rmtree(d)

    # Content providers Directory.GetFiles/GetDirectories over dirs the file list
    # alone wouldn't create (cities/, sounddata ambience trees, ...): ship them
    # empty rather than null-guarding every scan in the engine.
    with open(args.dir_list) as f:
        for rel in (l.strip() for l in f):
            if rel and not rel.startswith('#'):
                os.makedirs(os.path.join(out_tso, rel), exist_ok=True)

    missing = []
    with open(args.file_list) as f:
        rels = [l.strip() for l in f if l.strip() and not l.startswith('#')]
    for rel in rels:
        src = os.path.join(args.tso_dir, rel)
        if not os.path.isfile(src):
            missing.append(rel)
            continue
        copy_into(src, os.path.join(out_tso, rel))
    if missing:
        print(f'FATAL: {len(missing)} listed files missing under {args.tso_dir}:', file=sys.stderr)
        for m in missing: print('  ' + m, file=sys.stderr)
        sys.exit(1)

    overlay_src = os.path.join(args.repo, 'TSOClient', 'FSO.Content.TSO', 'Content')
    overlay_dst = os.path.join(out_work, 'Content')
    for root, dirs, files in os.walk(overlay_src):
        rel_root = os.path.relpath(root, overlay_src)
        if rel_root == 'Objects':
            files = [f for f in files if not f.endswith('.iff')]
        for f in files:
            copy_into(os.path.join(root, f), os.path.join(overlay_dst, rel_root, f))
    os.makedirs(os.path.join(overlay_dst, 'Objects'), exist_ok=True)
    n_packs = 0
    for f in sorted(os.listdir(args.packs)):
        if f.endswith('.iff'):
            copy_into(os.path.join(args.packs, f), os.path.join(overlay_dst, 'Objects', f))
            n_packs += 1

    entries = []
    all_dirs = []
    for base in ('tso', 'work'):
        broot = os.path.join(args.out, base)
        all_dirs.append(base)
        for root, dirs, files in os.walk(broot):
            for d in sorted(dirs):
                all_dirs.append(os.path.relpath(os.path.join(root, d), args.out))
            for f in sorted(files):
                p = os.path.join(root, f)
                rel = os.path.relpath(p, args.out)
                entries.append({'path': rel, 'size': os.path.getsize(p), 'sha256': sha256(p)})
    entries.sort(key=lambda e: e['path'])
    all_dirs.sort()

    tar_path = os.path.join(args.out, 'content.tar.gz')
    # Strict USTAR: the browser extractor (BrowserContentBoot) is a minimal
    # hand-rolled ustar reader — System.Formats.Tar is PlatformNotSupported on
    # browser-wasm. PAX/GNU extensions would smuggle in 'x'/'L' entries.
    over = [p for p in ([e['path'] for e in entries] + all_dirs) if len(p) > 100]
    if over:
        print('FATAL: paths exceed ustar name field (100):', over[:5], file=sys.stderr)
        sys.exit(1)
    # mtime=0 + sorted entries keeps the blob byte-stable across rebuilds.
    with open(tar_path, 'wb') as raw:
        with gzip.GzipFile(fileobj=raw, mode='wb', compresslevel=6, mtime=0) as gz:
            with tarfile.open(fileobj=gz, mode='w', format=tarfile.USTAR_FORMAT) as tar:
                # Explicit dir entries so empty dirs (cities/, sound trees) survive
                # extraction — content scans crash on their absence.
                for d in all_dirs:
                    info = tarfile.TarInfo(d)
                    info.type = tarfile.DIRTYPE
                    info.mode = 0o755; info.mtime = 0
                    tar.addfile(info)
                for e in entries:
                    full = os.path.join(args.out, e['path'])
                    info = tar.gettarinfo(full, arcname=e['path'])
                    info.mtime = 0; info.uid = info.gid = 0; info.uname = info.gname = ''
                    with open(full, 'rb') as fh:
                        tar.addfile(info, fh)

    total = sum(e['size'] for e in entries)
    manifest = {
        'files': entries,
        'dirs': all_dirs,
        'totalBytes': total,
        'tarGzBytes': os.path.getsize(tar_path),
        'packIffs': n_packs,
    }
    with open(os.path.join(args.out, 'content-manifest.json'), 'w') as f:
        json.dump(manifest, f, indent=1)
    print(f'{len(entries)} files, {total/1048576:.1f} MB raw, '
          f'{manifest["tarGzBytes"]/1048576:.1f} MB content.tar.gz, {n_packs} pack iffs')

if __name__ == '__main__':
    main()
