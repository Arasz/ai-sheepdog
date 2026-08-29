#!/usr/bin/env bash
# verify-publish-version.sh — Assert that VERSION at the current tree has a matching
# GitHub tag (refs/tags/v$VERSION) and a GitHub release. Publish must not ship a
# version that no release records.
#
# Usage: scripts/verify-publish-version.sh [version]
#   version  defaults to the content of the repo-root VERSION file
# Exit codes: 0 = linked; 1 = tag missing; 2 = release missing; 3 = VERSION malformed
set -euo pipefail

repo="${GITHUB_REPOSITORY:-Arasz/ai-sheepdog}"

if [ $# -ge 1 ]; then
  version="$1"
else
  version="$(tr -d '[:space:]' < VERSION)"
fi

if ! [[ "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
  echo "::error::VERSION must be a bare semver; found '$version'." >&2
  exit 3
fi

tag="v$version"
if ! git ls-remote --exit-code --tags origin "refs/tags/$tag" >/dev/null 2>&1; then
  echo "::error::Tag $tag does not exist. Tag the version (or push the VERSION change so release.yml runs) before publishing." >&2
  exit 1
fi

if ! gh release view "$tag" --repo "$repo" >/dev/null 2>&1; then
  echo "::error::Release $tag does not exist. Create it (release.yml normally does) before publishing." >&2
  exit 2
fi

echo "Publish gate OK: $tag exists with a release."
