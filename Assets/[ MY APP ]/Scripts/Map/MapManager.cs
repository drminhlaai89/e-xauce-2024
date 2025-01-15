using exauce;
using Michsky.UI.ModernUIPack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using TMPro;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MapManager : MonoBehaviour
{
    static MapManager m_instance;
    public static MapManager Instance { get => m_instance; }

    [Header("[ ZOOM ]")]
    [SerializeField] int m_minZoom = 3;
    [SerializeField] int m_maxZoom = 20;

    [Header("[ MARKERS ]")]
    [SerializeField] GameObject m_markerPrefab;
    public GameObject MarkerPrefab { get => m_markerPrefab; }

    [SerializeField] Transform m_markerContent;
    public Transform MarkerContent { get => m_markerContent; }

    [SerializeField] Camera m_mapCamera;

    OnlineMaps m_map;

    OnlineMapsTileSetControl m_control;
    OnlineMapsRawImageTouchForwarder m_forwarder;

    private void Awake()
    {
        m_instance = this;
    }

    void Start()
    {
        m_map = OnlineMaps.instance;
        m_control = OnlineMapsTileSetControl.instance;
        m_forwarder = FindAnyObjectByType<OnlineMapsRawImageTouchForwarder>();
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
            if (float.TryParse(longitudeMatch.Groups[1].Value, out float longitude) &&
                float.TryParse(latitudeMatch.Groups[1].Value, out float latitude))
            {
                // Instantiate your prefab as before
                GameObject markerObj = Instantiate(m_markerPrefab, m_markerContent);
                markerObj.name = "Marker_" + videoName;

                markerObj.GetComponentInChildren<TextMeshProUGUI>().text = System.IO.Path.GetFileNameWithoutExtension(videoName);

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

                Debug.Log($"Created marker for '{videoName}' at ({latitude}, {longitude}).");
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
        double px = longitude;
        double py = latitude;

        Vector2 screenPosition = m_control.GetScreenPosition(px, py);

        if (m_forwarder != null)
        {
            if (!m_map.InMapView(px, py))
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

        if (!markerObj.activeSelf) markerObj.SetActive(true);

        Vector2 point;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(markerObj.transform.parent as RectTransform, screenPosition, m_mapCamera, out point);
        markerObj.transform.localPosition = point;
    }
}