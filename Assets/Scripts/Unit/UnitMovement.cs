using System;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class UnitMovement : MonoBehaviour
{
    // EVENTS

    public event Action<NavMeshPath> OnMovementStart;
    public event Action OnMovementEnd;

    // VARIABLES

    [SerializeField] private Camera _mainCamera;
    NavMeshAgent _agent;
    bool _isMoving = false;

    // UNITY

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();

        if (_mainCamera == null)
            _mainCamera = Camera.main;
    }

    void Update()
    {
        if (_isMoving && _agent.remainingDistance - _agent.stoppingDistance <= 0)
        {
            _isMoving = false;
            OnMovementEnd.Invoke();
        }
    }

    // PUBLIC

    public NavMeshPath GetPathTo(Vector3 targetPosition)
    {
        NavMeshPath path = new NavMeshPath();
        _agent.CalculatePath(targetPosition, path);
        return path;
    }

    public bool MoveTo(Vector3 targetPosition) => MoveByPath(GetPathTo(targetPosition));
    public bool MoveByPath(NavMeshPath path)
    {
        if (_agent.SetPath(path))
        {
            _isMoving = true;
            OnMovementStart.Invoke(path);
            return true;
        }
        return false;
    }
}
