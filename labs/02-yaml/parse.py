import json
import sys

import yaml
from ruamel.yaml import YAML

path = sys.argv[1]
with open(path, encoding="utf-8") as f:
    text = f.read()

# PyYAML implements YAML 1.1
print("PyYAML 6.0.3   ", json.dumps(yaml.safe_load(text), default=str))
# ruamel.yaml implements YAML 1.2 by default
print("ruamel.yaml    ", json.dumps(YAML(typ="safe", pure=True).load(text), default=str))
