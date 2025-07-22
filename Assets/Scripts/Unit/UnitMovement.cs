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

    NavMeshAgent _agent;
    bool _isMoving = false;

    // UNITY

    public void Init(NavMeshAgent agent)
    {
        _agent = agent;
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
