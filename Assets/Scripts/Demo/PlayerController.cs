using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Settings")]
    public Transform cameraTransform;
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float rotationSpeed = 10f;
    public float gravity = -9.81f;

    // Componenti interni
    private CharacterController controller;
    private Vector3 velocity;
    private Animator animator;

    private const float inputDeadzone = 0.1f;

    // Variabile per il sistema di interazione
    private IInteractable currentInteractable = null; 

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    // ===== SISTEMA DI INTERAZIONE (MODIFICATO PER UI) =====
    private void OnTriggerEnter(Collider other)
    {
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null)
        {
            currentInteractable = interactable;
            Debug.Log("✅ Posso interagire con: " + other.name + ". Premi E!");
            
            // --- AGGIUNTA: MOSTRA L'IMMAGINE "PREMI E" ---
            if (InteractionUI.Instance != null && !DialogueUI.IsDialogueOpen) 
                InteractionUI.Instance.Show();
            // ---------------------------------------------
        }
    }

    private void OnTriggerExit(Collider other)
    {
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null && interactable == currentInteractable)
        {
            currentInteractable = null;
            Debug.Log("❌ Troppo lontano da: " + other.name);
            
            // --- AGGIUNTA: NASCONDI L'IMMAGINE ---
            if (InteractionUI.Instance != null) 
                InteractionUI.Instance.Hide();
            // -------------------------------------
        }
    }
    // ============================================

    void Update()
    {
        // 0. CHECK VALIDITY OF INTERACTABLE (Distrutto?)
        // Se l'oggetto è stato distrutto (es. raccolto), resettiamo tutto.
        if (currentInteractable != null && (currentInteractable as MonoBehaviour) == null)
        {
            currentInteractable = null;
            if (InteractionUI.Instance != null) InteractionUI.Instance.Hide();
        }
        // � GESTIONE VISIBILITÀ INTERAZIONE (FIX) 🟢
        // Deve stare PRIMA del return del dialogo, altrimenti non nasconde nulla!
        if (currentInteractable != null && InteractionUI.Instance != null)
        {
            if (DialogueUI.IsDialogueOpen)
            {
                // Nascondi se stiamo parlando
                if (InteractionUI.Instance.gameObject.activeSelf) InteractionUI.Instance.Hide();
            }
            else
            {
                // Mostra se siamo vicini e liberi
                if (!InteractionUI.Instance.gameObject.activeSelf) InteractionUI.Instance.Show();
            }
        }

        // 🔒 Se il dialogo è aperto O il notebook è aperto O il guardaroba è aperto, niente movimento
        if (DialogueUI.IsDialogueOpen || NotebookManager.IsNotebookOpen || WardrobeController.IsWardrobeOpen)
        {
            if (animator != null) animator.SetFloat("Speed", 0f);

            // Mantieni la gravità attiva
            // if (controller.isGrounded && velocity.y < 0f) velocity.y = -2f; 
            // velocity.y += gravity * Time.deltaTime;
            // controller.Move(new Vector3(0f, velocity.y, 0f) * Time.deltaTime);
            
            // Fix: Resetta totalmente la velocity per evitare scivolamenti strani
            return; 
        }

        // (Logica spostata sopra)

        // 🟢 INPUT INTERAZIONE 🟢
        if (currentInteractable != null && Input.GetKeyDown(KeyCode.E) && !DialogueUI.IsDialogueOpen)
        {
            currentInteractable.Interact(this);
        }

        // ===== MOVIMENTO NORMALE =====

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 inputDir = new Vector3(horizontal, 0f, vertical);

        if (inputDir.sqrMagnitude < inputDeadzone * inputDeadzone) inputDir = Vector3.zero;

        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float currentSpeedValue = isRunning ? runSpeed : walkSpeed;

        Vector3 moveDir = Vector3.zero;
        if (inputDir != Vector3.zero)
        {
            if (cameraTransform != null)
            {
                Vector3 camForward = cameraTransform.forward;
                camForward.y = 0f;
                camForward.Normalize();

                Vector3 camRight = cameraTransform.right;
                camRight.y = 0f;
                camRight.Normalize();

                moveDir = camForward * inputDir.z + camRight * inputDir.x;
            }
            else
            {
                moveDir = inputDir;
            }

            moveDir.Normalize();

            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        Vector3 horizontalVelocity = moveDir * currentSpeedValue;

        if (animator != null)
        {
            float animSpeed = (inputDir == Vector3.zero) ? 0f : horizontalVelocity.magnitude;
            animator.SetFloat("Speed", animSpeed);
        }

        if (controller.isGrounded && velocity.y < 0f) velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;

        Vector3 finalVelocity = horizontalVelocity + new Vector3(0f, velocity.y, 0f);
        controller.Move(finalVelocity * Time.deltaTime);
    }

    public void UpdateAnimator(Animator newAnimator)
    {
        if (newAnimator == null) 
        {
             Debug.LogError("⛔ ATTENZIONE: Sto provando ad assegnare un Animator VUOTO (Null)!");
             return;
        }

        this.animator = newAnimator;
        Debug.Log($"✅ PlayerController: Animator aggiornato con successo su '{newAnimator.name}'");
    }
}