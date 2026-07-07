using UnityEngine;

namespace DiveProtocol.Interaction
{
    /// <summary>
    /// 基础交互测试。
    /// 玩家按下E后，让物体颜色变暗。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ColorChangeInteractable
        : InteractableBase
    {
        private static readonly int BaseColorProperty =
            Shader.PropertyToID("_BaseColor");

        private static readonly int ColorProperty =
            Shader.PropertyToID("_Color");

        [Header("Visual")]

        [Tooltip("需要改变颜色的Renderer。")]
        [SerializeField]
        private Renderer targetRenderer;

        [Tooltip("变暗后保留的亮度比例。")]
        [SerializeField, Range(0f, 1f)]
        private float brightnessMultiplier = 0.35f;

        [Tooltip("开启后只能交互一次。关闭后可以反复切换明暗。")]
        [SerializeField]
        private bool oneShot = true;

        private MaterialPropertyBlock propertyBlock;
        private Color originalColor;

        private int activeColorProperty = -1;

        private bool isDarkened;
        private bool initialized;

        private void Awake()
        {
            InitializeRenderer();
        }

        public override bool CanInteract(
            GameObject interactor)
        {
            if (!base.CanInteract(interactor))
            {
                return false;
            }

            return !oneShot || !isDarkened;
        }

        public override void Interact(
            GameObject interactor)
        {
            if (!initialized)
            {
                InitializeRenderer();
            }

            if (!initialized)
            {
                return;
            }

            if (oneShot && isDarkened)
            {
                return;
            }

            if (oneShot)
            {
                isDarkened = true;
            }
            else
            {
                isDarkened = !isDarkened;
            }

            Color nextColor = isDarkened
                ? MultiplyBrightness(
                    originalColor,
                    brightnessMultiplier)
                : originalColor;

            targetRenderer.GetPropertyBlock(
                propertyBlock);

            propertyBlock.SetColor(
                activeColorProperty,
                nextColor);

            targetRenderer.SetPropertyBlock(
                propertyBlock);
        }

        private void InitializeRenderer()
        {
            if (targetRenderer == null)
            {
                targetRenderer =
                    GetComponent<Renderer>();
            }

            if (targetRenderer == null)
            {
                Debug.LogError(
                    $"[{nameof(ColorChangeInteractable)}] " +
                    "找不到Renderer。",
                    this);

                return;
            }

            Material material =
                targetRenderer.sharedMaterial;

            if (material == null)
            {
                Debug.LogError(
                    $"[{nameof(ColorChangeInteractable)}] " +
                    "Renderer没有材质。",
                    this);

                return;
            }

            if (material.HasProperty(BaseColorProperty))
            {
                activeColorProperty =
                    BaseColorProperty;
            }
            else if (material.HasProperty(ColorProperty))
            {
                activeColorProperty =
                    ColorProperty;
            }
            else
            {
                Debug.LogError(
                    $"[{nameof(ColorChangeInteractable)}] " +
                    "材质没有_BaseColor或_Color属性。",
                    this);

                return;
            }

            originalColor =
                material.GetColor(activeColorProperty);

            propertyBlock =
                new MaterialPropertyBlock();

            initialized = true;
        }

        private static Color MultiplyBrightness(
            Color color,
            float multiplier)
        {
            return new Color(
                color.r * multiplier,
                color.g * multiplier,
                color.b * multiplier,
                color.a);
        }

#if UNITY_EDITOR
        private void Reset()
        {
            targetRenderer =
                GetComponent<Renderer>();
        }
#endif
    }
}