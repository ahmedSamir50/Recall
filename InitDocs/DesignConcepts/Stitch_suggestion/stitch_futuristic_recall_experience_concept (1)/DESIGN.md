---
name: Synaptic Memory OS
colors:
  surface: '#111319'
  surface-dim: '#111319'
  surface-bright: '#37393f'
  surface-container-lowest: '#0c0e13'
  surface-container-low: '#15181e'
  surface-container: '#1b1e24'
  surface-container-high: '#282a30'
  surface-container-highest: '#32353b'
  on-surface: '#e1e2ea'
  on-surface-variant: '#9aa8a9'
  inverse-surface: '#e2e2e9'
  inverse-on-surface: '#2e3036'
  outline: '#849495'
  outline-variant: '#3a494b'
  surface-tint: '#00dce6'
  primary: '#e0fdff'
  on-primary: '#00373a'
  primary-container: '#00f2fe'
  on-primary-container: '#00373a'
  inverse-primary: '#00696f'
  secondary: '#dbb8ff'
  on-secondary: '#470083'
  secondary-container: '#6807ba'
  on-secondary-container: '#d0a6ff'
  tertiary: '#e1ffec'
  on-tertiary: '#003824'
  tertiary-container: '#67f4b7'
  on-tertiary-container: '#006e4b'
  error: '#ffb4ab'
  on-error: '#690005'
  error-container: '#93000a'
  on-error-container: '#ffdad6'
  primary-fixed: '#6ff6ff'
  primary-fixed-dim: '#00dce6'
  on-primary-fixed: '#002022'
  on-primary-fixed-variant: '#004f53'
  secondary-fixed: '#efdbff'
  secondary-fixed-dim: '#dbb8ff'
  on-secondary-fixed: '#2b0052'
  on-secondary-fixed-variant: '#6600b7'
  tertiary-fixed: '#6ffbbe'
  tertiary-fixed-dim: '#4edea3'
  on-tertiary-fixed: '#002113'
  on-tertiary-fixed-variant: '#005236'
  background: '#0e1015'
  on-background: '#e2e2e9'
  surface-variant: '#33353a'
  traffic-close: '#ff5f56'
  traffic-minimize: '#ffbd2e'
  traffic-maximize: '#27c93f'
  code-terminal-bg: '#090b0e'
typography:
  headline-lg:
    fontFamily: Space Grotesk
    fontSize: 20px
    fontWeight: '700'
    lineHeight: 28px
  headline-md:
    fontFamily: Space Grotesk
    fontSize: 18px
    fontWeight: '700'
    lineHeight: 24px
  headline-sm:
    fontFamily: Space Grotesk
    fontSize: 16px
    fontWeight: '600'
    lineHeight: 22px
  title-md:
    fontFamily: Space Grotesk
    fontSize: 15px
    fontWeight: '600'
    lineHeight: 20px
  title-sm:
    fontFamily: Space Grotesk
    fontSize: 14px
    fontWeight: '600'
    lineHeight: 18px
  body-lg:
    fontFamily: Inter
    fontSize: 16px
    fontWeight: '500'
    lineHeight: 24px
  body-md:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 20px
  body-sm:
    fontFamily: Inter
    fontSize: 13px
    fontWeight: '400'
    lineHeight: 18px
  code-body:
    fontFamily: JetBrains Mono
    fontSize: 11.5px
    fontWeight: '400'
    lineHeight: 18px
  label-md:
    fontFamily: JetBrains Mono
    fontSize: 11px
    fontWeight: '500'
    lineHeight: 16px
  label-sm:
    fontFamily: JetBrains Mono
    fontSize: 10px
    fontWeight: '500'
    lineHeight: 14px
  label-xs:
    fontFamily: JetBrains Mono
    fontSize: 9.5px
    fontWeight: '400'
    lineHeight: 12px
rounded:
  sm: 0.125rem
  DEFAULT: 0.25rem
  md: 0.375rem
  lg: 0.5rem
  xl: 0.75rem
  full: 9999px
spacing:
  gutter: 1.25rem
  gutter-sm: 0.75rem
  margin: 1.5rem
  margin-mobile: 1rem
  space-xs: 0.25rem
  space-sm: 0.5rem
  space-md: 0.75rem
  space-lg: 1rem
  space-xl: 1.5rem
  space-2xl: 2rem
---

## Brand & Style
Synaptic Memory OS is designed for high-performance personal neural computing, local AI intelligence, and cognitive retrieval. The visual aesthetic fuses dark utilitarian cyberpunk elegance with tactile, glassmorphic desktop luxury (reminiscent of Raycast, Arc, and macOS Pro workflows).

- **Visual Theme:** Technical Glassmorphism combined with Dense Monospace Cyber-HUD aesthetics.
- **Atmosphere:** Deep obsidian-space background tones illuminated by coherent bioluminescent cyan, neural violet, and encrypted emerald status indicators.
- **User Perception:** Uncompromising local privacy, ultra-low latency, and dense cryptographic precision.

