using UnityEngine;

public class SuspectButton : MonoBehaviour
{
    public string idSospettato;      // Metti qui "LeoValli"
    public GameObject outlineObj;    // Trascina qui la cornice rossa Selected_Outline

    public void CliccaSospettato()
    {
        // Questa funzione non ha parametri, quindi Unity la vede!
        // Fa da ponte e chiama quella complessa.
        AccusationUI.Instance.SelezionaSospettato(idSospettato, outlineObj);
    }
}