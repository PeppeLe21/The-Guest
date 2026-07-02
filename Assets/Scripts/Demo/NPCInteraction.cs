using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Componente principale per NPC interagibili.
/// Gestisce dialogo, movimento con NavMesh e animazioni.
/// </summary>
public class NPCInteraction : MonoBehaviour, IInteractable
{
    [Header("Configurazione NPC")]
    public NPCProfile npcProfile;

    [Header("Movimento")]
    [Tooltip("Dove va l'NPC quando ti invita dentro.")]
    public Transform insidePoint;
    
    [Header("Impostazioni NavMesh")]
    [Tooltip("Velocità di camminata.")]
    public float walkSpeed = 2f;
    
    [Tooltip("Velocità di rotazione.")]
    public float angularSpeed = 360f;

    // Componenti (auto-detect)
    private NPCMemory _memory;
    private NavMeshAgent _agent;
    private Animator _animator;
    
    // Stato
    private bool _hasInvitedPlayer = false;
    private bool _isMoving = false;
    
    // Posizione iniziale (dove stava alla porta)
    private Vector3 _doorPosition;
    private Quaternion _doorRotation;
    
    // Traccia dove sta andando
    private enum Destination { None, Inside, Door }
    private Destination _currentDestination = Destination.None;

    // Proprietà pubbliche
    public NPCMemory Memory => _memory;
    public bool HasInvitedPlayer => _hasInvitedPlayer;
    public bool IsMoving => _isMoving;

    private void Start()
    {
        // Auto-detect componenti
        _memory = GetComponent<NPCMemory>();
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();

        // Validazione
        if (_memory == null) 
            Debug.LogError($"[NPCInteraction] Manca NPCMemory su {gameObject.name}!");
        
        if (npcProfile == null)
            Debug.LogError($"[NPCInteraction] Manca NPCProfile su {gameObject.name}!");

        // Configura NavMeshAgent
        if (_agent != null)
        {
            _agent.speed = walkSpeed;
            _agent.angularSpeed = angularSpeed;
            _agent.updateRotation = true;
            _agent.updatePosition = true;
            _agent.autoBraking = true;
            
            // Aggancia automaticamente l'NPC alla NavMesh più vicina
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                // Salva la posizione iniziale (dove stava alla porta)
                _doorPosition = hit.position;
                _doorRotation = transform.rotation;
                
                _agent.Warp(hit.position);
                Debug.Log($"[NPCInteraction] {name} agganciato alla NavMesh. Posizione porta salvata.");
            }
            else
            {
                Debug.LogWarning($"[NPCInteraction] {name}: Nessuna NavMesh trovata entro 2 metri!");
            }
        }
        else
        {
            Debug.LogWarning($"[NPCInteraction] {name}: Manca NavMeshAgent! L'NPC non potrà camminare.");
        }
    }

    private void Update()
    {
        // Aggiorna animazione in base alla velocità
        UpdateAnimation();
        
        // Controlla se è arrivato a destinazione
        if (_isMoving && _agent != null)
        {
            if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
            {
                OnReachedDestination();
            }
        }
    }

    /// <summary>
    /// Aggiorna il parametro Speed dell'Animator
    /// </summary>
    private void UpdateAnimation()
    {
        if (_animator == null || _agent == null) return;
        
        // Passa la velocità reale all'Animator
        float speed = _agent.velocity.magnitude;
        _animator.SetFloat("Speed", speed);
    }

    /// <summary>
    /// Chiamato quando l'NPC arriva a destinazione
    /// </summary>
    private void OnReachedDestination()
    {
        _isMoving = false;
        Debug.Log($"[NPCInteraction] {npcProfile?.characterName} è arrivato a destinazione.");
        
        // Applica la rotazione corretta in base alla destinazione
        switch (_currentDestination)
        {
            case Destination.Inside:
                if (insidePoint != null)
                    transform.rotation = insidePoint.rotation;
                break;
                
            case Destination.Door:
                transform.rotation = _doorRotation;
                break;
        }
        
        _currentDestination = Destination.None;
    }

    // NOTA: GetTrustContext() rimosso - il trust è ora gestito da NPCProfile.GetInitialTrustForCostume()

    /// <summary>
    /// Chiamato quando l'LLM decide di invitarci (action: INVITE_IN)
    /// </summary>
    public void InvitePlayer()
    {
        if (_hasInvitedPlayer) return;

        Debug.Log($"[NPCInteraction] {npcProfile?.characterName}: 'Prego, entra pure!'");
        _hasInvitedPlayer = true;
    }

    /// <summary>
    /// Interazione con l'NPC (quando il player preme E)
    /// </summary>
    public void Interact(PlayerController player)
    {
        if (DialogueUI.IsDialogueOpen) return;
        if (Managers.Dialogue != null && Managers.Dialogue.IsInDialogue) return;

        Debug.Log($"[NPCInteraction] Interazione con {npcProfile?.characterName}");

        if (Managers.Dialogue != null)
        {
            Managers.Dialogue.StartDialogue(gameObject, openDoorFirst: false);
        }
    }

    /// <summary>
    /// Ordina all'NPC di camminare verso l'insidePoint
    /// </summary>
    public void MoveToInside()
    {
        if (insidePoint == null)
        {
            Debug.LogWarning($"[NPCInteraction] {name}: InsidePoint non assegnato!");
            return;
        }

        if (_agent == null)
        {
            Debug.LogWarning($"[NPCInteraction] {name}: NavMeshAgent mancante! Teletrasporto.");
            transform.position = insidePoint.position;
            transform.rotation = insidePoint.rotation;
            return;
        }

        if (!_agent.isOnNavMesh)
        {
            Debug.LogError($"[NPCInteraction] {name}: L'NPC non è sulla NavMesh! " +
                          "Verifica che sia posizionato sopra una superficie blu.");
            return;
        }

        // Inizia il movimento
        _isMoving = true;
        _currentDestination = Destination.Inside;
        _agent.isStopped = false;
        _agent.SetDestination(insidePoint.position);
        
        Debug.Log($"[NPCInteraction] {npcProfile?.characterName} sta camminando verso {insidePoint.name}...");
    }

    /// <summary>
    /// Ferma immediatamente l'NPC
    /// </summary>
    public void StopMoving()
    {
        if (_agent != null)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
        }
        _isMoving = false;
    }

    /// <summary>
    /// Ordina all'NPC di tornare alla porta (posizione iniziale)
    /// </summary>
    public void ReturnToDoor()
    {
        if (_agent == null || !_agent.isOnNavMesh)
        {
            // Fallback: teletrasporto
            transform.position = _doorPosition;
            transform.rotation = _doorRotation;
            return;
        }

        _isMoving = true;
        _currentDestination = Destination.Door;
        _agent.isStopped = false;
        _agent.SetDestination(_doorPosition);
        
        Debug.Log($"[NPCInteraction] {npcProfile?.characterName} sta tornando alla porta...");
    }

    /// <summary>
    /// Resetta lo stato di invito (quando il player esce dalla casa)
    /// </summary>
    public void ResetInvitation()
    {
        _hasInvitedPlayer = false;
        Debug.Log($"[NPCInteraction] {npcProfile?.characterName}: Invito resettato.");
    }
}