"""Validate the explicit release version before native CI builds or publishes."""
from pathlib import Path
import argparse
import re
import subprocess
import xml.etree.ElementTree as ET


def validate_version(candidate: str, tags: dict[str, str], head: str) -> str:
    if not re.fullmatch(r"\d+\.\d+\.\d+", candidate):
        raise ValueError("ReleaseVersion must be an explicit stable x.y.z version")
    version = tuple(map(int, candidate.split(".")))
    stable = {tag: tuple(map(int, tag[1:].split("."))) for tag in tags
              if re.fullmatch(r"v\d+\.\d+\.\d+", tag)}
    if stable and version < max(stable.values()):
        raise ValueError("ReleaseVersion is older than an existing stable release; bump Version.props")
    target = "v" + candidate
    if target in tags and tags[target] != head:
        raise ValueError(f"{target} already belongs to another commit; bump Version.props")
    return candidate


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--read-for-build", action="store_true", help="Read the source version without authorizing publication")
    args = parser.parse_args()
    candidate = ET.parse(Path(__file__).resolve().parents[1] / "Version.props").findtext(".//ReleaseVersion", "")
    if args.read_for_build:
        print(validate_version(candidate, {}, ""))
        return
    names = subprocess.check_output(["git", "tag", "--list", "v*"], text=True).splitlines()
    tags = {name: subprocess.check_output(["git", "rev-list", "-n", "1", name], text=True).strip()
            for name in names if re.fullmatch(r"v\d+\.\d+\.\d+", name)}
    head = subprocess.check_output(["git", "rev-parse", "HEAD"], text=True).strip()
    print(validate_version(candidate, tags, head))


if __name__ == "__main__":
    main()
