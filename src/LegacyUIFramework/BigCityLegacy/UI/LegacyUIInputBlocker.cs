using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace BigCityLegacy.UI
{
    /// <summary>
    /// Runtime bridge between IMGUI windows and the game's regular UI input system.
    ///
    /// Event.Use() only affects IMGUI. MadOut/BigCity UI mostly polls Input and checks
    /// RectTransform intersections through nHelp.isMouseIntersect/MenuPress, so visible
    /// IMGUI windows also need a small game-side blocker.
    /// </summary>
    internal sealed class LegacyUIInputBlocker : MonoBehaviour
    {
        private struct BlockRequest
        {
            public Rect Rect;
            public LegacyUIInputBlockMode Mode;
            public int Frame;
        }

        private static readonly Dictionary<int, BlockRequest> Requests = new Dictionary<int, BlockRequest>();
        private static readonly List<int> RemoveBuffer = new List<int>();
        private static LegacyUIInputBlocker instance;
        private static bool harmonyPatchAttempted;
        private static bool harmonyPatchInstalled;
        private static int nextOwnerId;

        private const int StaleFrameCount = 4;
        private const int CanvasSortingOrder = 32760;

        private Canvas canvas;
        private GraphicRaycaster raycaster;
        private readonly List<Image> blockers = new List<Image>();
        internal static int AllocateOwnerId()
        {
            if (nextOwnerId == int.MaxValue)
            {
                nextOwnerId = 0;
            }
            nextOwnerId++;
            return nextOwnerId;
        }

        internal static void ReportWindow(int ownerId, Rect rect, LegacyUIInputBlockMode mode)
        {
            if (ownerId == 0)
            {
                return;
            }

            EnsureInstance();
            EnsureGameInputPatches();

            if (mode == LegacyUIInputBlockMode.None || rect.width <= 0f || rect.height <= 0f)
            {
                ClearWindow(ownerId);
                return;
            }

            Requests[ownerId] = new BlockRequest
            {
                Rect = rect,
                Mode = mode,
                Frame = Time.frameCount
            };

            if (instance)
            {
                instance.RefreshVisualBlockers();
            }
        }

        internal static void ClearWindow(int ownerId)
        {
            if (ownerId == 0)
            {
                return;
            }

            if (Requests.Remove(ownerId) && instance)
            {
                instance.RefreshVisualBlockers();
            }
        }

        internal static bool IsMouseBlockedForGameInput()
        {
            Vector3 mousePosition = LegacyInput.MousePosition;
            return IsScreenPointBlockedForGameInput(new Vector2(mousePosition.x, mousePosition.y));
        }

        internal static bool IsScreenPointBlockedForGameInput(Vector2 screenPointBottomLeft)
        {
            CleanupStaleRequests();

            if (Requests.Count == 0)
            {
                return false;
            }

            Vector2 guiPoint = new Vector2(screenPointBottomLeft.x, Screen.height - screenPointBottomLeft.y);

            foreach (BlockRequest request in Requests.Values)
            {
                if (request.Mode == LegacyUIInputBlockMode.Screen)
                {
                    return true;
                }

                if (request.Mode == LegacyUIInputBlockMode.Window && request.Rect.Contains(guiPoint))
                {
                    return true;
                }
            }

            return false;
        }

        private static void EnsureInstance()
        {
            if (instance)
            {
                return;
            }

            GameObject go = new GameObject("LegacyUIFramework_InputBlocker");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            instance = go.AddComponent<LegacyUIInputBlocker>();
        }

        private static void CleanupStaleRequests()
        {
            if (Requests.Count == 0)
            {
                return;
            }

            int frame = Time.frameCount;
            RemoveBuffer.Clear();
            foreach (KeyValuePair<int, BlockRequest> pair in Requests)
            {
                if (frame - pair.Value.Frame > StaleFrameCount)
                {
                    RemoveBuffer.Add(pair.Key);
                }
            }

            for (int i = 0; i < RemoveBuffer.Count; i++)
            {
                Requests.Remove(RemoveBuffer[i]);
            }
        }

        private static void EnsureGameInputPatches()
        {
            if (harmonyPatchAttempted)
            {
                return;
            }

            harmonyPatchAttempted = true;

            try
            {
                Harmony harmony = new Harmony("bigcitylegacy.legacyuiframework.inputblock");
                int patched = 0;

                patched += PatchPostfix(
                    harmony,
                    "nHelp",
                    "isMouseIntersect",
                    new[] { typeof(RectTransform), typeof(Vector2) },
                    typeof(LegacyUIInputBlocker).GetMethod("NHelpIsMouseIntersectPostfix", BindingFlags.NonPublic | BindingFlags.Static));

                patched += PatchPostfix(
                    harmony,
                    "hVR",
                    "isMouseOrHeadIntersect",
                    new[] { typeof(RectTransform) },
                    typeof(LegacyUIInputBlocker).GetMethod("HvrIsMouseOrHeadIntersectPostfix", BindingFlags.NonPublic | BindingFlags.Static));

                patched += PatchPostfix(
                    harmony,
                    "MenuPress",
                    "isPressDown",
                    Type.EmptyTypes,
                    typeof(LegacyUIInputBlocker).GetMethod("MenuPressMouseButtonPostfix", BindingFlags.NonPublic | BindingFlags.Static));

                patched += PatchPostfix(
                    harmony,
                    "MenuPress",
                    "isPressUp",
                    Type.EmptyTypes,
                    typeof(LegacyUIInputBlocker).GetMethod("MenuPressMouseButtonPostfix", BindingFlags.NonPublic | BindingFlags.Static));

                patched += PatchPostfix(
                    harmony,
                    "MenuPress",
                    "isStillPresses",
                    Type.EmptyTypes,
                    typeof(LegacyUIInputBlocker).GetMethod("MenuPressMouseButtonPostfix", BindingFlags.NonPublic | BindingFlags.Static));

                harmonyPatchInstalled = patched > 0;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[LegacyUIFramework] Failed to install game input blockers: " + ex.Message);
            }
        }

        private static int PatchPostfix(Harmony harmony, string typeName, string methodName, Type[] parameters, MethodInfo postfix)
        {
            if (harmony == null || postfix == null)
            {
                return 0;
            }

            Type type = AccessTools.TypeByName(typeName);
            if (type == null)
            {
                return 0;
            }

            MethodInfo target = AccessTools.Method(type, methodName, parameters);
            if (target == null)
            {
                return 0;
            }

            harmony.Patch(target, postfix: new HarmonyMethod(postfix));
            return 1;
        }

        private static void NHelpIsMouseIntersectPostfix(RectTransform tr, Vector2 p, ref bool __result)
        {
            if (__result && IsScreenPointBlockedForGameInput(p))
            {
                __result = false;
            }
        }

        private static void HvrIsMouseOrHeadIntersectPostfix(RectTransform rect, ref bool __result)
        {
            if (__result && IsMouseBlockedForGameInput())
            {
                __result = false;
            }
        }

        private static void MenuPressMouseButtonPostfix(ref bool __result)
        {
            if (__result && IsMouseBlockedForGameInput())
            {
                __result = false;
            }
        }

        private void Awake()
        {
            BuildCanvas();
        }

        private void Update()
        {
            CleanupStaleRequests();
            RefreshVisualBlockers();
        }

        private void LateUpdate()
        {
            CleanupStaleRequests();
            RefreshVisualBlockers();
        }

        private void BuildCanvas()
        {
            if (canvas)
            {
                return;
            }

            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = CanvasSortingOrder;

            raycaster = gameObject.AddComponent<GraphicRaycaster>();
            raycaster.ignoreReversedGraphics = true;
        }

        private void RefreshVisualBlockers()
        {
            if (!canvas)
            {
                BuildCanvas();
            }

            CleanupStaleRequests();

            bool screenBlock = false;
            int windowBlockCount = 0;
            foreach (BlockRequest request in Requests.Values)
            {
                if (request.Mode == LegacyUIInputBlockMode.Screen)
                {
                    screenBlock = true;
                    break;
                }
                if (request.Mode == LegacyUIInputBlockMode.Window)
                {
                    windowBlockCount++;
                }
            }

            if (screenBlock)
            {
                EnsureBlockerCount(1);
                SetBlockerToScreen(blockers[0]);
                SetExtraBlockersInactive(1);
                return;
            }

            EnsureBlockerCount(windowBlockCount);

            int index = 0;
            foreach (BlockRequest request in Requests.Values)
            {
                if (request.Mode != LegacyUIInputBlockMode.Window)
                {
                    continue;
                }

                SetBlockerToGuiRect(blockers[index], request.Rect);
                index++;
            }

            SetExtraBlockersInactive(index);
        }

        private void EnsureBlockerCount(int count)
        {
            while (blockers.Count < count)
            {
                GameObject go = new GameObject("InputBlock");
                go.hideFlags = HideFlags.HideAndDontSave;
                go.transform.SetParent(transform, false);

                Image image = go.AddComponent<Image>();
                image.color = new Color(0f, 0f, 0f, 0f);
                image.raycastTarget = true;
                blockers.Add(image);
            }
        }

        private void SetExtraBlockersInactive(int fromIndex)
        {
            for (int i = fromIndex; i < blockers.Count; i++)
            {
                if (blockers[i])
                {
                    blockers[i].gameObject.SetActive(false);
                }
            }
        }

        private static void SetBlockerToScreen(Image image)
        {
            if (!image)
            {
                return;
            }

            image.gameObject.SetActive(true);
            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetBlockerToGuiRect(Image image, Rect guiRect)
        {
            if (!image)
            {
                return;
            }

            image.gameObject.SetActive(true);
            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(guiRect.x, -guiRect.y);
            rect.sizeDelta = new Vector2(Mathf.Max(0f, guiRect.width), Mathf.Max(0f, guiRect.height));
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        internal static bool IsHarmonyPatchInstalled()
        {
            return harmonyPatchInstalled;
        }
    }
}
