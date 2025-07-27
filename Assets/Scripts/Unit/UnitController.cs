using System;
using System.Collections;
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
    public bool IsDead { get; private set; }

    protected UnitMovement unitMovement;
    protected UnitAttack unitAttack;

    [SerializeField] LineRenderer _pathLineRenderer;
    [SerializeField] LineRenderer _indicatorRenderer;
    [SerializeField] SpriteRenderer _attackRangeRenderer;
    [SerializeField] NavMeshObstacle _unitFloorObstacle;
    [SerializeField] NavMeshAgent _unitAgent;
    PathType _currentPathLineState => GetPathTypeFromColor(_pathLineRenderer.startColor);
    // true when player selects this unit
    bool _isSelected = false;
    // point to which unit is moving
    Vector3? _targetPoint = null;

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
            DrawPath(path, PathType.Move); 
            _targetPoint = path.corners.Last(); 
        };
        unitMovement.OnMovementEnd += () =>
        {
            HidePath();
            _targetPoint = null;
        };
        unitAttack.Init(UnitSettings, _attackRangeRenderer);
        unitMovement.Init(_unitAgent);
    }

    private void Update()
    {
        if (_targetPoint != null)
            _attackRangeRenderer.transform.position = _targetPoint.Value;
    }

    // PUBLIC

    public void Kill(UnitAttack killer)
    {
        IsDead = true;
        OnDeath?.Invoke();
        Destroy(gameObject);
    }

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
        unitAttack.PreviewAttack(new Vector2(transform.position.x, transform.position.z));
        ShowIndicator(IndicatorType.Selected);
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
        unitAttack.HideAttackPreview();
        HideIndicator();
        if (_currentPathLineState == PathType.Preview)
            HidePath();
        if (!IsMoving)
            MakeAgentBecomeObstacle();
        else
            unitMovement.OnMovementEnd += MakeAgentBecomeObstacle;
        return true;
    }

    public bool DrawPathPreview(Vector3 targetPosition)
    {
        if (IsMoving) return false;

        NavMeshPath fullPath = GetPathTo(targetPosition);
        if (fullPath.status == NavMeshPathStatus.PathInvalid) return false;

        float pathLength = GetPathLength(fullPath);
        NavMeshPath pathToDraw;
        Vector3 finalPoint;

        if (pathLength > UnitSettings.Speed)
        {
            finalPoint = GetPointOnPath(fullPath, UnitSettings.Speed);
            pathToDraw = GetPathTo(finalPoint);
        }
        else
        {
            pathToDraw = fullPath;
            finalPoint = fullPath.corners.Length > 0 ? fullPath.corners.Last() : transform.position;
        }

        if (DrawPath(pathToDraw, PathType.Preview))
        {
            unitAttack.PreviewAttack(new Vector2(finalPoint.x, finalPoint.z));
            return true;
        }
        return false;
    }
    public bool DrawPathTo(Vector3 targetPosition, PathType pathColorType) => 
        DrawPath(GetPathTo(targetPosition), GetPathColor(pathColorType));
    public bool DrawPath(NavMeshPath path, PathType pathColorType) =>
        DrawPath(path, GetPathColor(pathColorType));
    public void HidePath() => _pathLineRenderer.positionCount = 0;
    public NavMeshPath GetPathTo(Vector3 targetPosition) => unitMovement.GetPathTo(targetPosition);
    public bool MoveTo(Vector3 targetPoint)
    {
        NavMeshPath path = GetPathTo(targetPoint);
        return MoveByPath(path);
    }
    public bool MoveByPath(NavMeshPath path)
    {
        if (path.status == NavMeshPathStatus.PathInvalid) return false;

        float pathLength = GetPathLength(path);

        if (pathLength > UnitSettings.Speed)
        {
            Vector3 finalPoint = GetPointOnPath(path, UnitSettings.Speed);
            NavMeshPath truncatedPath = GetPathTo(finalPoint);
            return unitMovement.MoveByPath(truncatedPath);
        }

        return unitMovement.MoveByPath(path);
    }

    public bool AttackEnemy(UnitController enemy) => unitAttack.AttackEnemy(enemy);

    public void ShowIndicator(IndicatorType indicator)
    {
        _indicatorRenderer.enabled = true;
        _indicatorRenderer.startColor = GetIndicatorColor(indicator);
        _indicatorRenderer.endColor = GetIndicatorColor(indicator);
    }
    public void HideIndicator()
    {
        _indicatorRenderer.enabled = false;
    }

    // PRIVATE

    private float GetPathLength(NavMeshPath path)
    {
        float length = 0.0f;
        if (path.corners.Length < 2)
            return 0;

        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            length += Vector3.Distance(path.corners[i], path.corners[i + 1]);
        }
        return length;
    }

    // Finds a point on the path at the specified distance from the start
    private Vector3 GetPointOnPath(NavMeshPath path, float distance)
    {
        if (path.corners.Length < 2)
            return transform.position;

        float currentDistance = 0f;
        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            float segmentLength = Vector3.Distance(path.corners[i], path.corners[i + 1]);
            if (currentDistance + segmentLength >= distance)
            {
                float distanceOnSegment = distance - currentDistance;
                float ratio = distanceOnSegment / segmentLength;
                return Vector3.Lerp(path.corners[i], path.corners[i + 1], ratio);
            }
            currentDistance += segmentLength;
        }

        return path.corners.Last();
    }

    private Color GetIndicatorColor(IndicatorType indicator)
    {
        switch (indicator)
        {
            case IndicatorType.Selected:
                return UnitSettings.SelectedUnitIndicatorColor;
            case IndicatorType.Attackable:
                return UnitSettings.AttackableUnitIndicatorColor;
            default:
                throw new NotImplementedException($"Not implemented behaviour for indicator type: {indicator.ToString()}");
        }
    }
    private Color GetPathColor(PathType pathType)
    {
        switch (pathType)
        {
            case PathType.Preview:
                return UnitSettings.PreviewPathColor;
            case PathType.Move:
                return UnitSettings.MovePathColor;
            default:
                throw new NotImplementedException($"Not implemented behaviour for path type: {pathType.ToString()}");
        }
    }
    private PathType GetPathTypeFromColor(Color color)
    {
        if (color == UnitSettings.PreviewPathColor)
            return PathType.Preview;
        else if (color == UnitSettings.MovePathColor)
            return PathType.Move;
        else
            return PathType.Undefined;
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

    public enum PathType 
    {
        Undefined,
        Preview,
        Move
    }

    public enum IndicatorType
    {
        NoIndicator,
        Selected,
        Attackable
    }
}
