# Loopit HTML5 package verification

- Repository: public, anonymous HTTP response 200.
- ZIP: `tidecraft-loopit.zip`, 15,173,008 bytes; root `index.html`, 228 files, no outer project folder.
- Runtime files: ordinary `.js`, `.wasm`, `.data`, all referenced by relative paths. No custom Content-Encoding is needed. No CDN or API key dependency.
- Editable Unity project: `source/Unity/Assets`, `Packages`, `ProjectSettings`.
- ZIP CRC, runtime path existence and every file's SHA-256 manifest were checked after extraction.
- Tested the extracted ZIP from `/nested/play/` inside an iframe with `sandbox="allow-scripts allow-same-origin allow-pointer-lock"` and a 390×844 viewport.
- Actual UI results: automatic startup, walls 1→2, towers 2→3, basin connected after digging, wave 1 advances and strikes a defence, pause preserves state.
- The browser test tool logged one MutationObserver initialization error in the iframe harness. None of the supplied game scripts use MutationObserver; gameplay loaded and the checks above passed. The later standalone public page logged no new errors.
- GitHub Pages built successfully from `989e397b1ccc5eedd9bd390a0f460804019a4244`; public wasm response is 200 with `Content-Type: application/wasm`.
- Public page reached `autoStart=true`, `engineLoaded=true`, `gameReady=true`, wave phase `build`, goal health 100.
- Release: https://github.com/wentaopeng714-cmd/tidecraft-unity/releases/tag/v0.3.1-loopit
- Download: https://github.com/wentaopeng714-cmd/tidecraft-unity/releases/download/v0.3.1-loopit/tidecraft-loopit.zip
- Play: https://wentaopeng714-cmd.github.io/tidecraft-unity/

Loopit itself was not available for an importer test. This is a self-contained HTML5/WebAssembly package, validated in a standalone browser and iframe; the target platform must permit WebGL 2 and WebAssembly.

Packaging is reproducible with `Tools/package_loopit.py`. Unity gameplay binaries are unchanged from the three-wave prototype; this package expands the compressed runtime files and embeds the loader configuration.

Anonymous direct download was verified with `?download=1`; the downloaded archive has identical SHA-256 to the delivered ZIP: `71a7a1521f8e31f066ea9412628a6676eab24fd78d3be26a8624257c86367cd1`. CRC and the root index were checked again on the downloaded file.
