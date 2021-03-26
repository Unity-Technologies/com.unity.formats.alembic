using System.Collections.Generic;
using NUnit.Framework;
using Unity.Coding.Editor.Formatting;


namespace ValidationTests
{
    class FormattingTests
    {
        [Test]
        public void Formatting_FormatsNothing()
        {
            var files = new List<string>();

            Formatting.ValidateAllFilesFormatted("Packages", files);
            var filenames = "";
            if (files.Count > 0)
            {
                foreach (var file in files)
                {
                    filenames += $"{file}\n";
                }
            }

            Assert.AreEqual(0, files.Count,
                "Some files have not been formatted correctly, please make sure to run the formatter on the following files\n" +
                filenames);
        }
    }
}
