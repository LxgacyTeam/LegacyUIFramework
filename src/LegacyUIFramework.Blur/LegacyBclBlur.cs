using System;
using UnityEngine;

namespace BigCityLegacy.UI.Blur
{
    /// <summary>Optional built-in-renderer integration for MadOut2 9.4 only.</summary>
    public sealed class LegacyBclBlur : MonoBehaviour, ILegacyUIBackdrop, ILegacyUIBackdropDiagnostics
    {
        public const string DiagnosticsRevision = "2026-10-27.2";
        private readonly BlurDiagnosticLog diagnostic = new BlurDiagnosticLog();
        public bool DiagnosticsEnabled { get { return diagnostic.Enabled; } set { diagnostic.Enabled = value; } }
        public Action<string> DiagnosticLog { get { return diagnostic.Writer; } set { diagnostic.Writer = value; } }
        private long requests, draws, preRenders, imageCallbacks, captures, windowReports, windowRepaints, rawRepaints;
        private long previousImages, previousCaptures, previousDraws;
        private long capturesAtAttach;
        private int lastWindowFrame = -100;
        private string lastWindow, lastDraw = "never requested", lastCapture = "no callbacks";
        private LegacyUIBackdropDiagnosticInfo lastWindowInfo;
        private float nextHealth, attachedAt;
        private readonly System.Collections.Generic.Dictionary<string, string> windowStages = new System.Collections.Generic.Dictionary<string, string>();
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
        private bool reportedFailure;

        public static LegacyBclBlur Create(GameObject host)
        { return Create(host, null, true); }

        public static LegacyBclBlur Create(GameObject host, Action<string> log, bool diagnosticsEnabled = true)
        {
            if (host == null) throw new ArgumentNullException("host");
            if (diagnosticsEnabled)
                BlurDiagnosticLog.Emit(log, "stage=create revision=" + DiagnosticsRevision + " game=" + Application.version +
                    " unity=" + Application.unityVersion + " platform=" + Application.platform + " batch=" + Application.isBatchMode +
                    " gpu=" + SystemInfo.graphicsDeviceType + " device=" + SystemInfo.graphicsDeviceName +
                    " uvTop=" + SystemInfo.graphicsUVStartsAtTop + " screen=" + Screen.width + "x" + Screen.height);
            string reason = Application.version != "9.4" ? "unsupported-game-version" :
                Application.isBatchMode ? "batch-mode" :
                SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null ? "null-graphics-device" : null;
            if (reason != null)
            {
                if (diagnosticsEnabled) BlurDiagnosticLog.Emit(log, "stage=create state=skipped reason=" + reason);
                return null;
            }
            LegacyBclBlur provider = host.AddComponent<LegacyBclBlur>();
            provider.DiagnosticLog = log; provider.DiagnosticsEnabled = diagnosticsEnabled;
            provider.diagnostic.Context = "provider=" + provider.GetInstanceID() + " ";
            provider.diagnostic.Write("create", "ready", "host=" + host.name + " enabled=" + provider.isActiveAndEnabled);
            return provider;
        }

