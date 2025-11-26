using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Controls multiple orbit objects that oscillate along meridians (lines of longitude) on a sphere.
/// Attach this script to the centerpoint object.
///
/// Coordinate system:
/// - Centerpoint is the center of the sphere
/// - +Y is North Pole, -Y is South Pole
/// - Orbits move along meridians (latitude changes, longitude stays fixed)
/// </summary>
public class OrbitOscillator : MonoBehaviour
{
    [System.Serializable]
    public class OrbitSettings
    {
        [Header("Orbit Reference")]
        [Tooltip("The orbit object to control")]
        public Transform orbitTransform;

        [Header("Oscillation Settings")]
        [Tooltip("Oscillation amplitude in degrees (latitude oscillation)")]
        [Range(0f, 90f)]
        public float amplitude = 30f;

        [Tooltip("Oscillation frequency (cycles per second)")]
        [Range(0.01f, 10f)]
        public float frequency = 1f;

        [Tooltip("Phase offset in degrees (0-360)")]
        [Range(0f, 360f)]
        public float phaseOffset = 0f;

        [Header("Runtime Info (Read Only)")]
        [SerializeField] private float radius;
        [SerializeField] private float initialLongitude; // Fixed longitude (経度)
        [SerializeField] private float initialLatitude;  // Initial latitude (緯度)

        public float Radius => radius;
        public float InitialLongitude => initialLongitude;
        public float InitialLatitude => initialLatitude;

        /// <summary>
        /// Initialize orbit parameters based on editor placement
        /// Uses centerpoint's local coordinate system:
        /// - centerpoint.up (+Y local) = North Pole
        /// - centerpoint.down (-Y local) = South Pole
        /// </summary>
        public void Initialize(Transform centerPoint)
        {
            if (orbitTransform == null || centerPoint == null)
                return;

            // Calculate radius from editor distance
            Vector3 toOrbitWorld = orbitTransform.position - centerPoint.position;
            radius = toOrbitWorld.magnitude;

            if (radius < 0.001f)
            {
                Debug.LogWarning($"[OrbitOscillator] {orbitTransform.name} is too close to center point!");
                return;
            }

            // Convert to centerpoint's local space
            Vector3 toOrbitLocal = centerPoint.InverseTransformDirection(toOrbitWorld);

            // Convert to spherical coordinates in local space
            // Longitude (経度): angle on local XZ plane from +X axis
            initialLongitude = Mathf.Atan2(toOrbitLocal.z, toOrbitLocal.x) * Mathf.Rad2Deg;

            // Latitude (緯度): angle from equator (local XZ plane), +90 = North Pole, -90 = South Pole
            float horizontalDistance = Mathf.Sqrt(toOrbitLocal.x * toOrbitLocal.x + toOrbitLocal.z * toOrbitLocal.z);
            initialLatitude = Mathf.Atan2(toOrbitLocal.y, horizontalDistance) * Mathf.Rad2Deg;

            Debug.Log($"[OrbitOscillator] Initialized {orbitTransform.name}: radius={radius:F2}, longitude={initialLongitude:F1}°, latitude={initialLatitude:F1}°");
        }

        /// <summary>
        /// Calculate current position based on time
        /// Oscillates along meridian from equator (latitude 0)
        /// Uses centerpoint's local coordinate system
        /// </summary>
        public Vector3 CalculatePosition(Transform centerPoint, float time)
        {
            if (orbitTransform == null || centerPoint == null || radius < 0.001f)
                return orbitTransform != null ? orbitTransform.position : Vector3.zero;

            // Calculate latitude oscillation from equator (latitude 0)
            float oscillation = amplitude * Mathf.Sin(2f * Mathf.PI * frequency * time + phaseOffset * Mathf.Deg2Rad);

            // Current latitude = oscillation from equator (0)
            // Clamp to valid range (-90 to +90)
            float currentLatitude = Mathf.Clamp(oscillation, -90f, 90f);

            // Convert spherical to Cartesian coordinates (in local space)
            float latRad = currentLatitude * Mathf.Deg2Rad;
            float lonRad = initialLongitude * Mathf.Deg2Rad;

            // Local Y = radius * sin(latitude)  -- height from equator
            // horizontal distance = radius * cos(latitude)
            // Local X = horizontal * cos(longitude)
            // Local Z = horizontal * sin(longitude)
            float localY = radius * Mathf.Sin(latRad);
            float horizontalRadius = radius * Mathf.Cos(latRad);
            float localX = horizontalRadius * Mathf.Cos(lonRad);
            float localZ = horizontalRadius * Mathf.Sin(lonRad);

            // Convert local offset to world space using centerpoint's rotation
            Vector3 localOffset = new Vector3(localX, localY, localZ);
            Vector3 worldOffset = centerPoint.TransformDirection(localOffset);

            return centerPoint.position + worldOffset;
        }
    }

