using BepInEx;
using BepInEx.Configuration;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using SPT.Reflection.Patching;

namespace SevenBoldPencil.Profiler
{
    [BepInPlugin("7Bpencil.ReduceFakeInteriorShadow", "7Bpencil.ReduceFakeInteriorShadow", "1.0.0")]
    public class Plugin : BaseUnityPlugin
	{
		public static ConfigEntry<float> ShadowOpacity;

        private void Awake()
		{
			ShadowOpacity = Config.Bind<float>("Main", "Shadow Opacity", 0.25f, new ConfigDescription("0 is disabled, 1 is original", new AcceptableValueRange<float>(0f, 1f)));
			new Patch_AmbientLight_method_8().Enable();
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

    public class Proxy_AmbientLight
    {
        private static TypedFieldInfo<AmbientLight, Material> __material_1 = new("material_1");
        private static TypedFieldInfo<AmbientLight, Material> __material_2 = new("material_2");
	    private static TypedFieldInfo<AmbientLight, string> __string_0 = new("string_0");
	    private static TypedFieldInfo<AmbientLight, Mesh> __mesh_0 = new("mesh_0");
	    private static TypedFieldInfo<AmbientLight, Matrix4x4> __matrix4x4_0 = new("matrix4x4_0");

        public AmbientLight __instance;

        public Material material_1 { get { return __material_1.Get(__instance); } set { __material_1.Set(__instance, value); } }
        public Material material_2 { get { return __material_2.Get(__instance); } set { __material_2.Set(__instance, value); } }
        public string string_0 { get { return __string_0.Get(__instance); } set { __string_0.Set(__instance, value); } }
        public Mesh mesh_0 { get { return __mesh_0.Get(__instance); } set { __mesh_0.Set(__instance, value); } }
        public Matrix4x4 matrix4x4_0 { get { return __matrix4x4_0.Get(__instance); } set { __matrix4x4_0.Set(__instance, value); } }
    }

    public class Patch_AmbientLight_method_8 : ModulePatch
    {
		private static Proxy_AmbientLight __instance__ = new();

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(AmbientLight), nameof(AmbientLight.method_8));
        }

        [PatchPrefix]
        public static bool Prefix(AmbientLight __instance, ref bool __result, CommandBuffer cmdBuf, StencilShadow ss, Vector3 camPos, bool disableColorPass = false)
        {
			__instance__.__instance = __instance;

			Bounds bounds = ss.Bounds;
			float num;
			if (!ss.Culling.PassCulling((bounds.center - camPos).sqrMagnitude, out num))
			{
				__result = false;
				__instance__.__instance = null;
				return false;
			}
			cmdBuf.BeginSample(__instance__.string_0 + "_unit");
			cmdBuf.DrawMesh(__instance__.mesh_0, __instance__.matrix4x4_0, __instance__.material_1);
			cmdBuf.DrawRenderer(ss.Renderer, __instance__.material_2, 0, 0);
			cmdBuf.DrawRenderer(ss.Renderer, __instance__.material_2, 0, 1);
			if (!disableColorPass)
			{
				cmdBuf.SetGlobalColor("_StencilAmbientColor", ss.Ambient * num * Plugin.ShadowOpacity.Value);
				cmdBuf.SetGlobalFloat("_StencilFogAttenuation", ss.FogAttenuation);
				cmdBuf.DrawMesh(__instance__.mesh_0, __instance__.matrix4x4_0, __instance__.material_2, 0, 2);
			}
			cmdBuf.DrawRenderer(ss.Renderer, __instance__.material_2, 0, 3);
			cmdBuf.DrawRenderer(ss.Renderer, __instance__.material_2, 0, 4);
			cmdBuf.EndSample(__instance__.string_0 + "_unit");

			__result = true;
			__instance__.__instance = null;
			return false;
        }
    }
}
