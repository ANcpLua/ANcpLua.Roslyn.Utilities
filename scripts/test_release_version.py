import unittest
from release_version import validate_version


class ReleaseVersionTests(unittest.TestCase):
    def test_explicit_major_after_old_line(self):
        self.assertEqual("3.0.0", validate_version("3.0.0", {"v2.2.48": "old", "v5.0.0-preview.1": "preview"}, "head"))

    def test_next_patch_requires_explicit_bump(self):
        with self.assertRaisesRegex(ValueError, "already belongs"):
            validate_version("3.0.0", {"v3.0.0": "published"}, "head")
        self.assertEqual("3.0.1", validate_version("3.0.1", {"v3.0.0": "published"}, "head"))

    def test_same_commit_rerun_is_allowed(self):
        self.assertEqual("3.0.0", validate_version("3.0.0", {"v3.0.0": "head"}, "head"))

    def test_stale_or_non_stable_versions_are_rejected(self):
        for candidate in ("2.2.49", "3.0.0-preview.1", "3.0", ""):
            with self.subTest(candidate=candidate), self.assertRaises(ValueError):
                validate_version(candidate, {"v3.0.0": "published"}, "head")
