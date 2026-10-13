# Recall — Design Bible v1.0
Single source of truth for the Recall UI (.NET 10 MAUI Blazor Hybrid). Status: LOCKED, approved 2026-10-13. Direction: "Stitch enhanced" (Synaptic Memory OS).
Reference prototype: DesignConcepts\Prototypes\stitch-enhanced\ (index.html, landing.html, tokens.css, shader.js).
Precedence: this file > prototype > older concepts. Changes need a version bump and a changelog line (§16).

## 1. Principles
1. Private by default, and visibly so. The local-only promise ("On this device · 0 KB sent") is on every app screen.
2. Truthful. No fake specs, no feature that v1 does not ship (no device sync, no ambient capture, no cloud).
3. One question, every kind. Photos, voice memos, documents, video frames and code are equal citizens.
4. Calm HUD. Glass over a slow living lattice; the content is the brightest thing on screen.
5. Bilingual from day one. English and Arabic (RTL) are both first-class; no string is EN-only.

## 2. Colour tokens
Defined in `tokens.css` on `:root,[data-theme=dark]` and `[data-theme=light]`. Components use only these vars.

| CSS var | Dark | Light | Role |
|---|---|---|---|
| `--bg` | #0B0D12 | #EEF1F4 | page background, shader fallback |
| `--surface` | #111319 | #F4F6F8 | base surface |
| `--c0` | #0C0E13 | #FFFFFF | lowest container |
| `--c1` | #15181E | #F7F8FA | inset rows, inputs |
| `--c2` | #1B1E24 | #EDF0F3 | hover fill |
| `--c3` | #282A30 | #DEE3E8 | tracks, skeleton |
| `--c4` | #32353B | #CDD4DB | inactive waveform |
| `--ink` | #E8E5DF | #12151A | primary text |
| `--ink-2` | #AEB9C4 | #3E4954 | secondary text |
| `--ink-3` | #8793A0 | #5B6672 | meta text, ≥12 px only |
| `--line` | rgba(174,185,196,.14) | rgba(18,21,26,.10) | default border |
| `--line-2` | rgba(174,185,196,.24) | rgba(18,21,26,.18) | strong border, ghost button |
| `--cyan` | #00F2FE | #00C4CE | primary fill, glow, rings |
| `--cyan-ink` | #00F2FE | #00737A | cyan text/icons on surfaces; primary fill in light |
| `--on-cyan` | #00262A | #FFFFFF | text on primary button |
| `--violet` | #C9A2FF | #6B21A8 | voice memo kind |
| `--violet-bg` | rgba(121,40,202,.22) | rgba(107,33,168,.09) | voice tag fill |
| `--emerald` | #67F4B7 | #047857 | privacy, success, verified |
| `--emerald-bg` | rgba(16,185,129,.16) | rgba(4,120,87,.09) | |
| `--amber` | #FFC46B | #8A4B00 | warning, video kind |
| `--amber-bg` | rgba(255,170,60,.14) | rgba(200,120,0,.10) | |
| `--err` | #FFB4AB | #B3261E | error |
| `--err-bg` | rgba(255,90,80,.14) | rgba(179,38,30,.08) | |

Kind mapping (always icon + word, never colour alone): Photo = cyan · Voice memo = violet · Document = emerald · Video = amber · Code = ink-2.
Photo hero scrim is fixed in both themes: linear-gradient(180deg, transparent 35%, rgba(8,10,14,.92)); text on it is #FFFFFF / #C7D2DC.

## 3. Elevation & glass
| Level | Class | Fill (dark / light) | Blur | Border | Shadow | Use |
|---|---|---|---|---|---|---|
| L1 | `.g1` | rgba(12,14,19,.78) / rgba(255,255,255,.74) | 24 px, saturate 1.2 | hairline via layout | none | header, rail, status, tab bar |
| L2 | `.g2` | rgba(25,28,33,.70) / rgba(255,255,255,.66) | 28 px | 1 px `--line` | `--shadow` | cards, panels, dialogs |
| L3 | `.g3` | rgba(34,38,46,.84) / rgba(255,255,255,.88) | 32 px | ring via `--glow` | `--glow` | command bar, focused/active surface, CTA band |
`--shadow` dark 0 20px 50px -24px rgba(0,0,0,.7); light 0 18px 40px -26px rgba(20,30,40,.35).
`--glow` dark 0 0 0 1px rgba(0,242,254,.35), 0 8px 40px -12px rgba(0,242,254,.35); light uses rgba(0,115,122,.35) and -14px.
Fallback when backdrop-filter is unsupported: raise fill alpha to .94.

