using UnityEngine;

public class ConstituencyData : MonoBehaviour
{
    public string ConstituencyCode; // e.g., "E14000530"
    public string ConstituencyName;
    public string RegionCode;
    public string RegionName;
    public int ElectionResult2019; // Example: Store an integer result.
    // Add other data fields as needed (e.g., floats for percentages, strings for party names).

    // You could add methods here to handle data-specific logic,
    // such as calculating percentage changes, determining the winning party, etc.
}