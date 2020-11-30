using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Formats.Alembic.Importer;

namespace UnityEditor.Formats.Alembic.Exporter.UnitTests
{
    class VisibilityTests
    {
        // All the GUIDs in the visibility folder
        [TestCase("1416eaf458d8341cfacec4fa81fd0527", TestName = "CameraStaticVisibility")]
        [TestCase("98d61f23b1a66466f93ad970d9117b00", TestName = "xTranslateVisibilityAnim")]
        [TestCase("38a3731774e2041268d2d2a9d27b69af", TestName = "meshStaticVisibility")]
        [TestCase("13c41684e05b74a789421d803ab4cb08", TestName = "meshVisibilityAnim")]
        [TestCase("369d852cb291c4c6aa11efc087bf3d2b", TestName = "pointCloudStaticVisibility")]
        [TestCase("620c76eda38c746fb8547eaa4ce417c3", TestName = "pointCloudVisibilityAnim")]
        [TestCase("b9cfb1cc6cd254c569e584c36db8f1b2", TestName = "xFormStaticVisibility")]
        [TestCase("8611f071574ba40368ca1da3092d3e22", TestName = "xFormVisibilityAnim")]
        public void VisibilityProperty(string guid)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            var go = PrefabUtility.InstantiatePrefab(asset) as GameObject;
            var player = go.GetComponent<AlembicStreamPlayer>();
            var abcNode = go.transform.GetChild(0).gameObject;
            player.UpdateImmediately(0);
            Assert.IsTrue(abcNode.activeSelf);

            player.UpdateImmediately(0.2f);
            Assert.IsFalse(abcNode.activeSelf);
        }
    }
}
