using System.Collections;
using UnityEngine;

public class UseGrenade : MonoBehaviour
{
    public GameObject grenadePrefab;
    public Transform throwPoint;

    public GrenadeType selectedGrenade = GrenadeType.Frag;

    [Header("Throw Settings")]
    public float flightTime = 0.8f;
    public float curveHeight = 2f;
    public float throwAnimationDelay = 0.25f;

    [Header("Trajectory Line")]
    public LineRenderer trajectoryLine;
    public int trajectoryPoints = 30;

    [Header("Cursor")]
    public Texture2D defaultCursor;
    public Texture2D equippedCursor;
    public Vector2 hotspot = Vector2.zero;

    [Header("Grenade Prefabs")]
    public GameObject fragPrefab;
    public GameObject smokePrefab;
    public GameObject flashPrefab;
    public GameObject molotovPrefab;

    [Header("Grenade Animations")]
    public Animator playerAnimator;

    [Header("Explosion Settings")]
    public float explosionRadius = 2.5f;
    public int explosionDamage = 25;
    public LayerMask enemyLayer;
    public Color explosionColor = Color.red;
    public int explosionCircleSegments = 40;
    public float explosionLineWidth = 0.05f;
    public float stunDuration = 2f;
    public float molotovBurnDuration = 3f;
    public float molotovBurnDamage = 5f;

    public bool isEquipped = false;

    private Camera mainCamera;
    private bool isThrowing = false;

    void Start()
    {
        mainCamera = Camera.main;

        if (trajectoryLine == null)
        {
            trajectoryLine = GetComponent<LineRenderer>();
        }

        if (trajectoryLine != null)
        {
            trajectoryLine.useWorldSpace = true;
            trajectoryLine.enabled = false;
        }

        UpdateCursor();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ToggleEquip();
        }

        if (isEquipped)
        {
            HandleGrenadeSelection();
        }

        UpdateTrajectoryLine();

