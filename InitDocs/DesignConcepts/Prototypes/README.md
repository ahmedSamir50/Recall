# Recall · Stitch enhanced (concept)
Open via a local server: `python3 -m http.server` then /index.html (app) · /landing.html · /lab.html (shader lab).
URL params: `?s=welcome|folders|model|ready|home|lattice&theme=dark|light&lang=en|ar&fail` (model screen hash failure).

## What stayed from Stitch
Obsidian glass HUD, cyan #00F2FE / violet / emerald, Space Grotesk + Inter + JetBrains Mono, the Voronoi lattice shader, the 48px header / 64→224 rail / 320 inspector / 36 status shell, the bento results, the command bar with segment switcher, latency pill and shortcut.

## What changed and why
- One token set (`tokens.css` CSS vars + `tw.js`) for dark AND light; v0/v1/v2 had three diverging palettes and v2 used another font + icon set.
- Contrast: primary text #E8E5DF, secondary #AEB9C4 (Stitch #9AA8A9 failed); light theme uses darkened ink accents (#00737A, #6B21A8, #047857).
- Truthful content: removed P2P mesh / E2EE sync / "MacBook Pro M3 Max" / 4K HEVC extraction / "JD"; "Cluster" → Find / Group / Tag; avatar AS; "Device sync isn't in this version" stub.
- Modality always labelled (icon + word), not colour alone. Added states: model download with SHA-256 failure + retry, unavailable-file placeholder, "audio search runs on desktop" notice, indexing-paused status.
- New onboarding (welcome → folders → models → ready), landing rewritten to plain-language claims.
- Full Arabic + RTL (logical properties, isolated LTR paths/timecodes, Arabic numerals, IBM Plex Sans Arabic).
- Phone: query on its own row, bottom tab bar; mid: collapsed rail; big: inspector.
- Shader (`shader.js`): DPR cap 1.5, single rAF, pauses on hidden/offscreen (verified: 1 frame during freeze), static frame under prefers-reduced-motion, eased theme cross-fade, light variant, stall-recovery on resume.

## Files
index.html + app.js (app), landing.html, tokens.css, tw.js, shader.js, lab.html, shots/ (+ shots/before-after/), demo.mp4, img/ (Stitch's sample photos).

## Limitations
Tailwind Play CDN + Google Fonts need network (port tokens.css to wwwroot for MAUI). Sample photos are Stitch's. Lattice graph view is a simple static concept. Perf measured in software GL (SwiftShader): ~20 ms/frame avg; real GPUs will be far lower.
