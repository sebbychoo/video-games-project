using UnityEngine;

namespace Procedural
{
    /// <summary>
    /// Simulates fluorescent light buzzing/flickering.
    /// Modulates Light intensity, emissive material, and an optional
    /// floor glow projector so the flicker is visible on the ground.
    /// Works with both generated fixtures and custom sprite fixtures.
    /// </summary>
    public class FluorescentFlicker : MonoBehaviour
    {
        [Header("Buzz Flicker")]
        [Tooltip("How intense the constant buzz flicker is. 0 = steady, 1 = wild.")]
        [Range(0f, 1f)]
        [SerializeField] float flickerStrength = 0.08f;

        [Tooltip("Speed of the buzz. Higher = faster.")]
        [SerializeField] float flickerSpeed = 30f;

        [Header("Random Dips (Brownouts)")]
        [Tooltip("Chance per second of a brief dip.")]
        [Range(0f, 1f)]
        [SerializeField] float dipChance = 0.03f;

        [Tooltip("How deep a dip goes. 0.5 = dims to half.")]
        [Range(0f, 1f)]
        [SerializeField] float dipDepth = 0.5f;

        [Tooltip("Min duration of a dip in seconds.")]
        [SerializeField] float dipDurationMin = 0.04f;

        [Tooltip("Max duration of a dip in seconds.")]
        [SerializeField] float dipDurationMax = 0.15f;

        [Header("Light Multiplier")]
        [Tooltip("Extra multiplier on the spot light flicker so floor pool visibly dims. Higher = more dramatic floor flicker.")]
        [Range(1f, 3f)]
        [SerializeField] float lightFlickerMultiplier = 1.8f;

        private Light[] _lights;
        private Renderer _renderer;
        private float[] _baseLightIntensity;
        private Color _baseEmission;
        private float _noiseOffset;
        private float _dipTimer;
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        /// <summary>
        /// Called by OfficeCeiling to set randomized values per fixture.
        /// </summary>
        public void SetValues(float strength, float speed, float dip, float depth, float lightMult)
        {
            flickerStrength = strength;
            flickerSpeed = speed;
            dipChance = dip;
            dipDepth = depth;
            lightFlickerMultiplier = lightMult;
        }

        private void Start()
        {
            _lights = GetComponentsInChildren<Light>();
            _renderer = GetComponent<Renderer>();

            _baseLightIntensity = new float[_lights.Length];
            for (int i = 0; i < _lights.Length; i++)
                _baseLightIntensity[i] = _lights[i].intensity;

            if (_renderer != null && _renderer.material.HasProperty(EmissionColor))
                _baseEmission = _renderer.material.GetColor(EmissionColor);

            _noiseOffset = Random.Range(0f, 100f);
        }

        private void Update()
        {
            // Perlin noise for constant subtle buzz
            float noise = Mathf.PerlinNoise(Time.time * flickerSpeed + _noiseOffset, 0f);
            float buzzFlicker = 1f - (noise - 0.5f) * 2f * flickerStrength;

            // Random dip (brownout)
            _dipTimer -= Time.deltaTime;
            if (_dipTimer <= 0f && Random.value < dipChance * Time.deltaTime * 60f)
                _dipTimer = Random.Range(dipDurationMin, dipDurationMax);

            float dipFactor = 1f;
            if (_dipTimer > 0f)
                dipFactor = 1f - dipDepth;

            float emissionFlicker = Mathf.Clamp(buzzFlicker * dipFactor, 0.1f, 1.2f);
            // Light gets a stronger version of the flicker so the floor pool visibly reacts
            float lightFlicker = Mathf.Clamp(1f - (1f - emissionFlicker) * lightFlickerMultiplier, 0.05f, 1.2f);

            // Apply to all child lights (spot lights)
            for (int i = 0; i < _lights.Length; i++)
            {
                if (_lights[i] != null)
                    _lights[i].intensity = _baseLightIntensity[i] * lightFlicker;
            }

            // Apply to emissive material (fixture glow)
            if (_renderer != null && _renderer.material.HasProperty(EmissionColor))
                _renderer.material.SetColor(EmissionColor, _baseEmission * emissionFlicker);
        }
    }
}
