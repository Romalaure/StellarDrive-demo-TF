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
    /// on the glass hides whatever is behind the mirror. Only mirrors near and in front of the
    /// viewer render.
    /// </summary>
    internal sealed class MirrorVisuals : DefaultShipPartVisuals, IContextAwarePart
    {
        public const string GlassName = "TF_NoPaint_MirrorGlass";

        private static readonly int EmissionMapId = Shader.PropertyToID("_EmissionMap");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

        private MeshRenderer _glass;
        private Vector3[] _corners;
        private Camera _camera;
        private RenderTexture _texture;
        private Material _material;
        private bool _hasContext;
        private bool _hooked;

        private void Awake()
        {
            Transform glass = transform.Find(GlassName);
            if (glass == null)
                return;
            _glass = glass.GetComponent<MeshRenderer>();
            Mesh mesh = glass.GetComponent<MeshFilter>().sharedMesh;
            // Lower-left, lower-right, upper-left in glass space, as built by MirrorCatalog.BuildGlass
            _corners = new[] { mesh.vertices[0], mesh.vertices[1], mesh.vertices[2], mesh.vertices[3] };
        }

        public void SetContext(ShipPartContext context)
        {
            _hasContext = true;
        }

        private void OnEnable()
        {
            if (!_hooked)
            {
                RenderPipelineManager.beginCameraRendering += BeforeCameraRenders;
                _hooked = true;
            }
        }

        private void OnDisable()
        {
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
            if (_texture != null)
            {
                _texture.Release();
                Destroy(_texture);
            }
            if (_material != null)
                Destroy(_material);
        }

        private void LateUpdate()
        {
            if (!_hasContext || _glass == null)
                return;
            Camera viewer = MainCamera.Get();
            bool active = viewer != null && _glass.isVisible && ShouldRender(viewer);
            if (active)
            {
                EnsureCamera(viewer);
                Place(viewer);
            }
            if (_camera != null && _camera.enabled != active)
                _camera.enabled = active;
        }

        private bool ShouldRender(Camera viewer)
        {
            Vector3 toViewer = viewer.transform.position - _glass.transform.position;
            if (toViewer.sqrMagnitude > Settings.MirrorMaxDistance.Value * Settings.MirrorMaxDistance.Value)
                return false;
            // Viewer in front of the glass
            return Vector3.Dot(toViewer, _glass.transform.up) > 0.02f;
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
            _camera.projectionMatrix = Matrix4x4.Frustum(left * ratio, right * ratio, bottom * ratio, top * ratio, near, Mathf.Max(viewer.farClipPlane, near + 1f));
        }

        private void EnsureCamera(Camera viewer)
        {
            if (_camera != null)
            {
                _camera.cullingMask = viewer.cullingMask;
                return;
            }

            Vector3 size = new Vector3(_corners.Max(c => c.x) - _corners.Min(c => c.x), 0f, _corners.Max(c => c.z) - _corners.Min(c => c.z));
            int width = Mathf.Clamp(Settings.MirrorResolution.Value, 64, 2048);
            int height = Mathf.Clamp(Mathf.RoundToInt(width * size.z / Mathf.Max(size.x, 0.01f)), 32, 2048);
            _texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { name = "TF_MirrorTexture", antiAliasing = 1 };
            _texture.Create();

            _material = new Material(_glass.sharedMaterial) { name = "TF_MirrorGlassInstance" };
            _material.SetTexture(EmissionMapId, _texture);
            _material.SetTexture(BaseMapId, null);
            _glass.sharedMaterial = _material;

            var cameraObject = new GameObject("TF_MirrorCamera");
            cameraObject.transform.SetParent(transform, false);
            _camera = cameraObject.AddComponent<Camera>();
            _camera.CopyFrom(viewer);
            _camera.targetTexture = _texture;
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
                data.requiresDepthTexture = false;
                data.requiresColorTexture = false;
            }
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
