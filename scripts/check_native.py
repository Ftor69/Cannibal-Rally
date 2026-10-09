"""Check source/config presence and report actual native build prerequisites."""
import argparse
import json
from pathlib import Path
import shutil
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]


def inspect(managed=None, base_mod_lib=None):
    required = ["Core/RallyState.cs", "Core/SidecarStore.cs", "Runtime/RallyMod.cs",
                "Runtime/BuggyFactory.cs", "Runtime/BuggyVehicle.cs", "Runtime/DriverSession.cs",
                "Tests/CoreTests.csproj", "Tests/Program.cs", "config.example.xml"]
    missing = [name for name in required if not (ROOT / "native" / name).is_file()]
    config = ET.parse(ROOT / "native/config.example.xml").getroot()
    if config.tag != "RallyConfig" or config.findtext("Profile") != "sandbox":
        raise ValueError("Invalid example config")
    ET.parse(ROOT / "native/Tests/CoreTests.csproj")
    dlls = ("UnityEngine.dll", "Assembly-CSharp.dll", "Assembly-CSharp-firstpass.dll", "bolt.dll")
    game_missing = list(dlls) if managed is None else [name for name in dlls if not (managed / name).is_file()]
    return {
        "source_layout": "pass" if not missing else "fail",
        "missing_sources": missing,
        "compiler_available": bool(shutil.which("dotnet") or shutil.which("msbuild") or shutil.which("mcs")),
        "missing_game_references": game_missing,
        "base_mod_lib_available": bool(base_mod_lib and base_mod_lib.is_file()),
        "native_build_verified": False,
        "in_game_verified": False,
        "melty_recipe_verified": False,
        "melty_release_ready": False,
    }


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--managed", type=Path, help="Your local TheForest_Data/Managed directory")
    parser.add_argument("--base-mod-lib", type=Path)
    parser.add_argument("--require-build", action="store_true")
    args = parser.parse_args()
    report = inspect(args.managed, args.base_mod_lib)
    print(json.dumps(report, indent=2))
    if report["missing_sources"] or (args.require_build and (
            not report["compiler_available"] or report["missing_game_references"] or not report["base_mod_lib_available"])):
        raise SystemExit(1)