## 4. Typography
Fonts (self-hosted in the app): Space Grotesk 500/600/700 (headings), Inter 400/500/600 (UI/body), JetBrains Mono 400/500 (labels, numbers, paths), IBM Plex Sans Arabic 400/500/600 (all Arabic text; replaces the other three under `lang=ar`).

| Token | Family | Size / line | Weight | Tracking | Use |
|---|---|---|---|---|---|
| display | Space Grotesk | 60/1.05 (md 48, phone 34) | 700 | -0.01em | welcome, landing hero (landing 72) |
| h1 | Space Grotesk | 28/1.2 (phone 24) | 600 | -0.01em | onboarding step titles |
| h2 | Space Grotesk | 22/1.25 | 600 | -0.01em | page titles |
| h3 | Space Grotesk | 18/1.3 (hero card 24) | 600 | -0.01em | card titles |
| section | Space Grotesk | 20/1.3 | 600 | | "Results" |
| title | Inter | 16/1.4 | 600 | | list item title |
| body | Inter | 14/1.5 | 400 | | default |
| lead | Inter | 16–17/1.55 | 400 | | intro paragraphs |
| small | Inter | 13/1.45 | 400 | | helper, notices |
| label | JetBrains Mono | 11/1.3 uppercase | 500 | +0.08em | kickers, section labels |
| mono | JetBrains Mono | 12/1.4 | 400 | +0.02em | paths, sizes, times, model names |
| tag | JetBrains Mono | 11/1 | 500 | | kind tags, pills |
Arabic: same sizes, label becomes 12 px with no uppercase and no tracking; headings 600. Minimum size anywhere: 11 px (EN), 12 px (AR).

## 5. Spacing, radius, sizing
Base 4 px. Scale: 4, 8, 12, 16, 20, 24, 32, 40, 64.
Card padding 16 (phone) / 20 (≥md). Grid gap 16. Section gap 24. Onboarding panel padding 20 / 28, max-width 640.
Content max-width 1500 (app), 1200 (landing). Radius: pill 999 · card 16 · CTA band 24 · button/input/row 12 · thumb 8 · tiny 6.
Hit targets: ≥40 px desktop, ≥44 px phone. Icon 18 px (nav), 16 px (buttons), 14 px (tags); stroke 1.6, round caps.

## 6. Shell anatomy
| Viewport | Width | Header | Navigation | Inspector | Status bar |
|---|---|---|---|---|---|
| Phone | <768 | 48 px: logo, wordmark, theme, language, avatar | bottom tab bar 64 px + safe area (Search, Lattice, Folders, Preferences) | bottom sheet | hidden; indexing state as a card |
| Mid | 768–1023 | 48 px + privacy pill | rail 64 px, icons only, tooltip labels | none | 36 px |
| Large | 1024–1535 | 48 px | rail 224 px: vault switcher, labelled items, on-device model card at bottom | none; details open as a drawer | 36 px |
| Wide | ≥1536 | 48 px | rail 224 px | 320 px sticky column, 20 px gap | 36 px |
Header order (LTR): logo + "Recall" | flex | privacy pill | theme | language | avatar "AS". RTL mirrors.
Rail items: Search, Lattice, Folders, Models, Preferences. Active: cyan-ink text, cyan 11% fill, 1 px inset cyan 28% ring, `aria-current=page`.
Status bar (mono 11): ● items count · indexing state (amber when paused) · flex · model names · Ready (emerald).
Main area scrolls; header, rail and status are fixed. Phone main has 80 px bottom padding for the tab bar.

## 7. Components
Each is one Razor component. Copy given EN / AR.

**Button** — primary (cyan fill, `--on-cyan` text; light uses `--cyan-ink` fill) · ghost (1 px `--line-2`, ink). 12 px radius, padding 10×16, 14/600. States: hover (primary: 4 px cyan 22% halo; ghost: `--c2` fill) · active scale .97 · focus 2 px `--cyan-ink` outline, 2 px offset · disabled opacity .45, no pointer · loading (spinner replaces leading icon, label kept).

**Pill / Chip** — 999 radius, 1 px `--line-2`, 12 px, no-wrap. Removable chip has a 12 px × button (aria-label "Remove {x}" / "إزالة {x}").

**KindTag** — icon + word, tag type, tinted fill per kind. EN: Photo, Voice memo, Document, Video, Code. AR: صورة، ملاحظة صوتية، مستند، فيديو، كود.

**PrivacyPill** — emerald on `--emerald-bg`, shield icon. "On this device · 0 KB sent" / "على هذا الجهاز · ٠ ك.ب مُرسَلة".

