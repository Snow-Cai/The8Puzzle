using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Tile : MonoBehaviour
{
    public Image tileImage;     // background image on the Button
    public CanvasGroup numberGroup; // optional: fade the number overlay

    public int value; // 0 means empty space
    public Button button;
    public TMP_Text buttonNum;

    private System.Action<Tile> onClickTile;

    public void Init(int v, System.Action<Tile> clickCb)
    {
        value = v;
        onClickTile = clickCb;

        // finds automatically
        if (!button) button = GetComponent<Button>();
        if (!buttonNum) buttonNum = GetComponentInChildren<TMP_Text>(true);

        buttonNum.text = (value == 0) ? "" : value.ToString();
        button.interactable = (value != 0); // interactable when its not 0
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClickTile?.Invoke(this));
    }

    public void SetIneractable(bool interactable)
    {
        if (button) button.interactable = interactable && (value != 0);
    }

    public void SetSprite(Sprite s)
    {
        if (tileImage == null) tileImage = GetComponent<Image>();
        if (tileImage != null) tileImage.sprite = s;
    }

    public void SetImageTint(Color c)
    {
        if (tileImage == null) tileImage = GetComponent<Image>();
        if (tileImage != null) tileImage.color = c;
    }

    public void SetNumberVisible(bool on)
    {
        if (buttonNum != null)
        {
            if (numberGroup != null)
            {
                numberGroup.alpha = on ? 1f : 0f;
                numberGroup.blocksRaycasts = on;
                numberGroup.interactable = on;
            }
            else
            {
                buttonNum.gameObject.SetActive(on);
            }
        }
    }

}
