//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using BepInEx;
using BepInEx.Configuration;
using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using SPT.Reflection.Patching;

namespace SevenBoldPencil.BrighterInteriors
{
    [BepInPlugin("7Bpencil.BrighterInteriors", "7Bpencil.BrighterInteriors", "1.1.0")]
    public class Plugin : BaseUnityPlugin
	{
		public static ConfigEntry<float> ShadowOpacity;

        private void Awake()
		{
			ShadowOpacity = Config.Bind<float>("Main", "Shadow Opacity", 0.7f, new ConfigDescription("0 is disabled, 1 is original", new AcceptableValueRange<float>(0f, 1f)));
			new Patch_AmbientLight_DrawStencilShadow().Enable();
        }
    }

    public struct TypedFieldInfo<I, F>
    {
        public FieldInfo Field;

        public TypedFieldInfo(string fieldName)
        {
            Field = AccessTools.Field(typeof(I), fieldName);
        }

        public void Set(I instance, F fieldValue)
        {
            Field.SetValue(instance, fieldValue);
        }

        public F Get(I instance)
        {
            return (F)Field.GetValue(instance);
        }
    }

    public struct Proxy_AmbientLight
    {
        private static TypedFieldInfo<AmbientLight, Material> __clearStencilMaterial = new("_clearStencilMaterial");
        private static TypedFieldInfo<AmbientLight, Material> __writeStencilMaterial = new("_writeStencilMaterial");
	    private static TypedFieldInfo<AmbientLight, Mesh> __quadMesh = new("_quadMesh");

        public Material _clearStencilMaterial { get { return __clearStencilMaterial.Get(__instance); } set { __clearStencilMaterial.Set(__instance, value); } }
        public Material _writeStencilMaterial { get { return __writeStencilMaterial.Get(__instance); } set { __writeStencilMaterial.Set(__instance, value); } }
        public Mesh _quadMesh { get { return __quadMesh.Get(__instance); } set { __quadMesh.Set(__instance, value); } }

		private AmbientLight __instance;

		public Proxy_AmbientLight(AmbientLight instance)
		{
			__instance = instance;
		}
    }

    public class Patch_AmbientLight_DrawStencilShadow : ModulePatch
    {
		public const string StencilShadowsUnitMarkerName = "StencilShadows_unit"; // same as AmbientLight._stencilShadowsMarkerName + "_unit"
        public static readonly int _StencilAmbientColor = Shader.PropertyToID("_StencilAmbientColor");
        public static readonly int _StencilFogAttenuation = Shader.PropertyToID("_StencilFogAttenuation");

        protected override MethodBase GetTargetMethod()
        {
			Type[] parameters = [typeof(CommandBuffer), typeof(StencilShadow), typeof(Vector3), typeof(bool)];
            return AccessTools.Method(typeof(AmbientLight), nameof(AmbientLight.DrawStencilShadow), parameters);
        }

        [PatchPrefix]
        public static bool Prefix(AmbientLight __instance, ref bool __result, CommandBuffer cmdBuf, StencilShadow ss, Vector3 camPos, bool disableColorPass = false)
        {
			var __instance__ = new Proxy_AmbientLight(__instance);
			var _clearStencilMaterial = __instance__._clearStencilMaterial;
			var _writeStencilMaterial = __instance__._writeStencilMaterial;
			var _quadMesh = __instance__._quadMesh;

			if (!ss.Culling.PassCulling((ss.Bounds.center - camPos).sqrMagnitude, out var num))
			{
				__result = false;
				return false;
			}
			cmdBuf.BeginSample(StencilShadowsUnitMarkerName);
			cmdBuf.DrawMesh(_quadMesh, Matrix4x4.identity, _clearStencilMaterial);
			cmdBuf.DrawRenderer(ss.Renderer, _writeStencilMaterial, 0, 0);
			cmdBuf.DrawRenderer(ss.Renderer, _writeStencilMaterial, 0, 1);
			if (!disableColorPass)
			{
				cmdBuf.SetGlobalColor(_StencilAmbientColor, ss.Ambient * num * Plugin.ShadowOpacity.Value);
				cmdBuf.SetGlobalFloat(_StencilFogAttenuation, ss.FogAttenuation);
				cmdBuf.DrawMesh(_quadMesh, Matrix4x4.identity, _writeStencilMaterial, 0, 2);
			}
			cmdBuf.DrawRenderer(ss.Renderer, _writeStencilMaterial, 0, 3);
			cmdBuf.DrawRenderer(ss.Renderer, _writeStencilMaterial, 0, 4);
			cmdBuf.EndSample(StencilShadowsUnitMarkerName);

			__result = true;
			return false;
        }
    }
}
