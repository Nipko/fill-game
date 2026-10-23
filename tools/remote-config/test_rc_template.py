"""Pruebas de rc_template.py (unittest; python -m unittest discover -s tools/remote-config -p "test_*.py")."""

import json
import re
import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import rc_defaults  # noqa: E402
import rc_template  # noqa: E402


def _gdd_text():
    return rc_defaults.GDD_08.read_text(encoding="utf-8").replace("\r\n", "\n")


def _defaults_json():
    with open(rc_defaults.OUT_JSON, "r", encoding="utf-8") as f:
        return json.load(f)["params"]


def _template(env="dev"):
    texts, _, _ = rc_template.generate(_gdd_text())
    return json.loads(texts[env])


def _back(type_, text):
    """El texto de Remote Config leído como lo lee el cliente (RcValue.TryFromRemote)."""
    if type_ == "int":
        return int(text)
    if type_ == "float":
        return float(text)
    if type_ == "bool":
        return {"true": True, "false": False}[text]
    if type_ == "string":
        return text
    return json.loads(text)


class RcTemplateTests(unittest.TestCase):
    def test_self_test_passes(self):
        self.assertEqual(rc_template.self_test(), 0)

    def test_committed_templates_are_the_generated_ones(self):
        texts, _, _ = rc_template.generate(_gdd_text())
        for env in rc_template.ENVIRONMENTS:
            path = rc_template.template_path(rc_template.OUT_DIR, env)
            self.assertEqual(rc_defaults._read(path), texts[env], path.name + ": ejecuta rc_template.py")

    def test_dev_and_prod_are_equal_because_08_9_has_no_per_environment_values(self):
        texts, _, _ = rc_template.generate(_gdd_text())
        self.assertEqual(texts["dev"], texts["prod"])

    def test_value_types_follow_the_compiled_types(self):
        defaults = _defaults_json()
        for key, p in _template()["parameters"].items():
            self.assertEqual(p["valueType"], rc_template.VALUE_TYPES[defaults[key]["type"]], key)
            self.assertIn(p["valueType"], ("STRING", "NUMBER", "BOOLEAN", "JSON"), key)

    def test_values_are_exactly_the_compiled_defaults(self):
        # Que publicar no cambie el comportamiento: cada valor, leído como lo lee el cliente, es el default de
        # RemoteConfigDefaults.json, con su mismo tipo (un int no puede llegar como 1.0 ni un bool como "True").
        defaults = _defaults_json()
        params = _template()["parameters"]
        for key, p in params.items():
            d = defaults[key]
            text = p["defaultValue"]["value"]
            back = _back(d["type"], text)
            self.assertEqual(back, d["default"], key)
            self.assertEqual(type(back), type(d["default"]) if d["type"] != "float" else float, key)
            if d["type"] == "int":
                self.assertRegex(text, r"^-?\d+$", key)
            if d["type"] == "json":
                self.assertEqual(text, json.dumps(d["default"], ensure_ascii=False, separators=(",", ":")), key)

    def test_every_publishable_default_is_in_the_template(self):
        # Fuera solo quedan las claves sin valor y las que Firebase no admite; ninguna se pierde sin explicación.
        defaults = _defaults_json()
        params = _template()["parameters"]
        _, pending, invalid = rc_template.generate(_gdd_text())
        for key, d in defaults.items():
            if d["default"] is None:
                self.assertIn(key, pending)
                self.assertNotIn(key, params)
            elif not rc_template.valid_key(key):
                self.assertIn(key, invalid)
                self.assertNotIn(key, params)
            else:
                self.assertIn(key, params)
        self.assertEqual(len(params) + len(pending) + len(invalid), len(defaults))

    def test_no_key_outside_08_9(self):
        keys_08 = [k for k, _, _, _ in rc_defaults.read_params(_gdd_text())]
        params = list(_template()["parameters"])
        self.assertTrue(set(params) <= set(keys_08))
        self.assertTrue(set(params) <= set(_defaults_json()))
        self.assertEqual(params, [k for k in keys_08 if k in params], "el orden es el de 08 §9")
        self.assertFalse(any("<" in k or "{" in k for k in params))

    def test_remote_config_limits(self):
        params = _template()["parameters"]
        self.assertLessEqual(len(params), rc_template.MAX_PARAMS)
        self.assertLessEqual(sum(len(p["defaultValue"]["value"]) for p in params.values()), rc_template.MAX_VALUE_CHARS)
        for key, p in params.items():
            self.assertLessEqual(len(key), 256, key)
            self.assertRegex(key, r"^[A-Za-z_][A-Za-z0-9_]*$", key)
            self.assertLessEqual(len(p["description"]), 256, key)
            self.assertTrue(p["description"].startswith("Rango 08 §9: "), key)

    def test_template_is_valid_for_the_rest_api(self):
        for env in rc_template.ENVIRONMENTS:
            texts, _, _ = rc_template.generate(_gdd_text())
            doc = json.loads(texts[env])
            self.assertEqual(set(doc), {"conditions", "parameters", "parameterGroups"})
            self.assertEqual(doc["conditions"], [])
            self.assertEqual(doc["parameterGroups"], {})
            self.assertEqual(rc_template.problems(doc), [])
            for key, p in doc["parameters"].items():
                self.assertEqual(set(p), {"defaultValue", "description", "valueType"}, key)
                self.assertEqual(set(p["defaultValue"]), {"value"}, key)
                self.assertIsInstance(p["defaultValue"]["value"], str, key)
                v, t = p["defaultValue"]["value"], p["valueType"]
                if t == "NUMBER":
                    float(v)
                elif t == "BOOLEAN":
                    self.assertIn(v, ("true", "false"), key)
                elif t == "JSON":
                    json.loads(v)

    def test_problems_catches_what_the_api_rejects(self):
        doc = _template()
        doc["parameters"]["mech_enabled.hoja"] = {"defaultValue": {"value": "true"}, "description": "", "valueType": "BOOLEAN"}
        doc["parameters"]["rc_schema"]["defaultValue"]["value"] = "1,5"
        doc["parameters"]["level_overrides"]["defaultValue"]["value"] = "{sin comillas: 1}"
        doc["version"] = {"versionNumber": "1"}
        found = rc_template.problems(doc)
        self.assertEqual(len(found), 4, found)
        self.assertTrue(any(re.search("mech_enabled.hoja", f) for f in found))

    def test_dotted_keys_are_the_only_invalid_ones(self):
        _, _, invalid = rc_template.generate(_gdd_text())
        self.assertTrue(invalid)
        for key in invalid:
            self.assertRegex(key, r"^(mech_enabled|mech_debut_level)\.[a-z]+$")


if __name__ == "__main__":
    unittest.main()
