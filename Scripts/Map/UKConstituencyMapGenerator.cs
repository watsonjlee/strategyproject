using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

public class UKConstituencyMapGenerator : MonoBehaviour
{
    [Header("Settings")]
    public string geoJsonFilePath = "Assets/UK_Constituencies.geojson";
    public float extrusionHeight = 0.1f;
    public float extrusionSeparation = 0.001f;
    public Material defaultMaterial;
    public Material highlightMaterial;
    public Material lineMaterial; // **NEW: Material for LineRenderer**
    public float lineWidth = 0.01f; // **NEW: Line width**
    public Transform mapContainer;

    [Header("Positioning")]
    [Tooltip("Scale factor to fit the map in Unity's space")]
    public float mapScale = 0.00001f;
    [Tooltip("Set to true to center the map at world origin")]
    public bool centerMap = true;

    [Header("Runtime")]
    public bool loadOnStart = false;
    public bool useStreamingAssets = false;

    [Header("Data Import")]
    public DataImporter dataImporter;

    private Dictionary<string, GameObject> constituencyObjects = new Dictionary<string, GameObject>();
    private GameObject selectedConstituency;
    private Bounds mapBounds;
    private Vector2 mapCentroid;
    private bool centroidCalculated = false;

    void Start()
    {
        if (loadOnStart)
        {
            if (useStreamingAssets)
            {
                geoJsonFilePath = Path.Combine(Application.streamingAssetsPath, "UK_Constituencies.geojson");
            }

            LoadGeoJsonFile();
        }
    }

    public void LoadGeoJsonFile()
    {
        StartCoroutine(LoadGeoJsonCoroutine());
    }

