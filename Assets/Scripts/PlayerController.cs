using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class PlayerController : MonoBehaviour
{
    // VARIABLES
    private float doubleClickTimeLimit = 0.25f;
    private float lastClickTime = -1f;

    protected bool haveChosenUnit => chosenUnitController != null;
    protected UnitController chosenUnitController
    {
        get => _chosenUnitController;
        private set
        {
            if (_chosenUnitController != null)
            {
                _chosenUnitController.OnDeath -= _chosenUnitController_OnDeath;
                _chosenUnitController.Deselect();
            }
            if (value != null && value.Select())
            {
                value.OnDeath += _chosenUnitController_OnDeath;
                _chosenUnitController = value;
            }
            else
                _chosenUnitController = null;
            previewPathMousePos = Vector3.zero;
        }
    }

    Camera _mainCamera;
    Vector3 previewPathMousePos = Vector3.zero;
    UnitController _chosenUnitController = null;

    // UNITY

    private void Start()
    {
        _mainCamera = Camera.main;
    }

    void Update()
    {
        // Show path or attack enemy on RMB

        if (Input.GetMouseButtonDown(1) && 
            haveChosenUnit)
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform.GetComponent<NavMeshSurface>() != null)
                {
                    chosenUnitController.DrawPathPreview(hit.point);
                    previewPathMousePos = hit.point;
                } else
                {
                    var hitUnitController = hit.transform.GetComponent<UnitController>();
                    if (hitUnitController != null)
                        chosenUnitController.AttackEnemy(hitUnitController);
                }
            }
        }

        // Handle single/double click

        if (Input.GetMouseButtonDown(0))
        {
            float timeSinceLastClick = Time.time - lastClickTime;

            if (timeSinceLastClick <= doubleClickTimeLimit)
                HandleDoubleClick();
            else
                HandleSingleClick();

            lastClickTime = Time.time;
        }
    }

    // PRIVATE

    private void _chosenUnitController_OnDeath() =>
        chosenUnitController = null;

    private void HandleSingleClick()
    {
        // Choose unit on single click

        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            var unitController = hit.transform.GetComponent<UnitController>();
            if (unitController != null)
                chosenUnitController = unitController;
        }
    }

    private void HandleDoubleClick()
    {
        // Move to point on double click

        if (haveChosenUnit &&
            previewPathMousePos != Vector3.zero)
        {
            chosenUnitController.MoveTo(previewPathMousePos);
        }
    }
}
