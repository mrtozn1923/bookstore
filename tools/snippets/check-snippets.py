"""Offline checks for the shared snippet files (no IDE needed)."""
import json
import sys
import xml.etree.ElementTree as ET

errors = []

snippets = json.load(open(".vscode/bookstore.code-snippets", encoding="utf-8"))
for name, s in snippets.items():
    for key in ("prefix", "body", "scope"):
        if key not in s:
            errors.append(f"VS Code '{name}': missing {key}")
print(f"VS Code: {len(snippets)} snippets")

ns = {"s": "http://schemas.microsoft.com/VisualStudio/2005/CodeSnippet"}
root = ET.parse("tools/snippets/visualstudio/bsfact.snippet").getroot()
for cs in root.findall("s:CodeSnippet", ns):
    title = cs.findtext("s:Header/s:Title", namespaces=ns)
    code = cs.find("s:Snippet/s:Code", ns)
    if not title or code is None or "Language" not in code.attrib:
        errors.append("Visual Studio: Title and Code/@Language are required")
    for lit in cs.findall(".//s:Literal", ns):
        lid = lit.findtext("s:ID", namespaces=ns)
        if lit.find("s:Default", ns) is None:
            errors.append(f"Visual Studio: literal {lid} has no Default")
        if f"${lid}$" not in code.text:
            errors.append(f"Visual Studio: literal {lid} is not used in Code")
    print(f"Visual Studio: '{title}' ({code.attrib['Language']}, {code.attrib.get('Kind')})")

rider = ET.parse("tools/snippets/rider/bookstore.DotSettings").getroot()
keys = [e.attrib["{http://schemas.microsoft.com/winfx/2006/xaml}Key"] for e in rider]
shortcuts = [e.text for e in rider if e.attrib["{http://schemas.microsoft.com/winfx/2006/xaml}Key"].endswith("/Shortcut/@EntryValue")]
print(f"Rider: {len(keys)} keys, shortcuts {shortcuts}")

for e in errors:
    print("ERROR", e)
sys.exit(1 if errors else 0)
