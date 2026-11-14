#!/usr/bin/env bash
set -euo pipefail
dotnet tool restore > /dev/null
dotnet puml-gen src/Bookstore.Institutional.Domain docs/diagrams/generated/domain \
  -dir -public -createAssociation -excludePaths "**/obj,**/bin" -allInOne > /dev/null
git diff --exit-code --stat -- docs/diagrams/generated
