using System.Collections;
using TMPro;
using UnityEngine;

// Put this on the narrating card's root object (the one you want to show and hide).
// Hook Show(string) to other cards' Button OnClick events.
public class NarratingCard : MonoBehaviour
{
    public TMP_Text textField;           // the TextMeshPro text inside this card
    public float autoHideSeconds = 0f;   // 0 = stay until Hide() or another card is clicked

    [Header("Optional: bring the card in front of the player when shown")]
    public bool placeInFrontOfPlayer = false;
    public float distance = 1.5f;
    public float heightOffset = -0.1f;

    Coroutine hideRoutine;

    // Call this from a Button's OnClick, typing the explanatory text in the string field.
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

    // Call this from a Button's OnClick and drag the clicked card's CardMessage into the slot.
    public void ShowFrom(CardMessage card)
    {
        if (card) Show(card.message);
    }

    // Hook this to a close button, or call it when a Timeline stage ends.
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
