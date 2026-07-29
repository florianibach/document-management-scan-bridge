#!/usr/bin/env python3
"""Validate planning documents and the repository-local skill without dependencies."""

from pathlib import Path
import re
import sys


ROOT = Path(__file__).resolve().parent.parent
SKILL = ROOT / ".codex/skills/scaffold-blazor-scan-bridge"
errors: list[str] = []


def error(path: Path, message: str) -> None:
    errors.append(f"{path.relative_to(ROOT)}: {message}")


markdown_files = [ROOT / "README.md", *sorted((ROOT / "docs").rglob("*.md"))]
for path in markdown_files:
    content = path.read_text(encoding="utf-8")
    if not content.startswith("# "):
        error(path, "must start with a level-one heading")
    if content != content.rstrip() + "\n":
        error(path, "must end with exactly one newline and no trailing whitespace")

    for target in re.findall(r"\[[^]]+\]\(([^)]+)\)", content):
        if target.startswith(("http://", "https://", "#")):
            continue
        resolved = (path.parent / target.split("#", 1)[0]).resolve()
        if not resolved.exists():
            error(path, f"contains a broken local link: {target}")

skill_file = SKILL / "SKILL.md"
skill_content = skill_file.read_text(encoding="utf-8")
frontmatter = re.match(r"^---\n(?P<body>.*?)\n---\n", skill_content, re.DOTALL)
if frontmatter is None:
    error(skill_file, "must begin with YAML frontmatter")
else:
    fields = {}
    for line in frontmatter.group("body").splitlines():
        key, separator, value = line.partition(":")
        if not separator:
            error(skill_file, f"invalid frontmatter line: {line}")
            continue
        fields[key.strip()] = value.strip()
    if set(fields) != {"name", "description"}:
        error(skill_file, "frontmatter must contain only name and description")
    if fields.get("name") != SKILL.name:
        error(skill_file, f"name must be {SKILL.name}")
    if not fields.get("description"):
        error(skill_file, "description must not be empty")

agent_metadata = SKILL / "agents/openai.yaml"
if "$scaffold-blazor-scan-bridge" not in agent_metadata.read_text(encoding="utf-8"):
    error(agent_metadata, "default prompt must reference the skill name")

if errors:
    print("\n".join(errors), file=sys.stderr)
    raise SystemExit(1)

print(f"Validated {len(markdown_files)} Markdown files and {SKILL.relative_to(ROOT)}")