    [Header("Orbit Objects")]
    [Tooltip("List of orbit objects with individual settings")]
    [SerializeField] private List<OrbitSettings> orbits = new List<OrbitSettings>();

    [Header("Global Settings")]
    [Tooltip("Enable/disable all oscillations")]
    [SerializeField] private bool isActive = true;

    [Tooltip("Global time scale for all orbits")]
    [Range(0.1f, 5f)]
    [SerializeField] private float timeScale = 1f;

    [Header("Auto-Detection")]
    [Tooltip("Automatically find child objects named 'orbit*'")]
    [SerializeField] private bool autoDetectOrbits = false;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    private float elapsedTime = 0f;
    private bool isInitialized = false;

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        if (autoDetectOrbits)
        {
            AutoDetectOrbitObjects();
        }

        // Initialize each orbit with its editor-placed position
        foreach (var orbit in orbits)
        {
            orbit.Initialize(transform);
        }

        isInitialized = true;
        Debug.Log($"[OrbitOscillator] Initialized with {orbits.Count} orbits");
    }

    void AutoDetectOrbitObjects()
    {
        // Find all child objects or objects named "orbit*"
        var foundOrbits = new List<Transform>();

        // Search in children
        foreach (Transform child in transform)
        {
            if (child.name.ToLower().StartsWith("orbit"))
            {
                foundOrbits.Add(child);
            }
        }

        // Search in scene for objects starting with "orbit"
        var allObjects = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        foreach (var obj in allObjects)
        {
            if (obj.name.ToLower().StartsWith("orbit") && !foundOrbits.Contains(obj))
            {
                foundOrbits.Add(obj);
            }
        }

        // Add found orbits that aren't already in the list
        foreach (var orbitTransform in foundOrbits)
        {
            bool alreadyExists = false;
            foreach (var existing in orbits)
            {
                if (existing.orbitTransform == orbitTransform)
                {
                    alreadyExists = true;
                    break;
                }
            }

            if (!alreadyExists)
            {
                var newOrbit = new OrbitSettings
                {
                    orbitTransform = orbitTransform,
                    amplitude = 45f,
                    frequency = 1f,
                    phaseOffset = 0f
                };
                orbits.Add(newOrbit);
                Debug.Log($"[OrbitOscillator] Auto-detected orbit: {orbitTransform.name}");
            }
        }
    }

    void Update()
    {
        if (!isActive || !isInitialized)
            return;

        elapsedTime += Time.deltaTime * timeScale;

        // Update each orbit position
        foreach (var orbit in orbits)
        {
            if (orbit.orbitTransform == null)
                continue;

            Vector3 newPosition = orbit.CalculatePosition(transform, elapsedTime);
            orbit.orbitTransform.position = newPosition;
        }
    }

    void OnDrawGizmos()
    {
        if (!showDebugGizmos)
            return;

        // Draw center point
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.1f);

        // Draw orbit paths
        foreach (var orbit in orbits)
        {
            if (orbit.orbitTransform == null)
                continue;

            float radius = orbit.Radius;
            float longitude = orbit.InitialLongitude;
            float latitude = orbit.InitialLatitude;

            if (radius <= 0)
            {
                // Not initialized yet, calculate from current position in local space
                Vector3 toOrbitWorld = orbit.orbitTransform.position - transform.position;
                radius = toOrbitWorld.magnitude;
                Vector3 toOrbitLocal = transform.InverseTransformDirection(toOrbitWorld);
                longitude = Mathf.Atan2(toOrbitLocal.z, toOrbitLocal.x) * Mathf.Rad2Deg;
                float horizontalDist = Mathf.Sqrt(toOrbitLocal.x * toOrbitLocal.x + toOrbitLocal.z * toOrbitLocal.z);
                latitude = Mathf.Atan2(toOrbitLocal.y, horizontalDist) * Mathf.Rad2Deg;
            }

            // Draw sphere wireframe (equator)
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.3f);
            DrawLatitudeCircle(radius, 0f); // Equator

            // Draw meridian for this orbit (the path it oscillates along)
            Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.6f);
            DrawMeridian(radius, longitude);

            // Draw oscillation range on meridian (from equator)
            Gizmos.color = Color.green;
            float minLat = Mathf.Clamp(-orbit.amplitude, -90f, 90f);
            float maxLat = Mathf.Clamp(orbit.amplitude, -90f, 90f);

            Vector3 minPos = SphericalToWorld(radius, longitude, minLat);
            Vector3 maxPos = SphericalToWorld(radius, longitude, maxLat);
            Vector3 equatorPos = SphericalToWorld(radius, longitude, 0f);

            // Draw equator point (oscillation center)
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(equatorPos, 0.04f);

            Gizmos.DrawWireSphere(minPos, 0.05f);
            Gizmos.DrawWireSphere(maxPos, 0.05f);
            Gizmos.DrawLine(transform.position, minPos);
            Gizmos.DrawLine(transform.position, maxPos);

            // Draw arc between min and max latitude
            Gizmos.color = Color.yellow;
            DrawMeridianArc(radius, longitude, minLat, maxLat);

            // Draw current orbit position
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(orbit.orbitTransform.position, 0.08f);

            // Draw line from center to orbit
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, orbit.orbitTransform.position);
        }
    }

    /// <summary>
    /// Convert spherical coordinates to Cartesian in local space
    /// </summary>
    Vector3 SphericalToCartesianLocal(float radius, float longitudeDeg, float latitudeDeg)
    {
        float latRad = latitudeDeg * Mathf.Deg2Rad;
        float lonRad = longitudeDeg * Mathf.Deg2Rad;

        float y = radius * Mathf.Sin(latRad);
        float horizontalRadius = radius * Mathf.Cos(latRad);
        float x = horizontalRadius * Mathf.Cos(lonRad);
        float z = horizontalRadius * Mathf.Sin(lonRad);

        return new Vector3(x, y, z);
    }

    /// <summary>
    /// Convert spherical coordinates to world position using centerpoint's rotation
    /// </summary>
    Vector3 SphericalToWorld(float radius, float longitudeDeg, float latitudeDeg)
    {
        Vector3 localPos = SphericalToCartesianLocal(radius, longitudeDeg, latitudeDeg);
        return transform.position + transform.TransformDirection(localPos);
    }

    /// <summary>
    /// Draw a latitude circle (parallel) in centerpoint's local space
    /// </summary>
    void DrawLatitudeCircle(float sphereRadius, float latitudeDeg, int segments = 64)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = SphericalToWorld(sphereRadius, 0f, latitudeDeg);

        for (int i = 1; i <= segments; i++)
        {
            float lon = i * angleStep;
            Vector3 newPoint = SphericalToWorld(sphereRadius, lon, latitudeDeg);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }

    /// <summary>
    /// Draw a full meridian (line of longitude)
    /// </summary>
    void DrawMeridian(float radius, float longitudeDeg, int segments = 64)
    {
        DrawMeridianArc(radius, longitudeDeg, -90f, 90f, segments);
    }

    /// <summary>
    /// Draw an arc along a meridian between two latitudes
    /// </summary>
    void DrawMeridianArc(float radius, float longitudeDeg, float minLat, float maxLat, int segments = 32)
    {
        float latStep = (maxLat - minLat) / segments;
        Vector3 prevPoint = SphericalToWorld(radius, longitudeDeg, minLat);

        for (int i = 1; i <= segments; i++)
        {
            float lat = minLat + i * latStep;
            Vector3 newPoint = SphericalToWorld(radius, longitudeDeg, lat);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }

    #region Public API

    /// <summary>
    /// Add a new orbit at runtime
    /// </summary>
    public void AddOrbit(Transform orbitTransform, float amplitude = 45f, float frequency = 1f, float phaseOffset = 0f)
    {
        var newOrbit = new OrbitSettings
        {
            orbitTransform = orbitTransform,
            amplitude = amplitude,
            frequency = frequency,
            phaseOffset = phaseOffset
        };
        newOrbit.Initialize(transform);
        orbits.Add(newOrbit);
    }

    /// <summary>
    /// Remove an orbit
    /// </summary>
    public void RemoveOrbit(Transform orbitTransform)
    {
        orbits.RemoveAll(o => o.orbitTransform == orbitTransform);
    }

    /// <summary>
    /// Set active state
    /// </summary>
    public void SetActive(bool active)
    {
        isActive = active;
    }

    /// <summary>
    /// Reset elapsed time
    /// </summary>
    public void ResetTime()
    {
        elapsedTime = 0f;
    }

    /// <summary>
    /// Get orbit settings by index
    /// </summary>
    public OrbitSettings GetOrbitSettings(int index)
    {
        if (index >= 0 && index < orbits.Count)
            return orbits[index];
        return null;
    }

    /// <summary>
    /// Get orbit settings by transform
    /// </summary>
    public OrbitSettings GetOrbitSettings(Transform orbitTransform)
    {
        return orbits.Find(o => o.orbitTransform == orbitTransform);
    }

    /// <summary>
    /// Get total orbit count
    /// </summary>
    public int OrbitCount => orbits.Count;

    #endregion
}
