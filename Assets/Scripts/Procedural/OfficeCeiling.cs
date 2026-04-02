using UnityEngine;

namespace Procedural
{
    /// <summary>
    /// Generates a ceiling plane with evenly-spaced office fluorescent lights.
    /// Attach to a room prefab root. It reads SpawnZones to figure out room size,
    /// then creates a ceiling quad and a grid of rectangular area lights underneath.
    /// </summary>
    public class OfficeCeiling : MonoBehaviour
    {
        [Header("Ceiling")]
        [Tooltip("Height of the ceiling above the room origin.")]
        [SerializeField] float ceilingHeight = 3f;

        [Tooltip("Manual ceiling size override. If X or Z > 0, uses this instead of auto-detecting from SpawnZones.")]
        [SerializeField] Vector2 manualSize = Vector2.zero;

        [Tooltip("Manual ceiling center offset from this transform. Only used when manualSize is set.")]
        [SerializeField] Vector3 manualCenterOffset = Vector3.zero;

        [Tooltip("If true, scans ALL renderers in children to compute bounds (covers full floor). If false, uses SpawnZones only.")]
        [SerializeField] bool useRendererBounds = true;

        [Tooltip("Extra padding added to each side of the ceiling so it extends past walls. Fixes dark gaps at wall edges.")]
        [SerializeField] float ceilingPadding = 1f;

        [Tooltip("Material for the ceiling. Leave null to auto-generate from ceilingTexture or flat color.")]
        [SerializeField] Material ceilingMaterial;

        [Tooltip("Ceiling tile texture (set Wrap Mode to Repeat). Used when ceilingMaterial is null.")]
        [SerializeField] Texture2D ceilingTexture;

        [Tooltip("How many times the ceiling texture repeats per world unit. Higher = smaller tiles.")]
        [SerializeField] float ceilingTilesPerUnit = 0.5f;

        [Tooltip("Color tint for the auto-generated ceiling material (used only if ceilingMaterial and ceilingTexture are null).")]
        [SerializeField] Color ceilingColor = new Color(0.9f, 0.9f, 0.88f);

        [Header("Lights")]
        [Tooltip("Spacing between lights in world units.")]
        [SerializeField] float lightSpacing = 4f;

        [Tooltip("Light color — warm white mimics office fluorescents.")]
        [SerializeField] Color lightColor = new Color(1f, 0.97f, 0.92f);

        [Tooltip("Intensity per light.")]
        [SerializeField] float lightIntensity = 1.2f;

        [Tooltip("Range of each point light.")]
        [SerializeField] float lightRange = 8f;

        [Tooltip("Outer cone angle of the spot light. Lower = tighter pool on floor, less wall spill.")]
        [Range(10f, 179f)]
        [SerializeField] float spotAngle = 70f;

        [Tooltip("Inner cone angle (full brightness area). Keep lower than spotAngle for soft edges.")]
        [Range(5f, 120f)]
        [SerializeField] float innerSpotAngle = 40f;

        [Tooltip("Optional light cookie for rectangular fluorescent look.")]
        [SerializeField] Texture lightCookie;

        [Tooltip("Intensity of the soft glow on the ceiling around each fixture. 0 = no ceiling glow.")]
        [Range(0f, 5f)]
        [SerializeField] float ceilingGlowIntensity = 0.6f;

        [Tooltip("Range of the ceiling glow point light.")]
        [SerializeField] float ceilingGlowRange = 2f;

        [Header("Light Fixture Visual")]
        [Tooltip("Length of the fluorescent fixture panel.")]
        [SerializeField] float fixtureLength = 1.2f;

        [Tooltip("Width of the fluorescent fixture panel.")]
        [SerializeField] float fixtureWidth = 0.3f;

        [Tooltip("Thickness/depth of the fixture box. Increase for more 3D look.")]
        [SerializeField] float fixtureDepth = 0.05f;

