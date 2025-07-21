using UnityEngine;

[CreateAssetMenu(fileName = "UnitSettings", menuName = "ScriptableObjects/UnitSettings")]
public class UnitSettings : ScriptableObject
{
    public Color PreviewPathColor = Color.blue;
    public Color MovePathColor = Color.yellow;
}
