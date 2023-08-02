using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace UnityEditor.Formats.Alembic.Tests
{
    class EditorModeTests
    {
        [UnityTest]
        public IEnumerator EnteringPlaymode_DoesNotYieldErrors()
        {
            yield return new EnterPlayMode();
            Assert.IsTrue(true);
            yield return new ExitPlayMode();
        }
    }
}
