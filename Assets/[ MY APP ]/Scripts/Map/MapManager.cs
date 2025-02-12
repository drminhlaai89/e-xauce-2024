using exauce;
using Michsky.UI.ModernUIPack;
using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon.StructWrapping;
using System.Collections;
using System.Globalization;
using UnityEngine.UIElements;

public class MapManager : MonoBehaviour
{
    static MapManager m_instance;
    public static MapManager Instance { get => m_instance; }

    [Header("[ ZOOM ]")]
    [SerializeField] int m_minZoom = 3;
    [SerializeField] int m_maxZoom = 20;

    [Header("[ ZOOM SETTINGS ]")]
    [SerializeField] private float m_zoomToGroupLevel = 11f;
    [SerializeField] private float m_zoomAnimationDuration = 0.5f;

    [Header("[ MARKERS ]")]
    [SerializeField] GameObject m_markerPrefab;
    public GameObject MarkerPrefab { get => m_markerPrefab; }

    [SerializeField] Transform m_markerContent;
    public Transform MarkerContent { get => m_markerContent; }

    [SerializeField] Camera m_mapCamera;

    OnlineMaps m_map;

    OnlineMapsTileSetControl m_control;
    OnlineMapsRawImageTouchForwarder m_forwarder;

    [Header("[ MARKER GROUPING ]")]
    [SerializeField] private Texture2D m_groupTexture;
    [SerializeField] private Texture2D m_groupFont;
    [SerializeField] private float m_groupDistance = 60f / OnlineMapsUtils.tileSize;

    private List<OnlineMapsMarker> m_markers = new List<OnlineMapsMarker>();

    private List<MarkerGroup> m_currentGroups;

    private void Awake()
    {
        m_instance = this;
    }

    void Start()
    {
        m_map = OnlineMaps.instance;
        m_control = OnlineMapsTileSetControl.instance;
        m_forwarder = FindAnyObjectByType<OnlineMapsRawImageTouchForwarder>();

        // Subscribe to zoom changes
        m_map.OnChangeZoom += () => GroupMarkers();
    }

    // Method to zoom in
    public void ZoomIn()
    {
        if (m_map.zoom < m_maxZoom)
        {
            m_map.zoom++;
        }
    }

    // Method to zoom out
    public void ZoomOut()
    {
        if (m_map.zoom > m_minZoom)
        {
            m_map.zoom--;
        }
    }

    public void CreateMarker(string description, FileInfo fileInfo)
    {
        ExtractCoordinate(description, fileInfo);
    }

    void OnMarkerClick(OnlineMapsMarkerBase markerBase)
    {
        OnlineMapsMarker onlineMarker = markerBase as OnlineMapsMarker;
        if (onlineMarker != null)
        {
            // Retrieve your custom data
            FileInfo fileInfo = onlineMarker["data"] as FileInfo;

            //Lấy event default của MainMenu EventHandler.
            EventHandler mainMenuEventHandler = UIManager.Instance.MainMenuUI.GetComponent<EventHandler>();
            UnityEvent mainMenuEvent = mainMenuEventHandler.onEnable;

            //Cập nhật lại event.
            UnityAction playVideoEvent = () =>
            {
                //Xóa event hiện tại.
                mainMenuEventHandler.onEnable.RemoveAllListeners();
                //Thêm lại event default vào.
                mainMenuEventHandler.onEnable = mainMenuEvent;
                //Thêm event PlayTabAnimation để sau khi thoát khỏi chế độ play video thì mở đúng tab trước khi play video được chạy.
                MobileManager.Instance.Tabs.ForEach(tab =>
                {
                    if (tab.activeInHierarchy)
                        mainMenuEventHandler.onEnable.AddListener(() => AnimationManager.Instance.PlayTabAnimation(MobileManager.Instance.Tabs.IndexOf(tab)));
                });

                VideoManager.Instance.ApplyVideoToPlay(fileInfo.FileStatus.location + ".mp4", fileInfo.IsPrivate);
            };

            playVideoEvent.Invoke();
        }

    }

