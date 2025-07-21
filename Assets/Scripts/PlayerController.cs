using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class PlayerController : MonoBehaviour
{
    // VARIABLES

    protected bool haveChosenUnit => chosenUnitController != null;
    protected UnitController chosenUnitController
    {
        get => _chosenUnitController;
        private set
        {
            if (_chosenUnitController != null)
                _chosenUnitController.OnDeath -= _chosenUnitController_OnDeath;
            if (value != null)
                value.OnDeath += _chosenUnitController_OnDeath;
            _chosenUnitController = value;
        }
    }

    Camera _mainCamera;
    Vector3 _lastMousePosition = Vector3.zero;
    UnitController _chosenUnitController = null;

    // UNITY

    private void Start()
    {
        _mainCamera = Camera.main;
    }

    void Update()
    {
        // Show path for chosen unit on every mouse movement

        if (Input.mousePosition != _lastMousePosition && 
            haveChosenUnit)
        {
            _lastMousePosition = Input.mousePosition;

            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit) && 
                hit.transform.GetComponent<NavMeshSurface>() != null)
            {
                chosenUnitController.DrawPathTo(hit.point, UnitController.PathColorType.Preview);
            }
        }
        
        // Check does player want to move unit

        if (Input.GetMouseButtonDown(0) && 
            haveChosenUnit)
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit) &&
                hit.transform.GetComponent<NavMeshSurface>() != null)
            {
                chosenUnitController.MoveTo(hit.point);
            }
        }

        // Try to choose unit on LMB

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
                chosenUnitController = hit.transform.GetComponent<UnitController>();
        }
    }

    // PRIVATE

    private void _chosenUnitController_OnDeath() =>
        chosenUnitController = null;
}
