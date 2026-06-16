using UnityEngine;

[CreateAssetMenu(fileName = "MissionData", menuName = "GALATAMA/Mission Data")]
public class MissionData : ScriptableObject
{
    [SerializeField] private string missionTitle;
    [SerializeField, TextArea(2, 5)] private string missionDescription;

    public string MissionTitle => missionTitle;
    public string MissionDescription => missionDescription;
}