    IEnumerator AdjustBackgroundSize(GameObject markerObj, TextMeshProUGUI titleText, RectTransform bgRect, float horizontalPadding, float verticalPadding)
    {
        // Wait until we get valid text dimensions
        int maxAttempts = 20;  // Increased from 10 to give more chances
        int attempts = 0;
        
        // Initial delay to let the text component initialize
        yield return new WaitForSeconds(0.1f);
        
        while (attempts < maxAttempts)
        {
            // Force layout and text updates
            titleText.ForceMeshUpdate();
            LayoutRebuilder.ForceRebuildLayoutImmediate(titleText.rectTransform);
            
            float textWidth = titleText.preferredWidth;
            float textHeight = titleText.preferredHeight;
            
            // Log every attempt for debugging
            Debug.Log($"Attempt {attempts + 1} for {markerObj.name}: Width={textWidth}, Height={textHeight}");
            
            // Check if we have valid dimensions
            if (textWidth > 0 && textHeight > 0)
            {
                Vector2 preferredSize = new Vector2(textWidth, textHeight);
                Vector2 newSize = new Vector2(preferredSize.x + horizontalPadding, preferredSize.y + verticalPadding);
                bgRect.sizeDelta = newSize;
                
                // Verify the size was actually set
                if (Mathf.Approximately(bgRect.sizeDelta.x, newSize.x) && 
                    Mathf.Approximately(bgRect.sizeDelta.y, newSize.y))
                {
                    Debug.Log($"Successfully sized marker '{markerObj.name}' to {bgRect.sizeDelta}");
                    yield break;
                }
            }
            
            attempts++;
            yield return new WaitForSeconds(0.05f); // Wait a bit longer between attempts
        }
        
        // If we get here, we failed to get valid dimensions
        Debug.LogError($"Failed to size marker after {maxAttempts} attempts: {markerObj.name}, " +
                      $"Text: '{titleText.text}', " +
                      $"Final dimensions: {titleText.preferredWidth}x{titleText.preferredHeight}, " +
                      $"Background size: {bgRect.sizeDelta}");
    }

    // Use Regex to extract Longitude and Latitude from the description
    private void ExtractCoordinate(string description, FileInfo fileInfo)
    {
        string videoName = fileInfo.VideoName.text;

        string longitudePattern = @"Longtitude:\s*([-+]?\d*\.\d+|\d+)";
        string latitudePattern = @"Latitude:\s*([-+]?\d*\.\d+|\d+)";

        var longitudeMatch = Regex.Match(description, longitudePattern, RegexOptions.IgnoreCase);
        var latitudeMatch = Regex.Match(description, latitudePattern, RegexOptions.IgnoreCase);

        if (longitudeMatch.Success && latitudeMatch.Success)
        {
            if (float.TryParse(longitudeMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float longitude) &&
                float.TryParse(latitudeMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float latitude))
            {
                // Add position debugging
                Debug.Log($"Creating marker at Lat: {latitude}, Lng: {longitude}");

                // Instantiate your prefab as before
                GameObject markerObj = Instantiate(m_markerPrefab, m_markerContent);
                markerObj.name = "Marker_" + videoName;

                TextMeshProUGUI titleText = markerObj.GetComponentInChildren<TextMeshProUGUI>();
                titleText.text = System.IO.Path.GetFileNameWithoutExtension(videoName);

                // Find your background
                RectTransform bgRect = markerObj.transform.Find("Background").GetComponent<RectTransform>();
                float horizontalPadding = 20f;
                float verticalPadding = 10f;

                // Start the coroutine to adjust the background size
                StartCoroutine(AdjustBackgroundSize(markerObj, titleText, bgRect, horizontalPadding, verticalPadding));

                // Create an OnlineMapsMarker
                OnlineMapsMarker onlineMarker = OnlineMapsMarkerManager.CreateItem(longitude, latitude);

                // Make the 2D marker texture fully transparent
                onlineMarker.texture = CreateTransparentTexture(32, 32);

                // Store your custom data in the marker
                onlineMarker["data"] = fileInfo;

                // Subscribe to the marker's OnClick event
                onlineMarker.OnClick += OnMarkerClick;

                m_map.OnMapUpdated += () => UpdateMarker(markerObj, onlineMarker, longitude, latitude);
                OnlineMapsCameraOrbit.instance.OnCameraControl += () => UpdateMarker(markerObj, onlineMarker, longitude, latitude);

                // Add this line after creating the online marker
                m_markers.Add(onlineMarker);

                Debug.Log($"Created marker for '{videoName}' at ({latitude}, {longitude}).");

                // Verify marker position after creation
                Debug.Log($"Marker position after creation: {onlineMarker.position}");

                // Call GroupMarkers after adding the new marker
                GroupMarkers();
            }
            else
            {
                Debug.LogError("Failed to parse longitude and latitude values.");
            }
        }
    }

