Status: accepted, 15 Oct 2026.

Context: two language lists were being collapsed.

Decision: user-facing chrome is Arabic and English. The retrieval golden set is English, Arabic, Chinese, Hindi, and French, within ±5 recall@10 points, and that run has not been recorded.

Consequences: a docs page must not drop Chinese, Hindi, or French, and must not add them as UI locales.
