using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UnitSettings", menuName = "ScriptableObjects/UnitSettings")]
public class UnitSettings : ScriptableObject
{
    public Color PreviewPathColor = Color.blue;
    public Color MovePathColor = Color.yellow;
    public Color SelectedUnitIndicatorColor = Color.yellow;
    public Color AttackableUnitIndicatorColor = Color.red;
    [Tooltip("Unit team index(starting from 0)")] public uint TeamId = 0;
    public float AttackRadius;
    public float Speed = 1;
}