    private IEnumerator LoadGeoJsonCoroutine()
    {
        ClearMap();
        centroidCalculated = false;
        mapBounds = new Bounds();

        string jsonContent = "";
        try
        {
            using (StreamReader reader = new StreamReader(geoJsonFilePath))
            {
                jsonContent = reader.ReadToEnd();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading GeoJSON file: {e.Message}");
            yield break;
        }

        GeoJsonReader geoJsonReader = new GeoJsonReader();
        FeatureCollection featureCollection = null;

        try
        {
            featureCollection = geoJsonReader.Read<FeatureCollection>(jsonContent);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing GeoJSON: {e.Message}");
            yield break;
        }

        if (featureCollection == null)
        {
            Debug.LogError("Failed to parse GeoJSON");
            yield break;
        }

        Debug.Log($"Loaded {featureCollection.Count} features");

        if (centerMap)
        {
            CalculateMapBounds(featureCollection);
            yield return null;
        }

        int count = 0;
        foreach (var feature in featureCollection)
        {
            string constituencyName = feature.Attributes["Name"].ToString();
            GameObject constituencyObject = new GameObject(constituencyName);
            constituencyObject.transform.SetParent(mapContainer);

            ConstituencyData constituencyData = constituencyObject.AddComponent<ConstituencyData>();
            constituencyData.ConstituencyName = constituencyName;
            constituencyData.ConstituencyCode = feature.Attributes["GSScode"].ToString();

            // **NEW: Add LineRenderer component to constituency object**
            LineRenderer lineRenderer = constituencyObject.AddComponent<LineRenderer>();
            lineRenderer.material = lineMaterial;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.useWorldSpace = false; // Use local space relative to constituency object

            var geometry = feature.Geometry;
            if (geometry is Polygon polygon)
            {
                CreateMeshFromPolygon(constituencyObject, polygon, lineRenderer); // Pass lineRenderer
            }
            else if (geometry is MultiPolygon multiPolygon)
            {
                CreateMeshFromMultiPolygon(constituencyObject, multiPolygon, lineRenderer); // Pass lineRenderer
            }

            constituencyObjects[constituencyName] = constituencyObject;
            var controller = constituencyObject.AddComponent<ConstituencyController>();
            controller.constituencyName = constituencyName;
            controller.OnSelected += SelectConstituency;

            count++;
            if (count % 10 == 0)
            {
                yield return null;
            }
        }

        Debug.Log($"Generated {count} constituency objects");

        if (dataImporter != null)
        {
            dataImporter.ImportElectionResultsFromCSV();
        }
        else
        {
            Debug.LogWarning("DataImporter reference not set in UKConstituencyMapGenerator. Data import will not be triggered automatically.");
        }
    }

    private void CalculateMapBounds(FeatureCollection featureCollection)
    {
        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double maxX = double.MinValue;
        double maxY = double.MinValue;

        foreach (var feature in featureCollection)
        {
            var geometry = feature.Geometry;

            if (geometry is Polygon polygon)
            {
                UpdateBoundsFromCoordinates(polygon.ExteriorRing.Coordinates, ref minX, ref minY, ref maxX, ref maxY);
            }
            else if (geometry is MultiPolygon multiPolygon)
            {
                for (int i = 0; i < multiPolygon.NumGeometries; i++)
                {
                    var poly = (Polygon)multiPolygon.GetGeometryN(i);
                    UpdateBoundsFromCoordinates(poly.ExteriorRing.Coordinates, ref minX, ref minY, ref maxX, ref maxY);
                }
            }
        }

        mapCentroid = new Vector2((float)((minX + maxX) / 2), (float)((minY + maxY) / 2));
        centroidCalculated = true;

        Debug.Log($"Map bounds calculated: Min({minX},{minY}), Max({maxX},{maxY}), Centroid({mapCentroid.x},{mapCentroid.y})");
    }

    private void UpdateBoundsFromCoordinates(Coordinate[] coordinates, ref double minX, ref double minY, ref double maxX, ref double maxY)
    {
        foreach (var coord in coordinates)
        {
            minX = Math.Min(minX, coord.X);
            minY = Math.Min(minY, coord.Y);
            maxX = Math.Max(maxX, coord.X);
            maxY = Math.Max(maxY, coord.Y);
        }
    }

    private void CreateMeshFromPolygon(GameObject parent, Polygon polygon, LineRenderer lineRenderer) // Added lineRenderer parameter
    {
        GameObject meshObject = new GameObject("Mesh");
        meshObject.transform.SetParent(parent.transform);

        var meshFilter = meshObject.AddComponent<MeshFilter>();
        var meshRenderer = meshObject.AddComponent<MeshRenderer>();
        var meshCollider = meshObject.AddComponent<MeshCollider>();

        meshRenderer.material = defaultMaterial;

        Mesh mesh = GenerateMesh(polygon, extrusionHeight);
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
        meshObject.transform.localPosition = Vector3.up * extrusionSeparation;

        // **NEW: Generate and set border points for LineRenderer**
        GenerateBorderLine(polygon, lineRenderer);
    }

    private void CreateMeshFromMultiPolygon(GameObject parent, MultiPolygon multiPolygon, LineRenderer lineRenderer) // Added lineRenderer parameter
    {
        for (int i = 0; i < multiPolygon.NumGeometries; i++)
        {
            var polygon = (Polygon)multiPolygon.GetGeometryN(i);

            GameObject meshObject = new GameObject($"Mesh_{i}");
            meshObject.transform.SetParent(parent.transform);

            var meshFilter = meshObject.AddComponent<MeshFilter>();
            var meshRenderer = meshObject.AddComponent<MeshRenderer>();
            var meshCollider = meshObject.AddComponent<MeshCollider>();

            meshRenderer.material = defaultMaterial;

            Mesh mesh = GenerateMesh(polygon, extrusionHeight);
            meshFilter.mesh = mesh;
            meshCollider.sharedMesh = mesh;
            meshObject.transform.localPosition = Vector3.up * extrusionSeparation;

            // **NEW: Generate and set border points for LineRenderer**
            GenerateBorderLine(polygon, lineRenderer);
        }
    }

    private void GenerateBorderLine(Polygon polygon, LineRenderer lineRenderer)
    {
        var exteriorRing = polygon.ExteriorRing;
        var coordinates = exteriorRing.Coordinates;
        Vector3[] borderPoints = TransformCoordinates(coordinates);

        // **NEW: Set positions for LineRenderer**
        lineRenderer.positionCount = borderPoints.Length;
        lineRenderer.SetPositions(borderPoints);
        lineRenderer.loop = true; // Close the loop for polygon borders
    }


    private Mesh GenerateMesh(Polygon polygon, float height)
    {
        var exteriorRing = polygon.ExteriorRing;
        var coordinates = exteriorRing.Coordinates;

        Vector3[] vertices = TransformCoordinates(coordinates);

        Mesh mesh = new Mesh();
        List<Vector3> allVertices = new List<Vector3>();
        allVertices.AddRange(vertices);

        for (int i = 0; i < vertices.Length; i++)
        {
            allVertices.Add(vertices[i] + Vector3.up * height);
        }

        List<int> triangles = new List<int>();
        for (int i = 1; i < vertices.Length - 1; i++)
        {
            triangles.Add(0);
            triangles.Add(i);
            triangles.Add(i + 1);

            triangles.Add(vertices.Length);
            triangles.Add(vertices.Length + i + 1);
            triangles.Add(vertices.Length + i);
        }

        for (int i = 0; i < vertices.Length - 1; i++)
        {
            triangles.Add(i);
            triangles.Add(i + vertices.Length);
            triangles.Add(i + 1);

            triangles.Add(i + 1);
            triangles.Add(i + vertices.Length);
            triangles.Add(i + 1 + vertices.Length);
        }

        triangles.Add(vertices.Length - 1);
        triangles.Add(vertices.Length * 2 - 1);
        triangles.Add(0);

        triangles.Add(0);
        triangles.Add(vertices.Length * 2 - 1);
        triangles.Add(vertices.Length);

        mesh.vertices = allVertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        return mesh;
    }

    private Vector3[] TransformCoordinates(Coordinate[] coordinates)
    {
        List<Vector3> transformedCoords = new List<Vector3>();
        Vector2 polygonCentroid = centerMap ? mapCentroid : CalculatePolygonCentroid(coordinates);

        foreach (var coord in coordinates)
        {
            if (centerMap && centroidCalculated)
            {
                transformedCoords.Add(new Vector3(
                    (float)(coord.X - mapCentroid.x) * mapScale,
                    0,
                    (float)(coord.Y - mapCentroid.y) * mapScale
                ));
            }
            else
            {
                transformedCoords.Add(new Vector3(
                    (float)(coord.X - polygonCentroid.x) * mapScale,
                    0,
                    (float)(coord.Y - polygonCentroid.y) * mapScale
                ));
            }
        }

        return transformedCoords.ToArray();
    }

    private Vector2 CalculatePolygonCentroid(Coordinate[] coordinates)
    {
        double sumX = 0;
        double sumY = 0;

        foreach (var coord in coordinates)
        {
            sumX += coord.X;
            sumY += coord.Y;
        }

        return new Vector2(
            (float)(sumX / coordinates.Length),
            (float)(sumY / coordinates.Length)
        );
    }

    public void ClearMap()
    {
        foreach (var obj in constituencyObjects.Values)
        {
            Destroy(obj);
        }
        constituencyObjects.Clear();
        selectedConstituency = null;
    }

    public void SelectConstituency(string constituencyName)
    {
        if (selectedConstituency != null)
        {
            foreach (var renderer in selectedConstituency.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.material = defaultMaterial;
            }
        }

        if (constituencyObjects.TryGetValue(constituencyName, out GameObject newSelection))
        {
            selectedConstituency = newSelection;
            foreach (var renderer in selectedConstituency.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.material = highlightMaterial;
            }
            Debug.Log($"Selected constituency: {constituencyName}");
        }
    }
}