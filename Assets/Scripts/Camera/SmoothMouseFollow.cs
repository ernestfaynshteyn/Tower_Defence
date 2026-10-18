using UnityEngine;
using CodeMonkey.Utils;
using System.Collections.Generic;

public class SmoothMouseFollow : MonoBehaviour
{
    private Transform aimTransform;
    private RangeIndicatorScaler rangeIndicator;
    private readonly List<Collider2D> targetColliders = new List<Collider2D>(64);
    private ContactFilter2D targetFilter;

    public bool HasAutomaticTarget { get; private set; }
    public Vector2 AimDirection { get; private set; } = Vector2.right;

    private void Awake()
    {
        aimTransform = transform.Find("Aim");
        rangeIndicator = GetComponent<RangeIndicatorScaler>();
        targetFilter = ContactFilter2D.noFilter;
        targetFilter.useTriggers = Physics2D.queriesHitTriggers;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            GlobalData.AutomaticPrimaryAiming = !GlobalData.AutomaticPrimaryAiming;
            Debug.Log("Auto shoot " + (GlobalData.AutomaticPrimaryAiming ? "ON" : "OFF") + ".");
        }

        HandlingAiming();
    }

    private void HandlingAiming()
    {
        Vector2 origin = transform.position;
        Vector2 aimPosition;

        if (GlobalData.AutomaticPrimaryAiming && TryFindNearestEnemy(out EnemyHealth target))
        {
            HasAutomaticTarget = true;
            aimPosition = target.transform.position;
        }
        else
        {
            HasAutomaticTarget = false;
            aimPosition = UtilsClass.GetMouseWorldPosition();
        }

        Vector2 direction = aimPosition - origin;
        if (direction.sqrMagnitude > 0.0001f)
            AimDirection = direction.normalized;

        float angle = Mathf.Atan2(AimDirection.y, AimDirection.x) * Mathf.Rad2Deg;
        if (aimTransform != null)
            aimTransform.eulerAngles = new Vector3(0, 0, angle);
        transform.eulerAngles = new Vector3(0, 0, angle);
    }

    private bool TryFindNearestEnemy(out EnemyHealth nearest)
    {
        nearest = null;
        Vector2 center = rangeIndicator != null ? rangeIndicator.WorldCenter : (Vector2)transform.position;
        float radius = rangeIndicator != null && rangeIndicator.WorldRadius > 0f
            ? rangeIndicator.WorldRadius
            : 100f;

        targetColliders.Clear();
        Physics2D.OverlapCircle(center, radius, targetFilter, targetColliders);

        float bestDistance = float.PositiveInfinity;
        for (int i = 0; i < targetColliders.Count; i++)
        {
            Collider2D targetCollider = targetColliders[i];
            EnemyHealth enemy = targetCollider != null
                ? targetCollider.GetComponentInParent<EnemyHealth>()
                : null;
            if (enemy == null || enemy.CurrentHealth <= 0f)
                continue;

            float distance = ((Vector2)enemy.transform.position - center).sqrMagnitude;
            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            nearest = enemy;
        }

        return nearest != null;
    }
}
