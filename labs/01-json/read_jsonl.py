import json
from decimal import Decimal

# each line is parsed on its own; a broken line does not stop the rest
with open("broken.jsonl", encoding="utf-8") as f:
    for number, line in enumerate(f, start=1):
        try:
            order = json.loads(line, parse_float=Decimal)
            print(number, order["orderNumber"], order["total"])
        except json.JSONDecodeError as e:
            print(number, "skipped:", e.msg)