        [Tooltip("Minimum distance from room edge before a light can spawn. Prevents lights clipping into walls.")]
        [SerializeField] float wallMargin = 1.5f;

        [Header("Flicker")]
        [Tooltip("Enable flickering on this floor. Turn on for creepy levels.")]
        [SerializeField] bool enableFlicker = false;

        [Tooltip("How strong the constant buzz is. 0 = steady, 1 = wild.")]
        [Range(0f, 1f)]
        [SerializeField] float flickerStrength = 0.08f;

        [Tooltip("Speed of the buzz oscillation.")]
        [SerializeField] float flickerSpeed = 30f;

        [Tooltip("Chance per second of a brief brownout dip.")]
        [Range(0f, 1f)]
        [SerializeField] float dipChance = 0.03f;

        [Tooltip("How deep a dip goes. 0.5 = half brightness.")]
        [Range(0f, 1f)]
        [SerializeField] float dipDepth = 0.5f;

        [Tooltip("Extra multiplier on spot light flicker so floor pool visibly dims.")]
        [Range(1f, 3f)]
        [SerializeField] float lightFlickerMultiplier = 1.8f;

        [Tooltip("How much each light's flicker values randomly vary from the master settings. 0 = all identical, 1 = very different.")]
        [Range(0f, 1f)]
        [SerializeField] float flickerRandomness = 0.4f;

        [Tooltip("Layer name for the floor. Spot lights will ONLY illuminate this layer so walls stay clean.")]
        [SerializeField] string floorLayerName = "Default";

        private GameObject _ceilingRoot;

        private void Start()
        {
            BuildCeiling();
        }

        public void BuildCeiling()
        {
            if (_ceilingRoot != null)
                Destroy(_ceilingRoot);

            _ceilingRoot = new GameObject("Ceiling");
            _ceilingRoot.transform.SetParent(transform, false);

            Bounds roomBounds = ComputeRoomBounds();
            Debug.Log($"[OfficeCeiling] Room bounds center={roomBounds.center} size={roomBounds.size} | Ceiling Y={transform.position.y + ceilingHeight}");
            CreateCeilingQuad(roomBounds);
            CreateLightGrid(roomBounds);
        }

        /// <summary>
        /// Computes the combined bounds of all SpawnZones in this room.
        /// Falls back to a default size if none are found.
        /// </summary>
        private Bounds ComputeRoomBounds()
        {
            // 1) Manual override — use exact size you set in Inspector
            if (manualSize.x > 0f && manualSize.y > 0f)
            {
                Vector3 center = transform.position + manualCenterOffset;
                return new Bounds(center, new Vector3(manualSize.x, 0f, manualSize.y));
            }

            // 2) Scan all renderers in children for true full-floor bounds
            if (useRendererBounds)
            {
                Renderer[] renderers = GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Bounds combined = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++)
                        combined.Encapsulate(renderers[i].bounds);
                    return combined;
                }
            }

            // 3) Fallback: SpawnZones
            SpawnZone[] zones = GetComponentsInChildren<SpawnZone>();
            if (zones.Length == 0)
                return new Bounds(transform.position, new Vector3(10f, 0f, 10f));

            Bounds zoneBounds = zones[0].GetBounds();
            for (int i = 1; i < zones.Length; i++)
                zoneBounds.Encapsulate(zones[i].GetBounds());

