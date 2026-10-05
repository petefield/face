#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 1 ]]; then
	echo "Usage: $0 <emotion>" >&2
	exit 1
fi

curl -X POST "http://blinky.local:5000/emotion/$1"
