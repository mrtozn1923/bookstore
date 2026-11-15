import sys

# tomllib ships with Python (TOML 1.0); tomli 2.4+ implements TOML 1.1
for name in ("tomllib", "tomli"):
    module = __import__(name)
    try:
        with open(sys.argv[1], "rb") as f:
            data = module.load(f)
        print(f"{name:8}", data["Shipping"]["Cutoff"], data["Shipping"]["MaxWeightGrams"], data["Shipping"]["Rates"][1])
    except Exception as e:
        print(f"{name:8}", type(e).__name__, e)