**CommandBar** (L3) — segmented tablist Find / Group / Tag (ابحث / جمّع / وسِّم), query field, latency pill ("38 ms"), shortcut "Ctrl K" (macOS shows ⌘K). Phone: query on its own first row, segments below. States: empty (placeholder "Search your memory in a sentence" / "ابحث في ذاكرتك بجملة"), typing (caret), searching (latency pill shows spinner), no results (§8 Home).

**ContextChips** — label "Understood as" / "فُهم على أنه" + removable chips (place, kind, entity, time). Horizontal scroll on phone.

**ResultCard** (L2, 16 radius, hover lift 2 px + glow, whole card is a button opening details):
- Photo: 16:10 (phone) / 2:1 (md) / 16:9 (xl) image, scrim, kind tag + "98% match" / "٩٨٪ تطابق", meta line (place · date), title, note, "Stored locally · never uploaded" / "مخزّن محليًا · لا يُرفع أبدًا".
- Voice memo: tag + time "00:14 / 00:30", title, play button (violet), waveform (always LTR), transcript in original language with its own `lang`/`dir`, highlighted query term, translation line "Translated: …" / "مترجم: …".
- Document: tag, file name (`bdi`), page + snippet, text-line preview.
- Video frames: tag + container (MP4), 3 thumbnails with LTR timecodes, best match ringed cyan.
- Code: tag, file name, line number, 6-line mono excerpt, no syntax colours beyond ink/ink-2/cyan.
- Unavailable: dashed `--line-2` border, amber alert icon, "File unavailable" / "الملف غير متاح", LTR path, "Moved or on a disconnected drive. Kept as a placeholder." / "نُقل أو على قرص غير متصل. يبقى كعنصر نائب.", ghost "Locate" / "حدّد موقعه".
- Skeleton: `--c3` blocks, shimmer off under reduced motion.

**Notice** — icon + title + small text. Audio-on-desktop: "Audio search runs on desktop" / "البحث الصوتي على الكمبيوتر"; "2 voice memos weren't searched on this phone." / "لم يُبحث في ملاحظتين صوتيتين على هذا الهاتف."

**Inspector** — label "Details" / "التفاصيل", preview, title, "Why this matched" / "لماذا ظهرت هذه النتيجة" (3 checked reasons), meta (File/الملف, Size/الحجم), actions "Open file" / "افتح الملف", "Show in folder" / "اعرض في المجلد".

**Stepper** — three 4 px bars, done/current cyan-ink, rest `--c3`; text "Step 2 of 3" / "الخطوة ٢ من ٣".

**FolderRow** — kind icon, folder name, LTR path (mono 12, truncate), item count, checkbox (accent cyan-ink). Whole row is the label.

**ModelRow** — name, LTR "Model · size", optional "Desktop only" / "للكمبيوتر فقط" pill, status right, 6 px progress track. States: Queued / في الانتظار (ink-3) · Downloading 62% / جارٍ التحميل ٦٢٪ (cyan, gradient cyan→violet) · Verified / تم التحقق (emerald, full bar) · Failed (err border 50%, full err bar, "Checksum didn't match — file discarded" / "البصمة غير مطابقة — حُذف الملف", primary "Retry download" / "أعد التحميل").

**EmptyState** — icon in L3 tile, title, one sentence, one action.

**Toggles** — Theme (sun/moon icon, aria-label "Theme" / "المظهر"); Language (globe + target language name "العربية" / "English").

**Avatar** — 32 px circle, violet on `--violet-bg`, initials "AS".

**LatticeBackground** — fixed canvas + scrim (§11).

## 8. Screens & states
**Welcome** — privacy pill; kicker "Private memory engine · v1" / "محرّك ذاكرة خاص · الإصدار ١"; display "Everything you saw, said and wrote — found in a sentence." (second line cyan) / "كل ما رأيته وقلته وكتبته — تجده بجملة واحدة."; lead; primary "Choose folders" / "اختر المجلدات"; ghost "How it stays private" / "كيف تبقى بياناتك خاصة"; 4 facts: No account, No cloud copy, Works offline, Delete anytime / بلا حساب، بلا نسخة سحابية، يعمل دون إنترنت، احذف متى شئت.

**Folders (1/3)** — "Pick what Recall may read" / "اختر ما يُسمح لريكول بقراءته"; FolderRows; dashed "Add folder" / "إضافة مجلد"; skipped note; Back / Continue (رجوع / متابعة). States: none selected → Continue disabled + helper "Pick at least one folder" / "اختر مجلدًا واحدًا على الأقل"; permission denied → row error "Recall can't read this folder" / "لا يستطيع ريكول قراءة هذا المجلد".

