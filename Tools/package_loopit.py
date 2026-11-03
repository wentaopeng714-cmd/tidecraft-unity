#!/usr/bin/env python3
"""Build a self-contained HTML5 import ZIP; index.html is at the archive root."""
import argparse
import gzip
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import zipfile

project = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('--staging', type=Path, required=True)
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args()
stage = args.staging.resolve()
if stage.exists():
    raise SystemExit('Staging directory already exists; use a new empty path')
shutil.copytree(project / 'WebPreview', stage)
config = json.loads((stage / 'build-config.json').read_text())
# Ordinary .wasm/.js/.data files avoid needing special compression headers.
for key in ('dataUrl', 'frameworkUrl', 'codeUrl'):
    old = stage / config[key]
    new = old.with_name(old.name.removesuffix('.unityweb'))
    new.write_bytes(gzip.decompress(old.read_bytes()))
    old.unlink()
    config[key] = new.relative_to(stage).as_posix()
(stage / 'build-config.json').write_text(json.dumps(config, indent=2) + '\n')
html = (stage / 'index.html').read_text()
old = "const res=await fetch('build-config.json');if(!res.ok)throw new Error('配置加载失败');const build=await res.json();"
assert old in html, 'Launcher format changed; update the packager before continuing'
html = html.replace(old, 'const build=' + json.dumps(config, separators=(',', ':')) + ';')
html = html.replace("autoSyncPersistentDataPath:true,", "autoSyncPersistentDataPath:true,dataCaching:false,")
(stage / 'index.html').write_text(html)
tracked = subprocess.check_output(['git', 'ls-files', '-z'], cwd=project).decode().split('\0')
for name in tracked:
    if name.startswith(('Assets/', 'Packages/', 'ProjectSettings/')) or name in ('README.md', 'ART_DIRECTION.md'):
        src = project / name
        if src.is_file():
            dst = stage / 'source' / 'Unity' / name
            dst.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(src, dst)
instructions = '''沙岸工坊 · Loopit HTML5 网页运行包

入口是 ZIP 根目录的 index.html。保留 Build/、cover.jpg、licenses/ 的相对位置。
包中带齐游戏、模型、字体、声音和 WebAssembly 运行文件，不需要 CDN、API Key 或 Unity 编辑器。
打开页面会自动加载，手机竖屏。先画墙、放塔或筑沙堤，再点“迎战海浪”，保护青色旗台三轮。

导入：作为 HTML5 网页游戏导入此 ZIP，入口选择 index.html。
托管环境需要支持 WebGL 2、WebAssembly 与相对资源请求。Build 中使用普通 .wasm/.js/.data 文件，无需自定义 Content-Encoding。
可编辑 Unity 工程在 source/Unity/，不是网页入口。
本地预览需使用 HTTP 服务：python3 -m http.server 8000，然后打开 http://localhost:8000。

已按独立静态网站与 iframe 嵌入方式进行浏览器验证。尚未在 Loopit 导入器内实测；平台若限制 WebAssembly，需要按其限制另行适配。
'''
(stage / 'README-Loopit.txt').write_text(instructions)
commit = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=project).decode().strip()
info = {'name': 'Tidecraft', 'entry': 'index.html', 'orientation': 'portrait',
        'engine': 'Unity WebGL', 'sourceCommit': commit,
        'requirements': ['WebGL 2', 'WebAssembly', 'HTTP(S) static hosting'],
        'files': {p.relative_to(stage).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest()
                  for p in sorted(stage.rglob('*')) if p.is_file()}}
(stage / 'PACKAGE_INFO.json').write_text(json.dumps(info, ensure_ascii=False, indent=2) + '\n')
args.output.parent.mkdir(parents=True, exist_ok=True)
with zipfile.ZipFile(args.output, 'w', zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
    for p in sorted(stage.rglob('*')):
        if p.is_file():
            archive.write(p, p.relative_to(stage).as_posix())
with zipfile.ZipFile(args.output) as archive:
    assert archive.testzip() is None
    assert 'index.html' in archive.namelist()
    assert archive.namelist().count('index.html') == 1
print(json.dumps({'archive': str(args.output.resolve()), 'bytes': args.output.stat().st_size,
                  'staging': str(stage), 'files': len(info['files']) + 1}))
