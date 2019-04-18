using NUnit.Framework;
using UnityEngine.Formats.Alembic.Sdk;

namespace UnityEditor.Formats.Alembic.Exporter.UnitTests
{
    class EditorTests
    {
        [Test]
        public void MarshalTests()
        {
            Assert.AreEqual(72,System.Runtime.InteropServices.Marshal.SizeOf(typeof(aePolyMeshData)));
        }
    }
}