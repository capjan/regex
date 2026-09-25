#!/usr/bin/env python3
"""Reads a Cobertura report, writes a coverage badge (SVG) and a Markdown summary."""
import sys
import xml.etree.ElementTree as ET

report, badge, summary = sys.argv[1:4]
root = ET.parse(report).getroot()
line = float(root.attrib["line-rate"]) * 100
branch = float(root.attrib["branch-rate"]) * 100

color = "#4c1" if line >= 90 else "#dfb317" if line >= 75 else "#e05d44"
label, value = "coverage", f"{line:.0f}%"
lw, vw = 61, 6 * len(value) + 14
w = lw + vw

with open(badge, "w") as f:
    f.write(f"""<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="20" role="img" aria-label="{label}: {value}">
<linearGradient id="s" x2="0" y2="100%"><stop offset="0" stop-color="#bbb" stop-opacity=".1"/><stop offset="1" stop-opacity=".1"/></linearGradient>
<clipPath id="r"><rect width="{w}" height="20" rx="3" fill="#fff"/></clipPath>
<g clip-path="url(#r)"><rect width="{lw}" height="20" fill="#555"/><rect x="{lw}" width="{vw}" height="20" fill="{color}"/><rect width="{w}" height="20" fill="url(#s)"/></g>
<g fill="#fff" text-anchor="middle" font-family="Verdana,Geneva,DejaVu Sans,sans-serif" font-size="11">
<text x="{lw / 2}" y="15" fill="#010101" fill-opacity=".3">{label}</text><text x="{lw / 2}" y="14">{label}</text>
<text x="{lw + vw / 2}" y="15" fill="#010101" fill-opacity=".3">{value}</text><text x="{lw + vw / 2}" y="14">{value}</text>
</g></svg>
""")

rows = "\n".join(
    f"| {c.attrib['name']} | {float(c.attrib['line-rate']) * 100:.0f}% |"
    for c in sorted(root.iter("class"), key=lambda c: c.attrib["name"])
    if "/" not in c.attrib["name"] and "<" not in c.attrib["name"]
)
with open(summary, "w") as f:
    f.write(f"## Code coverage\n\nLines: **{line:.1f}%** · Branches: **{branch:.1f}%**\n\n| Class | Lines |\n|---|---|\n{rows}\n")
