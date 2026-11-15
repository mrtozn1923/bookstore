import sys

import yaml

with open(sys.argv[1], encoding="utf-8") as f:
    workflow = yaml.safe_load(f)

print(list(workflow.keys()))
print(workflow.get("on"))
