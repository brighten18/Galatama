using UnityEngine;

[CreateAssetMenu(fileName = "FishData", menuName = "Fishing/FishData")]
public class FishData : ScriptableObject
{
    public string itemName;         // harus sama dengan nama prefab di Resources
    public float weight;            // berat ikan, untuk AI nanti
    public float swimSpeed;         // kecepatan renang, untuk AI nanti
}
