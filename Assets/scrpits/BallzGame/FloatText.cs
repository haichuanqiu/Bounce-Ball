using System.Collections;
using BallzGame.Managers;
using TMPro;
using UnityEngine;

namespace Utils
{
    public class FloatText : MonoBehaviour
    {
        public TMP_Text text;


        public void DoFloatText(
            int number,
            Vector3 position,
            Color color,
            Vector2 direction, float duration = 0.6f,Transform parent = null
        )
        {

            DoFloatText(number.ToString(), position,color, direction, duration, parent);
        }

        public void DoFloatText(
            string content,
            Vector3 position,
            Color color,
            Vector2 direction, float duration = 0.6f,Transform parent = null
        )
        {
            if (parent == null)
            {
                parent=GameManager.Instance.VisualEffectsParent;
            }
            transform.SetParent(parent);
            transform.position = position;

            text.text = content;
            text.color = color;

            StartCoroutine(FloatRoutine(direction.normalized, duration));
        }

        [System.Serializable]
        public class FloatTextData
        {
            public int number;
            public Vector3 position;
            public Color color;
        }

        IEnumerator FloatRoutine(Vector2 dir, float duration)
        {

            float time = 0f;

            Vector3 startPos = transform.position;

            // 👉 位移强度
            float distance = 1.2f;

            // 👉 初始scale（弹）
            transform.localScale = Vector3.zero;

            Color startColor = text.color;


            while (time < duration)
            {
                time += Time.deltaTime;

                float t = time / duration;

                // =========================
                // 🎯 缓动（更自然）
                // =========================
                float easeOut = 1 - Mathf.Pow(1 - t, 3); // cubic out

                // =========================
                // 📍 位移
                // =========================
                Vector3 offset = (Vector3)dir * (distance * easeOut);

                transform.position = startPos + offset;

                // =========================
                // 🔥 弹一下 scale
                // =========================
                float scale;

                if (t < 0.2f)
                {
                    // 快速放大
                    scale = Mathf.Lerp(0, 1.2f, t / 0.2f);
                }
                else
                {
                    // 回弹到1
                    float t2 = (t - 0.2f) / 0.8f;
                    scale = Mathf.Lerp(1.2f, 1f, t2);
                }

                transform.localScale = Vector3.one * scale;

                // =========================
                // 🌫️ 淡出
                // =========================
                float alpha = 1 - t;

                text.color = new Color(
                    startColor.r,
                    startColor.g,
                    startColor.b,
                    alpha
                );

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}