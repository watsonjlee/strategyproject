using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class MapGenerator : MonoBehaviour
{
    public GameObject hexPrefab;
    public string hexjsonFilePath = "Data/constituencies.hexjson";
    public float hexRadius = 0.5f;

    void Start()
    {
        GenerateMap();
    }

    public void GenerateMap()
    {
        if (hexPrefab == null)
        {
            Debug.LogError("Hex Prefab not assigned!");
            return;
        }

        JObject hexjsonData = LoadHexJson();

        if (hexjsonData == null)
        {
            return;
        }

        JObject hexes = (JObject)hexjsonData["hexes"];

        foreach (JProperty hexProperty in hexes.Properties())
        {
            // hexProperty.Value is the JObject containing q, r, s, name, n, e, AND p.
            JObject hexData = (JObject)hexProperty.Value;

            // --- Get Constituency Name, q, and r ---
            string constituencyName = hexData["n"]?.ToString() ?? ""; // Corrected key to "n"
            float q = hexData["q"].Value<float>();
            float r = hexData["r"].Value<float>();

            GameObject hexObject = Instantiate(hexPrefab);
            hexObject.name = constituencyName; // Set the name.
            hexObject.transform.position = AxialToWorld((int)q, (int)r);
        }
    }

    private JObject LoadHexJson()
    {
        string absoluteHexjsonPath = Path.Combine(Application.dataPath, hexjsonFilePath);

        if (!File.Exists(absoluteHexjsonPath))
        {
            Debug.LogError(".hexjson file not found: " + absoluteHexjsonPath);
            return null;
        }

        try
        {
            string jsonText = File.ReadAllText(absoluteHexjsonPath);
            return JObject.Parse(jsonText);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error loading or parsing .hexjson: " + e.Message);
            return null;
        }
    }

    Vector3 AxialToWorld(int q, int r)
    {
        float x = hexRadius * (1.5f * q);
        float z = hexRadius * (Mathf.Sqrt(3f) / 2f * q + Mathf.Sqrt(3f) * r);
        return new Vector3(x, 0, z);
    }
}