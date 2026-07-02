using UnityEngine;

public class InteractionUI : MonoBehaviour
{
    public static InteractionUI Instance; // Per chiamarlo da ovunque

    private void Awake()
    {
        // Singleton pattern: assicura che ce ne sia solo uno
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        else Instance = this;
        
        Hide(); // Nascondi appena inizia il gioco
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}