        private void LateUpdate()
        {
            ReportHealth();
            if (!EnableBlur || Time.frameCount - requestedFrame > 2)
            {
                if (diagnostic.ShouldWrite("activity", EnableBlur ? "idle" : "disabled"))
                    diagnostic.Write("activity", EnableBlur ? "idle" : "disabled", "lastRequestAge=" + (Time.frameCount - requestedFrame));
                Detach(); ReleaseResources(); return;
            }
            // CamCurrent's cameras are often disabled pose proxies. Cam.me.unityCam
            // is the actual camera rendered by MadOut2; never use Cam's UI camera.
            Camera candidate = Cam.me ? Cam.me.unityCam : null;
            if (!IsUsable(candidate)) candidate = Camera.main;
            if (!IsUsable(candidate))
            {
                if (diagnostic.ShouldWrite("camera", "no-usable-camera"))
                    diagnostic.Write("camera", "no-usable-camera", "Cam.me=" + BlurDiagnosticLog.CameraText(Cam.me ? Cam.me.unityCam : null) +
                        " main=" + BlurDiagnosticLog.CameraText(Camera.main) + " " + BlurDiagnosticLog.CameraInventory());
                Detach(); ReleaseResources(); return;
            }
            if (candidate == sceneCamera && capture) return;
            Detach(); ReleaseResources();
            sceneCamera = candidate;
            capture = candidate.gameObject.AddComponent<LegacyBclBlurCapture>();
            capture.Owner = this;
            attachedAt = Time.realtimeSinceStartup;
            capturesAtAttach = captures;
            if (diagnostic.ShouldWrite("camera", "attached-" + candidate.GetInstanceID()))
                diagnostic.Write("camera", "attached", "captureEnabled=" + capture.isActiveAndEnabled + " " + BlurDiagnosticLog.CameraText(candidate) +
                    " " + BlurDiagnosticLog.CameraInventory());
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
            string skip = !EnableBlur ? "disabled" : !source ? "null-source" : capturedFrame == Time.frameCount ? "duplicate-frame" :
                Time.frameCount - requestedFrame > 2 ? "no-recent-request" :
                !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB32) ? "unsupported-ARGB32" : null;
            if (skip != null)
            {
                lastCapture = skip;
                if (diagnostic.ShouldWrite("capture", skip)) diagnostic.Write("capture", skip, "frame=" + Time.frameCount);
                return;
            }
            RenderTexture previousTarget = RenderTexture.active;
            try
            {
                if (!material)
                {
                    Shader shader = null;
                    string shaderSource = "scene-camera";
                    global::BlurBehind native = sceneCamera.GetComponent<global::BlurBehind>();
                    if (native) shader = native.blurShader;
                    if (!shader)
                        foreach (global::BlurBehind effect in global::BlurBehind.arr)
                            if (effect && effect.blurShader) { shader = effect.blurShader; shaderSource = "native-blur-list:" + effect.name; break; }
                    if (!shader) { shader = Shader.Find("Hidden/Blur Behind/Blur"); shaderSource = "Shader.Find"; }
                    if (!shader || !shader.isSupported)
                    {
                        lastCapture = !shader ? "shader-missing" : "shader-unsupported";
                        if (diagnostic.ShouldWrite("shader", lastCapture)) diagnostic.Write("shader", lastCapture,
                            "source=" + shaderSource + " shader=" + (shader ? shader.name : "<none>") +
                            " nativeBlurCount=" + global::BlurBehind.arr.Count);
                        return;
                    }
                    material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                    if (diagnostic.ShouldWrite("shader", "ready:" + shader.GetInstanceID()))
                        diagnostic.Write("shader", "ready", "name=" + shader.name + " source=" + shaderSource +
                            " supported=" + shader.isSupported + " passes=" + material.passCount +
                            " _MainTex=" + material.HasProperty("_MainTex") + " _Parameter=" + material.HasProperty("_Parameter"));
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
                        lastCapture = "texture-create-failed";
                        if (diagnostic.ShouldWrite("texture", lastCapture)) diagnostic.Write("texture", lastCapture, width + "x" + height);
                        ReleaseTexture(); return;
                    }
                    if (diagnostic.ShouldWrite("texture", width + "x" + height))
                        diagnostic.Write("texture", "created", "source=" + BlurDiagnosticLog.TextureText(source) +
                            " output=" + BlurDiagnosticLog.TextureText(texture) + " downsample=" + divisor);
                }
                else if (!texture.IsCreated() && !texture.Create())
                {
                    lastCapture = "texture-recreate-failed";
                    if (diagnostic.ShouldWrite("texture", lastCapture)) diagnostic.Write("texture", lastCapture, BlurDiagnosticLog.TextureText(texture));
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
                captures++; lastCapture = "ready";
                if (diagnostic.ShouldWrite("capture", "ready")) diagnostic.Write("capture", "ready",
                    "source=" + BlurDiagnosticLog.TextureText(source) + " output=" + BlurDiagnosticLog.TextureText(texture) +
                    " iterations=" + Mathf.Clamp(Iterations, 0, 4) + " radius=" + Radius +
                    " scenePixels=" + BlurDiagnosticLog.RectText(textureScreenRect));
            }
            catch (Exception ex)
            {
                lastCapture = "exception";
                if (diagnostic.ShouldWrite("capture", "exception")) diagnostic.Write("capture", "exception", ex.ToString());
                if (!reportedFailure) { reportedFailure = true; Debug.LogWarning("LegacyUIFramework.Blur: " + ex.Message); }
                ReleaseResources();
            }
            finally { RenderTexture.active = previousTarget; }
        }