            return zoneBounds;
        }

        private void CreateCeilingQuad(Bounds roomBounds)
        {
            Vector3 center = roomBounds.center;
            center.y = transform.position.y + ceilingHeight;
            Vector3 scale = new Vector3(
                roomBounds.size.x + ceilingPadding * 2f,
                roomBounds.size.z + ceilingPadding * 2f,
                1f);

            // Single quad facing down with a subtle base emission so it's not pitch black
            Material mat = BuildCeilingMaterial(roomBounds);

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "CeilingPanel";
            quad.transform.SetParent(_ceilingRoot.transform, false);
            quad.transform.position = center;
            quad.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            quad.transform.localScale = scale;

            Collider col = quad.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Renderer rend = quad.GetComponent<Renderer>();
            rend.material = mat;
        }

        private Material BuildCeilingMaterial(Bounds roomBounds)
        {
            if (ceilingMaterial != null)
                return ceilingMaterial;

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            Material mat = new Material(litShader);
            mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);

            // Add subtle emission so the ceiling is visible even without direct light
            mat.EnableKeyword("_EMISSION");
            Color baseCol = ceilingColor;

            if (ceilingTexture != null)
            {
                mat.mainTexture = ceilingTexture;
                mat.SetColor("_BaseColor", Color.white);
                float tilingX = roomBounds.size.x * ceilingTilesPerUnit;
                float tilingZ = roomBounds.size.z * ceilingTilesPerUnit;
                mat.mainTextureScale = new Vector2(tilingX, tilingZ);
                // Dim emission based on ceiling color
                mat.SetColor("_EmissionColor", baseCol * ceilingGlowIntensity * 0.3f);
            }
            else
            {
                mat.SetColor("_BaseColor", baseCol);
                mat.color = baseCol;
                mat.SetColor("_EmissionColor", baseCol * ceilingGlowIntensity * 0.3f);
            }

            return mat;
        }

        private void CreateLightGrid(Bounds roomBounds)
        {
            // Inset the grid by wallMargin so lights never clip into walls
            float minX = roomBounds.min.x + wallMargin;
            float maxX = roomBounds.max.x - wallMargin;
            float minZ = roomBounds.min.z + wallMargin;
            float maxZ = roomBounds.max.z - wallMargin;

            if (minX >= maxX || minZ >= maxZ) return; // room too small for lights

            // Center the grid within the inset area
            float areaX = maxX - minX;
            float areaZ = maxZ - minZ;
            int countX = Mathf.Max(1, Mathf.FloorToInt(areaX / lightSpacing) + 1);
            int countZ = Mathf.Max(1, Mathf.FloorToInt(areaZ / lightSpacing) + 1);
            float stepX = countX > 1 ? areaX / (countX - 1) : 0f;
            float stepZ = countZ > 1 ? areaZ / (countZ - 1) : 0f;

            float y = transform.position.y + ceilingHeight - fixtureDepth * 0.5f;

            int index = 0;
            for (int ix = 0; ix < countX; ix++)
            {
                float x = countX > 1 ? minX + stepX * ix : (minX + maxX) * 0.5f;
                for (int iz = 0; iz < countZ; iz++)
                {
                    float z = countZ > 1 ? minZ + stepZ * iz : (minZ + maxZ) * 0.5f;
                    CreateLightFixture(new Vector3(x, y, z), index);
                    index++;
                }
            }
        }

        private void CreateLightFixture(Vector3 position, int index)
        {
            // --- Fixture housing (thin 3D box for slight depth) ---
            GameObject fixture = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fixture.name = $"LightFixture_{index}";
            fixture.transform.SetParent(_ceilingRoot.transform, false);
            fixture.transform.position = position;
            fixture.transform.localScale = new Vector3(fixtureLength, fixtureDepth, fixtureWidth);

            Collider col = fixture.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Don't cast shadows
            Renderer rend = fixture.GetComponent<Renderer>();
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;

            // Emissive material (URP compatible) — the glowing panel face
            Material emissive;
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit != null)
            {
                emissive = new Material(urpLit);
                emissive.SetColor("_BaseColor", lightColor);
                emissive.EnableKeyword("_EMISSION");
                emissive.SetColor("_EmissionColor", lightColor * lightIntensity * 2f);
                emissive.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                emissive = new Material(Shader.Find("Standard"));
                emissive.color = lightColor;
                emissive.EnableKeyword("_EMISSION");
                emissive.SetColor("_EmissionColor", lightColor * lightIntensity);
            }
            rend.material = emissive;

            // --- Flicker / buzz effect (only if enabled for this floor) ---
            if (enableFlicker)
            {
                var flicker = fixture.AddComponent<FluorescentFlicker>();
                // Randomize each light's values around the master settings
                float r = flickerRandomness;
                flicker.SetValues(
                    Mathf.Clamp01(flickerStrength + Random.Range(-r * 0.1f, r * 0.1f)),
                    Mathf.Max(1f, flickerSpeed + Random.Range(-r * 15f, r * 15f)),
                    Mathf.Clamp01(dipChance + Random.Range(-r * 0.02f, r * 0.04f)),
                    Mathf.Clamp01(dipDepth + Random.Range(-r * 0.2f, r * 0.2f)),
                    Mathf.Max(1f, lightFlickerMultiplier + Random.Range(-r * 0.5f, r * 0.5f))
                );
            }

            // --- Spot light pointing down ---
            GameObject lightObj = new GameObject($"Light_{index}");
            lightObj.transform.SetParent(fixture.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, -0.03f, 0f);
            lightObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            Light spotLight = lightObj.AddComponent<Light>();
            spotLight.type = LightType.Spot;
            spotLight.color = lightColor;
            spotLight.intensity = lightIntensity;
            spotLight.range = lightRange;
            spotLight.spotAngle = spotAngle;
            spotLight.innerSpotAngle = innerSpotAngle;
            spotLight.shadows = LightShadows.None;

            // Only light the floor layer so walls stay clean
            int floorLayer = LayerMask.NameToLayer(floorLayerName);
            if (floorLayer >= 0)
                spotLight.cullingMask = 1 << floorLayer;

            if (lightCookie != null)
                spotLight.cookie = lightCookie;

            // --- Ceiling glow halo around fixture ---
            if (ceilingGlowIntensity > 0f)
            {
                CreateCeilingGlowHalo(fixture.transform, position, index);
            }
        }

        private void CreateCeilingGlowHalo(Transform parent, Vector3 fixturePos, int index)
        {
            // Emissive quad sitting just below the ceiling, larger than the fixture,
            // faking the light halo you'd see around a fluorescent panel
            GameObject halo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            halo.name = $"CeilingGlow_{index}";
            halo.transform.SetParent(_ceilingRoot.transform, false);

            // Position just barely below the ceiling so it doesn't z-fight
            Vector3 pos = fixturePos;
            pos.y += 0.01f;
            halo.transform.position = pos;
            halo.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

            // Glow area is bigger than the fixture
            float glowSize = ceilingGlowRange;
            halo.transform.localScale = new Vector3(
                fixtureLength + glowSize * 2f,
                fixtureWidth + glowSize * 2f,
                1f);

            Collider col = halo.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Renderer rend = halo.GetComponent<Renderer>();
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;

            // Semi-transparent emissive material for the glow
            Shader urpLit = Shader.Find("Universal Render Pipeline/Unlit");
            if (urpLit == null) urpLit = Shader.Find("Unlit/Color");
            Material glowMat = new Material(urpLit);

            // Transparent blend so it fades at edges
            glowMat.SetInt("_Surface", 1); // Transparent
            glowMat.SetInt("_Blend", 0);   // Alpha
            glowMat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            glowMat.SetInt("_ZWrite", 0);
            glowMat.SetFloat("_DstBlend", 10); // OneMinusSrcAlpha
            glowMat.SetFloat("_SrcBlend", 5);  // SrcAlpha
            glowMat.renderQueue = 3000;
            glowMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            glowMat.EnableKeyword("_ALPHAPREMULTIPLY_ON");

            Color glowColor = lightColor;
            glowColor.a = Mathf.Clamp01(ceilingGlowIntensity * 0.4f);
            glowMat.color = glowColor;
            if (glowMat.HasProperty("_BaseColor"))
                glowMat.SetColor("_BaseColor", glowColor);

            rend.material = glowMat;
        }
    }
}
