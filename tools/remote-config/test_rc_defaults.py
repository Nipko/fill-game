"""Pruebas de rc_defaults.py (unittest; python -m unittest discover -s tools/remote-config -p "test_*.py")."""

import json
import re
import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import rc_defaults  # noqa: E402


def _gdd_text():
    return rc_defaults.GDD_08.read_text(encoding="utf-8").replace("\r\n", "\n")


class RcDefaultsTests(unittest.TestCase):
    def test_self_test_passes(self):
        self.assertEqual(rc_defaults.self_test(), 0)

    def test_committed_files_are_the_generated_ones(self):
        j, cs = rc_defaults.generate(_gdd_text())
        self.assertEqual(rc_defaults._read(rc_defaults.OUT_JSON), j, "RemoteConfigDefaults.json: ejecuta rc_defaults.py")
        self.assertEqual(rc_defaults._read(rc_defaults.OUT_CS), cs, "RemoteConfigDefaults.Generated.cs: ejecuta rc_defaults.py")

    def test_every_param_has_a_known_type_and_a_value_or_a_pending_note(self):
        doc = json.loads(rc_defaults.generate(_gdd_text())[0])
        self.assertEqual(doc["format"], "af-rc-defaults/1")
        for key, entry in doc["params"].items():
            self.assertIn(entry["type"], ("int", "float", "bool", "string", "json"), key)
            if entry["default"] is None:
                self.assertTrue(entry.get("pending"), key + ": sin default y sin nota")
            else:
                self.assertNotIn("pending", entry, key)

    def test_retired_aliases_are_absent(self):
        # Cada tramo de «Alias retirados» es «alias, alias (cap.) → claves vigentes»; los alias de la izquierda no existen en
        # el JSON. Se saltan los que nombran una clave vigente con una forma antigua («`x` en forma JSON», «`x` con tramos»,
        # «`x` = 300»), que siguen existiendo con su forma nueva.
        text = _gdd_text()
        start = text.index("**Alias retirados**")
        line = text[start:text.index("\n", start)]
        params = json.loads(rc_defaults.generate(text)[0])["params"]
        checked = 0
        for segment in line.split(";"):
            if "→" not in segment:
                continue
            left = segment.split("→")[0]
            for m in re.finditer(r"`([a-z0-9_]+)`(?! en forma| con | = )", left):
                checked += 1
                self.assertNotIn(m.group(1), params, m.group(1) + " es un alias retirado de 08 §9")
        self.assertGreater(checked, 100)


if __name__ == "__main__":
    unittest.main()