        public bool Draw(Rect rect)
        {
            if (Event.current == null || Event.current.type != EventType.Repaint) return false;
            requests++;
            if (!EnableBlur || !isActiveAndEnabled)
            { TraceDraw("provider-disabled", rect, new Rect()); return false; }
            requestedFrame = Time.frameCount;
            if (!texture) { TraceDraw("no-texture", rect, new Rect()); return false; }
            if (Time.frameCount - capturedFrame > 2) { TraceDraw("stale-texture", rect, new Rect()); return false; }
            if (!IsUsable(sceneCamera)) { TraceDraw("camera-unusable", rect, new Rect()); return false; }
            Rect physical = LegacyUI.ToPhysicalRect(rect);
            if (physical.xMin < textureScreenRect.xMin || physical.xMax > textureScreenRect.xMax ||
                physical.yMin < textureScreenRect.yMin || physical.yMax > textureScreenRect.yMax)
            { TraceDraw("outside-camera-viewport", physical, new Rect()); return false; }
            Rect uv = GetTextureCoordinates(physical, textureScreenRect);
            GUI.DrawTextureWithTexCoords(rect, texture, uv, true);
            draws++;
            TraceDraw("drawn", physical, uv);
            return true;
        }

        private void TraceDraw(string code, Rect rect, Rect uv)
        {
            lastDraw = code;
            if (diagnostic.ShouldWrite("draw", code)) diagnostic.Write("draw", code,
                "frame=" + Time.frameCount + " captureAge=" + (Time.frameCount - capturedFrame) +
                " rect=" + BlurDiagnosticLog.RectText(rect) + " scenePixels=" + BlurDiagnosticLog.RectText(textureScreenRect) +
                " uv=" + BlurDiagnosticLog.RectText(uv) + " guiScale=" + LegacyUI.Scale + " alpha=" + GUI.color.a);
        }

        public void ReportWindow(LegacyUIBackdropDiagnosticInfo info)
        {
            if (!DiagnosticsEnabled) return;
            windowReports++; lastWindowFrame = Time.frameCount; lastWindow = info.Title;
            lastWindowInfo = info;
            if (info.CurrentEvent == EventType.Repaint) windowRepaints++;
            if (info.RawEvent == EventType.Repaint) rawRepaints++;
            string title = info.Title ?? "<untitled>";
            string stage;
            if (!windowStages.TryGetValue(title, out stage))
            { stage = "window:" + title; windowStages.Add(title, stage); }
            string code = !info.Requested ? "disabled-by-window" : info.Drawn ? "drawn" :
                info.CurrentEvent != EventType.Repaint ?
                    (info.RawEvent == EventType.Repaint || info.EntryEvent == EventType.Repaint ? "repaint-consumed-before-backdrop" : "not-repaint") :
                    "provider-declined";
            // Layout skips are expected. Report the first event and subsequent
            // repaint outcomes, rather than alternating Layout/Repaint every frame.
            if (windowStages.Count > 0 && info.RawEvent != EventType.Repaint && info.EntryEvent != EventType.Repaint &&
                info.CurrentEvent != EventType.Repaint && windowReports > 1) return;
            if (diagnostic.ShouldWrite(stage, code)) diagnostic.Write(stage, code,
                "entry=" + info.EntryEvent + " event=" + info.CurrentEvent + " raw=" + info.RawEvent +
                " requested=" + info.Requested + " result=" + info.Drawn + " rect=" + BlurDiagnosticLog.RectText(info.Rect) +
                " backgroundAlpha=" + info.BackgroundAlpha + " tint=" + info.TintMultiplier + " guiAlpha=" + info.Opacity);
        }

        internal void NotePreRender() { preRenders++; }
        internal void NoteImage(RenderTexture source, RenderTexture destination)
        {
            imageCallbacks++;
            if (diagnostic.ShouldWrite("image-callback", "received")) diagnostic.Write("image-callback", "received",
                "source=" + BlurDiagnosticLog.TextureText(source) + " destination=" + BlurDiagnosticLog.TextureText(destination));
        }

