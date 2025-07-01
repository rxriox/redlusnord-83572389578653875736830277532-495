using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[AddComponentMenu("UI/Effects/Panel In-Game Gradient")]
public class PanelInGameGradient : BaseMeshEffect
{
    [Tooltip("El color superior del degradado.")]
    public Color m_color1 = Color.white;
    [Tooltip("El color inferior del degradado.")]
    public Color m_color2 = Color.white;
    [Tooltip("El ángulo del degradado en grados.")]
    [Range(-180f, 180f)]
    public float m_angle = -90f; // -90 para que sea vertical (de arriba a abajo)
    public bool m_ignoreRatio = true;

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive())
        {
            return;
        }

        var vertexList = new List<UIVertex>();
        vh.GetUIVertexStream(vertexList);
        int count = vertexList.Count;

        if (count > 0)
        {
            float cos = Mathf.Cos(m_angle * Mathf.Deg2Rad);
            float sin = Mathf.Sin(m_angle * Mathf.Deg2Rad);
            Rect r = graphic.rectTransform.rect;
            Vector2 dir = new Vector2(cos, sin);

            if (!m_ignoreRatio)
            {
                dir.x *= r.width / r.height;
                dir = dir.normalized;
            }

            Vector2 origin = r.center - dir * (Vector2.Dot(r.size / 2, dir));
            float min = float.MaxValue, max = float.MinValue;

            for (int i = 0; i < count; i++)
            {
                float d = Vector2.Dot(vertexList[i].position - (Vector3)origin, dir);
                if (d > max) max = d;
                if (d < min) min = d;
            }

            float range = max - min;
            for (int i = 0; i < count; i++)
            {
                UIVertex uiVertex = vertexList[i];
                float d = Vector2.Dot(uiVertex.position - (Vector3)origin, dir);
                uiVertex.color = Color32.Lerp(m_color1, m_color2, (d - min) / range);
                vertexList[i] = uiVertex;
            }

            vh.Clear();
            vh.AddUIVertexTriangleStream(vertexList);
        }
    }
    public void Refresh()
    {
        // Comprobamos que el componente gráfico esté activo antes de marcarlo como "sucio".
        if (graphic != null)
        {
            graphic.SetVerticesDirty();
        }
    }
}