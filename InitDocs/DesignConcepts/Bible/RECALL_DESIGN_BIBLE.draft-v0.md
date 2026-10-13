# Recall — Design Bible (single source of truth)
Status: **LOCKED** · approved 2026-10-13 · direction "Stitch enhanced" (Synaptic Memory OS). Reference build: `index.html`, `landing.html`, `tokens.css`, `shader.js`.

## 1. Direction
Obsidian glass HUD over a calm living lattice. Cyan = action/focus, violet = voice, emerald = privacy/success, amber = warnings & video. Plain, truthful language; local-only promise visible on every screen. No fake specs, no device sync, no ambient capture.

## 2. Tokens (`tokens.css`, CSS vars; switch via `html[data-theme]`)
| Token | Dark | Light | Use |
|---|---|---|---|
| --bg | #0B0D12 | #EEF1F4 | page |
| --surface | #111319 | #F4F6F8 | base |
| --c0…c4 | #0C0E13 #15181E #1B1E24 #282A30 #32353B | #FFF #F7F8FA #EDF0F3 #DEE3E8 #CDD4DB | containers (low→high) |
| --ink | #E8E5DF | #12151A | primary text |
| --ink-2 | #AEB9C4 | #3E4954 | secondary text (min) |
| --ink-3 | #8793A0 | #5B6672 | meta only, ≥12px |
| --line / --line-2 | ink-2 @14% / 24% | ink @10% / 18% | borders |
| --cyan / --cyan-ink | #00F2FE / #00F2FE | #00C4CE / #00737A | fill / text-on-surface |
| --on-cyan | #00262A | #FFFFFF | text on primary button |
| --violet | #C9A2FF | #6B21A8 | voice |
| --emerald | #67F4B7 | #047857 | private/ok |
| --amber | #FFC46B | #8A4B00 | warn/video |
| --err | #FFB4AB | #B3261E | error |
Glass: **g1** chrome (bg .78, blur 24) · **g2** cards (.70, blur 28, 1px line, shadow) · **g3** focus surfaces (.84, blur 32, cyan ring + glow). Radii: card 16, button 12, input 12, pill 999. Never hard-code hex in components.

## 3. Type
Space Grotesk (heads) · Inter (body) · JetBrains Mono (labels, numbers, paths) · IBM Plex Sans Arabic (all Arabic; replaces all three in `lang=ar`, no letter-spacing).
Scale (px/line): display 60/1.05 (phone 34) · h1 28 · h2 22 · h3 18 · title 16/600 · body 14/1.5 (lead 16–17) · small 13 · label 11 mono uppercase +.08em (AR 12, no caps) · min 11.

## 4. Spacing & layout
4-px grid; steps 4 8 12 16 20 24 32 40 64. Card padding 16 (phone) / 20. Grid gap 16. Content max 1500 (app), 1200 (landing). Results bento: 1 col <768, 6 col md, 12 col xl.

## 5. Shell anatomy
| Viewport | Header | Nav | Inspector | Status |
|---|---|---|---|---|
| Phone <768 | 48, logo+theme+lang+avatar | bottom tab bar (Search, Lattice, Folders, Preferences) + safe-area | none (sheet) | hidden |
| Mid 768–1023 | 48 + privacy pill | rail 64 icons | none | 36 |
| Large 1024–1535 | 48 | rail 224 labelled + vault + model card | none | 36 |
| Wide ≥1536 | 48 | 224 | 320 sticky | 36 |
Privacy pill "On this device · 0 KB sent" in header ≥md; status bar = items, indexing state, models, Ready.

## 6. Components (one Razor component each)
Button (primary/ghost, 12 r, press scale .97, focus ring 2px cyan-ink) · Pill/Chip (removable context chip) · KindTag (icon + word: Photo/Voice memo/Document/Video/Code — never colour alone) · CommandBar (Find/Group/Tag segmented tablist, query, latency pill, Ctrl K) · ResultCard variants: Photo (hero, gradient scrim, white text), Voice (play, waveform LTR, transcript + translation), Document, VideoFrames (3 thumbs, best-match ring), Unavailable (dashed, path, Locate), Notice (audio desktop-only) · Inspector (why matched, file meta, Open / Show in folder) · Stepper (3 bars) · FolderRow (checkbox, LTR path, item count) · ModelRow (state: queued / downloading % / verified / hash-failed + Retry) · Rail, TabBar, StatusBar, ThemeToggle, LangToggle, Avatar (initials) · LatticeBackground (shader).