## Colors
The palette leverages high-contrast luminescence over a stepped obsidian baseline.

- **Primary (`#00f2fe`):** Emitted for active attention vectors, search accents, cursor pulses, and high-confidence neural linkages.
- **Secondary (`#7928ca` / `#dbb8ff`):** Dedicated to multilingual semantic parsing, voice audio memos, and secondary cross-modal alignment.
- **Tertiary (`#10b981` / `#67f4b7`):** Hardware telemetries, air-gap indicators, online NPU activity, and local integrity verification.
- **Surfaces (`#0c0e13` to `#32353b`):** Deep layered dark tones providing optical separation with low-opacity borders rather than heavy opaque fills.

## Typography
Typography is divided across three clear functional roles:

1. **Space Grotesk (Headlines & Brand):** Technical, structural geometric sans used for application titles, memory headings, and key sectional cards.
2. **Inter (Body & Content):** Clear, highly legible humanist-neutral sans for human transcriptions, long-form thoughts, and user quotes.
3. **JetBrains Mono (Metadata & Telemetry):** Strict monospace typeface reserved for latency numbers, coordinate tags, key shortcuts (`⌘K`), vector distances, file paths, and syntax-highlighted code segments.

## Layout & Spacing
The layout implements an application cockpit architecture designed around fixed-height window chrome with fluid inner panels.

- **Grid Architecture:** 12-column bento grid for primary multimedia results (`xl:grid-cols-12`), collapsing to single-column feeds on smaller devices.
- **Dock & Rail Spacing:**
  - Header: Fixed `48px` (`3rem`) height.
  - Slim navigation rail: `64px` (`4rem`) collapsed, expanding to `224px` (`14rem`) on larger screens.
  - Inspector side-panel: Fixed `320px` (`20rem`) width on screens 1536px and above.
  - Status bar: Fixed `36px` (`2.25rem`) baseline.
- **Rhythm:** Dense, info-rich spacing driven by 4px (`0.25rem`) and 8px (`0.5rem`) increments, ensuring high data density without visual clutter.

## Elevation & Depth
Depth is realized through optical glass translucency, subtle edge luminance, and layered backdrop blurs rather than diffuse black drop shadows.

- **Level 0 (Canvas Base):** Solid deep background `#0e1015` underlaid by real-time WebGL neural lattice canvas animations.
- **Level 1 (Dock & Rails):** `rgba(12, 14, 19, 0.8)` with `backdrop-filter: blur(24px)` and fine hairline borders (`border: 1px solid rgba(255, 255, 255, 0.05)`).
- **Level 2 (Bento Cards - Glass Surface):** `rgba(25, 28, 33, 0.72)` with `backdrop-filter: blur(28px)` and subtle top/edge highlight (`border: 1px solid rgba(255, 255, 255, 0.07)`).
- **Level 3 (Command Bar / Active Focus):** `rgba(34, 38, 46, 0.82)` with `backdrop-filter: blur(32px)`, bordered by `rgba(0, 242, 254, 0.25)` accompanied by a vibrant cyan aura: `box-shadow: 0 0 25px -4px rgba(0, 242, 254, 0.25), 0 0 8px 0 rgba(0, 242, 254, 0.15)`.

## Shapes
Shapes adhere to a modern desktop OS curvature scheme:

- **Outer Cards & Command Bars:** `1rem` to `1.25rem` (`rounded-2xl`) for major container boundaries.
- **Buttons, Codeblocks & Badges:** `0.5rem` (`rounded-lg`) to `0.75rem` (`rounded-xl`) for internal interactive targets.
- **Tags, Indicators & Keycaps:** Fully rounded `9999px` (`rounded-full`) for status indicators, pill switches, and hardware indicators.
- **Keyboard Shortcut Badges (`<kbd>`):** `0.5rem` (`rounded-lg`) compact containers with interior inset outlines.

## Components

### Buttons & Interactive Controls
- **Primary Action:** Solid glowing fill in `#00f2fe` with `#00373a` text, bold font, and subtle hover scale/lift.
- **Secondary Glass Action:** Dark glass surface with hairline border (`rgba(255, 255, 255, 0.08)`) and high-contrast hover states.
- **Window Actions:** macOS standard 12px circular controls (`#ff5f56`, `#ffbd2e`, `#27c93f`) with 1px border.

### Command & Search Bar
- Floating glassmorphic dock combining segment switcher, primary input with blinking accent cursor, live latency pill (`0.38ms`), and tactile shortcut hints (`⌘K`).

### Bento Cards
- Rounded multi-layer cards encapsulating specific modal data (rich video frames, interactive SVG waveforms, syntactical code panes, and telemetry radars).
- Overlaid image badges with backdrop blurs (`rgba(0, 0, 0, 0.6)`).

### Chips & Filter Pills
- Compact pills styled with monospace labels, leading emojis or material icons, and dismissive close triggers (`hover:text-white`).

### Data & Status Badges
- Strict technical badges utilizing colored circular status dots (such as pulsing green pings for hardware air-gaps or neural lattice status).