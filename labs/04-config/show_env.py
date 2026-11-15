import json

from dotenv import dotenv_values

keys = ["SHIPPING_NOTE", "SHIPPING_TRACKING_URL", "SHIPPING_TEMPLATE", "SHIPPING_DOUBLE", "SHIPPING_SINGLE", "SHIPPING_SPACES", "SHIPPING_EXPORTED"]
values = dotenv_values("sample.env")
print(json.dumps({k: values[k] for k in keys}))
