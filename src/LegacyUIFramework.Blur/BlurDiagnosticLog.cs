using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace BigCityLegacy.UI.Blur
{
    // Log changes, not frames. Even rapidly alternating outcomes are limited to
    // one message per stage every two seconds; health summaries have their own timer.
    internal sealed class BlurDiagnosticLog
    {
        private sealed class Gate { internal string Code; internal float Next; }
        private readonly Dictionary<string, Gate> gates = new Dictionary<string, Gate>();
        internal bool Enabled = true;
        internal Action<string> Writer;
        internal string Context = "";

        internal bool ShouldWrite(string stage, string code)
        { return ShouldWrite(stage, code, Time.realtimeSinceStartup); }

        internal bool ShouldWrite(string stage, string code, float now)
        {
            if (!Enabled) return false;
            Gate gate;
            if (!gates.TryGetValue(stage, out gate))
            { gate = new Gate { Next = float.NegativeInfinity }; gates.Add(stage, gate); }
            if (gate.Code == code || now < gate.Next) return false;
            gate.Code = code; gate.Next = now + 2f;
            return true;
        }

        internal void Write(string stage, string code, string details)
        {
            if (Enabled) Emit(Writer, Context + "stage=" + stage + " state=" + code + " " + details);
        }

        internal static void Emit(Action<string> writer, string message)
        {
            string line = "[BCL Blur] " + message;
            try { if (writer != null) writer(line); else Debug.Log(line); }
            catch { Debug.Log(line); } // Diagnostics must not interrupt rendering.
        }

        internal static string RectText(Rect rect)
        {
            return string.Format(CultureInfo.InvariantCulture, "({0:0.##},{1:0.##},{2:0.##},{3:0.##})",
                rect.x, rect.y, rect.width, rect.height);
        }

        internal static string TextureText(RenderTexture texture)
        {
            if (!texture) return "<none/backbuffer>";
            return texture.name + "#" + texture.GetInstanceID() + " " + texture.width + "x" + texture.height +
                " format=" + texture.format + " created=" + texture.IsCreated() + " aa=" + texture.antiAliasing +
                " sRGB=" + texture.sRGB + " texelY=" + texture.texelSize.y.ToString("0.######", CultureInfo.InvariantCulture);
        }

        internal static string CameraText(Camera camera)
        {
            if (!camera) return "<none>";
            string path = camera.name;
            Transform parent = camera.transform.parent;
            for (int i = 0; parent && i < 8; i++, parent = parent.parent) path = parent.name + "/" + path;
            global::BlurBehind native = camera.GetComponent<global::BlurBehind>();
            return path + "#" + camera.GetInstanceID() + " enabled=" + camera.enabled +
                " active=" + camera.gameObject.activeInHierarchy + " type=" + camera.cameraType +
                " depth=" + camera.depth.ToString("0.##", CultureInfo.InvariantCulture) +
                " path=" + camera.actualRenderingPath + " clear=" + camera.clearFlags +
                " mask=0x" + camera.cullingMask.ToString("X8") + " rect=" + RectText(camera.rect) +
                " pixels=" + RectText(camera.pixelRect) + " target=" + TextureText(camera.targetTexture) +
                " nativeBlur=" + (native ? ("enabled=" + native.enabled + " shader=" +
                    (native.blurShader ? native.blurShader.name : "<none>")) : "<none>");
        }

        internal static string CameraInventory()
        {
            Camera[] cameras = Camera.allCameras;
            var parts = new List<string>();
            foreach (Camera camera in cameras) parts.Add(CameraText(camera));
            return "renderingCameraCount=" + cameras.Length + " nativeBlurCount=" + global::BlurBehind.arr.Count +
                " cameras=[" + string.Join(" | ", parts.ToArray()) + "]";
        }
    }
}
