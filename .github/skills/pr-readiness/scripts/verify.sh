#!/usr/bin/env bash
set -euo pipefail
dotnet build
dotnet test
git diff --check
