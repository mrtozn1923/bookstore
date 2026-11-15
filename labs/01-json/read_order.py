import json

with open("order.json", encoding="utf-8") as f:
    order = json.load(f)
print(order["orderId"], order["status"], order["total"])
print(json.dumps(order))
