using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Equiptable_Items : MonoBehaviour
{
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // cek item ini sedang diequip
        if (!IsEquipped()) return;

        if (PlayerInputManager.Instance.InteractOBJ && !InventorySystem.Instance.isOpen)
        {
            animator.SetTrigger("Tangkap");
            // DIPERBAIKI: reset hanya saat input diterima
            PlayerInputManager.Instance.ResetInteractOBJInput();
        }
    }

    private bool IsEquipped()
    {
        return EquipSystem.Instance.isnowEquipped &&
               transform.parent == EquipSystem.Instance.ToolsHolder.transform;
    }
}