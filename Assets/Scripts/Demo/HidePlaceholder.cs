using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Fondamentale per rilevare il click
using TMPro;

public class HidePlaceholder : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [Header("Trascina qui i componenti")]
    public TMP_InputField myInputField; // La casella di input
    public GameObject placeholderObject; // L'oggetto Placeholder (il testo grigio)

    // Quando CLICCHI sulla casella
    public void OnSelect(BaseEventData eventData)
    {
        // Nascondi subito il placeholder
        if(placeholderObject != null)
            placeholderObject.SetActive(false);
    }

    // Quando CLICCHI FUORI dalla casella
    public void OnDeselect(BaseEventData eventData)
    {
        // Riaccendi il placeholder SOLO se non hai scritto nulla
        if (myInputField != null && string.IsNullOrEmpty(myInputField.text))
        {
            if(placeholderObject != null)
                placeholderObject.SetActive(true);
        }
    }
}