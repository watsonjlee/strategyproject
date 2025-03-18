using UnityEngine;

public class ConstituencyData : MonoBehaviour
{
    public string ConstituencyCode;
    public string ConstituencyName;
    // Election Results Data (Example - adjust based on your CSV)
    public string ElectedParty2019; // Party that won in 2019
    public float VoteShareElectedParty2019; // Vote share of the elected party
    // You can add more fields here for other parties, vote counts, etc., as needed from your CSV

    // Optional. Add these *IF* you load them from somewhere else.
    // public string RegionCode;
    // public string RegionName;
    // public int ElectionResult2019; // Consider if this is still needed, might be redundant now.
}