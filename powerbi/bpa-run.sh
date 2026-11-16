#!/usr/bin/env bash
set -euo pipefail

TE2_VERSION="2.21.6"
TE2_ZIP="TabularEditor.Portable.zip"
TE2_URL="https://github.com/TabularEditor/TabularEditor/releases/download/${TE2_VERSION}/${TE2_ZIP}"

echo "=== Downloading Tabular Editor 2 v${TE2_VERSION} ==="
curl -fsSL -o "${TE2_ZIP}" "${TE2_URL}"
unzip -q "${TE2_ZIP}" -d te2
chmod +x te2/TabularEditor.exe 2>/dev/null || true

MODEL_PATH="powerbi/TalentPulse.Dataset"
RULES_PATH="powerbi/bpa-rules.json"
LOG_FILE="bpa-results.xml"

echo "=== Running Best Practice Analyzer ==="
# TabularEditor 2 on Linux runs via mono; on Windows it runs natively.
# GitHub Actions ubuntu runner has mono available.
mono te2/TabularEditor.exe "${MODEL_PATH}" -A "${RULES_PATH}" -V 2 -TRX "${LOG_FILE}" || BPA_EXIT=$?

echo "=== BPA Results ==="
if [ -f "${LOG_FILE}" ]; then
    # Print violations to stdout for the CI log
    grep -E "(error|warning|message)" "${LOG_FILE}" | head -60 || true
fi

exit "${BPA_EXIT:-0}"
