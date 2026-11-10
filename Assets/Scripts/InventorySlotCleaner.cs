
using UnityEngine;
using UnityEngine.UI;

public static class InventorySlotCleaner
{
    public static void Clean(Transform container)
    {
        if (container == null)
        {
            Debug.LogError("[INVENTÁRIO] Container não encontrado.");
            return;
        }

        Slot[] slots = container.GetComponentsInChildren<Slot>(true);

        int cleanedSlots = 0;

        foreach (Slot slot in slots)
        {
            bool hasData =
                slot.inventoryItem != null &&
                slot.inventoryItem.itemPrefab != null;

            bool hasVisual =
                slot.currentItem != null &&
                HasImage(slot.currentItem);

            // Se não houver dados ou imagem, o slot deve ser zerado.
            if (!hasData || !hasVisual)
            {
                if (slot.currentItem != null)
                    Object.Destroy(slot.currentItem);

                slot.currentItem = null;
                slot.inventoryItem = null;

                slot.UpdateQuantityDisplay();

                cleanedSlots++;
            }
        }

        Debug.Log(
            "[INVENTÁRIO] Limpeza concluída. Slots verificados: " +
            slots.Length +
            " | Slots zerados: " +
            cleanedSlots
        );
    }

    private static bool HasImage(GameObject item)
    {
        if (item == null)
            return false;

        Image image = item.GetComponentInChildren<Image>(true);

        if (image != null && image.sprite != null)
            return true;

        RawImage rawImage = item.GetComponentInChildren<RawImage>(true);

        if (rawImage != null && rawImage.texture != null)
            return true;

        SpriteRenderer spriteRenderer =
            item.GetComponentInChildren<SpriteRenderer>(true);

        if (spriteRenderer != null && spriteRenderer.sprite != null)
            return true;

        return false;
    }
}
