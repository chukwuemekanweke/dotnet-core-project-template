#!/usr/bin/env python3
"""DEVOPS-012 parity check for the shared .NET CI workflow's artifacts.

Temporary: retire together with .github/workflows/ci-shared-validation.yml once
parity is proven and ci.yml's duplicated verification jobs are removed.

Asserts that the combined test-results artifact produced by the toolkit's
dotnet-ci.yml contains exactly one TRX per test project the legacy ci.yml would
discover, that every test executed (no skips, no failures, non-zero count), and
that the combined coverage artifact holds a parseable Cobertura report.
"""

from __future__ import annotations

import argparse
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

TRX_NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
CATEGORIES = {
    "unit": ("tests/unit", ".UnitTests.csproj"),
    "integration": ("tests/integration", ".IntegrationTests.csproj"),
}


def legacy_discovery(repo: Path, category: str) -> list[str]:
    """Mirror ci.yml: find tests/<category> -type f -iname '*.<Category>Tests.csproj'."""
    root, suffix = CATEGORIES[category]
    return sorted(
        p.stem
        for p in (repo / root).rglob("*")
        if p.is_file() and p.name.lower().endswith(suffix.lower())
    )


def check_trx(path: Path) -> list[str]:
    problems: list[str] = []
    try:
        counters = ET.parse(path).getroot().find("t:ResultSummary/t:Counters", TRX_NS)
    except ET.ParseError as error:
        return [f"{path.name}: unparseable TRX ({error})"]
    if counters is None:
        return [f"{path.name}: TRX has no ResultSummary/Counters"]

    def count(name: str) -> int:
        return int(counters.get(name, "0"))

    total, executed = count("total"), count("executed")
    if total == 0:
        problems.append(f"{path.name}: zero tests discovered")
    if executed != total:
        problems.append(f"{path.name}: executed {executed} of {total} tests")
    for name in ("failed", "error", "timeout", "aborted", "notExecuted", "inconclusive"):
        if count(name):
            problems.append(f"{path.name}: {name}={count(name)}")
    print(f"  {path.name}: total={total} executed={executed} passed={count('passed')}")
    return problems


def check_coverage(coverage_root: Path) -> list[str]:
    report = coverage_root / "Cobertura.xml"
    if not report.is_file() or report.stat().st_size == 0:
        return [f"combined coverage report missing or empty: {report}"]
    try:
        root = ET.parse(report).getroot()
    except ET.ParseError as error:
        return [f"combined Cobertura.xml unparseable ({error})"]
    if root.tag != "coverage" or root.get("line-rate") is None:
        return ["combined Cobertura.xml lacks a <coverage line-rate=...> root"]
    print(f"  Cobertura.xml: line-rate={root.get('line-rate')} branch-rate={root.get('branch-rate')}")
    return []


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, required=True)
    parser.add_argument("--test-results", type=Path, required=True)
    parser.add_argument("--coverage", type=Path, required=True)
    args = parser.parse_args()

    problems: list[str] = []
    trx_files = sorted(args.test_results.rglob("*.trx"))
    for category in CATEGORIES:
        expected = legacy_discovery(args.repo, category)
        suffix = CATEGORIES[category][1].removesuffix(".csproj")
        found = sorted(
            p.name.removesuffix("-results.trx")
            for p in trx_files
            if p.name.removesuffix("-results.trx").endswith(suffix)
        )
        print(f"{category}: legacy discovery={len(expected)} shared TRX={len(found)}")
        if not expected:
            problems.append(f"{category}: legacy discovery found no projects")
        if found != expected:
            missing = sorted(set(expected) - set(found))
            extra = sorted(set(found) - set(expected))
            problems.append(f"{category}: project set differs (missing={missing}, extra={extra})")
        for path in trx_files:
            if path.name.removesuffix("-results.trx").endswith(suffix):
                problems.extend(check_trx(path))

    print("coverage:")
    problems.extend(check_coverage(args.coverage))

    if problems:
        print("\nParity check FAILED:", file=sys.stderr)
        for problem in problems:
            print(f"  - {problem}", file=sys.stderr)
        return 1
    print("\nParity check passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
