import re
from pathlib import Path


def score(st: str) -> int:
    return {"done": 100, "partial": 50, "not-started": 0}[st]


def parent_word(scores: list[int]) -> str:
    if not scores:
        return "not-started"
    if all(s == 100 for s in scores):
        return "done"
    if all(s == 0 for s in scores):
        return "not-started"
    return "partial"


def mean(scores: list[int]) -> int:
    return round(sum(scores) / len(scores)) if scores else 0


def parse_status(cell: str) -> str | None:
    m = re.search(r"`(done|partial|not-started)`", cell)
    return m.group(1) if m else None


def is_task(label: str) -> bool:
    return bool(
        re.match(r"^([0-9]+\.[0-9]+\.[0-9]+|A\.2\.[0-9]+|P\.[0-9]+\.[0-9]+)$", label)
    )


def main() -> None:
    path = Path("docs/plan.md")
    text = path.read_text(encoding="utf-8")
    start = text.index("## Progress\n")
    end = text.index("\n## Behavior matrix", start)
    block = text[start:end]
    lines = block.splitlines()
    t0 = next(i for i, line in enumerate(lines) if line.startswith("| | Status"))
    table_lines = [line for line in lines[t0 + 2 :] if line.startswith("|")]

    entries: list[tuple[str, str, bool, bool]] = []
    for line in table_lines:
        parts = [p.strip() for p in line.strip("|").split("|")]
        if len(parts) < 2:
            continue
        label, status_cell = parts[0], parts[1]
        st = parse_status(status_cell)
        if st is None:
            continue
        if label == "7.2.1":
            st = "done"
        entries.append(
            (
                label,
                st,
                is_task(label),
                label.startswith("**All epics**"),
            )
        )

    out: list[tuple[str, str, int]] = []
    all_task_scores: list[int] = []
    i = 0
    while i < len(entries):
        label, st, task, footer = entries[i]
        if footer:
            out.append((label, parent_word(all_task_scores), mean(all_task_scores)))
            i += 1
            continue
        if task:
            s = score(st)
            all_task_scores.append(s)
            out.append((label, st, s))
            i += 1
            continue
        if label.startswith("**Epic"):
            j = i + 1
            epic_scores: list[int] = []
            while j < len(entries) and entries[j][2]:
                epic_scores.append(score(entries[j][1]))
                j += 1
            out.append((label, parent_word(epic_scores), mean(epic_scores)))
            i += 1
            continue
        if label.startswith("**Phase") or label.startswith("**Launch Addendum"):
            j = i + 1
            phase_scores: list[int] = []
            while j < len(entries):
                if entries[j][3]:
                    break
                nlabel = entries[j][0]
                if j != i and (
                    nlabel.startswith("**Phase") or nlabel.startswith("**Launch Addendum")
                ):
                    break
                if entries[j][2]:
                    phase_scores.append(score(entries[j][1]))
                j += 1
            out.append((label, parent_word(phase_scores), mean(phase_scores)))
            i += 1
            continue
        i += 1

    new_table = [
        "## Progress",
        "",
        "Counted 24 Oct 2026. This is the only progress table.",
        "",
        "| | Status | Percent |",
        "|---|---|---|",
    ]
    for label, status, pct in out:
        new_table.append(f"| {label} | `{status}` | {pct} |")

    new_block = "\n".join(new_table) + "\n"
    path.write_text(text[:start] + new_block + text[end:], encoding="utf-8")
    print("All epics:", out[-1])


if __name__ == "__main__":
    main()
