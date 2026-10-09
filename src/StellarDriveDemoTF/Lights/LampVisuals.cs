using System;
using Ships.Interface.Model.Parts;
using Ships.Interface.Model.Parts.Common;
using Ships.Interface.Model.Parts.State;
using Ships.Interface.Model.Parts.StateTypes;
using Ships.Parts.Common;
using Ships.Parts.Common.Model;
using Ships.Parts.Common.Utils;
using StellarDriveDemoTF.Common;
using StellarDriveDemoTF.Paint;
using UnityEngine;

namespace StellarDriveDemoTF.Lights
{
    /// <summary>
    /// Drives a lamp's light and glowing diffuser. Lamps are built on the Signal Display part, whose
    /// server-side updater already reads the signal cable into SignalDisplayState.SignalValue and
    /// syncs it. Without a cable the lamp stays on; with one it follows the signal (0 = off, 1 = full).
    /// Painting the lamp sets the light color.
    /// Everything is looked up from child objects, since this component is cloned by Instantiate.
    /// </summary>
    internal sealed class LampVisuals : DefaultShipPartVisuals, IContextAwarePart
    {
        public const string LightName = "TF_LampLight";
        public const string DiffuserName = "TF_NoPaint_Diffuser";

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private Light _light;
        private Renderer[] _diffusers;
        private float _baseIntensity;
        private MaterialPropertyBlock _block;

        private bool _hasContext;
        private PartKey _key;
        private double _signal;
        private float _nextRefresh;
        private float _shownLevel = -1f;
        private Color _shownColor;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            Transform lightObject = transform.Find(LightName);
            _light = lightObject != null ? lightObject.GetComponent<Light>() : null;
            _baseIntensity = _light != null ? _light.intensity : 0f;
            Transform diffuser = transform.Find(DiffuserName);
            _diffusers = diffuser != null ? diffuser.GetComponents<Renderer>() : new Renderer[0];
            // Build menu previews have no context: show the lamp lit, without casting light
            if (_light != null)
                _light.enabled = false;
            Apply(1f, LampCatalog.DefaultLight, castLight: false);
        }

        public void SetContext(ShipPartContext context)
        {
            _hasContext = true;
            _key = new PartKey(context.Part.ShipId, context.Part.PartId);
            _nextRefresh = 0f;
        }

        public override void SetNetworkState(IStatefulPartState state) => ReadSignal(state);

        public override void ReactToNetworkStateChange(StatefulPartStateChangeEvent change) => ReadSignal(change.NewState);

        private void ReadSignal(IStatefulPartState state)
        {
            if (state is SignalDisplayState display)
                _signal = display.SignalValue;
            _nextRefresh = 0f;
        }

        private void Update()
        {
            if (!_hasContext || Time.unscaledTime < _nextRefresh)
                return;
            _nextRefresh = Time.unscaledTime + 0.2f;

            float level = 1f;
            var cables = GameServices.CablesClient;
            if (cables != null && cables.TryGetCableId(new EntityPartSocket(_key.ShipId, _key.PartId, 0), out _))
                level = Mathf.Clamp01((float)Math.Abs(_signal));

            Color color = PaintNet.Client.TryGet(_key, out PaintData paint) ? (Color)paint.Color : LampCatalog.DefaultLight;
            if (Mathf.Approximately(level, _shownLevel) && color == _shownColor)
                return;
            Apply(level, color, castLight: true);
        }

        private void Apply(float level, Color color, bool castLight)
        {
            _shownLevel = level;
            _shownColor = color;
            if (_light != null && castLight)
            {
                _light.enabled = level > 0.001f;
                _light.intensity = _baseIntensity * level;
                _light.color = color;
            }
            foreach (Renderer renderer in _diffusers)
            {
                renderer.GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, Color.Lerp(new Color(0.35f, 0.36f, 0.38f), color, level));
                _block.SetColor(EmissionColorId, color * (3f * level));
                renderer.SetPropertyBlock(_block);
            }
        }
    }
}
