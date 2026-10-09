import importlib.util
from pathlib import Path
import tempfile
import unittest
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]


def module(name):
    spec = importlib.util.spec_from_file_location(name, ROOT / "scripts" / f"{name}.py")
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result


class PreparationTests(unittest.TestCase):
    def test_missing_game_is_not_reported_as_release_ready(self):
        report = module("check_native").inspect()
        self.assertEqual(report["source_layout"], "pass")
        self.assertEqual(len(report["missing_game_references"]), 4)
        self.assertFalse(report["native_build_verified"])
        self.assertFalse(report["melty_release_ready"])

    def test_generated_project_preserves_real_framework_and_is_repeatable(self):
        prepare = module("prepare_modapi")
        ns = prepare.NS
        original = f'''<Project xmlns="{ns}"><PropertyGroup>
<AssemblyName>CannibalRallyExperimental</AssemblyName>
<TargetFrameworkVersion>v3.5</TargetFrameworkVersion></PropertyGroup><ItemGroup>
<Reference Include="BaseModLib"><HintPath>actual-loader.dll</HintPath></Reference>
<Reference Include="UnityEngine"/><Reference Include="Assembly-CSharp"/>
<Reference Include="bolt"/></ItemGroup></Project>'''
        with tempfile.TemporaryDirectory() as folder:
            project = Path(folder) / "CannibalRallyExperimental.csproj"
            project.write_text(original)
            prepare.prepare(project)
            prepare.prepare(project)
            root = ET.parse(project).getroot()
            self.assertEqual(root.findtext(f".//{{{ns}}}TargetFrameworkVersion"), "v3.5")
            self.assertEqual(root.findtext(f".//{{{ns}}}HintPath"), "actual-loader.dll")
            entries = root.findall(f".//{{{ns}}}Compile")
            self.assertEqual(len(entries), 6)
            self.assertEqual(len({e.get("Include") for e in entries}), 6)
            for entry in entries:
                self.assertTrue((Path(folder) / entry.get("Include")).is_file())
            self.assertEqual(project.with_suffix(".csproj.pre-rally.bak").read_text(), original)
            self.assertTrue(all(r.findtext(f"{{{ns}}}Private") == "False"
                                for r in root.findall(f".//{{{ns}}}Reference")))

    def test_refuses_to_modify_an_unrelated_project(self):
        prepare = module("prepare_modapi")
        with tempfile.TemporaryDirectory() as folder:
            project = Path(folder) / "other.csproj"
            original = f'<Project xmlns="{prepare.NS}"><PropertyGroup><AssemblyName>Other</AssemblyName></PropertyGroup></Project>'
            project.write_text(original)
            with self.assertRaises(ValueError):
                prepare.prepare(project)
            self.assertEqual(project.read_text(), original)


if __name__ == "__main__":
    unittest.main()
