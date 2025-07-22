using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public bool IsMoving => _targetPoint != null;

    protected UnitMovement unitMovement;
    protected UnitAttack unitAttack;

    PathColorType _currentPathLineState => GetPathTypeFromColor(_pathLineRenderer.startColor);
    // true when player selects this unit
    bool _isSelected = false;
    // point to which unit is moving
    Vector3? _targetPoint = null;
    [SerializeField] LineRenderer _pathLineRenderer;
    [SerializeField] LineRenderer _indicatorRenderer;
    [SerializeField] SpriteRenderer _attackRangeRenderer;
    [SerializeField] NavMeshObstacle _unitFloorObstacle;
    [SerializeField] NavMeshAgent _unitAgent;

    // UNITY

    private void Start()
    {
        unitMovement = GetComponent<UnitMovement>();
        unitAttack = GetComponent<UnitAttack>();
        if (_pathLineRenderer == null)
            _pathLineRenderer = GetComponent<LineRenderer>();
        Assert.IsNotNull(_pathLineRenderer);
        if (_indicatorRenderer == null)
            _indicatorRenderer = GetComponent<LineRenderer>();
        Assert.IsNotNull(_indicatorRenderer);
        if (_attackRangeRenderer == null)
            _attackRangeRenderer = GetComponent<SpriteRenderer>();
        Assert.IsNotNull(_attackRangeRenderer);
        if (_unitFloorObstacle == null)
            _unitFloorObstacle = GetComponent<NavMeshObstacle>();
        Assert.IsNotNull(_unitFloorObstacle);
        if (_unitAgent == null)
            _unitAgent = GetComponent<NavMeshAgent>();
        Assert.IsNotNull(_unitAgent);
        _attackRangeRenderer.transform.localScale = 
            new(
                UnitSettings.AttackRadius * 2, 
                UnitSettings.AttackRadius * 2,
                _attackRangeRenderer.transform.localScale.z);

        unitMovement.OnMovementStart += (path) => 
        { 
            DrawPath(path, PathColorType.Move); 
            _targetPoint = path.corners.Last(); 
        };
        unitMovement.OnMovementEnd += () =>
        {
            if (!_isSelected) HidePath();
            _targetPoint = null;
        };
    }

    private void Update()
    {
        if (_targetPoint != null)
            _attackRangeRenderer.transform.position = _targetPoint.Value;
    }

    // PUBLIC

    public bool Select()
    {
        IEnumerator DelayedAgentEnableCoroutine()
        {
            yield return null;
            _unitAgent.enabled = true;
        }
        if (_isSelected || IsMoving) return false;
        _isSelected = true;
        _attackRangeRenderer.enabled = true;
        _indicatorRenderer.enabled = true;
        _indicatorRenderer.startColor = UnitSettings.SelectedUnitIndicatorColor;
        _indicatorRenderer.endColor = UnitSettings.SelectedUnitIndicatorColor;
        _unitFloorObstacle.enabled = false;
        // If we disable floor obstacle and enable agent at the same time
        // agent will be teleported for a small amount of units for some reason
        // so we wait one frame to make agent enabled and floor disabled
        StartCoroutine(DelayedAgentEnableCoroutine());
        return true;
    }
    public bool Deselect()
    {
        void MakeAgentBecomeObstacle()
        {
            _unitAgent.enabled = false;
            _unitFloorObstacle.enabled = true;
            // Unfollow event if function is called from event
            unitMovement.OnMovementEnd -= MakeAgentBecomeObstacle;
        }
        if (!_isSelected) return false;
        _isSelected = false;
        _attackRangeRenderer.enabled = false;
        _indicatorRenderer.enabled = false;
        if (_currentPathLineState == PathColorType.Preview)
            HidePath();
        if (!IsMoving)
            MakeAgentBecomeObstacle();
        else
            unitMovement.OnMovementEnd += MakeAgentBecomeObstacle;
        return true;
    }

    public bool DrawPathPreview(Vector3 targetPosition)
    {
        if (IsMoving) return false; // Already drawing path for movement

        NavMeshPath path = GetPathTo(targetPosition);
        if (DrawPath(path, PathColorType.Preview))
        {
            _attackRangeRenderer.transform.position = path.corners.Last();
            return true;
        }
        return false;
    }
    public bool DrawPathTo(Vector3 targetPosition, PathColorType pathColorType) => 
        DrawPath(GetPathTo(targetPosition), GetPathColor(pathColorType));
    public bool DrawPath(NavMeshPath path, PathColorType pathColorType) =>
        DrawPath(path, GetPathColor(pathColorType));
    public void HidePath() => _pathLineRenderer.positionCount = 0;
    public NavMeshPath GetPathTo(Vector3 targetPosition) => unitMovement.GetPathTo(targetPosition);
    public bool MoveTo(Vector3 targetPoint) => unitMovement.MoveTo(targetPoint);
    public bool MoveByPath(NavMeshPath path) => unitMovement.MoveByPath(path);

    // PRIVATE

    // Makes indicator for all attackable enemies inside attack circle red
    // targetPosition - center of attack circle
    private void IndicateAllAttackableEnemies(Vector3 targetPosition)
    {

    }

    // Returns all attackable enemies inside attack circle
    // targetPosition - center of attack circle
    private List<UnitController> GetAllAttackableEnemies(Vector3 targetPosition)
    {
        throw new NotImplementedException();
    }
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
    private PathColorType GetPathTypeFromColor(Color color)
    {
        if (color == UnitSettings.PreviewPathColor)
            return PathColorType.Preview;
        else if (color == UnitSettings.MovePathColor)
            return PathColorType.Move;
        else
            return PathColorType.Undefined;
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
        Undefined,
        Preview,
        Move
    }
}
