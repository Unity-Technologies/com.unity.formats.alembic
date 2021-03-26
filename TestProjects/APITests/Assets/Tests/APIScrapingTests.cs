using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Unity.Coding.Editor.ApiScraping;
using UnityEditor;

namespace ValidationTests
{
    class APIScrapingTests
    {
        static readonly string[] k_ApiFilesGUID =
        {
            "e3305ff7d49e0425aac9b9cb6fac4e88", //Unity.Formats.Alembic.Runtime.api
            "3b49e2e2daa394a349ba61947bd337f6" //Unity.Formats.Alembic.Editor.api
        };

        string[] m_ApiFileContents = new string[k_ApiFilesGUID.Length];

        [SetUp]
        public void Setup()
        {
            for (var i = 0; i < k_ApiFilesGUID.Length; i++)
            {
                var apiAssetPath = AssetDatabase.GUIDToAssetPath(k_ApiFilesGUID[i]);
                m_ApiFileContents[i] = File.ReadAllText(apiAssetPath);
            }
        }

        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < m_ApiFileContents.Length; i++)
            {
                var apiAssetPath = AssetDatabase.GUIDToAssetPath(k_ApiFilesGUID[i]);
                File.WriteAllText(apiAssetPath, m_ApiFileContents[i]);
            }
        }

        [Test]
        public void APIScraping_AllFilesUpToDate()
        {
            var files = new List<string>();
            var allScraped = ApiScraping.ValidateAllFilesScraped(files);
            var filenames = "";
            if (files.Count > 0)
            {
                foreach (var file in files)
                {
                    filenames += $"{file}\n";
                }
            }


            Assert.IsTrue(allScraped,
                "Some .api files have not been generated. Please make sure to run ApiScraping.Scrape() or configure the com.unity.coding package to your project in order to regenerate api files. Here are the api files that need to be generated\n" + filenames);
        }
    }
}
