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

    [Header("Audio")]
    [SerializeField] private AudioSource useItemSfxSource;
    [SerializeField] private AudioClip useItemSfx;
    [SerializeField, Range(0f, 1f)] private float useItemSfxVolume = 1f;

    void Start()
    {
        animator = GetComponent<Animator>();
        sphereCollider = GetComponent<SphereCollider>();

        sphereCollider.enabled = false;
        animator.enabled = false;
    }

    void Update()
    {
        if (QuizSessionLock.IsLocked)
        {
            if (PlayerInputManager.Instance != null)
                PlayerInputManager.Instance.ResetInteractOBJInput();
            return;
        }

        if (!IsEquipped())
        {
            return;            
        }


        // Jika masih cooldown / sedang animasi
        if (isUsingItem)
        {
            return;
        }

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

        PlayUseItemSfx();
        animator.SetTrigger("Tangkap");

        EnableSphereCollider();

        // Delay cooldown
        yield return new WaitForSeconds(interactCooldown);

        DisableSphereCollider();

        isUsingItem = false;
    }

    private void PlayUseItemSfx()
    {
        if (useItemSfxSource == null || useItemSfx == null)
            return;

        useItemSfxSource.PlayOneShot(useItemSfx, Mathf.Clamp01(useItemSfxVolume));
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
        bool isEquipped = EquipSystem.Instance.isnowEquipped &&
                          transform.parent == EquipSystem.Instance.ToolsHolder.transform;
        
        animator.enabled = isEquipped;
        
        return isEquipped;
    }
}
