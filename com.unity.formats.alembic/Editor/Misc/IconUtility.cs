using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.Formats.Alembic
{
    internal static partial class IconUtility
    {
        /// <summary>
        /// Store previously loaded icons in this cache for re-usage.
        /// </summary>
        static readonly Dictionary<string, Texture2D> s_CachedIcons = new Dictionary<string, Texture2D>();
        internal static readonly string s_IconNamePattern = @"(?<baseName>\w*\w*)(?<scaleSuffix>@\d*x)?.png";

        /// <summary>
        /// Indicates if the icon is unique to a skin or common to all.
        /// </summary>
        public enum IconType
        {
            UniqueToSkin,
            CommonToAllSkin
        }

        [InitializeOnLoadMethod]
        [ExcludeFromCoverage]
        static void PreloadIconsOnStart()
        {
            EditorApplication.delayCall += PreloadIconsWithDelay;
        }

        static void PreloadIconsWithDelay()
        {
            foreach (string relativeFilePath in GetIconsFilePath())
            {
                // Load the icon and the selected version of it.
                LoadIcon(relativeFilePath, IconType.UniqueToSkin);
                LoadIcon(relativeFilePath + "-selected", IconType.CommonToAllSkin);
            }
        }

        /// <summary>
        /// Load Icon from Editor Default Resources.
        /// File must contain extension.
        /// </summary>
        /// <param name="path">Path of the icon texture to load, with a forward slash as the directory separator.</param>
        /// <param name="type">Skin type.</param>
        /// <returns>Icon texture, or null if no icon is found.</returns>
        public static Texture2D LoadIcon(string path, IconType type)
        {
            if (string.IsNullOrEmpty(path))
                throw new NullReferenceException("path");

            if (s_CachedIcons.TryGetValue(path, out var icon))
                return icon;

            string fullPath = BuildFullPath(path, type);
            icon = EditorGUIUtility.Load(fullPath) as Texture2D;

            if (icon == null)
                return null;

            s_CachedIcons.Add(path, icon);
            return icon;
        }

        static string BuildFullPath(string path, IconType type)
        {
            var scaleSuffix = EditorGUIUtility.pixelsPerPoint > 1f ? "@2x" : "";

            return Path.Combine(
                PackageUtility.editorResourcesFolder,
                "Icons",
                GetThemeFolder(type),
                $"{path}{scaleSuffix}.png");
        }

        static string BuildBasePath(IconType type)
        {
            return Path.Combine(
                PackageUtility.editorResourcesFolder,
                "Icons",
                GetThemeFolder(type));
        }

        static IEnumerable<string> GetIconsFilePath()
        {
            // Captures file path from "Editor Default Resources/Icons/<theme>", with optional scale suffix (e.g. @2x).
            var regex = new Regex(s_IconNamePattern);

            var basePath = BuildBasePath(IconType.UniqueToSkin);
            var files = Directory.GetFiles(basePath, "*.png", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                // Standardize on forward slashes as directory separators.
                var path = file.Replace(Path.DirectorySeparatorChar, '/');

                var match = regex.Match(path);

                // Skip all files ending with a size number. Example: "@2x.png"
                if (match.Groups["scaleSuffix"].Success)
                    continue;

                var baseName = match.Groups["baseName"];
                Debug.Assert(baseName.Success, $"Found icon file with unexpected path: {file}.");
                yield return baseName.Value;
            }
        }

        static string GetThemeFolder(IconType type)
        {
            return type switch
            {
                IconType.CommonToAllSkin => "Common",
                IconType.UniqueToSkin => EditorGUIUtility.isProSkin ? "Dark" : "Light",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
            };
        }
    }
}
