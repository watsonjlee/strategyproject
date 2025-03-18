using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;

public class DataImporter : MonoBehaviour
{
    [Header("Settings")]
    public string electionResultsCSVFile = "Assets/Data/parl-election-results-2019.csv";
    public bool useStreamingAssets = false;

    [Header("Party Color Mapping")]
    public Material defaultPartyMaterial; // Default material if party not found
    public List<PartyMaterialMap> partyMaterialMaps = new List<PartyMaterialMap>(); // List for party-material mappings

    [Header("Runtime")]
    public bool loadOnStart = false;

    void Start()
    {
        if (loadOnStart)
        {
            ImportElectionResultsFromCSV();
        }
    }

    public void ImportElectionResultsFromCSV()
    {
        string filePath = electionResultsCSVFile;
        if (useStreamingAssets)
        {
            filePath = Path.Combine(Application.streamingAssetsPath, electionResultsCSVFile);
        }

        if (!File.Exists(filePath))
        {
            Debug.LogError($"CSV file not found: {filePath}");
            return;
        }

        Dictionary<string, ConstituencyData> constituencyDataMap = GetConstituencyDataMap();

        if (constituencyDataMap.Count == 0)
        {
            Debug.LogWarning("No ConstituencyData components found in the scene. Make sure your map is generated first.");
            return;
        }

        try
        {
            string[] lines = File.ReadAllLines(filePath);

            if (lines.Length <= 1) // Check if the CSV has data (beyond headers)
            {
                Debug.LogWarning("CSV file is empty or contains only headers.");
                return;
            }

            // Assuming the first line is the header row, you might need to adjust based on your CSV
            string[] headers = lines[0].Split(','); // Basic comma-separated split. Adjust if your CSV is different.

            // Determine column indices (adjust these based on your CSV header)
            int constituencyCodeColumnIndex = -1;
            int partyColumnIndex = -1;
            int voteShareColumnIndex = -1; // Example for vote share

            // **IMPORTANT:** Adjust these header names to EXACTLY match your CSV header row!
            string constituencyCodeHeader = "ONS ID";
            string partyHeader = "First party";
            string voteShareHeader = ""; // Skipping vote share for now

            for (int i = 0; i < headers.Length; i++)
            {
                if (headers[i].Trim() == constituencyCodeHeader) constituencyCodeColumnIndex = i;
                else if (headers[i].Trim() == partyHeader) partyColumnIndex = i;
                else if (headers[i].Trim() == voteShareHeader) voteShareColumnIndex = i;
            }

            if (constituencyCodeColumnIndex == -1 || partyColumnIndex == -1)
            {
                Debug.LogError("Essential columns (Constituency Code or Party) not found in CSV headers. Check header names and code.");
                Debug.LogError($"Headers found in CSV: {string.Join(", ", headers)}");
                return;
            }

            for (int i = 1; i < lines.Length; i++) // Start from the second line to skip headers
            {
                string[] fields = lines[i].Split(','); // Split each line into fields

                if (fields.Length <= constituencyCodeColumnIndex || fields.Length <= partyColumnIndex)
                {
                    Debug.LogWarning($"Skipping line {i + 1} due to insufficient columns.");
                    continue; // Skip lines that don't have enough data
                }

                string constituencyCode = fields[constituencyCodeColumnIndex].Trim();
                string party = fields[partyColumnIndex].Trim();
                float voteShare = 0f; // Default value (skipped for now)

                if (constituencyDataMap.TryGetValue(constituencyCode, out ConstituencyData data))
                {
                    data.ElectedParty2019 = party;
                    data.VoteShareElectedParty2019 = voteShare;

                    // Apply Material based on Party
                    ApplyPartyMaterial(data, party);
                }
                else
                {
                    Debug.LogWarning($"Constituency code '{constituencyCode}' from CSV not found in scene objects.");
                }
            }

            Debug.Log("Election Results CSV data imported and linked to constituencies.");

        }
        catch (Exception e)
        {
            Debug.LogError($"Error reading or parsing CSV file: {e.Message}");
        }
    }

    private void ApplyPartyMaterial(ConstituencyData constituencyData, string partyName)
    {
        Material partyMaterialToApply = defaultPartyMaterial; // Default material

        // Look up party name in the mapping
        foreach (var map in partyMaterialMaps)
        {
            if (map.partyName.ToLower() == partyName.ToLower()) // Case-insensitive comparison
            {
                partyMaterialToApply = map.material;
                break; // Found a match, no need to continue searching
            }
        }

        // Apply the material to all MeshRenderers in the constituency GameObject
        MeshRenderer[] meshRenderers = constituencyData.GetComponentsInChildren<MeshRenderer>();
        foreach (var renderer in meshRenderers)
        {
            renderer.material = partyMaterialToApply;
        }
    }

    // --- NEW Class for Party-Material Mapping ---
    [System.Serializable]
    public class PartyMaterialMap
    {
        public string partyName;
        public Material material;
    }

    // Helper function to quickly find all ConstituencyData components by ConstituencyCode
    private Dictionary<string, ConstituencyData> GetConstituencyDataMap()
    {
        Dictionary<string, ConstituencyData> dataMap = new Dictionary<string, ConstituencyData>();
        ConstituencyData[] allConstituencyData = FindObjectsOfType<ConstituencyData>();

        foreach (ConstituencyData data in allConstituencyData)
        {
            if (!string.IsNullOrEmpty(data.ConstituencyCode))
            {
                dataMap[data.ConstituencyCode] = data;
            }
            else
            {
                Debug.LogWarning($"ConstituencyData component found on GameObject '{data.gameObject.name}' but ConstituencyCode is not set.  This data will not be accessible by the importer.");
            }
        }
        return dataMap;
    }
}