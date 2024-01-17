using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace UnityEditor.Formats.Alembic
{
    class IconUtilityTests
    {
        [Test]
        [TestCase("")]
        [TestCase(null)]
        public void LoadIcon_InvalidPath_ThrowsException(string path)
        {
            Assert.Throws<NullReferenceException>(() =>
            {
                IconUtility.LoadIcon(null, IconUtility.IconType.UniqueToSkin);
            });
        }

        // Related bug: https://jira.unity3d.com/browse/SEQ-1207
        [Test]
        public void LoadIcon_Uncached_ReturnsIcon()
        {
            IconUtility.cachedIcons.Clear();
            var icon = IconUtility.LoadIcon("StrandBasedHair", IconUtility.IconType.UniqueToSkin);
            Assert.IsNotNull(icon);
        }

        [Test]
        public void IconName_ConstraintEnforcedInDarkAndLightFolders()
        {
            var regex = new Regex(IconUtility.s_IconNamePattern);
            string[] folders = {"Dark", "Light"};  // icons in the "Common" folder does not follow the same naming convention

            foreach (var folder in folders)
            {
                var basePath = Path.Combine(PackageUtility.editorResourcesFolder, "Icons", folder);
                var files = Directory.GetFiles(basePath, "*.png", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    // Standardize on forward slashes as directory separators.
                    var path = file.Replace(Path.DirectorySeparatorChar, '/');

                    var match = regex.Match(path);

                    var baseName = match.Groups["baseName"];
                    Assert.IsTrue(baseName.Success, $"Icon with invalid name found : {path}.");
                }
            }
        }

        [Test]
        [TestCase("StrandBasedHair",  true)]
        [TestCase("Strand Based Hair", true)]
        [TestCase("StrandBasedHair@2x", true)]
        [TestCase("Strand Based Hair@2x", true)]
        public void IconName_RegexMatchVerification(string iconName, bool expectedResult)
        {
            var regex = new Regex(IconUtility.s_IconNamePattern);
            var path = $"Packages/com.unity.formats.alembic/Editor/Editor Default Resources/Icons/Light/{iconName}.png";

            var match = regex.Match(path);
            var baseName = match.Groups["baseName"];

            Assert.AreEqual(baseName.Success, expectedResult, $"Icon name {path} did not properly match the Icon Name regex.");
        }

        [TestCase("StrandBasedHair", false)]
        [TestCase("StrandBasedHair@2x", true)]
        [TestCase("StrandBasedHair@16x", true)]
        [TestCase("StrandBasedHair@64x", true)]
        [TestCase("StrandBasedHair@128x", true)]
        [TestCase("StrandBasedHair@2", false)]
        [TestCase("StrandBased Hair@2", false)]
        public void IconName_ScaleSuffixRegex_Matched(string iconName, bool expectedResult)
        {
            var regex = new Regex(IconUtility.s_IconNamePattern);
            var path = $"Packages/com.unity.formats.alembic/Editor/Editor Default Resources/Icons/Light/{iconName}.png";

            var match = regex.Match(path);
            var scaleSuffix = match.Groups["scaleSuffix"];

            Assert.AreEqual(scaleSuffix.Success, expectedResult, "ScaleSuffix did not match the Scale Suffix pattern.");
        }

        [UnityTest]
        public IEnumerator PreloadIcons_AfterDomainReload()
        {
            IconUtility.cachedIcons.Clear();

            // Force a domain reload. This triggers functions with the InitializeOnLoadMethod attribute.
            EditorUtility.RequestScriptReload();
            yield return new WaitForDomainReload();

            Assert.IsTrue(IconUtility.cachedIcons.ContainsKey("StrandBasedHair"));
            Assert.IsNotNull(IconUtility.cachedIcons["StrandBasedHair"]);
        }
    }
}
