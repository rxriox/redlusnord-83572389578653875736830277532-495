using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
[ExecuteInEditMode]
public class UIGradientEditable : MonoBehaviour
{
    public enum GradientDirection
    {
        TopToBottom,
        TopRightToBottomLeft,
        RightToLeft,
        BottomRightToTopLeft,
        BottomToTop,
        BottomLeftToTopRight,
        LeftToRight,
        TopLeftToBottomRight
    }

    [Tooltip("El color sólido del degradado.")]
    public Color gradientColor = Color.black;

    [Tooltip("La dirección en la que se aplicará el degradado (de color sólido a transparente).")]
    public GradientDirection direction = GradientDirection.BottomToTop;

    private Image image;
    private Material materialInstance;

    private void OnEnable()
    {
        image = GetComponent<Image>();
        UpdateGradient();
    }

    private void OnValidate()
    {
        if (image == null)
        {
            image = GetComponent<Image>();
        }
        UpdateGradient();
    }

    public void UpdateGradient()
    {
        if (image == null) return;

        if (materialInstance == null)
        {
            materialInstance = new Material(Shader.Find("UI/Custom/URP_GradientOverlay"));
            image.material = materialInstance;
        }

        materialInstance.SetColor("_GradientColor", gradientColor);
        materialInstance.SetInt("_GradientDirection", (int)direction);
    }
}