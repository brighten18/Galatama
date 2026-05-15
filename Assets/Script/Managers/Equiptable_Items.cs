using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SphereCollider))]
public class Equiptable_Items : MonoBehaviour
{
    private Animator animator;
    private SphereCollider sphereCollider;

    private bool isUsingItem = false;

    [Header("Delay / Cooldown")]
    public float interactCooldown = 2f;

    void Start()
    {
        animator = GetComponent<Animator>();
        sphereCollider = GetComponent<SphereCollider>();

        sphereCollider.enabled = false;
    }

    void Update()
    {
        if (!IsEquipped())
            return;

        // Jika masih cooldown / sedang animasi
        if (isUsingItem)
            return;

        if (PlayerInputManager.Instance.InteractOBJ &&
            !InventorySystem.Instance.isOpen)
        {
            StartCoroutine(UseItemCoroutine());

            PlayerInputManager.Instance.ResetInteractOBJInput();
        }
    }

    IEnumerator UseItemCoroutine()
    {
        isUsingItem = true;

        animator.SetTrigger("Tangkap");

        EnableSphereCollider();

        // Delay cooldown
        yield return new WaitForSeconds(interactCooldown);

        DisableSphereCollider();

        isUsingItem = false;
    }

    public void EnableSphereCollider()
    {
        sphereCollider.enabled = true;
    }

    public void DisableSphereCollider()
    {
        sphereCollider.enabled = false;
    }

    private bool IsEquipped()
    {
        return EquipSystem.Instance.isnowEquipped &&
               transform.parent == EquipSystem.Instance.ToolsHolder.transform;
    }
}