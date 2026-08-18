#!/usr/bin/env python3
"""Summarize BenchmarkDotNet JSON results into a shields.io endpoint badge payload.

Usage: benchmark_badge.py <results-directory> <output-json>

The badge reports the geometric mean of every benchmark's mean execution time and of the
allocated bytes per operation. A geometric mean is used so that fast and slow workloads
contribute equally to the single headline number shown on the badge.
"""

import glob
import json
import math
import os
import sys


def geometric_mean(values):
    positive = [v for v in values if v > 0]
    if not positive:
        return 0.0
    return math.exp(sum(math.log(v) for v in positive) / len(positive))


def format_time(nanoseconds):
    for limit, unit, scale in ((1e3, "ns", 1.0), (1e6, "us", 1e3), (1e9, "ms", 1e6)):
        if nanoseconds < limit:
            return f"{nanoseconds / scale:.2f} {unit}"
    return f"{nanoseconds / 1e9:.2f} s"


def format_bytes(count):
    for limit, unit, scale in ((1024, "B", 1.0), (1024 ** 2, "KB", 1024.0), (1024 ** 3, "MB", 1024.0 ** 2)):
        if count < limit:
            return f"{count / scale:.2f} {unit}"
    return f"{count / 1024 ** 3:.2f} GB"


def load_benchmarks(results_directory):
    benchmarks = []
    for path in sorted(glob.glob(os.path.join(results_directory, "*-report-full-compressed.json"))):
        with open(path, encoding="utf-8") as report:
            benchmarks.extend(json.load(report).get("Benchmarks", []))
    return benchmarks


def build_badge(benchmarks):
    means = [b["Statistics"]["Mean"] for b in benchmarks if b.get("Statistics")]
    allocated = [(b.get("Memory") or {}).get("BytesAllocatedPerOperation") or 0 for b in benchmarks]
    if not means:
        raise SystemExit("No benchmark statistics found in the given results directory.")

    message = f"{format_time(geometric_mean(means))} | {format_bytes(geometric_mean(allocated))}/op"
    return {
        "schemaVersion": 1,
        "label": f"benchmark geomean ({len(means)} cases)",
        "message": message,
        "color": "blue",
    }


def main(argv):
    if len(argv) != 3:
        raise SystemExit(f"Usage: {os.path.basename(argv[0])} <results-directory> <output-json>")

    badge = build_badge(load_benchmarks(argv[1]))
    output = argv[2]
    directory = os.path.dirname(output)
    if directory:
        os.makedirs(directory, exist_ok=True)
    with open(output, "w", encoding="utf-8") as handle:
        json.dump(badge, handle, indent=2)
        handle.write("\n")
    print(f"{badge['label']}: {badge['message']}")


if __name__ == "__main__":
    main(sys.argv)
