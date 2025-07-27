using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

public class PlayerController : NetworkBehaviour
{
    // VARIABLES
    public uint TeamId => teamId;

    private float doubleClickTimeLimit = 0.25f;
    private Coroutine clickCoroutine = null;

    protected uint teamId = uint.MaxValue;
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

    TurnManager _turnManager;
    Camera _mainCamera;
    Vector3 previewPathMousePos = Vector3.zero;
    UnitController _chosenUnitController = null;

    // UNITY

    private void Start()
    {
        _mainCamera = Camera.main;
        _turnManager = FindObjectOfType<TurnManager>();
        _turnManager.OnTurnStart += (_) => { if (IsOwner) UnchooseUnitServerRpc(); };
    }

    void Update()
    {
        if (!IsOwner)
            return;

        // Show path or attack enemy on RMB
        if (Input.GetMouseButtonDown(1) && haveChosenUnit)
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform.GetComponent<NavMeshSurface>() != null)
                {
                    ShowPathPreviewServerRpc(chosenUnitController.gameObject.name, hit.point);
                    previewPathMousePos = hit.point;
                }
                else
                {
                    var hitUnitController = hit.transform.GetComponent<UnitController>();
                    if (hitUnitController != null)
                        AttackUnitServerRpc(chosenUnitController.gameObject.name, hitUnitController.gameObject.name);
                }
            }
        }

        // Handle single/double click
        IEnumerator SingleClickCoroutine()
        {
            // Wait needed time
            yield return new WaitForSeconds(doubleClickTimeLimit);

            // If coroutine wasnt stopped then it is single click
            HandleSingleClick();
            
            clickCoroutine = null;
        }
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit) && 
                hit.transform.GetComponent<NavMeshSurface>() != null) // Double clicks can only happen on NavMeshSurface
            {
                if (clickCoroutine == null)
                {
                    clickCoroutine = StartCoroutine(SingleClickCoroutine());
                }
                else
                {
                    StopCoroutine(clickCoroutine);
                    clickCoroutine = null;
                    HandleDoubleClick();
                }
            } else
            {
                if (clickCoroutine != null)
                {
                    StopCoroutine(clickCoroutine);
                    clickCoroutine = null;
                }
                HandleSingleClick();
            }

        }
    }

    // PUBLIC

    public void Init(uint teamId)
    {
        Assert.IsTrue(IsServer);
        this.teamId = teamId;
        ACKInitClientRpc(teamId);
    }

    [ClientRpc]
    public void ACKInitClientRpc(uint teamId)
    {
        this.teamId = teamId;
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
            var unitObjName = hit.transform.gameObject.name;
            if (unitController != null)
                ChooseUnitServerRpc(unitObjName);
            else
                UnchooseUnitServerRpc();
        }
    }

    private void HandleDoubleClick()
    {
        // Move to point on double click

        if (haveChosenUnit &&
            previewPathMousePos != Vector3.zero)
        {
            MoveChoosenUnitServerRpc(
                chosenUnitController.gameObject.name, 
                new Vector2(previewPathMousePos.x, previewPathMousePos.z));
        }
    }

    // NETWORK

    [ServerRpc(RequireOwnership = true)]
    void ShowPathPreviewServerRpc(FixedString128Bytes unit, Vector3 previewPoint)
    {
        if (!_turnManager.CanMove(((int)teamId))) return;
        var unitObj = unit.GetGameObject();
        if (unitObj != null)
        {
            var unitController = unitObj.GetComponent<UnitController>();
            if (unitController != null && unitController.DrawPathPreview(previewPoint))
                ACKShowPathPreviewClientRpc(unit, previewPoint);
        }
    }

    [ClientRpc]
    void ACKShowPathPreviewClientRpc(FixedString128Bytes unit, Vector3 previewPoint)
    {
        if (IsHost) return; // Already did it on server

        var unitObj = unit.GetGameObject();
        if (unitObj != null)
        {
            var unitController = unitObj.GetComponent<UnitController>();
            if (unitController != null)
                unitController.DrawPathPreview(previewPoint);
        }
    }

    [ServerRpc(RequireOwnership = true)]
    void AttackUnitServerRpc(FixedString128Bytes attacker, FixedString128Bytes attackable)
    {
        var attackerObj = attacker.GetGameObject();
        var attackableObj = attackable.GetGameObject();
        if ( attackerObj != null && 
             attackerObj != null)
        {
            var attackerController = attackerObj.GetComponent<UnitController>();
            var attackableController = attackableObj.GetComponent<UnitController>();
            if (attackerController != null &&
                attackableController != null &&
                
                attackerController.UnitSettings.TeamId == teamId &&
                attackerController.UnitSettings.TeamId != 
                attackableController.UnitSettings.TeamId &&
                
                attackerController.AttackEnemy(attackableController) &&
                _turnManager.TryUseAttack((int)teamId)
                )
            {
                ACKAttackUnitClientRpc(attacker, attackable);
            }
        }
    }

    [ClientRpc]
    void ACKAttackUnitClientRpc(FixedString128Bytes attacker, FixedString128Bytes attackable)
    {
        if (IsHost) return; // Already did it on server
        var attackerObj = attacker.GetGameObject();
        var attackableObj = attackable.GetGameObject();
        if (attackerObj != null &&
             attackerObj != null)
        {
            var attackerController = attackerObj.GetComponent<UnitController>();
            var attackableController = attackableObj.GetComponent<UnitController>();
            if (attackerController != null &&
                attackableController != null)
                attackerController.AttackEnemy(attackableController);
        }
    }

    // destination - world coordinates(x and z)
    [ServerRpc(RequireOwnership = true)]
    void MoveChoosenUnitServerRpc(FixedString128Bytes unit, Vector2 destination)
    {
        if (!_turnManager.TryUseMove((int)teamId)) return;
        var unitObj = unit.GetGameObject();
        if (unitObj != null)
        {
            var unitController = unitObj.GetComponent<UnitController>();
            if (unitController != null && 
                teamId == unitController.UnitSettings.TeamId && 
                chosenUnitController.MoveTo(new Vector3(destination.x, 0, destination.y)))
            {
                ACKMoveChoosenUnitClientRpc(unit, destination);
            }
        }
    }

    [ClientRpc]
    void ACKMoveChoosenUnitClientRpc(FixedString128Bytes unit, Vector2 destination)
    {
        if (IsHost) return; // Already did it on server
        var unitObj = unit.GetGameObject();
        if (unitObj != null)
        {
            var unitController = unitObj.GetComponent<UnitController>();
            if (unitController != null)
                chosenUnitController.MoveTo(new Vector3(destination.x, 0, destination.y));
        }
    }
    
    void UnchooseUnit()
    {
        if (!haveChosenUnit) return;
        chosenUnitController.Deselect();
        chosenUnitController = null;
    }

    [ServerRpc(RequireOwnership = true)]
    void UnchooseUnitServerRpc()
    {
        UnchooseUnit();
        ACKUnchooseUnitClientRpc();
    }

    [ClientRpc]
    void ACKUnchooseUnitClientRpc()
    {
        if (IsHost) return; // Already did it on server
        UnchooseUnit();
    }

    [ServerRpc(RequireOwnership = true)]
    void ChooseUnitServerRpc(FixedString128Bytes unit)
    {
        if (!_turnManager.CanAttack(((int)teamId)) &&
            !_turnManager.CanAttack(((int)teamId)))
            return;
        if (haveChosenUnit)
            UnchooseUnitServerRpc();
        var unitObj = unit.GetGameObject();
        if (unitObj != null)
        {
            var unitController = unitObj.GetComponent<UnitController>();
            if (unitController == null ||
                unitController.UnitSettings.TeamId != teamId)
                return;
            chosenUnitController = unitController;
            if (chosenUnitController == unitController) // if updated successfully
            {
                chosenUnitController.Select();
                ACKChooseUnitClientRpc(unit);
            }
        }
    }

    [ClientRpc]
    void ACKChooseUnitClientRpc(FixedString128Bytes unit)
    {
        if (IsHost) return; // Already did it on server
        var unitObj = unit.GetGameObject();
        if (unitObj != null)
        {
            var unitController = unitObj.GetComponent<UnitController>();
            chosenUnitController = unitController;
            chosenUnitController.Select();
        }
    }
}
