#!/usr/bin/env python3
"""Application-side checks for the devops-toolkit image publishing contract (DEVOPS-012).

  config    Validate .github/docker-images.json: schema version 1, exactly the four
            deployable components, existing Dockerfiles, Docker Hub repositories,
            no build args or extra keys (credentials never belong in this file).
  manifest  Validate a published release manifest against that configuration:
            same four components and repositories, expected deployment id and
            source commit, sha256 digests. Prints image@digest references, one per
            line, for registry resolution by the caller.
  deploy    Validate the deployment configuration against the image configuration:
            deploy/vps-deploy-stack.production.json, the rendered production Compose
            (docker compose config --format json) and the deploy-production job's
            inputs (extracted to JSON) must agree on stack, path, environment,
            image repositories, component-to-service mapping and secret-bearing
            services.

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
EXPECTED_SERVICES = {"webapi": "webapi", "consumer": "consumer", "jobs": "jobs", "migrator": "database-migrator"}
MIGRATION_SERVICE = "database-migrator"


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


def verify_deploy(images: list[dict], stack_path: Path, compose_path: Path, deploy_path: Path) -> list[str]:
    problems: list[str] = []
    stack = json.loads(stack_path.read_text(encoding="utf-8"))
    compose = json.loads(compose_path.read_text(encoding="utf-8"))
    deploy = json.loads(deploy_path.read_text(encoding="utf-8"))
    services = compose.get("services", {})

    configured = sorted(image["image"] for image in images)
    if sorted(stack.get("allowedImageRepositories", [])) != configured:
        problems.append("stack allowedImageRepositories must list exactly the configured image repositories")
    for key, expected in (("stack-name", stack.get("stackName")), ("environment", stack.get("environment")),
                          ("remote-deployment-path", stack.get("deploymentPath"))):
        if deploy.get(key) != expected:
            problems.append(f"deploy-production {key} {deploy.get(key)!r} != stack registration {expected!r}")
    if deploy.get("compose-files", "").strip() != "deploy/compose.production.yml":
        problems.append("deploy-production compose-files must be deploy/compose.production.yml")

    mapping = json.loads(deploy.get("component-service-map") or "{}")
    resolved = {component: mapping.get(component, component) for component in EXPECTED_COMPONENTS}
    if resolved != EXPECTED_SERVICES:
        problems.append(f"component-service-map resolves to {resolved}, expected {EXPECTED_SERVICES}")
    for service in EXPECTED_SERVICES.values():
        if service not in services:
            problems.append(f"production Compose has no '{service}' service")
    extra = sorted(set(services) - set(EXPECTED_SERVICES.values()))
    if extra:
        problems.append(f"production Compose defines non-release services {extra}; keep infrastructure external or register it deliberately")

    if deploy.get("migration-service") != MIGRATION_SERVICE:
        problems.append(f"migration-service must be {MIGRATION_SERVICE}")
    if services.get(MIGRATION_SERVICE, {}).get("profiles") != ["migration"]:
        problems.append(f"{MIGRATION_SERVICE} must be only in the 'migration' profile")
    secret_services = sorted(stack.get("runtimeSecrets", {}).get("services", {}))
    if secret_services != sorted(EXPECTED_SERVICES.values()):
        problems.append(f"stack runtimeSecrets services {secret_services} must be exactly {sorted(EXPECTED_SERVICES.values())}")
    if stack.get("runtimeSecrets", {}).get("services", {}).get(MIGRATION_SERVICE, {}).get("startAtBoot") is not False:
        problems.append(f"{MIGRATION_SERVICE} runtime secrets must not start at boot")
    health = [line.strip() for line in deploy.get("health-services", "").splitlines() if line.strip()]
    if sorted(health) != sorted(service for service in EXPECTED_SERVICES.values() if service != MIGRATION_SERVICE):
        problems.append(f"health-services {health} must be the three long-running services")
    for service in health:
        if "healthcheck" not in services.get(service, {}):
            problems.append(f"health service '{service}' has no Compose healthcheck")
    return problems


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("mode", choices=["config", "manifest", "deploy"])
    parser.add_argument("--repo", type=Path, default=Path("."))
    parser.add_argument("--config", default=".github/docker-images.json")
    parser.add_argument("--manifest", type=Path)
    parser.add_argument("--deployment-id")
    parser.add_argument("--source-commit")
    parser.add_argument("--stack", type=Path, default=Path("deploy/vps-deploy-stack.production.json"))
    parser.add_argument("--compose-json", type=Path, help="docker compose config --profile '*' --format json output")
    parser.add_argument("--deploy-json", type=Path, help="jobs.deploy-production.with as JSON")
    args = parser.parse_args()

    images, problems = load_config(args.repo, args.config)
    references: list[str] = []
    if args.mode == "manifest":
        if not (args.manifest and args.deployment_id and args.source_commit):
            parser.error("manifest mode requires --manifest, --deployment-id and --source-commit")
        references, manifest_problems = verify_manifest(images, args.manifest, args.deployment_id, args.source_commit)
        problems.extend(manifest_problems)
    if args.mode == "deploy":
        if not (args.compose_json and args.deploy_json):
            parser.error("deploy mode requires --compose-json and --deploy-json")
        problems.extend(verify_deploy(images, args.repo / args.stack, args.compose_json, args.deploy_json))

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
