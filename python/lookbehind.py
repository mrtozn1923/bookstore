import re

for pattern in [r"(?<=price: )\d+", r"(?<=price:\s*)\d+"]:
    try:
        print(pattern, re.findall(pattern, "price: 450 TRY"))
    except re.error as e:
        print(pattern, "re.error:", e)
