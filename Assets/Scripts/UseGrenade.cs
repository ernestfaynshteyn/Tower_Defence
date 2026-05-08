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

    [Header("Trajectory Line")]
    public LineRenderer trajectoryLine;
    public int trajectoryPoints = 30;

    [Header("Cursor")]
    public Texture2D defaultCursor;
    public Texture2D equippedCursor;
    public Vector2 hotspot = Vector2.zero;

    public bool isEquipped = false;

    private Camera mainCamera;

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
            ThrowGrenadeToMouse();
        }
    }

    void UpdateCursor()
    {
        if (isEquipped)
            Cursor.SetCursor(equippedCursor, hotspot, CursorMode.Auto);
        else
            Cursor.SetCursor(defaultCursor, hotspot, CursorMode.Auto);
    }

    void UpdateTrajectoryLine()
    {
        if (trajectoryLine == null) return;

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

    void HandleGrenadeSelection()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            selectedGrenade = GrenadeType.Frag;
            Debug.Log("Selected Frag Grenade");
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            selectedGrenade = GrenadeType.Smoke;
            Debug.Log("Selected Smoke Grenade");
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            selectedGrenade = GrenadeType.Flash;
            Debug.Log("Selected Flash Grenade");
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            selectedGrenade = GrenadeType.Molotov;
            Debug.Log("Selected Molotov Grenade");
        }
    }
    void ThrowGrenadeToMouse()
    {
        if (!isEquipped) return;

        if (!Inventory.instance.UseGrenade(selectedGrenade))
            return;

        Vector2 startPos = throwPoint.position;
        Vector2 targetPos = GetMouseWorldPosition();

        GameObject grenade = Instantiate(grenadePrefab, startPos, Quaternion.identity);

        Grenades grenadeScript = grenade.GetComponent<Grenades>();
        if (grenadeScript != null)
        {
            grenadeScript.grenadeType = selectedGrenade;
        }

        Rigidbody2D rb = grenade.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            StartCoroutine(MoveGrenadeInCurve(rb, startPos, targetPos));
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

    IEnumerator MoveGrenadeInCurve(Rigidbody2D rb, Vector2 start, Vector2 target)
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
            rb.gameObject.GetComponent<grenadedamage>().Explosion();
        }
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