        private void ReportHealth()
        {
            if (!DiagnosticsEnabled || Time.realtimeSinceStartup < nextHealth ||
                (Time.frameCount - requestedFrame > 4 && Time.frameCount - lastWindowFrame > 4)) return;
            nextHealth = Time.realtimeSinceStartup + 5f;
            diagnostic.Write("health", "summary", "window=" + (lastWindow ?? "<none>") +
                " windowEvent(entry/current/raw)=" + lastWindowInfo.EntryEvent + "/" + lastWindowInfo.CurrentEvent + "/" + lastWindowInfo.RawEvent +
                " windowRequest/result=" + lastWindowInfo.Requested + "/" + lastWindowInfo.Drawn +
                " windowAlpha/background/tint=" + lastWindowInfo.Opacity + "/" + lastWindowInfo.BackgroundAlpha + "/" + lastWindowInfo.TintMultiplier +
                " windowEvents=" + windowReports + " rawRepaints=" + rawRepaints + " acceptedRepaints=" + windowRepaints +
                " drawRequests=" + requests + " draws=" + draws + " preRenders=" + preRenders + " imageCallbacks=" + imageCallbacks +
                " captures=" + captures + " delta(images/captures/draws)=" + (imageCallbacks - previousImages) + "/" +
                (captures - previousCaptures) + "/" + (draws - previousDraws) + " lastDraw=" + lastDraw +
                " lastCapture=" + lastCapture + " requestAge=" + (Time.frameCount - requestedFrame) +
                " captureAge=" + (Time.frameCount - capturedFrame) + " captureComponent=" + (capture ? capture.isActiveAndEnabled.ToString() : "<none>") +
                " output=" + BlurDiagnosticLog.TextureText(texture) + " shader=" + (material && material.shader ? material.shader.name : "<none>") +
                " settings(downsample/iterations/radius)=" + Downsample + "/" + Iterations + "/" + Radius +
                " camera=" + BlurDiagnosticLog.CameraText(sceneCamera));
            previousImages = imageCallbacks; previousCaptures = captures; previousDraws = draws;
            if (capture && Time.realtimeSinceStartup - attachedAt > 1f && captures == capturesAtAttach &&
                diagnostic.ShouldWrite("health-camera-inventory", "no-frames:" + sceneCamera.GetInstanceID()))
                diagnostic.Write("health", "no-captured-frames", BlurDiagnosticLog.CameraInventory());
        }

        public static Rect GetTextureCoordinates(Rect physical, Rect scene)
        {
            if (scene.width <= 0f || scene.height <= 0f) return new Rect();
            return new Rect((physical.x - scene.x) / scene.width,
                1f - (physical.yMax - scene.y) / scene.height, physical.width / scene.width, physical.height / scene.height);
        }

        private void Detach()
        {
            if (capture && diagnostic.ShouldWrite("lifecycle", "detached"))
                diagnostic.Write("lifecycle", "detached", "camera=" + BlurDiagnosticLog.CameraText(sceneCamera));
            if (capture) { capture.Owner = null; capture.enabled = false; Destroy(capture); }
            capture = null; sceneCamera = null;
        }
        private void ReleaseTexture()
        { if (texture) { texture.Release(); Destroy(texture); texture = null; } }
        private void ReleaseResources()
        {
            if ((texture || material) && diagnostic.ShouldWrite("resources", "released"))
                diagnostic.Write("resources", "released", "output=" + BlurDiagnosticLog.TextureText(texture));
            ReleaseTexture(); if (material) { Destroy(material); material = null; } capturedFrame = -100; }
        private void OnDisable()
        {
            if (diagnostic.ShouldWrite("lifecycle", "disabled")) diagnostic.Write("lifecycle", "disabled", "frame=" + Time.frameCount);
            Detach(); ReleaseResources();
        }
        private void OnDestroy() { Detach(); ReleaseResources(); }
    }

    // Appended to the actual scene camera. Its source precedes IMGUI rendering;
    // the outgoing image is always forwarded unchanged, including on failure.
    public sealed class LegacyBclBlurCapture : MonoBehaviour
    {
        internal LegacyBclBlur Owner;
        private void OnPreRender() { if (Owner) Owner.NotePreRender(); }
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            try { if (Owner) { Owner.NoteImage(source, destination); Owner.Capture(source); } }
            finally { Graphics.Blit(source, destination); }
        }
    }
}
