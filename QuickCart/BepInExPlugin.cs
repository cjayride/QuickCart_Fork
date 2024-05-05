﻿using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace QuickCart
{
    [BepInPlugin("cjayride.QuickCart", "Quick Cart", "0.4.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static ConfigEntry<string> hotKey;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<float> attachDistance;
        public static ConfigEntry<bool> allowOutOfPlaceAttach;
        private static BepInExPlugin context;

        private void Awake()
        {
            context = this;
            hotKey = Config.Bind<string>("General", "Hotkey", "v", "The hotkey used to attach/detach a nearby cart");
            attachDistance = Config.Bind<float>("General", "AttachDistance", 5, "Maximum distance to attach a cart from.");
            allowOutOfPlaceAttach = Config.Bind<bool>("General", "AllowOutOfPlaceAttach", true, "Allow attaching the cart even when out of place");
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        private void Update()
        {
            if (!modEnabled.Value || AedenthornUtils.IgnoreKeyPresses(true))
                return;
            if (AedenthornUtils.CheckKeyDown(hotKey.Value))
            {
                float closest = attachDistance.Value;
                Vagon closestVagon = null;
                Vector3 position = Player.m_localPlayer.transform.position + Vector3.up;
                foreach (Collider collider in Physics.OverlapSphere(position, attachDistance.Value))
                {
                    Vagon v = collider.gameObject.GetComponent<Vagon>();
                    if (!v)
                        v = collider.transform.parent?.gameObject.GetComponent<Vagon>();
                    if (collider.attachedRigidbody && v && Vector3.Distance(collider.ClosestPoint(position), position) < closest && (v.IsAttached(Player.m_localPlayer) || !v.InUse()))
                    {
                        closest = Vector3.Distance(collider.ClosestPoint(position), position);
                        closestVagon = collider.transform.parent.gameObject.GetComponent<Vagon>();
                    }
                }
                if (closestVagon != null)
                {
                    closestVagon.Interact(Player.m_localPlayer, false, false);
                }
            }
        }

        [HarmonyPatch(typeof(Vagon), "CanAttach")]
        static class Vagon_CanAttach_Patch
        {
            static bool Prefix(Vagon __instance, GameObject go, ref bool __result)
            {
                if (!modEnabled.Value || !allowOutOfPlaceAttach.Value || __instance.transform.up.y < 0.1f || go != Player.m_localPlayer.gameObject)
                    return true;
                __result = !Player.m_localPlayer.IsTeleporting() && !Player.m_localPlayer.InDodge() && Vector3.Distance(go.transform.position + __instance.m_attachOffset, __instance.m_attachPoint.position) < attachDistance.Value;
                return false;
            }
        }
    }
}