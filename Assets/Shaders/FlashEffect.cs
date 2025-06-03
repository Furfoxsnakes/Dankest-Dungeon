using UnityEngine;

public class FlashEffect : MonoBehaviour
{
    private Material material;
    
    void Start()
    {
        // Get reference to the material
        material = GetComponent<SpriteRenderer>().material;
    }
    
    public void TriggerFlash()
    {
        // Set current time as trigger time
        material.SetFloat("_TriggerTime", Time.time);
        // Enable flashing
        material.SetFloat("_FlashActive", 1.0f);
    }
    
    // Optional: Stop the flash effect early if needed
    public void StopFlash()
    {
        material.SetFloat("_FlashActive", 0.0f);
    }
}