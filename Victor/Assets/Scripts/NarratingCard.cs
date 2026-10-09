using System.Collections;
using TMPro;
using UnityEngine;

public class NarratingCard : MonoBehaviour
{
    public TMP_Text textField;
    public float autoHideSeconds = 0f;

    [Header("Optional: bring the card in front of the player when shown")]
    public bool placeInFrontOfPlayer = false;
    public float distance = 1.5f;
    public float heightOffset = -0.1f;

    Coroutine hideRoutine;

    public void Show(string message)
    {
        gameObject.SetActive(true);
        if (textField) textField.text = message;

        if (placeInFrontOfPlayer && Camera.main)
        {
            Transform head = Camera.main.transform;
            Vector3 flatForward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            transform.position = head.position + flatForward * distance + Vector3.up * heightOffset;
            transform.rotation = Quaternion.LookRotation(flatForward, Vector3.up);
        }

        if (hideRoutine != null) StopCoroutine(hideRoutine);
        if (autoHideSeconds > 0f) hideRoutine = StartCoroutine(HideAfter(autoHideSeconds));
    }

    public void ShowFrom(CardMessage card)
    {
        if (card) Show(card.message);
    }

    public void Hide()
    {
        if (hideRoutine != null) StopCoroutine(hideRoutine);
        gameObject.SetActive(false);
    }

    IEnumerator HideAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        gameObject.SetActive(false);
    }
}
