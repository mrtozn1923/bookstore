import fnmatch

# fnmatch turns a glob into the regex it stands for
for glob in ["*.cs", "Order?.cs", "[!.]*.log"]:
    print(f"{glob:12} {fnmatch.translate(glob)}")
