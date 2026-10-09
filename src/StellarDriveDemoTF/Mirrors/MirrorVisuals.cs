using System.Collections.Generic;
using System.Linq;
using Ships.Parts.Common;
using Ships.Parts.Common.Model;
using Ships.Parts.Common.Utils;
using StellarDriveDemoTF.Common;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace StellarDriveDemoTF.Mirrors
{
    /// <summary>
    /// Real-time planar mirror. A camera sits at the viewer's eye reflected through the glass plane
    /// and looks through the glass with an off-axis frustum whose image plane is exactly the glass
    /// rectangle, so its render maps 1:1 onto the glass UVs (mirrored left to right). The near plane
    /// on the glass hides whatever is behind the mirror.
    ///
    /// Cost control: only the nearest mirrors in the viewer's frustum refresh (MirrorMaxActive), at a
    /// capped rate (MirrorFps), with a short far plane (MirrorFarClip), lower LOD, no shadows or
    /// post-processing, and an image sized to how big the mirror looks on screen.
    /// </summary>
    internal sealed class MirrorVisuals : DefaultShipPartVisuals, IContextAwarePart
    {
        public const string GlassName = "TF_NoPaint_MirrorGlass";
        private const string CameraName = "TF_MirrorCamera";
        private const int SizeStep = 128;

        private static readonly int EmissionMapId = Shader.PropertyToID("_EmissionMap");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

        private static readonly List<MirrorVisuals> Mirrors = new List<MirrorVisuals>();
        private static readonly List<MirrorVisuals> Candidates = new List<MirrorVisuals>();
        private static int _rankedFrame = -1;
        private static bool _lodHooked;
        private static float _savedLodBias = -1f;

        private MeshRenderer _glass;
        private Vector3[] _corners;
        private float _aspect;
        private Camera _camera;
        private RenderTexture _texture;
        private Material _material;
        private bool _hasContext;
        private bool _hooked;
        private bool _selected;
        private float _distance;
        private float _lastRender = float.NegativeInfinity;
        private Vector3 _lastEye = new Vector3(float.MaxValue, 0f, 0f);

        // What a planar mirror shows depends only on where the eye is relative to the glass, not
        // where it looks: re-render quickly while that changes, and only now and then otherwise
        // (for things moving in the reflection)
        private bool NeedsRender(Camera viewer)
        {
            if (_texture == null)
                return true;
            float since = Time.unscaledTime - _lastRender;
            bool moved = (EyeInGlass(viewer) - _lastEye).sqrMagnitude > 0.02f * 0.02f;
            if (moved)
                return since >= 1f / Mathf.Clamp(Settings.MirrorFps.Value, 1, 120);
            return since >= 1f / Mathf.Clamp(Settings.MirrorIdleFps.Value, 0.05f, 120f);
        }

        private Vector3 EyeInGlass(Camera viewer) => _glass.transform.InverseTransformPoint(viewer.transform.position);

        private void Awake()
        {
            Transform glass = transform.Find(GlassName);
            if (glass == null)
                return;
            _glass = glass.GetComponent<MeshRenderer>();
            Mesh mesh = glass.GetComponent<MeshFilter>().sharedMesh;
            // Lower-left, lower-right, upper-left in glass space, as built by MirrorCatalog.BuildGlass
            _corners = new[] { mesh.vertices[0], mesh.vertices[1], mesh.vertices[2], mesh.vertices[3] };
            float width = _corners.Max(c => c.x) - _corners.Min(c => c.x);
            float height = _corners.Max(c => c.z) - _corners.Min(c => c.z);
            _aspect = height / Mathf.Max(width, 0.01f);
        }

        public void SetContext(ShipPartContext context)
        {
            _hasContext = true;
        }

        private void OnEnable()
        {
            Mirrors.Add(this);
            if (!_hooked)
            {
                RenderPipelineManager.beginCameraRendering += BeforeCameraRenders;
                _hooked = true;
            }
            if (!_lodHooked)
            {
                RenderPipelineManager.beginCameraRendering += LowerLod;
                RenderPipelineManager.endCameraRendering += RestoreLod;
                _lodHooked = true;
            }
        }

        private void OnDisable()
        {
            Mirrors.Remove(this);
            if (_hooked)
            {
                RenderPipelineManager.beginCameraRendering -= BeforeCameraRenders;
                _hooked = false;
            }
            if (_camera != null)
                _camera.enabled = false;
        }

        private void OnDestroy()
        {
            if (_camera != null)
                Destroy(_camera.gameObject);
            ReleaseTexture();
            if (_material != null)
                Destroy(_material);
        }

        private void LateUpdate()
        {
            if (!_hasContext || _glass == null)
                return;
            Camera viewer = MainCamera.Get();
            bool render = false;
            if (viewer != null)
            {
                Rank(viewer);
                if (_selected && NeedsRender(viewer))
                {
                    EnsureCamera(viewer);
                    EnsureTexture(viewer);
                    Place(viewer);
                    render = true;
                    _lastRender = Time.unscaledTime;
                    _lastEye = EyeInGlass(viewer);
                }
            }
            // A camera enabled during LateUpdate renders this frame; disabled ones keep their last image
            if (_camera != null && _camera.enabled != render)
                _camera.enabled = render;
        }

        // Once per frame: picks the nearest mirrors that face the viewer inside its frustum
        private static void Rank(Camera viewer)
        {
            if (_rankedFrame == Time.frameCount)
                return;
            _rankedFrame = Time.frameCount;
            Vector3 eye = viewer.transform.position;
            float maxDistance = Settings.MirrorMaxDistance.Value;

            Candidates.Clear();
            foreach (MirrorVisuals mirror in Mirrors)
            {
                mirror._selected = false;
                if (!mirror._hasContext || mirror._glass == null)
                    continue;
                Transform glass = mirror._glass.transform;
                Vector3 toViewer = eye - glass.position;
                mirror._distance = toViewer.magnitude;
                if (mirror._distance > maxDistance || Vector3.Dot(toViewer, glass.up) <= 0.02f)
                    continue;
                if (!mirror.InView(viewer))
                    continue;
                Candidates.Add(mirror);
            }
            Candidates.Sort((a, b) => a._distance.CompareTo(b._distance));
            int count = Mathf.Min(Candidates.Count, Mathf.Max(0, Settings.MirrorMaxActive.Value));
            for (int i = 0; i < count; i++)
                Candidates[i]._selected = true;
        }

        // Conservative frustum test: out of view only when every corner is past the same screen edge
        // (GeometryUtility is avoided: its Span overloads do not compile against net472)
        private bool InView(Camera viewer)
        {
            Transform glass = _glass.transform;
            int left = 0, right = 0, below = 0, above = 0, behind = 0;
            foreach (Vector3 corner in _corners)
            {
                Vector3 v = viewer.WorldToViewportPoint(glass.TransformPoint(corner));
                if (v.z <= 0f) behind++;
                if (v.x < 0f) left++;
                if (v.x > 1f) right++;
                if (v.y < 0f) below++;
                if (v.y > 1f) above++;
            }
            int n = _corners.Length;
            // Corners behind the eye project mirrored, so a mirror cut by the screen plane counts as seen
            if (behind > 0)
                return behind < n;
            return behind < n && left < n && right < n && below < n && above < n;
        }

        // Runs again right before the mirror camera renders, once the viewer camera has its final pose
        private void BeforeCameraRenders(ScriptableRenderContext context, Camera camera)
        {
            if (camera != _camera || camera == null)
                return;
            Camera viewer = MainCamera.Get();
            if (viewer != null)
                Place(viewer);
        }

        private static void LowerLod(ScriptableRenderContext context, Camera camera)
        {
            if (camera == null || camera.name != CameraName)
                return;
            _savedLodBias = QualitySettings.lodBias;
            QualitySettings.lodBias = _savedLodBias * 0.5f;
        }

        private static void RestoreLod(ScriptableRenderContext context, Camera camera)
        {
            if (camera == null || camera.name != CameraName || _savedLodBias < 0f)
                return;
            QualitySettings.lodBias = _savedLodBias;
            _savedLodBias = -1f;
        }

        private void Place(Camera viewer)
        {
            Transform glass = _glass.transform;
            Vector3 normal = glass.up;
            Vector3 origin = glass.TransformPoint(_corners[0]);
            Vector3 eye = viewer.transform.position;
            float distance = Vector3.Dot(eye - origin, normal);
            if (distance < 0.01f)
                return;

            Vector3 reflectedEye = eye - 2f * distance * normal;
            Transform cameraTransform = _camera.transform;
            cameraTransform.SetPositionAndRotation(reflectedEye, Quaternion.LookRotation(normal, glass.forward));

            // Frustum through the glass rectangle, its near plane on the glass
            float left = float.MaxValue, right = float.MinValue, bottom = float.MaxValue, top = float.MinValue;
            foreach (Vector3 corner in _corners)
            {
                Vector3 local = cameraTransform.InverseTransformPoint(glass.TransformPoint(corner));
                float scale = distance / Mathf.Max(local.z, 0.0001f);
                left = Mathf.Min(left, local.x * scale);
                right = Mathf.Max(right, local.x * scale);
                bottom = Mathf.Min(bottom, local.y * scale);
                top = Mathf.Max(top, local.y * scale);
            }
            float near = distance * 1.002f + 0.002f;
            float ratio = near / distance;
            float far = Mathf.Min(viewer.farClipPlane, near + Mathf.Max(Settings.MirrorFarClip.Value, 5f));
            _camera.projectionMatrix = Matrix4x4.Frustum(left * ratio, right * ratio, bottom * ratio, top * ratio, near, Mathf.Max(far, near + 1f));
        }

        // The image is about as wide as the mirror looks on screen, in steps, up to MirrorResolution
        private void EnsureTexture(Camera viewer)
        {
            int max = Mathf.Clamp(Settings.MirrorResolution.Value, SizeStep, 2048);
            int wanted = Mathf.Clamp(Mathf.CeilToInt(ScreenWidth(viewer) / SizeStep) * SizeStep, SizeStep, max);
            if (_texture != null)
            {
                int current = _texture.width;
                // Grow at once, shrink only when clearly too big, so it does not flip every frame
                if (wanted <= current && wanted > current / 2)
                    return;
            }

            ReleaseTexture();
            int height = Mathf.Clamp(Mathf.RoundToInt(wanted * _aspect), 32, 2048);
            _texture = new RenderTexture(wanted, height, 16, RenderTextureFormat.ARGB32) { name = "TF_MirrorTexture", antiAliasing = 1 };
            _texture.Create();
            _camera.targetTexture = _texture;
            _material.SetTexture(EmissionMapId, _texture);
        }

        private float ScreenWidth(Camera viewer)
        {
            Transform glass = _glass.transform;
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            foreach (Vector3 corner in _corners)
            {
                Vector3 screen = viewer.WorldToScreenPoint(glass.TransformPoint(corner));
                if (screen.z <= 0f)
                    return float.MaxValue;
                minX = Mathf.Min(minX, screen.x);
                maxX = Mathf.Max(maxX, screen.x);
                minY = Mathf.Min(minY, screen.y);
                maxY = Mathf.Max(maxY, screen.y);
            }
            // Seen at an angle the mirror is narrow on screen but still shows a full-width image
            return Mathf.Max(maxX - minX, (maxY - minY) / Mathf.Max(_aspect, 0.01f));
        }

        private void ReleaseTexture()
        {
            if (_texture == null)
                return;
            if (_camera != null)
                _camera.targetTexture = null;
            _texture.Release();
            Destroy(_texture);
            _texture = null;
        }

        private void EnsureCamera(Camera viewer)
        {
            if (_camera != null)
            {
                ApplyWorldSettings(viewer);
                return;
            }

            _material = new Material(_glass.sharedMaterial) { name = "TF_MirrorGlassInstance" };
            _material.SetTexture(BaseMapId, null);
            _glass.sharedMaterial = _material;

            var cameraObject = new GameObject(CameraName);
            cameraObject.transform.SetParent(transform, false);
            _camera = cameraObject.AddComponent<Camera>();
            _camera.CopyFrom(viewer);
            _camera.targetTexture = null;
            _camera.depth = viewer.depth - 1f;
            _camera.allowHDR = false;
            _camera.allowMSAA = false;
            _camera.useOcclusionCulling = false;
            _camera.enabled = false;
            var data = _camera.GetUniversalAdditionalCameraData();
            if (data != null)
            {
                data.renderType = CameraRenderType.Base;
                data.renderPostProcessing = false;
                data.renderShadows = false;
                data.antialiasing = AntialiasingMode.None;
                data.stopNaN = false;
                data.dithering = false;
                data.volumeLayerMask = 0;
                data.requiresDepthOption = CameraOverrideOption.Off;
                data.requiresColorOption = CameraOverrideOption.Off;
            }
            ApplyWorldSettings(viewer);
        }

        // Layers never worth reflecting, and the costly scenery ones skipped unless MirrorReflectWorld
        private static readonly string[] AlwaysHidden = { "UI", "WorldUI", "BuildToolRender", "PartThumbnailRender", "Id" };
        private static readonly string[] Scenery = { "Planet", "Water", "DistantObject" };
        private static int _hiddenBits = -1, _sceneryBits;

        private void ApplyWorldSettings(Camera viewer)
        {
            bool world = Settings.MirrorReflectWorld.Value;
            if (_hiddenBits < 0)
            {
                _hiddenBits = LayerBits(AlwaysHidden);
                _sceneryBits = LayerBits(Scenery);
            }
            int mask = viewer.cullingMask & ~_hiddenBits;
            if (!world)
                mask &= ~_sceneryBits;
            _camera.cullingMask = mask;
            if (world)
            {
                _camera.clearFlags = viewer.clearFlags;
            }
            else
            {
                // A plain sky-colored background instead of the sky and planet
                _camera.clearFlags = CameraClearFlags.SolidColor;
                Color sky = RenderSettings.fog ? RenderSettings.fogColor : RenderSettings.ambientSkyColor;
                _camera.backgroundColor = new Color(sky.r, sky.g, sky.b, 1f);
            }
        }

        private static int LayerBits(string[] names)
        {
            int bits = 0;
            foreach (string name in names)
            {
                int layer = LayerMask.NameToLayer(name);
                if (layer >= 0)
                    bits |= 1 << layer;
            }
            return bits;
        }
    }

    /// <summary>Finds the camera the player sees through, ignoring cameras that render to textures.</summary>
    internal static class MainCamera
    {
        private static Camera _camera;
        private static float _nextLookup;

        public static Camera Get()
        {
            if (_camera != null && _camera.isActiveAndEnabled)
                return _camera;
            if (Time.unscaledTime < _nextLookup)
                return null;
            _nextLookup = Time.unscaledTime + 1f;
            _camera = Camera.main;
            if (_camera == null || _camera.targetTexture != null)
            {
                _camera = Camera.allCameras
                    .Where(c => c.targetTexture == null && c.isActiveAndEnabled && c.name != "TF_MirrorCamera")
                    .OrderByDescending(c => c.depth)
                    .FirstOrDefault();
            }
            return _camera;
        }
    }
}
