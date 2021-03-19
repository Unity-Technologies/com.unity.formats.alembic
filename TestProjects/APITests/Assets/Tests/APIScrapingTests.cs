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
            "8af856e55aba3a842bb65372ce3b86ce", //Unity.Recorder.Editor.api
            "428b801a7dffc63478778d1e864cb2b1", // Unity.Recorder.api
            "ad280bc61d5f41f44a0b91cb3364ca2e" // Unity.Recorder.Base.api
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
            var shouldBe = "";
            if (files.Count > 0)
            {
                foreach (var file in files)
                {
                    filenames += $"{file}\n";
                    shouldBe += "<<<<<<<<<<<<<<<<\n" + File.ReadAllText(file) + "\n<<<<<<<<<<<<<<<<<<<<<<";
                }
            }


            Assert.IsTrue(allScraped,
                "Some .api files have not been generated. Please make sure to run ApiScraping.Scrape() or configure the com.unity.coding package to your project in order to regenerate api files. Here are the api files that need to be generated\n" +
                filenames + "ShouldBe:" + shouldBe);
        }
    }
}
