using System;
using UnityEngine;

// One contextual panel at a time. The hotbar is intentionally outside this session.
public sealed class InventoryPanelSession : MonoBehaviour
{
    [SerializeField] private Collider2D interactionArea;
    private static InventoryPanelSession active;
    private Action close;
    private Transform player;
    private bool requiresProximity;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() { active = null; }

    public static InventoryPanelSession Bind(GameObject panel, Collider2D area)
    {
        var session = panel.GetComponent<InventoryPanelSession>();
        if (session == null) session = panel.AddComponent<InventoryPanelSession>();
        session.interactionArea = area;
        return session;
    }

    public static void Open(GameObject panel, Action closePanel)
    {
        if (panel == null) return;
        var session = panel.GetComponent<InventoryPanelSession>();
        if (session == null) session = panel.AddComponent<InventoryPanelSession>();
        if (active != null && active != session) active.Close();
        CancelMovement();
        active = session;
        session.close = closePanel;
        session.requiresProximity = session.interactionArea != null;
    }

    public static void Closed(GameObject panel)
    {
        if (active == null || active.gameObject != panel) return;
        active.close = null;
        active = null;
        CancelMovement();
    }

    private static void CancelMovement()
    {
        InventoryStackCursor.Cancel();
        ItemDragHandler.CancelAllDrags();
    }

    private void LateUpdate()
    {
        if (active != this || !requiresProximity) return;
        if (interactionArea == null || !interactionArea.enabled || !interactionArea.gameObject.activeInHierarchy)
        {
            Close();
            return;
        }
        if (player == null)
        {
            var actor = GameObject.FindGameObjectWithTag("Player");
            if (actor != null) player = actor.transform;
        }
        if (player != null)
        {
            foreach (var collider in player.GetComponentsInChildren<Collider2D>())
                if (collider.enabled && collider.CompareTag("Player") && interactionArea.IsTouching(collider)) return;
        }
        Close();
    }

    private void Close()
    {
        if (active != this) return;
        var callback = close;
        close = null;
        active = null;
        CancelMovement();
        callback?.Invoke();
    }

    private void OnDisable() { Close(); }
}
