using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections; 

public class IntroManager : MonoBehaviour
{
    [Header("La Storia")]
    [TextArea(3, 10)]
    public string[] storyBlocks; 

    [Header("Impostazioni Gioco")]
    // --- MODIFICA QUI: Mettiamo il nome esatto della tua scena ---
    public string gameSceneName = "Demo"; 
    
    [Tooltip("Velocità scrittura lettere (Consigliato: 0.04 - 0.06)")]
    public float typingSpeed = 0.05f; 

    [Header("Timings Regia")]
    [Tooltip("Durata del nero iniziale prima che succeda qualcosa")]
    public float initialWait = 4.0f;        
    public float fadeDuration = 4.0f;       
    public float delayAfterPickup = 1.5f;   
    public float delayBetweenCharAndBubble = 1.0f; 

    [Header("Animazioni UI")]
    public float popBubbleDuration = 0.5f;     
    public float characterFadeDuration = 2.0f; 

    [Header("Riferimenti Scena")]
    public CanvasGroup blackScreen; 
    public GameObject characterObj; 
    public GameObject bubbleObj;    
    public GameObject nextButton;   

    [Header("Riferimenti Testo")]
    public TextMeshProUGUI textDisplay; 

    [Header("Audio")]
    public AudioSource sfxSource;   
    public AudioClip ringSound;     
    public AudioClip pickupSound;
    public AudioClip typingSound; 

    private int currentIndex = 0;
    private bool sequenceFinished = false; 
    private bool isTyping = false;         
    private Coroutine typingCoroutine;     
    private Image characterImage; 

    void Start()
    {
        textDisplay.text = ""; 
        if (nextButton != null) nextButton.SetActive(false);
        
        // SETUP OMINO
        if (characterObj != null) 
        {
            characterObj.SetActive(false);
            characterImage = characterObj.GetComponent<Image>();
            if (characterImage != null)
            {
                Color c = characterImage.color;
                c.a = 0f; 
                characterImage.color = c;
            }
        }
        
        // SETUP NUVOLA
        if (bubbleObj != null) 
        {
            bubbleObj.SetActive(false);
            bubbleObj.transform.localScale = new Vector3(0f, 1f, 1f); 
        }

        if (blackScreen != null) blackScreen.alpha = 1f;

        StartCoroutine(IntroSequence());
    }

    void Update()
    {
        // --- Avanzamento con tastiera (INVIO o SPAZIO) ---
        if (sequenceFinished)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            {
                OnClickNext();
            }
        }
        // --------------------------------------------------
    }

    IEnumerator IntroSequence()
    {
        // 1. BUIO (4 sec)
        yield return new WaitForSeconds(initialWait);

        // 2. SQUILLO
        if (sfxSource != null && ringSound != null)
        {
            sfxSource.clip = ringSound;
            sfxSource.Play();
        }

        StartCoroutine(FadeOutBlackScreen());

        if (ringSound != null)
            yield return new WaitForSeconds(ringSound.length);
        else
            yield return new WaitForSeconds(3.0f);

        // 3. CORNETTA
        if (sfxSource != null && pickupSound != null)
        {
            sfxSource.PlayOneShot(pickupSound);
            yield return new WaitForSeconds(pickupSound.length); 
        }

        // 4. PAUSA
        yield return new WaitForSeconds(delayAfterPickup);

        // 5. OMINO
        if (characterObj != null)
        {
            characterObj.SetActive(true);
            yield return StartCoroutine(FadeInCharacter());
        }
        
        yield return new WaitForSeconds(delayBetweenCharAndBubble);

        // 6. NUVOLA
        if (bubbleObj != null)
        {
            bubbleObj.SetActive(true);
            yield return StartCoroutine(AnimateBubbleSlide());
        }
        
        yield return new WaitForSeconds(0.2f);

        // 7. START
        currentIndex = 0;
        if (nextButton != null) nextButton.SetActive(true); 
        sequenceFinished = true; 

        if (storyBlocks.Length > 0)
        {
            typingCoroutine = StartCoroutine(TypeText(storyBlocks[currentIndex]));
        }
    }

    // --- LOGICA PASSAGGIO SCENA ---
    public void OnClickNext()
    {
        if (!sequenceFinished) return;
        
        // Se sta scrivendo, completa il testo
        if (isTyping)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            textDisplay.text = storyBlocks[currentIndex]; 
            isTyping = false;
            if (sfxSource != null) sfxSource.pitch = 1f; 
            return;
        }

        // Se ha finito di scrivere, vai avanti
        currentIndex++;

        // Se ci sono ancora frasi, mostrale...
        if (currentIndex < storyBlocks.Length)
        {
            typingCoroutine = StartCoroutine(TypeText(storyBlocks[currentIndex]));
        }
        // ... ALTRIMENTI CARICA LA SCENA "DEMO"
        else
        {
            StartGame();
        }
    }

    void StartGame()
    {
        Debug.Log("Intro finita. Caricamento scena: " + gameSceneName);
        SceneManager.LoadScene(gameSceneName);
    }

    // --- LE ALTRE FUNZIONI (Invariate) ---
    IEnumerator TypeText(string content)
    {
        isTyping = true;
        textDisplay.text = ""; 
        foreach (char letter in content.ToCharArray())
        {
            textDisplay.text += letter; 
            if (sfxSource != null && typingSound != null)
            {
                sfxSource.pitch = Random.Range(0.9f, 1.1f);
                sfxSource.PlayOneShot(typingSound);
            }
            yield return new WaitForSeconds(typingSpeed);
        }
        isTyping = false;
        if (sfxSource != null) sfxSource.pitch = 1f; 
    }

    IEnumerator FadeInCharacter()
    {
        if (characterImage == null) yield break;
        float timer = 0f;
        Color startColor = characterImage.color;
        startColor.a = 0f; 
        characterImage.color = startColor;
        while (timer < characterFadeDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / characterFadeDuration;
            Color currentColor = characterImage.color;
            currentColor.a = Mathf.Lerp(0f, 1f, progress);
            characterImage.color = currentColor;
            yield return null;
        }
        Color finalColor = characterImage.color;
        finalColor.a = 1f;
        characterImage.color = finalColor;
    }

    IEnumerator AnimateBubbleSlide()
    {
        float timer = 0f;
        bubbleObj.transform.localScale = new Vector3(0f, 1f, 1f);
        while (timer < popBubbleDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / popBubbleDuration;
            float currentScaleX = Mathf.SmoothStep(0f, 1f, progress);
            bubbleObj.transform.localScale = new Vector3(currentScaleX, 1f, 1f);
            yield return null;
        }
        bubbleObj.transform.localScale = Vector3.one;
    }

    IEnumerator FadeOutBlackScreen()
    {
        if (blackScreen == null) yield break;
        float startAlpha = blackScreen.alpha;
        float time = 0;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            blackScreen.alpha = Mathf.Lerp(startAlpha, 0f, time / fadeDuration);
            yield return null;
        }
        blackScreen.alpha = 0f; 
        blackScreen.gameObject.SetActive(false); 
    }
}