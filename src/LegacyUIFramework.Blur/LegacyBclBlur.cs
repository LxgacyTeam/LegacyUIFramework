using System;
using UnityEngine;

namespace BigCityLegacy.UI.Blur
{
    /// <summary>Optional built-in-renderer integration for MadOut2 9.4 only.</summary>
    public sealed class LegacyBclBlur : MonoBehaviour, ILegacyUIBackdrop
    {
        public bool EnableBlur = true;
        public int Downsample = 4;
        public int Iterations = 2;
        public float Radius = 2f;
        private LegacyBclBlurCapture capture;
        private Camera sceneCamera;
        private Material material;
        private RenderTexture texture;
        private Rect textureScreenRect;
        private int requestedFrame = -100, capturedFrame = -100;

        public static LegacyBclBlur Create(GameObject host)
        {
            if (host == null) throw new ArgumentNullException("host");
            if (Application.version != "9.4" || Application.isBatchMode ||
                SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return null;
            return host.AddComponent<LegacyBclBlur>();
        }

        private void LateUpdate()
        {
            if (!EnableBlur || Time.frameCount - requestedFrame > 2)
            {
                Detach(); ReleaseResources(); return;
            }
            // CamCurrent's cameras are often disabled pose proxies. Cam.me.unityCam
            // is the actual camera rendered by MadOut2; never use Cam's UI camera.
            Camera candidate = Cam.me ? Cam.me.unityCam : null;
            if (!IsUsable(candidate)) candidate = Camera.main;
            if (!IsUsable(candidate))
            {
                Detach(); ReleaseResources(); return;
            }
            if (candidate == sceneCamera && capture) return;
            Detach(); ReleaseResources();
            sceneCamera = candidate;
            capture = candidate.gameObject.AddComponent<LegacyBclBlurCapture>();
            capture.Owner = this;
        }

        private static bool IsUsable(Camera camera)
        {
            // GameUI.CheckSize assigns MainRenderTexture to the real scene camera
            // when render scaling is enabled. It remains the source of the scene.
            // Other offscreen cameras (previews/minimap) are not presentation cameras.
            return camera && camera.enabled && camera.gameObject.activeInHierarchy &&
                (!camera.targetTexture || (Cam.me && camera == Cam.me.unityCam));
        }

        private static Rect GetScreenRect(Camera camera)
        {
            // GameUI displays the entire scene target in a fullscreen RawImage.
            // Its resolution is independent of Screen (including supersampling).
            // Camera.pixelRect is measured in target pixels, not screen pixels.
            if (camera.targetTexture) return new Rect(0f, 0f, Screen.width, Screen.height);
            Rect viewport = camera.pixelRect;
            return new Rect(viewport.x, Screen.height - viewport.yMax, viewport.width, viewport.height);
        }

        internal void Capture(RenderTexture source)
        {
            if (!EnableBlur || !source || capturedFrame == Time.frameCount ||
                Time.frameCount - requestedFrame > 2 ||
                !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB32)) return;
            RenderTexture previousTarget = RenderTexture.active;
            try
            {
                if (!material)
                {
                    Shader shader = null;
                    global::BlurBehind native = sceneCamera.GetComponent<global::BlurBehind>();
                    if (native) shader = native.blurShader;
                    if (!shader)
                        foreach (global::BlurBehind effect in global::BlurBehind.arr)
                            if (effect && effect.blurShader) { shader = effect.blurShader; break; }
                    if (!shader) shader = Shader.Find("Hidden/Blur Behind/Blur");
                    if (!shader || !shader.isSupported)
                    {
                        return;
                    }
                    material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                }
                int divisor = Mathf.Clamp(Downsample, 1, 8);
                int width = Mathf.Max(1, source.width / divisor), height = Mathf.Max(1, source.height / divisor);
                if (!texture || texture.width != width || texture.height != height)
                {
                    ReleaseTexture();
                    texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
                    { name = "LegacyUI_BCL_Backdrop", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp,
                        hideFlags = HideFlags.HideAndDontSave, useMipMap = false };
                    if (!texture.Create())
                    {
                        ReleaseTexture(); return;
                    }
                }
                else if (!texture.IsCreated() && !texture.Create())
                {
                    return;
                }
                Graphics.Blit(source, texture);
                RenderTexture temporary = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
                try
                {
                    temporary.filterMode = FilterMode.Bilinear;
                    int iterations = Mathf.Clamp(Iterations, 0, 4);
                    float radius = float.IsNaN(Radius) ? 2f : Mathf.Clamp(Radius, 0f, 8f);
                    for (int i = 0; i < iterations; i++)
                    {
                        // Same four-tap pass and _Parameter convention as native BlurBehind.
                        float x = radius * (i + 1) / width, y = radius * (i + 1) / height;
                        material.SetVector("_Parameter", new Vector4(x, y, -x, -y));
                        Graphics.Blit(texture, temporary, material, 0);
                        material.SetVector("_Parameter", new Vector4(y * height / width, -x * width / height,
                            -y * height / width, x * width / height));
                        Graphics.Blit(temporary, texture, material, 0);
                    }
                }
                finally { RenderTexture.ReleaseTemporary(temporary); }
                textureScreenRect = GetScreenRect(sceneCamera);
                capturedFrame = Time.frameCount;
            }
            catch (Exception)
            {
                ReleaseResources();
            }
            finally { RenderTexture.active = previousTarget; }
        }

        public bool Draw(Rect rect)
        {
            if (Event.current == null || Event.current.type != EventType.Repaint) return false;
            if (!EnableBlur || !isActiveAndEnabled)
            { return false; }
            requestedFrame = Time.frameCount;
            if (!texture) { return false; }
            if (Time.frameCount - capturedFrame > 2) { return false; }
            if (!IsUsable(sceneCamera)) { return false; }
            Rect physical = LegacyUI.ToPhysicalRect(rect);
            if (physical.xMin < textureScreenRect.xMin || physical.xMax > textureScreenRect.xMax ||
                physical.yMin < textureScreenRect.yMin || physical.yMax > textureScreenRect.yMax)
            { return false; }
            Rect uv = GetTextureCoordinates(physical, textureScreenRect);
            GUI.DrawTextureWithTexCoords(rect, texture, uv, true);
            return true;
        }

        public static Rect GetTextureCoordinates(Rect physical, Rect scene)
        {
            if (scene.width <= 0f || scene.height <= 0f) return new Rect();
            return new Rect((physical.x - scene.x) / scene.width,
                1f - (physical.yMax - scene.y) / scene.height, physical.width / scene.width, physical.height / scene.height);
        }

        private void Detach()
        {
            if (capture) { capture.Owner = null; capture.enabled = false; Destroy(capture); }
            capture = null; sceneCamera = null;
        }
        private void ReleaseTexture()
        { if (texture) { texture.Release(); Destroy(texture); texture = null; } }
        private void ReleaseResources()
        {
            ReleaseTexture(); if (material) { Destroy(material); material = null; } capturedFrame = -100; }
        private void OnDisable()
        {
            Detach(); ReleaseResources();
        }
        private void OnDestroy() { Detach(); ReleaseResources(); }
    }

    // Appended to the actual scene camera. Its source precedes IMGUI rendering;
    // the outgoing image is always forwarded unchanged, including on failure.
    public sealed class LegacyBclBlurCapture : MonoBehaviour
    {
        internal LegacyBclBlur Owner;
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            try { if (Owner) { Owner.Capture(source); } }
            finally { Graphics.Blit(source, destination); }
        }
    }
}
