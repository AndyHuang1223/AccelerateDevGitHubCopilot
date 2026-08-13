#!/usr/bin/env bash
set -euo pipefail
git status --short
git diff --cached --name-status
git diff --cached