## 7. Screens & states
- **Welcome**: privacy pill, kicker, headline (2nd line cyan), lead, CTAs "Choose folders" / "How it stays private", 4 facts (no account, no cloud copy, offline, delete anytime).
- **Folders (1/3)**: user-picked folders only, add folder, "skipped" note. Back/Continue.
- **Models (2/3)**: vision+text, language model, speech-to-text (Desktop only). Continue disabled until all required verified. Hash failure: red row, "Checksum didn't match — file discarded", Retry; SHA-256 note.
- **Ready (3/3)**: counts, "Start searching".
- **Home**: command bar → "Understood as" chips → results header (count across kinds, sort) → bento → inspector (wide). Required: empty state (no results / no folders), unavailable card, audio-desktop-only notice (mobile), indexing-paused status, loading skeletons.
- **Lattice (secondary)**: same shell, nodes coloured AND labelled by kind, "Device sync isn't in this version" stub.
- **Landing**: nav, hero + live result preview, 3 facts, 4 privacy cards (incl. honest limits), 3 steps, CTA, footer. Claims must be true for v1.

## 8. RTL / Arabic
`dir=rtl` on `<html>`; logical properties only (ms/me/ps/pe/start/end, border-s); directional icons get `.flip`. Paths, file names, extensions, timecodes, model names, shortcuts in `<bdi dir=ltr>`. Arabic-Indic digits in AR UI copy; mono values stay LTR. Foreign-language transcripts keep their own `dir/lang`. Waveforms and lattice canvas are always LTR. No uppercase/letter-spacing in AR.

## 9. Motion & shader
Durations 160–500 ms, ease cubic-bezier(.2,.8,.2,1); entrance `rise` 8px. Hover: card lift 2px + glow. `prefers-reduced-motion`: all CSS motion off, shader draws one static frame. Shader: one WebGL canvas fixed behind app, DPR ≤1.5, single rAF loop, pause on `visibilitychange`/offscreen/freeze, resume with stall recovery, time ×0.6, eased pointer parallax ≤4%, theme cross-fade ~0.4 s, scrim gradient above it. If WebGL fails → solid --bg. Never place text directly on the shader without g1/g2 glass.

## 10. Accessibility
Body text ≥4.5:1 (ink ≈15:1, ink-2 ≥7:1 on dark); ink-3 only for meta ≥12px. Primary button text on cyan uses --on-cyan. Visible focus on every control; tablists use role/aria-selected; nav aria-current; icons aria-hidden with text labels; touch targets ≥40 px (phone 44). Status never by colour alone.

## 11. Blazor port map (Recall.UI)
- `wwwroot/css/tokens.css` ← this `tokens.css` verbatim.
- Tailwind CLI build (no CDN): `tailwind.config.js` content `**/*.razor`, extends spacing header/rail/railx/insp/status + fonts; output `wwwroot/css/app.css`. Self-host fonts in `wwwroot/fonts` (offline app).
- `wwwroot/js/lattice.js` ← `shader.js` (ES module, JS interop `mountLattice/setTheme/destroy`).
- Theme & locale: `ThemeService`, `LocaleService` (en/ar, sets `html[dir,lang,data-theme]`, persisted in Preferences); strings in `.resx` (en, ar).
- Razor: `Layout/AppShell.razor` (Header, Rail, TabBar, StatusBar, Inspector slot, LatticeBackground) · `Pages/Onboarding/{Welcome,Folders,Models,Ready}.razor` · `Pages/Home.razor` · `Pages/Lattice.razor` · `Components/{Button,Pill,KindTag,CommandBar,ResultCard*,Inspector,Stepper,FolderRow,ModelRow,EmptyState}.razor`. Landing is a website, not in the app.
