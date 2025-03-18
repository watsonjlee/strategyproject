using System;
using UnityEngine;

public class ConstituencyController : MonoBehaviour
{
    public string constituencyName;
    public event Action<string> OnSelected;

    private void OnMouseDown()
    {
        // Call the OnSelected event when this constituency is clicked
        OnSelected?.Invoke(constituencyName);
    }

    private void OnMouseEnter()
    {
        // Optional: Add hover effect
    }

    private void OnMouseExit()
    {
        // Optional: Remove hover effect
    }
}