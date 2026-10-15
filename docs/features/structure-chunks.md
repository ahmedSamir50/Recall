Status: open

The in-code split is the 8192 token budget in text-chunking.md. A heading, a table, and a fenced block are not read as structure yet.

When this is built, a file that fits with its breadcrumb stays one vector. Past the budget, split on a Markdown heading, keep a fenced block whole when it fits, keep a pipe table whole when it fits, split HTML before h1 through h6, keep a table, pre, or code element whole when it fits, and split a PDF on a page. The breadcrumb is the heading path inside the existing title prefix. A unit larger than the budget uses the rune cut and repeats the breadcrumb. A code file splits on a blank line inside the budget, otherwise the rune cut. Chunks are not averaged. v1 does not extract entities or relations.
