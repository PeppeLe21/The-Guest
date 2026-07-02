using UnityEngine;
using UnityEngine.EventSystems; // Fondamentale per rilevare il mouse

public class SimpleButtonAnim : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Impostazioni Animazione")]
    [Tooltip("Quanto diventa grande quando ci passi sopra (es. 1.1 = 110%)")]
    public float hoverScale = 1.1f;
    [Tooltip("Quanto diventa piccolo quando clicchi (es. 0.95 = 95%)")]
    public float clickScale = 0.95f;
    [Tooltip("Velocità dell'animazione")]
    public float speed = 10f;

    private Vector3 originalScale;
    private Vector3 targetScale;

    void Awake()
    {
        // Memorizza la grandezza originale all'avvio (così se il bottone è grande torna grande)
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    void OnEnable()
    {
        // Quando il bottone viene riattivato, resetta la scala
        transform.localScale = originalScale;
        targetScale = originalScale;
    }

    void Update()
    {
        // Questa riga fa la magia: sposta dolcemente la scala attuale verso quella desiderata
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * speed);
    }

    // --- EVENTI DEL MOUSE ---

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Mouse sopra -> Ingrandisci
        targetScale = originalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Mouse esce -> Torna normale
        targetScale = originalScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Click premuto -> Rimpicciolisci un po' (effetto pressione)
        targetScale = originalScale * clickScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Click rilasciato -> Torna allo stato "Sopra" (perché il mouse è ancora lì)
        targetScale = originalScale * hoverScale;
    }
}