#!/usr/bin/env python3
"""Application-side checks for the devops-toolkit image publishing contract (DEVOPS-012).

  config    Validate .github/docker-images.json: schema version 1, exactly the four
            deployable components, existing Dockerfiles, Docker Hub repositories,
            no build args or extra keys (credentials never belong in this file).
  manifest  Validate a published release manifest against that configuration:
            same four components and repositories, expected deployment id and
            source commit, sha256 digests. Prints image@digest references, one per
            line, for registry resolution by the caller.

The toolkit's own validation (schema, completeness, write-once tags) still runs at
publish time; this catches application misconfiguration on pull requests and
confirms the published release matches what this repository declared.
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

EXPECTED_COMPONENTS = ["webapi", "consumer", "jobs", "migrator"]
IMAGE_PATTERN = re.compile(r"^docker\.io/[a-z0-9]+(?:[._-][a-z0-9]+)*/[a-z0-9]+(?:[._-][a-z0-9]+)*$")
DIGEST_PATTERN = re.compile(r"^sha256:[0-9a-f]{64}$")
ALLOWED_KEYS = {"component", "image", "dockerfile"}


def load_config(repo: Path, config_path: str) -> tuple[list[dict], list[str]]:
    problems: list[str] = []
    config = json.loads((repo / config_path).read_text(encoding="utf-8"))
    if set(config) != {"schemaVersion", "images"} or config.get("schemaVersion") != 1:
        problems.append("config must contain only schemaVersion (1) and images")
    images = config.get("images", [])
    components = [image.get("component") for image in images]
    if components != EXPECTED_COMPONENTS:
        problems.append(f"components must be exactly {EXPECTED_COMPONENTS} in order, got {components}")
    for image in images:
        name = image.get("component")
        extra = set(image) - ALLOWED_KEYS
        if extra:
            problems.append(f"{name}: unexpected keys {sorted(extra)} (no build args, contexts or credentials)")
        if not IMAGE_PATTERN.match(image.get("image", "")) or "/example/" in image.get("image", ""):
            problems.append(f"{name}: image must be a real docker.io/<namespace>/<repository>")
        dockerfile = repo / image.get("dockerfile", "")
        if not dockerfile.is_file():
            problems.append(f"{name}: dockerfile not found: {image.get('dockerfile')}")
        elif "id=GITHUB_USERNAME" not in (text := dockerfile.read_text(encoding="utf-8")) or "id=GITHUB_PAT" not in text:
            problems.append(f"{name}: dockerfile no longer mounts GITHUB_USERNAME/GITHUB_PAT; revisit package-auth-components")
    return images, problems


def verify_manifest(images: list[dict], manifest_path: Path, deployment_id: str, source_commit: str) -> tuple[list[str], list[str]]:
    problems: list[str] = []
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    if manifest.get("deployment_id") != deployment_id:
        problems.append(f"deployment_id {manifest.get('deployment_id')!r} != {deployment_id!r}")
    if manifest.get("source_commit") != source_commit:
        problems.append(f"source_commit {manifest.get('source_commit')!r} != {source_commit!r}")
    expected = {image["component"]: image["image"] for image in images}
    published = {component.get("name"): component for component in manifest.get("components", [])}
    if sorted(published) != sorted(expected):
        problems.append(f"manifest components {sorted(published)} != configured {sorted(expected)}")
    references: list[str] = []
    for name, image in expected.items():
        component = published.get(name)
        if component is None:
            continue
        if component.get("image") != image:
            problems.append(f"{name}: manifest image {component.get('image')!r} != configured {image!r}")
        if not DIGEST_PATTERN.match(component.get("digest", "")):
            problems.append(f"{name}: digest is not an immutable sha256 digest")
        references.append(f"{component.get('image')}@{component.get('digest')}")
    return references, problems


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("mode", choices=["config", "manifest"])
    parser.add_argument("--repo", type=Path, default=Path("."))
    parser.add_argument("--config", default=".github/docker-images.json")
    parser.add_argument("--manifest", type=Path)
    parser.add_argument("--deployment-id")
    parser.add_argument("--source-commit")
    args = parser.parse_args()

    images, problems = load_config(args.repo, args.config)
    references: list[str] = []
    if args.mode == "manifest":
        if not (args.manifest and args.deployment_id and args.source_commit):
            parser.error("manifest mode requires --manifest, --deployment-id and --source-commit")
        references, manifest_problems = verify_manifest(images, args.manifest, args.deployment_id, args.source_commit)
        problems.extend(manifest_problems)

    if problems:
        print("Release image check FAILED:", file=sys.stderr)
        for problem in problems:
            print(f"  - {problem}", file=sys.stderr)
        return 1
    for reference in references:
        print(reference)
    print(f"{args.mode} check passed for {len(images)} components.", file=sys.stderr)
    return 0


if __name__ == "__main__":
    sys.exit(main())
