using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Tambahkan ke setiap GameObject quick slot agar klik kiri dengan kursor
/// memilih slot tersebut — setara dengan menekan tombol angka yang sesuai.
/// </summary>
public class QuickSlotClickHandler : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("Index slot ini (1-6), sesuai urutan quick slot dari kiri ke kanan.")]
    public int slotIndex;

    /// <summary>
    /// Dipanggil saat pointer klik di atas slot ini (hanya klik kiri).
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (EquipSystem.Instance == null) return;

        EquipSystem.Instance.SelectSlot(slotIndex);
    }
}
