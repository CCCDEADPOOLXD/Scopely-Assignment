using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class RouletteController : MonoBehaviour
{
    [Header("Wheel & Animation")]
    public Transform wheelTransform;
    public float spinDuration = 4f;
    public int extraSpins = 3;
    public Animator ballAnimator;
    public ParticleSystem[] confettiCannons;

    [Header("World Space VFX Prefabs (Optional)")]
    public GameObject chipPlaceVFXPrefab;
    public GameObject ballBlastVFXPrefab;
    public Transform ballBlastSpawnPoint; 

    [Header("Audio (Optional)")]
    public AudioSource sfxSource;
    public AudioSource wheelAudioSource;
    public AudioClip buttonTapClip;
    public AudioClip chipPlaceClip;
    public AudioClip wheelRollClip;
    public AudioClip cashRegisterClip;

    [Header("UI Panels (Attach CanvasGroups)")]
    public CanvasGroup namePanel;
    public CanvasGroup bettingUIPanel;
    public CanvasGroup resultPanel;

    [Header("Player & Timer UI")]
    public TMP_InputField nameInputField;
    public TextMeshProUGUI nameWarningText; 
    public TextMeshProUGUI playerNameDisplay;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI timerText;       
    
    public RectTransform timerRect; 
    public float totalGameTime = 60f;
    private float currentTime;
    private float maxTimerWidth;
    private bool gameIsActive = false;
    private bool isGameOver = false;

    [Header("Betting Data & VFX Tracking")]
    public GameObject coinPrefab;
    public GameObject floatingTextPrefab; 
    public Vector3 floatingTextOffset = new Vector3(0, -20f, 0); 
    public float floatingTextTravelY = -100f; 
    public float resultDelayAfterBlast = 2.0f; 

    public int playerCoins = 1000;
    public int defaultBetAmount = 50;
    private int startingCoins; 

    private bool isSpinning = false;
    private List<PlayerBet> activeBets = new List<PlayerBet>();
    private List<GameObject> activeBettingVFX = new List<GameObject>(); 

    private readonly int[] wheelNumbers = {
        0, 32, 15, 19, 4, 21, 2, 25, 17, 34, 6, 27, 13, 36, 11, 30, 8, 23, 10,
        5, 24, 16, 33, 1, 20, 14, 31, 9, 22, 18, 29, 7, 28, 12, 35, 3, 26
    };

    private readonly List<int> redNumbers = new List<int> {
        1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36
    };

    private void Start()
    {
        startingCoins = playerCoins;
        UpdateCoinsUI();
        
        if (timerRect != null) maxTimerWidth = timerRect.sizeDelta.x;
        if (timerText != null) timerText.text = Mathf.CeilToInt(totalGameTime).ToString() + "s";

        if (bettingUIPanel != null)
        {
            bettingUIPanel.alpha = 0; 
            bettingUIPanel.interactable = false; 
            bettingUIPanel.blocksRaycasts = false;
        }
        if (resultPanel != null)
        {
            resultPanel.alpha = 0; 
            resultPanel.interactable = false; 
            resultPanel.blocksRaycasts = false;
        }
    }

    private void Update()
    {
        if (gameIsActive)
        {
            currentTime -= Time.deltaTime;
            float clampedTime = Mathf.Max(0f, currentTime);
            
            if (timerRect != null)
            {
                float newWidth = Mathf.Lerp(0, maxTimerWidth, clampedTime / totalGameTime);
                timerRect.sizeDelta = new Vector2(newWidth, timerRect.sizeDelta.y);
            }

            if (timerText != null)
            {
                timerText.text = Mathf.CeilToInt(clampedTime).ToString() + "s";
            }

            if (currentTime <= 0)
            {
                TriggerGameOver();
            }
        }
    }

    public void PlayButtonTapSFX()
    {
        if (sfxSource != null && buttonTapClip != null)
            sfxSource.PlayOneShot(buttonTapClip);
    }

    public void OpenNameWindow()
    {
        PlayButtonTapSFX();
        if (nameWarningText != null)
        {
            nameWarningText.text = "Enter your name";
            nameWarningText.color = Color.white;
        }
        StartCoroutine(FadeCanvasGroup(namePanel, namePanel.alpha, 1f, 0.5f));
    }

    public void SubmitNameAndStart()
    {
        PlayButtonTapSFX();

        if (nameInputField.text.Length >= 4 && nameInputField.text.Length <= 20)
        {
            playerNameDisplay.text = nameInputField.text;
            
            if (nameWarningText != null)
            {
                nameWarningText.text = "Enter your name";
                nameWarningText.color = Color.white;
            }

            if (!gameIsActive)
            {
                currentTime = totalGameTime;
                gameIsActive = true;
                isGameOver = false;
            }

            StartCoroutine(FadeCanvasGroup(namePanel, 1f, 0f, 0.5f));
            StartCoroutine(FadeCanvasGroup(bettingUIPanel, bettingUIPanel.alpha, 1f, 0.5f));
        }
        else
        {
            if (nameWarningText != null)
            {
                nameWarningText.text = "Name must be between 4 and 20 letters!";
                nameWarningText.color = Color.red;
            }
        }
    }

    public void PlaceBet(string betZone)
    {
        if (isSpinning || playerCoins < defaultBetAmount) return;

        playerCoins -= defaultBetAmount;
        UpdateCoinsUI();

        if (sfxSource != null && chipPlaceClip != null)
            sfxSource.PlayOneShot(chipPlaceClip);
        
        StartCoroutine(AnimateFloatingText(-defaultBetAmount));

        GameObject clickedButton = EventSystem.current.currentSelectedGameObject;
        GameObject newCoin = null;

        if (clickedButton != null)
        {
            newCoin = Instantiate(coinPrefab, clickedButton.transform);
            Vector2 randomOffset = new Vector2(Random.Range(-5f, 5f), Random.Range(-5f, 5f));
            newCoin.GetComponent<RectTransform>().anchoredPosition = randomOffset;

            if (chipPlaceVFXPrefab != null && Camera.main != null)
            {
                Vector3 screenPos = clickedButton.transform.position;
                screenPos.z = Mathf.Abs(Camera.main.transform.position.z);
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
                worldPos.z = 0f;
                GameObject vfx = Instantiate(chipPlaceVFXPrefab, worldPos, Quaternion.identity);
                activeBettingVFX.Add(vfx);
                Destroy(vfx, 2f);
            }
        }

        activeBets.Add(new PlayerBet(betZone, defaultBetAmount, newCoin));
    }

    public void SpinWheel()
    {
        if (isSpinning || activeBets.Count == 0) return;
        PlayButtonTapSFX();
        StartCoroutine(SpinRoutine());
    }

    private IEnumerator SpinRoutine()
    {
        isSpinning = true;

        foreach (GameObject vfx in activeBettingVFX) { if (vfx != null) Destroy(vfx); }
        activeBettingVFX.Clear();

        StartCoroutine(FadeCanvasGroup(bettingUIPanel, 1f, 0f, 0.5f));

        if (wheelAudioSource != null && wheelRollClip != null)
        {
            wheelAudioSource.clip = wheelRollClip;
            wheelAudioSource.loop = true;
            wheelAudioSource.Play();
        }

        if (ballAnimator != null) ballAnimator.SetTrigger("Drop");

        int winningIndex = Random.Range(0, wheelNumbers.Length);
        int winningNumber = wheelNumbers[winningIndex];

        float pocketAngle = 360f / wheelNumbers.Length;
        float targetAngle = winningIndex * pocketAngle;
        float totalRotation = targetAngle + (360f * extraSpins);
        
        float currentRotation = wheelTransform.localEulerAngles.z;
        float elapsedTime = 0f;

        while (elapsedTime < spinDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / spinDuration;
            float tParam = t * t * (3f - 2f * t); 
            float newZ = Mathf.Lerp(currentRotation, currentRotation - totalRotation, tParam);
            
            wheelTransform.localEulerAngles = new Vector3(0, 0, newZ);
            yield return null;
        }

        wheelTransform.localEulerAngles = new Vector3(0, 0, currentRotation - totalRotation);

        if (wheelAudioSource != null) wheelAudioSource.Stop();

        // 1. Play the Blast/Reset Animation via Animator Trigger
        if (ballAnimator != null) ballAnimator.SetTrigger("Reset");

        // 2. Spawn Explosion VFX
        if (ballBlastVFXPrefab != null)
        {
            Vector3 spawnPos = ballBlastSpawnPoint != null ? ballBlastSpawnPoint.position : wheelTransform.position;
            GameObject blast = Instantiate(ballBlastVFXPrefab, spawnPos, Quaternion.identity);
            Destroy(blast, 2.5f);
        }

        // 3. Delay for the animation and explosion to finish before showing results
        yield return new WaitForSeconds(resultDelayAfterBlast);

        CalculatePayouts(winningNumber);
    }

    private void CalculatePayouts(int winNum)
    {
        int totalWon = 0;
        int totalBetThisRound = 0;

        foreach (PlayerBet bet in activeBets)
        {
            totalBetThisRound += bet.amount;
            bool wonThisBet = false;
            int payoutMultiplier = 0;

            if (int.TryParse(bet.betType, out int betNumber)) { if (winNum == betNumber) { wonThisBet = true; payoutMultiplier = 36; } }
            else if (bet.betType == "1st_12" && winNum >= 1 && winNum <= 12) { wonThisBet = true; payoutMultiplier = 3; }
            else if (bet.betType == "2nd_12" && winNum >= 13 && winNum <= 24) { wonThisBet = true; payoutMultiplier = 3; }
            else if (bet.betType == "3rd_12" && winNum >= 25 && winNum <= 36) { wonThisBet = true; payoutMultiplier = 3; }
            else if (bet.betType == "1-18" && winNum >= 1 && winNum <= 18) { wonThisBet = true; payoutMultiplier = 2; }
            else if (bet.betType == "19-36" && winNum >= 19 && winNum <= 36) { wonThisBet = true; payoutMultiplier = 2; }
            else if (bet.betType == "Even" && winNum % 2 == 0 && winNum != 0) { wonThisBet = true; payoutMultiplier = 2; }
            else if (bet.betType == "Odd" && winNum % 2 != 0) { wonThisBet = true; payoutMultiplier = 2; }
            else if (bet.betType == "Red" && redNumbers.Contains(winNum)) { wonThisBet = true; payoutMultiplier = 2; }
            else if (bet.betType == "Black" && !redNumbers.Contains(winNum) && winNum != 0) { wonThisBet = true; payoutMultiplier = 2; }

            if (wonThisBet) totalWon += bet.amount * payoutMultiplier;
            if (bet.coinVisual != null) Destroy(bet.coinVisual);
        }

        activeBets.Clear();
        playerCoins += totalWon;
        UpdateCoinsUI();

        if (sfxSource != null && cashRegisterClip != null)
            sfxSource.PlayOneShot(cashRegisterClip);

        if (totalWon > 0)
        {
            resultText.text = "Round Won!\n+" + totalWon + " Coins";
            StartCoroutine(AnimateFloatingText(totalWon));
            
            // Confetti cannons blast if the player wins
            foreach (ParticleSystem ps in confettiCannons) { if (ps != null) ps.Play(); }
        }
        else
        {
            resultText.text = "Round Lost!\n-" + totalBetThisRound + " Coins";
        }

        StartCoroutine(FadeCanvasGroup(resultPanel, 0f, 1f, 0.5f));
    }

    public void OnContinuePressed()
    {
        PlayButtonTapSFX();

        if (isGameOver)
        {
            RestartGameAndAskName();
            return;
        }

        StartCoroutine(FadeCanvasGroup(resultPanel, 1f, 0f, 0.4f));

        if (gameIsActive)
        {
            StartCoroutine(FadeCanvasGroup(bettingUIPanel, 0f, 1f, 0.5f));
            isSpinning = false; 
        }
    }

    private void TriggerGameOver()
    {
        gameIsActive = false;
        isGameOver = true;
        isSpinning = false;

        if (wheelAudioSource != null) wheelAudioSource.Stop();
        
        StartCoroutine(FadeCanvasGroup(bettingUIPanel, bettingUIPanel.alpha, 0f, 0.5f));

        int netProfit = playerCoins - startingCoins;
        
        if (netProfit > 0)
            resultText.text = "Time's Up!\nYou Won " + netProfit + " money overall!";
        else if (netProfit < 0)
            resultText.text = "Time's Up!\nYou Lost " + Mathf.Abs(netProfit) + " money overall!";
        else
            resultText.text = "Time's Up!\nYou broke even.";

        if (sfxSource != null && cashRegisterClip != null)
            sfxSource.PlayOneShot(cashRegisterClip);

        StartCoroutine(FadeCanvasGroup(resultPanel, resultPanel.alpha, 1f, 0.8f));
    }

    public void RestartGameAndAskName()
    {
        PlayButtonTapSFX();

        gameIsActive = false;
        isGameOver = false;
        isSpinning = false;

        if (wheelAudioSource != null) wheelAudioSource.Stop();

        foreach (GameObject vfx in activeBettingVFX) { if (vfx != null) Destroy(vfx); }
        activeBettingVFX.Clear();

        foreach (PlayerBet bet in activeBets)
        {
            if (bet.coinVisual != null) Destroy(bet.coinVisual);
        }
        activeBets.Clear();

        playerCoins = startingCoins;
        currentTime = totalGameTime;
        UpdateCoinsUI();

        if (timerRect != null) timerRect.sizeDelta = new Vector2(maxTimerWidth, timerRect.sizeDelta.y);
        if (timerText != null) timerText.text = Mathf.CeilToInt(totalGameTime).ToString() + "s";

        if (nameInputField != null) nameInputField.text = "";
        if (nameWarningText != null)
        {
            nameWarningText.text = "Enter your name";
            nameWarningText.color = Color.white;
        }

        StartCoroutine(FadeCanvasGroup(resultPanel, resultPanel.alpha, 0f, 0.4f));
        StartCoroutine(FadeCanvasGroup(bettingUIPanel, bettingUIPanel.alpha, 0f, 0.4f));
        StartCoroutine(FadeCanvasGroup(namePanel, namePanel.alpha, 1f, 0.5f));
    }

    private void UpdateCoinsUI()
    {
        if (coinsText != null) coinsText.text = "Coins: " + playerCoins;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float startAlpha, float endAlpha, float duration)
    {
        if (cg == null) yield break;

        if (endAlpha == 0f) { cg.interactable = false; cg.blocksRaycasts = false; }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            yield return null;
        }
        cg.alpha = endAlpha;

        if (endAlpha > 0f) { cg.interactable = true; cg.blocksRaycasts = true; }
    }

    private IEnumerator AnimateFloatingText(int amount)
    {
        if (floatingTextPrefab == null || coinsText == null) yield break;

        GameObject fTextObj = Instantiate(floatingTextPrefab, coinsText.transform.parent);
        
        fTextObj.transform.position = coinsText.transform.position + floatingTextOffset;
        
        TextMeshProUGUI fText = fTextObj.GetComponent<TextMeshProUGUI>();
        fText.text = amount > 0 ? "+" + amount : amount.ToString();
        fText.color = amount > 0 ? Color.green : Color.red;

        float duration = 1.2f;
        float elapsed = 0f;
        Vector3 startPos = fTextObj.transform.position;
        Vector3 endPos = startPos + new Vector3(0, floatingTextTravelY, 0); 

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            fTextObj.transform.position = Vector3.Lerp(startPos, endPos, t);
            
            Color c = fText.color;
            c.a = Mathf.Lerp(1f, 0f, t);
            fText.color = c;

            yield return null;
        }

        if (fTextObj != null) Destroy(fTextObj);
    }
}

[System.Serializable]
public class PlayerBet
{
    public string betType;
    public int amount;
    public GameObject coinVisual;

    public PlayerBet(string type, int amt, GameObject coin)
    {
        betType = type;
        amount = amt;
        coinVisual = coin;
    }
}