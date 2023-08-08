using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEditor.VersionControl;
using UnityEngine;

namespace UnityEditor.Formats.Alembic.Importer
{
    public struct SpeedTreeFile
    {
        public string File;
        public string Source;

        public static SpeedTreeFile ReadFromXML(XmlReader reader)
        {
            return new SpeedTreeFile
            {
                File = reader["File"], Source = reader["Source"]
            };
        }
    }

    public class SpeedTreeMaterial
    {
        public int ID;
        public string Name;
        public bool IsTwoSided;
        public float VertexOpacity;
        public bool FlipOpacity;
        public bool FlipNormalsOnBackside;

        public SpeedTreeFile Color;
        public SpeedTreeFile Opacity;
        public SpeedTreeFile Normal;
        public SpeedTreeFile Gloss;
        public Vector3 Specular;
        public SpeedTreeFile SubsurfaceColor;
        public SpeedTreeFile SubsurfaceAmount;
        public SpeedTreeFile AO;
        public float Height;

        // custom attributes
        public Vector3 Custom;
        public float Custom2;

        public static Vector3 ReadRGBFromXML(XmlReader reader)
        {
            return new Vector3(Convert.ToSingle(reader["ColorR"]), Convert.ToSingle(reader["ColorG"]), Convert.ToSingle(reader["ColorB"]));
        }
    }
    public class SpeedTreeXMLImporter
    {

        static List<SpeedTreeMaterial> ReadMaterialsFromXML(string sidecarFilePath)
        {
             int expectedCount = 0;
            bool flipOpacityForMesh = false;
            List<SpeedTreeMaterial> materials = new List<SpeedTreeMaterial>();

            using var reader = XmlReader.Create(sidecarFilePath);
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element when reader.Name == "Materials":
                        expectedCount = Convert.ToInt32(reader["Count"]);
                        flipOpacityForMesh = reader["FlipOpacity"] == "1";
                        break;

                    case XmlNodeType.Element when reader.Name == "Material":
                        var material = new SpeedTreeMaterial();

                        material.ID = Convert.ToInt32(reader["ID"]);
                        material.Name = reader["Name"];
                        material.IsTwoSided = reader["TwoSided"] == "1";
                        material.VertexOpacity = Convert.ToSingle(reader["VertexOpacity"]);
                        material.FlipNormalsOnBackside = reader["FlipNormalsOnBackside"] == "1";
                        material.FlipOpacity = flipOpacityForMesh;

                        reader.ReadToDescendant("Map");

                        do
                        {
                            switch (reader["Name"])
                            {
                                case "Color":
                                    material.Color = SpeedTreeFile.ReadFromXML(reader);
                                    break;
                                case "Opacity":
                                    material.Opacity = SpeedTreeFile.ReadFromXML(reader);
                                    break;
                                case "Normal":
                                    material.Normal = SpeedTreeFile.ReadFromXML(reader);
                                    break;
                                case "Gloss":
                                    material.Gloss= SpeedTreeFile.ReadFromXML(reader);
                                    break;
                                case "AO":
                                    material.AO = SpeedTreeFile.ReadFromXML(reader);
                                    break;
                                case "SubsurfaceColor":
                                    material.SubsurfaceColor = SpeedTreeFile.ReadFromXML(reader);
                                    break;
                                case "SubsurfaceAmount":
                                    material.SubsurfaceAmount = SpeedTreeFile.ReadFromXML(reader);
                                    break;
                                case "Specular":
                                    material.Specular = SpeedTreeMaterial.ReadRGBFromXML(reader);
                                    break;
                                case "Height":
                                    material.Height = Convert.ToSingle(reader["Value"]);
                                    break;
                                case "Custom":
                                    material.Custom = SpeedTreeMaterial.ReadRGBFromXML(reader);
                                    break;
                                case "Custom2":
                                    material.Custom2 = Convert.ToSingle(reader["Value"]);
                                    break;
                                default:
                                    Debug.Log("UNRECOGNIZED KEY:" + reader["Name"]);
                                    break;
                            };
                        } while (reader.ReadToNextSibling("Map"));

                        materials.Add(material);
                        break;
                }
            }

            if (materials.Count != expectedCount)
            {
                Debug.Log("INCORRECT NUMBER OF MATERIALS");
            }

            return materials;
        }

        public static Texture2D LoadPNG(SpeedTreeFile filePath) {

            Texture2D tex = null;
            byte[] fileData;

            string assetsFilePath = AssetDatabase.FindAssets("n: Pf{")

            if (File.Exists(filePath)) 	{
                fileData = File.ReadAllBytes(filePath);
                tex = new Texture2D(0, 0);
                tex.LoadImage(fileData);
            }
            return tex;
        }

        static void CreateMaterialsAssets(List<SpeedTreeMaterial> materials)
        {
            foreach (var matDesc in materials)
            {
                var newMaterial = new Material(Shader.Find("Standard"));

                newMaterial.EnableKeyword("_NORMALMAP");
                newMaterial.EnableKeyword("_METALLICGLOSSMAP");

                newMaterial.SetTexture("_MainTex", LoadPNG(matDesc.Color));
                //Set the Normal map using the Texture you assign in the Inspector
                newMaterial.SetTexture("_NormalMap", LoadPNG(matDesc.Normal));
                //Set the Metallic Texture as a Texture you assign in the Inspector
                newMaterial.SetTexture ("_MetallicGlossMap", LoadPNG(matDesc.Gloss));
                newMaterial.SetTexture("_OcclusionMap", LoadPNG(matDesc.AO));

                newMaterial.SetFloat("_Smoothness", matDesc.Specular.x);

                AssetDatabase.CreateAsset(newMaterial, $"Assets/{matDesc.Name}.mat");
                break;
            }
        }

        public static void LoadMaterialsFromSidecar(string sidecarFilePath)
        {
            var materials = ReadMaterialsFromXML(sidecarFilePath);
            CreateMaterialsAssets(materials);
        }
    }
}