        if (Input.GetMouseButtonDown(0))
        {
            StartCoroutine(ThrowGrenadeRoutine());
        }
    }

    void HandleGrenadeSelection()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SelectGrenade(GrenadeType.Frag);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SelectGrenade(GrenadeType.Smoke);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SelectGrenade(GrenadeType.Flash);
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            SelectGrenade(GrenadeType.Molotov);
        }
    }

    void SelectGrenade(GrenadeType grenadeType)
    {
        selectedGrenade = grenadeType;

        Debug.Log("Selected " + selectedGrenade + " Grenade");
    }

    GameObject GetSelectedGrenadePrefab(GrenadeType grenadeType)
    {
        switch (grenadeType)
        {
            case GrenadeType.Frag:
                return fragPrefab;

            case GrenadeType.Smoke:
                return smokePrefab;

            case GrenadeType.Flash:
                return flashPrefab;

            case GrenadeType.Molotov:
                return molotovPrefab;

            default:
                return fragPrefab;
        }
    }


    IEnumerator ThrowGrenadeRoutine()
    {
        if (!isEquipped) yield break;
        if (isThrowing) yield break;

        GrenadeType grenadeToThrow = selectedGrenade;

        if (!Inventory.instance.UseGrenade(grenadeToThrow))
            yield break;

        isThrowing = true;

        yield return new WaitForSeconds(throwAnimationDelay);

        SpawnGrenadeToMouse(grenadeToThrow);

        isThrowing = false;
    }

    void SpawnGrenadeToMouse(GrenadeType grenadeType)
    {
        Vector2 startPos = throwPoint.position;
        Vector2 targetPos = GetMouseWorldPosition();

        grenadePrefab = GetSelectedGrenadePrefab(grenadeType);

        if (grenadePrefab == null)
        {
            Debug.LogWarning("No grenade prefab assigned for " + grenadeType);
            return;
        }

        GameObject grenade = Instantiate(grenadePrefab, startPos, Quaternion.identity);

        Grenades grenadeScript = grenade.GetComponent<Grenades>();

        if (grenadeScript != null)
        {
            grenadeScript.grenadeType = grenadeType;
        }

        Rigidbody2D rb = grenade.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            StartCoroutine(MoveGrenadeInCurve(rb, startPos, targetPos, grenadeType));
        }
    }

    void UpdateCursor()
    {
        if (isEquipped)
        {
            Cursor.SetCursor(equippedCursor, hotspot, CursorMode.Auto);
        }
        else
        {
            Cursor.SetCursor(defaultCursor, hotspot, CursorMode.Auto);
        }
    }

    void UpdateTrajectoryLine()
    {
        if (trajectoryLine == null)
        {
            return;
        }

        if (!isEquipped)
        {
            trajectoryLine.enabled = false;
            return;
        }

        trajectoryLine.enabled = true;
        trajectoryLine.positionCount = trajectoryPoints;

        Vector2 startPos = throwPoint.position;
        Vector2 targetPos = GetMouseWorldPosition();
        Vector2 controlPoint = GetCurveControlPoint(startPos, targetPos);

        for (int i = 0; i < trajectoryPoints; i++)
        {
            float t = i / (float)(trajectoryPoints - 1);
            Vector2 point = GetBezierPoint(startPos, controlPoint, targetPos, t);

            trajectoryLine.SetPosition(i, new Vector3(point.x, point.y, 0f));
        }
    }

    Vector2 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPos = Input.mousePosition;

        float distanceFromCamera = Mathf.Abs(mainCamera.transform.position.z - throwPoint.position.z);
        mouseScreenPos.z = distanceFromCamera;

        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0f;

        return mouseWorldPos;
    }

    Vector2 GetCurveControlPoint(Vector2 start, Vector2 target)
    {
        return (start + target) / 2f + Vector2.up * curveHeight;
    }

    Vector2 GetBezierPoint(Vector2 start, Vector2 control, Vector2 end, float t)
    {
        Vector2 posA = Vector2.Lerp(start, control, t);
        Vector2 posB = Vector2.Lerp(control, end, t);

        return Vector2.Lerp(posA, posB, t);
    }

    IEnumerator MoveGrenadeInCurve(Rigidbody2D rb, Vector2 start, Vector2 target, GrenadeType grenadeType)
    {
        float timer = 0f;

        Vector2 controlPoint = GetCurveControlPoint(start, target);

        while (timer < flightTime)
        {
            if (rb == null) yield break;

            timer += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(timer / flightTime);

            Vector2 curvePos = GetBezierPoint(start, controlPoint, target, t);

            rb.MovePosition(curvePos);

            yield return new WaitForFixedUpdate();
        }

        if (rb != null)
        {
            rb.MovePosition(target);
            rb.linearVelocity = Vector2.zero;

            GrenadeDamage grenadeDamage = rb.gameObject.GetComponent<GrenadeDamage>();

            if (grenadeDamage != null)
            {
                grenadeDamage.Explosion();
            }

            Explode(target, grenadeType);
        }
    }

    void Explode(Vector2 position, GrenadeType grenadeType)
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(position, explosionRadius, enemyLayer);

        foreach (Collider2D enemyCollider in hitEnemies)
        {
            EnemyHealth enemyHealth = enemyCollider.GetComponent<EnemyHealth>();

            if (enemyHealth == null) continue;

            if (grenadeType == GrenadeType.Flash)
            {
                enemyHealth.Stun(stunDuration);
            }
            else if (grenadeType == GrenadeType.Molotov)
            {
                enemyHealth.ApplyBurn(molotovBurnDuration, molotovBurnDamage);
            }
            else
            {
                enemyHealth.TakeDamage(explosionDamage);
            }
        }

        StartCoroutine(ShowExplosionCircle(position));
    }

    IEnumerator ShowExplosionCircle(Vector2 position)
    {
        GameObject circleObj = new GameObject("ExplosionRadius");
        circleObj.transform.position = position;

        LineRenderer lr = circleObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.startWidth = explosionLineWidth;
        lr.endWidth = explosionLineWidth;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = explosionColor;
        lr.endColor = explosionColor;
        lr.positionCount = explosionCircleSegments;

        for (int i = 0; i < explosionCircleSegments; i++)
        {
            float angle = i * (2f * Mathf.PI / explosionCircleSegments);
            Vector3 point = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * explosionRadius + (Vector3)position;
            lr.SetPosition(i, point);
        }

        yield return null; // wait exactly one frame

        Destroy(circleObj);
    }

    public void EquipGrenade()
    {
        isEquipped = true;
        UpdateCursor();
    }

    public void UnequipGrenade()
    {
        isEquipped = false;
        UpdateCursor();
    }

    public void ToggleEquip()
    {
        isEquipped = !isEquipped;
        UpdateCursor();
    }
}