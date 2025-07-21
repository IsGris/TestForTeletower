using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions;

[RequireComponent(typeof(UnitMovement), typeof(UnitAttack))]
public class UnitController : MonoBehaviour
{
    // EVENTS

    public event Action OnDeath;

    // VARIABLES

    public UnitSettings UnitSettings;
    
    protected UnitMovement unitMovement;
    protected UnitAttack unitAttack;

    [SerializeField] LineRenderer _pathLineRenderer;

    // UNITY

    private void Start()
    {
        unitMovement = GetComponent<UnitMovement>();
        unitAttack = GetComponent<UnitAttack>();
        if (_pathLineRenderer == null)
            _pathLineRenderer = GetComponent<LineRenderer>();
        Assert.IsNotNull(_pathLineRenderer);

        unitMovement.OnMovementStart += (path) => { DrawPath(path, PathColorType.Move); };
        unitMovement.OnMovementEnd += () => { HidePath(); };
    }

    // PUBLIC

    public bool DrawPathTo(Vector3 targetPosition, PathColorType pathColorType) => 
        DrawPath(GetPathTo(targetPosition), GetPathColor(pathColorType));
    public bool DrawPath(NavMeshPath path, PathColorType pathColorType) =>
        DrawPath(path, GetPathColor(pathColorType));
    public void HidePath() => _pathLineRenderer.positionCount = 0;
    public NavMeshPath GetPathTo(Vector3 targetPosition) => unitMovement.GetPathTo(targetPosition);
    public bool MoveTo(Vector3 targetPoint) => unitMovement.MoveTo(targetPoint);
    public bool MoveByPath(NavMeshPath path) => unitMovement.MoveByPath(path);

    // PRIVATE

    private Color GetPathColor(PathColorType pathColorType)
    {
        switch (pathColorType)
        {
            case PathColorType.Preview:
                return UnitSettings.PreviewPathColor;
            case PathColorType.Move:
                return UnitSettings.MovePathColor;
            default:
                throw new NotImplementedException($"Not implemented behaviour for path color type: {pathColorType.ToString()}");
        }
    }
    private bool DrawPath(NavMeshPath path, Color? pathColor = null)
    {
        if (path == null || path.status == NavMeshPathStatus.PathInvalid) return false;

        if (pathColor != null)
        {
            _pathLineRenderer.startColor = pathColor.Value;
            _pathLineRenderer.endColor = pathColor.Value;
        }

        _pathLineRenderer.positionCount = path.corners.Length;
        _pathLineRenderer.SetPositions(path.corners);
        return true;
    }

    // OTHER

    public enum PathColorType 
    {
        Preview,
        Move
    }
}