    // Helper function to create a transparent texture
    private Texture2D CreateTransparentTexture(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
        Color32[] pixels = new Color32[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color32(0, 0, 0, 0); // Fully transparent
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    void UpdateMarker(GameObject markerObj, OnlineMapsMarker onlineMarker, float longitude, float latitude)
    {
        // If marker is currently disabled (due to clustering), hide it and return immediately
        if (!onlineMarker.enabled)
        {
            markerObj.SetActive(false);
            return;
        }

        // Otherwise, proceed with your normal position and visibility logic
        Vector2 screenPosition = m_control.GetScreenPosition(longitude, latitude);

        if (m_forwarder != null)
        {
            if (!m_map.InMapView(longitude, latitude))
            {
                markerObj.SetActive(false);
                return;
            }

            screenPosition = m_forwarder.MapToForwarderSpace(screenPosition);
        }

        if (screenPosition.x < 0 || screenPosition.x > Screen.width ||
            screenPosition.y < 0 || screenPosition.y > Screen.height)
        {
            markerObj.SetActive(false);
            return;
        }

        // Now we are sure it's enabled and on screen
        if (!markerObj.activeSelf) markerObj.SetActive(true);

        // Calculate the local position inside the parent RectTransform
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            markerObj.transform.parent as RectTransform,
            screenPosition,
            m_mapCamera,
            out Vector2 point);

        markerObj.transform.localPosition = point;
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Convert to radians
        lat1 = lat1 * Math.PI / 180.0;
        lon1 = lon1 * Math.PI / 180.0;
        lat2 = lat2 * Math.PI / 180.0;
        lon2 = lon2 * Math.PI / 180.0;

        // Haversine formula
        double dlon = lon2 - lon1;
        double dlat = lat2 - lat1;
        double a = Math.Pow(Math.Sin(dlat / 2), 2) +
                   Math.Cos(lat1) * Math.Cos(lat2) *
                   Math.Pow(Math.Sin(dlon / 2), 2);
        double c = 2 * Math.Asin(Math.Sqrt(a));

        // Radius of earth in kilometers
        double r = 6371;

        // Calculate distance
        return c * r;
    }

    private void GroupMarkers()
    {
        // Remove old groups first
        if (m_currentGroups != null)
        {
            foreach (var group in m_currentGroups)
            {
                group.Remove();
            }
        }

        List<MarkerGroup> groups = new List<MarkerGroup>();

        // Show individual markers when zoomed in, show groups when zoomed out
        if (m_map.zoom >= 10)
        {
            // Enable all individual markers
            foreach (var marker in m_markers)
            {
                marker.enabled = true;
                GameObject markerObj = GameObject.Find("Marker_" + (marker["data"] as FileInfo).VideoName.text);
                if (markerObj != null)
                {
                    markerObj.SetActive(true);
                }
            }
        }
        else
        {
            // Create a list of markers to process
            List<OnlineMapsMarker> markersToGroup = new List<OnlineMapsMarker>(m_markers);

            for (int j = 0; j < markersToGroup.Count;)
            {
                OnlineMapsMarker marker = markersToGroup[j];
                List<OnlineMapsMarker> nearbyMarkers = new List<OnlineMapsMarker> { marker };

                // Get actual coordinates
                double lat1 = marker.position.y;
                double lng1 = marker.position.x;

                int k = j + 1;
                while (k < markersToGroup.Count)
                {
                    OnlineMapsMarker marker2 = markersToGroup[k];
                    double lat2 = marker2.position.y;
                    double lng2 = marker2.position.x;

                    // Calculate distance in kilometers
                    double distance = CalculateDistance(lat1, lng1, lat2, lng2);

                    // Convert to approximate screen distance at current zoom level
                    // At zoom level 0, one pixel represents roughly 156543.03392 meters at the equator
                    double pixelDistance = distance * 1000 * Math.Pow(2, m_map.zoom) / 156543.03392;

                    // If markers are closer than 50 pixels at current zoom level, group them
                    if (pixelDistance < 50)
                    {
                        nearbyMarkers.Add(marker2);
                        markersToGroup.RemoveAt(k);
                    }
                    else
                    {
                        k++;
                    }
                }

                if (nearbyMarkers.Count > 1)
                {
                    // Create a group for these markers
                    MarkerGroup group = new MarkerGroup(m_map, (int)m_map.zoom, m_groupTexture);
                    groups.Add(group);
                    foreach (var m in nearbyMarkers)
                    {
                        group.Add(m);
                    }
                    markersToGroup.RemoveAt(j);
                }
                else
                {
                    // Singleton marker, leave it enabled
                    OnlineMapsMarker singletonMarker = nearbyMarkers[0];
                    singletonMarker.enabled = true;
                    GameObject singletonObj = GameObject.Find("Marker_" + (singletonMarker["data"] as FileInfo).VideoName.text);
                    if (singletonObj != null)
                    {
                        singletonObj.SetActive(true);
                    }
                    markersToGroup.RemoveAt(j);
                }
            }
        }

        // Apply the Apply method only to groups with count >1
        foreach (MarkerGroup g in groups)
        {
            g.Apply(m_groupFont);
        }

        m_currentGroups = groups;
    }

    private class MarkerGroup
    {
        public OnlineMaps map;
        public List<OnlineMapsMarker> markers;
        public OnlineMapsMarker instance;

        public Vector2 center;
        public double tilePositionX, tilePositionY;
        public int zoom;

        private List<GameObject> markerObjects;

        public MarkerGroup(OnlineMaps map, int zoom, Texture2D texture)
        {
            this.map = map;
            markers = new List<OnlineMapsMarker>();
            markerObjects = new List<GameObject>();
            this.zoom = zoom;
            instance = OnlineMapsMarkerManager.instance.Create(Vector2.zero, texture);
            instance.align = OnlineMapsAlign.Center;
            instance.range = new OnlineMapsRange(zoom, zoom);

            float targetSize = 40f; // Desired size in pixels
            float scale = targetSize / Mathf.Max(texture.width, texture.height);
            instance.scale = scale;

            // Add click handler
            instance.OnClick += OnGroupClick;
        }

        public void Add(OnlineMapsMarker marker)
        {
            markers.Add(marker);

            GameObject markerObj = GameObject.Find("Marker_" + (marker["data"] as FileInfo).VideoName.text);
            if (markerObj != null)
            {
                markerObjects.Add(markerObj);
                markerObj.SetActive(false);
            }

            center = markers.Aggregate(Vector2.zero, (current, m) => current + m.position) / markers.Count;
            instance.position = center;
            map.projection.CoordinatesToTile(center.x, center.y, zoom, out tilePositionX, out tilePositionY);
            instance.label = markers.Count.ToString();

            marker.enabled = false;
        }

        public void Remove()
        {
            foreach (var marker in markers)
            {
                marker.enabled = true;
            }

            foreach (var obj in markerObjects)
            {
                if (obj != null)
                    obj.SetActive(true);
            }

            if (instance != null)
                OnlineMapsMarkerManager.instance.Remove(instance);
        }

        public void Apply(Texture2D font)
        {
            // 1) read the base icon
            int width = instance.texture.width;
            int height = instance.texture.height;
            Color[] baseColors = instance.texture.GetPixels();

            // 2) read the digits from the sprite sheet
            Color[] fontColors = font.GetPixels();
            int cw = font.width / 5;  // 5 columns (digits per row)
            int ch = font.height / 2; // 2 rows

            // 3) figure out where to center the text
            string countStr = markers.Count.ToString();
            char[] cText = countStr.ToCharArray();

            // Adjust spacing between digits (reduce this value to bring digits closer)
            float digitSpacing = 0.8f; // Was implicitly 1.0 before

            // Center calculation with adjusted spacing
            int sx = (int)(width / 2f - (cText.Length * digitSpacing * cw) / 2f);
            int sy = (int)(height / 2f - ch / 2f);

            // 4) blend each digit into the base icon
            for (int i = 0; i < cText.Length; i++)
            {
                // figure out which sub-rectangle in the font we need
                int digitIndex = cText[i] - '0' - 1;
                if (digitIndex < 0) digitIndex += 10; // handle zero

                int fx = (digitIndex % 5) * cw;       // which column
                int fy = (1 - digitIndex / 5) * ch;   // which row (top or bottom)

                // Calculate x position with adjusted spacing
                int adjustedX = sx + (int)(i * digitSpacing * cw);

                // blend the digit's pixels
                for (int x = 0; x < cw; x++)
                {
                    for (int y = 0; y < ch; y++)
                    {
                        int fontIndex = (fy + y) * font.width + (fx + x);
                        int baseIndex = (sy + y) * width + (adjustedX + x);

                        // safety check
                        if (fontIndex < 0 || fontIndex >= fontColors.Length) continue;
                        if (baseIndex < 0 || baseIndex >= baseColors.Length) continue;

                        Color fc = fontColors[fontIndex];
                        // alpha blend
                        baseColors[baseIndex] = Color.Lerp(baseColors[baseIndex],
                            new Color(fc.r, fc.g, fc.b, 1), fc.a);
                    }
                }
            }

            // 5) commit changes to a new texture & replace the marker icon
            Texture2D newTexture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            newTexture.SetPixels(baseColors);
            newTexture.Apply();

            instance.texture = newTexture;

            // 6) Optionally remove built-in label so you only see your blended text
            //instance.label = markers.Count.ToString();
        }

        private void OnGroupClick(OnlineMapsMarkerBase markerBase)
        {
            // Calculate the bounds of all markers in the group
            double minLat = double.MaxValue, maxLat = double.MinValue;
            double minLng = double.MaxValue, maxLng = double.MinValue;

            foreach (var marker in markers)
            {
                double lat = marker.position.y;
                double lng = marker.position.x;

                minLat = Math.Min(minLat, lat);
                maxLat = Math.Max(maxLat, lat);
                minLng = Math.Min(minLng, lng);
                maxLng = Math.Max(maxLng, lng);
            }

            // Add padding to the bounds (40% for better visibility)
            double latPadding = (maxLat - minLat) * 0.4;
            double lngPadding = (maxLng - minLng) * 0.4;

            minLat -= latPadding;
            maxLat += latPadding;
            minLng -= lngPadding;
            maxLng += lngPadding;

            // Calculate center point
            double centerLat = (minLat + maxLat) / 2;
            double centerLng = (minLng + maxLng) / 2;

            // Calculate appropriate zoom level based on the spread of markers
            double latSpread = maxLat - minLat;
            double lngSpread = maxLng - minLng;
            double maxSpread = Math.Max(latSpread, lngSpread);

            // Calculate zoom level that will show all markers
            float targetZoom = Mathf.Clamp(
                (float)(Math.Log(360 / maxSpread) / Math.Log(2)),
                10f, // Changed from 11.5f to 10.5f (slightly higher than grouping threshold)
                14f    // Maximum zoom level
            );

            // Start zoom animation
            MapManager.Instance.StartCoroutine(AnimateZoom(
                new Vector2((float)centerLng, (float)centerLat),
                targetZoom
            ));
        }

        private IEnumerator AnimateZoom(Vector2 targetPosition, float targetZoom)
        {
            float startTime = Time.time;
            float duration = MapManager.Instance.m_zoomAnimationDuration;
            Vector2 startPosition = map.position;
            float startZoom = map.zoom;

            while (Time.time - startTime < duration)
            {
                float t = (Time.time - startTime) / duration;

                // Use smooth step for more pleasing animation
                t = t * t * (3f - 2f * t);

                // Interpolate position and zoom
                map.position = Vector2.Lerp(startPosition, targetPosition, t);
                map.zoom = (int)Mathf.Lerp(startZoom, targetZoom, t);

                yield return null;
            }

            // Ensure we end up exactly at the target
            map.position = targetPosition;
            map.zoom = (int)targetZoom;
        }
    }
}