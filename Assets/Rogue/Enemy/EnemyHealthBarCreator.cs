#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace Rogue
{
    public class EnemyHealthBarCreator : EditorWindow
    {
        [MenuItem("Rogue/Create Enemy Health Bar Prefab")]
        public static void ShowWindow()
        {
            GetWindow<EnemyHealthBarCreator>("Health Bar Creator");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("敌人血量条预制件创建工具", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("此工具将创建一个敌人血量条预制件，包含：\n" +
                                  "- 背景Panel\n" +
                                  "- 血量Slider\n" +
                                  "- 血量文本（可选）", MessageType.Info);

            EditorGUILayout.Space();

            if (GUILayout.Button("创建血量条预制件"))
            {
                CreateHealthBarPrefab();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("创建完成后，请将预制件拖拽到ConfigAuthoring的EnemyHealthBarPrefabGO字段中。", MessageType.Info);
        }

        private void CreateHealthBarPrefab()
        {
            // 创建Canvas（如果场景中没有的话）
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("Canvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
            }

            // 创建血量条根对象
            GameObject healthBarGO = new GameObject("EnemyHealthBar");
            healthBarGO.transform.SetParent(canvas.transform, false);

            // 添加RectTransform并设置尺寸
            RectTransform rectTransform = healthBarGO.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(100, 20);

            // 添加背景Image
            Image backgroundImage = healthBarGO.AddComponent<Image>();
            backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f); // 半透明灰色背景

            // 创建Slider
            GameObject sliderGO = new GameObject("HealthSlider");
            sliderGO.transform.SetParent(healthBarGO.transform, false);
            
            RectTransform sliderRect = sliderGO.AddComponent<RectTransform>();
            sliderRect.anchorMin = Vector2.zero;
            sliderRect.anchorMax = Vector2.one;
            sliderRect.offsetMin = Vector2.zero;
            sliderRect.offsetMax = Vector2.zero;

            Slider slider = sliderGO.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            // 创建Slider的Background
            GameObject backgroundGO = new GameObject("Background");
            backgroundGO.transform.SetParent(sliderGO.transform, false);
            
            RectTransform bgRect = backgroundGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            Image bgImage = backgroundGO.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
            bgImage.type = Image.Type.Sliced;

            // 创建Fill Area
            GameObject fillAreaGO = new GameObject("Fill Area");
            fillAreaGO.transform.SetParent(sliderGO.transform, false);
            
            RectTransform fillAreaRect = fillAreaGO.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            // 创建Fill
            GameObject fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(fillAreaGO.transform, false);
            
            RectTransform fillRect = fillGO.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            Image fillImage = fillGO.AddComponent<Image>();
            fillImage.color = Color.green;
            fillImage.type = Image.Type.Sliced;

            // 设置Slider的引用
            slider.targetGraphic = fillImage;
            slider.fillRect = fillRect;

            // 创建血量文本
            GameObject textGO = new GameObject("HealthText");
            textGO.transform.SetParent(healthBarGO.transform, false);
            
            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text healthText = textGO.AddComponent<Text>();
            healthText.text = "100/100";
            healthText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            healthText.fontSize = 12;
            healthText.color = Color.white;
            healthText.alignment = TextAnchor.MiddleCenter;

            // 创建预制件
            string prefabPath = "Assets/Rogue/Enemy/EnemyHealthBarPrefab.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(healthBarGO, prefabPath);
            
            // 删除场景中的临时对象
            DestroyImmediate(healthBarGO);

            // 选中创建的预制件
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);

            Debug.Log($"血量条预制件创建成功：{prefabPath}");
            EditorUtility.DisplayDialog("成功", "血量条预制件创建成功！\n请在ConfigAuthoring中设置EnemyHealthBarPrefabGO字段。", "确定");
        }
    }
}
#endif