**Models (2/3)** — "Download the on-device models" / "نزّل النماذج على جهازك"; ModelRows (Vision + text embeddings, Language model, Speech-to-text desktop only); SHA-256 note. States: downloading, verified, hash failure + retry, offline ("No connection — download resumes when you're back online" / "لا يوجد اتصال — يُستأنف التحميل عند عودة الاتصال"), low disk ("Need 2.2 GB free" / "تحتاج ٢٫٢ غ.ب مساحة فارغة"). Continue disabled until all required models are Verified.

**Ready (3/3)** — emerald check tile, "Your memory is ready" / "ذاكرتك جاهزة", counts, "Start searching" / "ابدأ البحث".

**Home** — CommandBar → ContextChips → header "Results · 6 matches across 4 kinds" / "النتائج · ٦ نتائج من ٤ أنواع" + sort pill → bento (1/6/12 cols) → Inspector (wide). States: first run empty ("Ask anything you remember" + 3 example queries), searching (skeletons), no results ("Nothing matched. Try fewer words or remove a chip." / "لا نتائج. جرّب كلمات أقل أو أزل وسمًا."), no folders (CTA to Folders), indexing in progress/paused (status bar + phone card), unavailable card, audio-desktop-only notice (phone), Group mode (results grouped under headed clusters), Tag mode (selection checkboxes + "Add tag" bar).

**Lattice (secondary)** — same shell; label "Lattice" / "الشبكة", title "How your memories connect" / "كيف تترابط ذكرياتك"; L2 canvas, nodes coloured and labelled by kind, size = relevance; stub "Device sync isn't in this version." / "المزامنة بين الأجهزة ليست في هذا الإصدار."; canvas always LTR.

**Preferences** — Folders, Models, Theme (System/Dark/Light), Language, Delete index ("Delete everything Recall stored" destructive, confirm dialog). No ambient-capture toggles, no sync.

**Landing (website, not in the app)** — sticky L1 nav; hero (kicker platforms, display "Your memory. Searchable. Private.", lead, two CTAs) + live result preview; 3 facts (0 KB sent, 38 ms, 4 kinds); "Built for privacy first" 4 cards (Local only, One search every kind, Verified models, Honest limits); 3 steps; L3 CTA band; footer. Every claim must hold for v1.

## 9. RTL & Arabic
- Set `dir` and `lang` on `<html>`; never per component.
- Use logical properties only: margin/padding-inline, inset-inline-start, border-inline-start, text-align:start (Tailwind ms-/me-/ps-/pe-/start-/end-/border-s/border-e).
- Mirror directional icons (arrows, play) with `.flip`; never mirror logos, media, checkmarks, waveforms, charts, lattice canvas.
- Wrap paths, file names, extensions, timecodes, model names, sizes and shortcuts in `<bdi dir="ltr">`.
- Arabic UI copy uses Arabic-Indic digits (٠–٩) and Arabic separators (٬ ٫ ٪); mono technical values stay Latin and LTR.
- Foreign content keeps its own `lang` and `dir` (Italian transcript stays LTR inside RTL UI).
- No uppercase, no letter-spacing, no italics for emphasis in Arabic; use weight.
- Truncation uses ellipsis at the logical end; test each screen in AR at phone width.

## 10. Motion
Easing `cubic-bezier(.2,.8,.2,1)`. Durations: press 160 ms, hover/state 200–250 ms, enter 500 ms (`rise`: opacity 0→1, translateY 8→0), stagger ≤40 ms, theme cross-fade 400 ms, progress 300 ms.
Caret blink 1 s steps. No bounce, no parallax on content, no auto-playing media.
`prefers-reduced-motion: reduce` → all animations and transitions off, skeleton shimmer off, shader static.

## 11. Shader (LatticeBackground) safety
- One WebGL canvas, `position:fixed; inset:0; z-index:0`, `aria-hidden`, behind a radial scrim. Never more than one per window.
- DPR capped at 1.5; resize via ResizeObserver inside the draw loop (no flash).
- Single rAF loop; pause on `visibilitychange`, offscreen (IntersectionObserver), page freeze; resume with stall recovery (restart if no frame for 400 ms).
- Time runs at 0.6×; pointer parallax eased, ≤4% of the field; no pointer effect on touch.
- Reduced motion: draw one static frame and stop.
- Theme change cross-fades the `u_light` uniform (~0.4 s); no reload.
- Failure (no WebGL / context lost): hide canvas, show `--bg`; retry once on `webglcontextrestored`.
- Text never sits on bare shader; always on L1/L2/L3 glass.
- Budget: ≤2 ms GPU per frame on integrated GPU; app must stay usable with the shader off (Preferences ▸ "Reduce background motion").

