/** @type {import('tailwindcss').Config} */
module.exports = {
  darkMode: ['selector', '[data-theme=dark]'],
  content: ['./**/*.{razor,cs,html}', '../Recall.Maui/**/*.{razor,cs,html}'],
  theme: {
    extend: {
      colors: {
        bg: 'var(--bg)',
        surface: 'var(--surface)',
        ink: 'var(--ink)',
        'ink-2': 'var(--ink-2)',
        'ink-3': 'var(--ink-3)',
        cyan: 'var(--cyan)',
        'cyan-ink': 'var(--cyan-ink)',
        violet: 'var(--violet)',
        emerald: 'var(--emerald)',
        amber: 'var(--amber)',
        err: 'var(--err)',
        primary: 'var(--cyan-ink)',
        secondary: 'var(--violet)',
        tertiary: 'var(--emerald)',
        background: 'var(--bg)',
        error: 'var(--err)',
      },
      fontFamily: {
        head: ['Space Grotesk', 'IBM Plex Sans Arabic', 'sans-serif'],
        mono: ['JetBrains Mono', 'IBM Plex Sans Arabic', 'monospace'],
      },
      borderRadius: {
        card: '1rem',
      },
      spacing: {
        header: '48px',
        rail: '64px',
        railx: '224px',
        insp: '320px',
        status: '36px',
      },
    },
  },
  plugins: [],
};
