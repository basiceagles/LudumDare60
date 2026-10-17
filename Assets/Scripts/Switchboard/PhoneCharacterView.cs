using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class PhoneCharacterView : MonoBehaviour
{
    private Image portrait;

    public static PhoneCharacterView GetOrCreate(GameObject dialoguePanel, PhoneCharacterView prefab = null)
    {
        if (dialoguePanel == null)
        {
            return null;
        }

        PhoneCharacterView view = dialoguePanel.GetComponentInChildren<PhoneCharacterView>(true);
        if (view != null)
        {
            return view;
        }

        if (prefab == null)
        {
            prefab = Resources.Load<PhoneCharacterView>("PhoneCharacter");
        }
        if (prefab == null)
        {
            Debug.LogError("Не назначен префаб персонажа. Укажите Character Prefab или добавьте Resources/PhoneCharacter.prefab.", dialoguePanel);
            return null;
        }

        view = Instantiate(prefab, dialoguePanel.transform, false);
        view.gameObject.name = "NPC_Icon";
        view.ShowSprite(null);
        return view;
    }

    public void ShowSprite(Sprite sprite)
    {
        if (portrait == null)
        {
            portrait = GetComponent<Image>();
        }
        if (portrait == null)
        {
            return;
        }

        portrait.raycastTarget = false;
        portrait.preserveAspect = true;
        portrait.sprite = sprite;
        portrait.enabled = sprite != null;
        if (sprite != null && !gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
    }

    public void AttachTo(Transform position)
    {
        if (transform.parent != position)
        {
            transform.SetParent(position, false);
        }
        if (transform is RectTransform rect && position is RectTransform parentRect)
        {
            rect.anchorMin = parentRect.pivot;
            rect.anchorMax = parentRect.pivot;
            rect.anchoredPosition3D = Vector3.zero;
        }
        else
        {
            transform.localPosition = Vector3.zero;
        }
    }

    private void OnDisable()
    {
        ShowSprite(null);
    }
}
