using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InfinityCode.OnlineMapsExamples;

public class MapZoomControl : MonoBehaviour
{
    public OnlineMaps map;          // Reference to the Online Maps component
    public int minZoom = 3;         // Minimum zoom level
    public int maxZoom = 20;        // Maximum zoom level

    // Method to zoom in
    public void ZoomIn()
    {
        if (map.zoom < maxZoom)
        {
            map.zoom++;
        }
    }

    // Method to zoom out
    public void ZoomOut()
    {
        if (map.zoom > minZoom)
        {
            map.zoom--;
        }
    }
}
