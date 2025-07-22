using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitAttack : MonoBehaviour
{
    // VARIABLES

    private List<UnitController> _attackableEnemies = new();

    protected UnitSettings unitSettings;
    private SpriteRenderer _attackRangeRenderer;

    // PUBLIC

    public void Init(UnitSettings unitSettings, SpriteRenderer attackRangeRenderer)
    {
        this.unitSettings = unitSettings;
        _attackRangeRenderer = attackRangeRenderer;
    } 

    public void PreviewAttack(Vector2 targetPosition)
    {
        _attackRangeRenderer.transform.position = new Vector3(targetPosition.x, _attackRangeRenderer.transform.position.y, targetPosition.y);
        IndicateAttackable(targetPosition);
    }

    public void HideAttackPreview()
    {
        _attackRangeRenderer.enabled = false;
        HideAttackableIndicator();
    }

    // Returns all attackable enemies inside attack circle
    // targetPosition - center of attack circle(x and z coordinates in world space)
    public List<UnitController> GetAllAttackableEnemies(Vector2 targetPosition)
    {
        const int ColliderCheckHeight = 1000;

        List<UnitController> result = new();

        RaycastHit[] castResult = Physics.CapsuleCastAll(
            point1: new Vector3(targetPosition.x, transform.position.y - ColliderCheckHeight / 2, targetPosition.y),
            point2: new Vector3(targetPosition.x, transform.position.y + ColliderCheckHeight / 2, targetPosition.y),
            radius: unitSettings.AttackRadius,
            direction: Vector3.up
            );
        foreach (var castHit in castResult)
        {
            var castUnitController = castHit.transform.GetComponent<UnitController>();
            if (castUnitController != null &&
                unitSettings.TeamId != castUnitController.UnitSettings.TeamId)
                result.Add(castUnitController);
        }

        return result;
    }

    public bool AttackEnemy(UnitController enemy)
    {
        if (!_attackableEnemies.Contains(enemy)) return false;
        enemy.Kill(this);
        return true;
    }

    // PRIVATE

    // Delete all null elements from _attackableEnemies
    void UpdateAttackableEnemies()
    {
        _attackableEnemies.RemoveAll(item => item == null || item.IsDead);
    }

    // Makes indicator for all attackable enemies inside attack circle red
    // targetPosition - center of attack circle(x and z coordinates in world coordinates)
    private void IndicateAttackable(Vector2 targetPosition)
    {
        HideAttackableIndicator();
        foreach (var enemy in _attackableEnemies)
            enemy.OnDeath -= UpdateAttackableEnemies;
        _attackableEnemies = GetAllAttackableEnemies(targetPosition);
        foreach (var enemy in _attackableEnemies)
            enemy.OnDeath += UpdateAttackableEnemies;
        foreach (var unit in _attackableEnemies)
        {
            unit.ShowIndicator(UnitController.IndicatorType.Attackable);
        }
    }

    private void OnDestroy()
    {
        foreach (var enemy in _attackableEnemies)
            enemy.OnDeath -= UpdateAttackableEnemies;
    }

    private void HideAttackableIndicator()
    {
        foreach (var unit in _attackableEnemies)
        {
            unit.HideIndicator();
        }
    }
}
