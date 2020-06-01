﻿using System.Collections.Generic;
using System.Linq;
using Assets.Urho3DExporter.Scripts.Editor;
using UnityEditor;
using UnityEngine;

namespace Urho3DExporter
{
    public class TextureMetadataCollection
    {
        public Dictionary<Texture, TextureMetadata> _textures = new Dictionary<Texture, TextureMetadata>();
        public TextureMetadataCollection(DestinationFolder urhoDataFolder)
        {
            var allMaterials = AssetDatabase.FindAssets("").Select(_=>AssetContext.Create(_, urhoDataFolder)).Where(_=>_.Type == typeof(Material));
            foreach (var asset in allMaterials)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(asset.AssetPath);
                var description = new MaterialDescription(material);
                if (description.MetallicRoughness != null)
                {
                    var metallicRoughness = description.MetallicRoughness;
                    if (metallicRoughness.MetallicGloss != null)
                    {
                        var meta = AddTexture(metallicRoughness.MetallicGloss, new TextureReferences(TextureSemantic.MetallicGlossiness, 1.0f,
                            (metallicRoughness.SmoothnessTextureChannel == SmoothnessTextureChannel.MetallicOrSpecularAlpha)
                                ? metallicRoughness.MetallicGloss
                                : metallicRoughness.Albedo, metallicRoughness.SmoothnessTextureChannel));
                    }
                    AddTexture(metallicRoughness.Albedo, new TextureReferences(TextureSemantic.MainTexture));
                    AddTexture(metallicRoughness.Albedo, new TextureReferences(TextureSemantic.MainTextureDetail));
                }
                else if (description.SpecularGlossiness != null)
                {
                    var specularGlossiness = description.SpecularGlossiness;
                    if (specularGlossiness.PBRSpecular != null)
                    {
                        var meta = AddTexture(specularGlossiness.PBRSpecular, new TextureReferences(TextureSemantic.SpecularGlossiness, 1.0f,
                            (specularGlossiness.SmoothnessTextureChannel == SmoothnessTextureChannel.MetallicOrSpecularAlpha)
                                ? specularGlossiness.PBRSpecular
                                : specularGlossiness.Albedo, specularGlossiness.SmoothnessTextureChannel));
                    }
                    AddTexture(specularGlossiness.Albedo, new TextureReferences(TextureSemantic.MainTexture));
                    AddTexture(specularGlossiness.Albedo, new TextureReferences(TextureSemantic.MainTextureDetail));
                }
                else
                {
                    var legacy = description.Legacy;
                    AddTexture(legacy.Diffuse, new TextureReferences(TextureSemantic.MainTexture));
                    AddTexture(legacy.Specular, new TextureReferences(TextureSemantic.Specular));
                    AddCommonTextures(legacy);
                }
            }
        }

        private void AddCommonTextures(ShaderArguments legacy)
        {
            AddTexture(legacy.Occlusion, new TextureReferences(TextureSemantic.Occlusion));
            AddTexture(legacy.Bump, new TextureReferences(TextureSemantic.Bump, legacy.BumpScale));
            AddTexture(legacy.Detail, new TextureReferences(TextureSemantic.Detail));
            AddTexture(legacy.DetailNormal, new TextureReferences(TextureSemantic.DetailNormal));
            AddTexture(legacy.Emission, new TextureReferences(TextureSemantic.Emission));
            AddTexture(legacy.Parallax, new TextureReferences(TextureSemantic.Parallax));
        }

        private TextureMetadata EnsureTexture(Texture tex)
        {
            if (tex == null)
                return null;
            if (_textures.TryGetValue(tex, out var meta))
                return meta;
            meta = new TextureMetadata(){Texture = tex};
            _textures.Add(tex, meta);
            return meta;
        }

        private bool AddTexture(Texture tex, TextureReferences reference)
        {
            var meta = EnsureTexture(tex);
            if (meta == null)
                return false;
            return meta.References.Add(reference);
        }

        public IEnumerable<TextureReferences> ResolveReferences(Texture texture)
        {
            if (_textures.TryGetValue(texture, out var references))
            {
                foreach (var reference in references.References)
                {
                    yield return reference;
                }
            }
            else
            {
                yield return new TextureReferences(TextureSemantic.Other);
            }
        }
    }
}