## 12. Accessibility
- Contrast targets: body ≥4.5:1, large text and icons ≥3:1. Dark: ink on bg ≈15:1, ink-2 ≈9:1. Light: ink ≈16:1, ink-2 ≈8:1. ink-3 only for meta ≥12 px.
- Status and kind are never shown by colour alone (icon + word).
- Visible focus on every interactive element (2 px `--cyan-ink`, 2 px offset). Logical tab order follows reading direction.
- Semantics: segmented control = `role=tablist`/`tab` + `aria-selected`; nav `aria-current`; progress `role=progressbar` with value text; live region (polite) for search result counts and download status.
- Icons decorative (`aria-hidden`) when labelled; icon-only buttons have `aria-label` in both languages.
- Keyboard: Ctrl/⌘K focuses search, Esc clears/closes, arrows move within segments and results.
- Text scales to 200% without loss; layout reflows at 320 px wide.

## 13. Blazor / Razor port map (Recall.UI)
Assets:
- `wwwroot/css/tokens.css` ← prototype `tokens.css` (verbatim; this file wins on conflicts).
- Tailwind via CLI at build (no CDN): `tailwind.config.js` content `["./**/*.razor","./**/*.cs"]`; extend `spacing` {header:48px, rail:64px, railx:224px, insp:320px, status:36px}, `fontFamily` {head, mono}, `borderRadius` {card:1rem}. Output `wwwroot/css/app.css`; wire as an MSBuild target before build.
- `wwwroot/fonts/` self-hosted woff2 for the four families (app is offline).
- `wwwroot/js/lattice.js` ← `shader.js` (ES module). Interop: `mountLattice(canvas,{theme,maxDpr})`, `setTheme`, `set reduced`, `destroy`.
Services:
- `ThemeService` (System/Dark/Light → `html[data-theme]`, persisted via MAUI Preferences).
- `LocaleService` (en/ar → `html[lang,dir]`, `CultureInfo`, digit shaping helper).
- Strings in `Resources/Strings.resx` + `Strings.ar.resx`; keys match §7–8 copy.
Layout & pages:
- `Layout/AppShell.razor` = `AppHeader`, `NavRail`, `TabBar`, `StatusBar`, `InspectorHost`, `LatticeBackground`.
- `Pages/Onboarding/Welcome.razor`, `Folders.razor`, `Models.razor`, `Ready.razor` (route `/onboarding/{step}`).
- `Pages/Home.razor` (`/`), `Pages/Lattice.razor` (`/lattice`), `Pages/Preferences.razor` (`/preferences`).
Components (`Components/`): `Button`, `Pill`, `ContextChip`, `KindTag`, `PrivacyPill`, `CommandBar`, `ContextChips`, `ResultGrid`, `PhotoCard`, `VoiceCard`, `DocumentCard`, `VideoFramesCard`, `CodeCard`, `UnavailableCard`, `SkeletonCard`, `Notice`, `Inspector`, `Stepper`, `FolderRow`, `ModelRow`, `EmptyState`, `ThemeToggle`, `LanguageToggle`, `Avatar`.
Each component: parameters for data + state enum; no inline hex; scoped `.razor.css` only for layout specifics.

## 14. Do / Don't
Do
- Use token vars and the three glass levels only.
- Label every kind and status with icon + word.
- Show the privacy pill and real numbers.
- Keep English and Arabic copy in sync in the same PR.
- Isolate every LTR technical string with `bdi`.
- Gate Continue on real completion (folders chosen, models verified).
- Test dark/light × EN/AR × phone/mid/wide before merge.
Don't
- Don't claim sync, mesh, cloud, E2EE, hardware specs or features not in v1.
- Don't use colour alone, ink-3 for body text, or text directly on the shader.
- Don't add a second canvas, uncapped DPR, or motion that ignores reduced-motion.
- Don't use left/right CSS properties or mirror media and waveforms.
- Don't use other fonts, icon sets (no Material Symbols) or radii outside §5.
- Don't add ambient-capture toggles or fake avatars/names.

## 15. QA checklist
Contrast check per theme · RTL screenshot pass at 390 px · reduced-motion pass · shader paused when window hidden · keyboard-only run of onboarding · 200% text zoom · hash-fail and offline states reachable in a debug build.

## 16. Changelog
- 1.0 (2026-10-13): first locked version, from approved Stitch-enhanced prototype.
