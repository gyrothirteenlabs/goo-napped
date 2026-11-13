using System;
using UnityEditor;
using UnityEngine;

namespace GooNapped.EditorTools
{
    // Nama game + ikon SEMENTARA untuk build (nama final diputuskan tim setelah game jadi, lihat CLAUDE.md).
    // Dipasang hanya selama BuildPlayer lalu dikembalikan, supaya ProjectSettings tidak berubah (keputusan tim).
    // Ganti nama/ikon di sini. Logo + ikon dibuat art/tools/gen_logo.py.
    public static class Branding
    {
        public const string ProductName = "Goo-napped";
        public const string CompanyName = "Goo-napped Team";
        public const string Version = "0.2.0-jam";
        public const string IconPath = "Assets/_Game/Art/UI/icon_goonapped.png";
        public const string LogoPath = "Assets/_Game/Art/UI/logo_goonapped.png";

        // Import tekstur UI tanpa blur (folder Art/UI tidak lewat PixelSpriteImporter).
        public static void EnsureImport()
        {
            foreach (var path in new[] { IconPath, LogoPath })
            {
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;
                bool dirty = ti.filterMode != FilterMode.Point || ti.textureCompression != TextureImporterCompression.Uncompressed || ti.mipmapEnabled;
                if (!dirty) continue;
                ti.textureType = TextureImporterType.Default;
                ti.filterMode = FilterMode.Point;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.npotScale = TextureImporterNPOTScale.None;
                ti.SaveAndReimport();
            }
        }

        public sealed class Scope : IDisposable
        {
            readonly string product, company, version;
            readonly Texture2D[] icons;

            public Scope()
            {
                EnsureImport();
                product = PlayerSettings.productName;
                company = PlayerSettings.companyName;
                version = PlayerSettings.bundleVersion;
                icons = PlayerSettings.GetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, IconKind.Any);
                PlayerSettings.productName = ProductName;
                PlayerSettings.companyName = CompanyName;
                PlayerSettings.bundleVersion = Version;
                var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
                if (icon != null) PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            }

            public void Dispose()
            {
                PlayerSettings.productName = product;
                PlayerSettings.companyName = company;
                PlayerSettings.bundleVersion = version;
                PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, icons, IconKind.Any);
                AssetDatabase.SaveAssets();   // build menyimpan ProjectSettings dengan nama sementara: tulis ulang nilai asli ke disk
            }
        }
    }
}
