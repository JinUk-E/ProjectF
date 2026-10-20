using UnityEngine;

namespace Fortress
{
    /// <summary>Keeps the command screen composition visible on narrow and ultrawide windows.</summary>
    [ExecuteAlways]
    public sealed class MenuDesignViewport : MonoBehaviour
    {
        RectTransform rect;
        RectTransform parent;
        Vector2 lastSize;
        void OnEnable()
        {
            rect=transform as RectTransform;
            parent=transform.parent as RectTransform;
            lastSize=new Vector2(-1,-1);
            Fit();
        }
        void LateUpdate()
        {
            if(parent!=null && parent.rect.size!=lastSize) Fit();
        }
        void Fit()
        {
            if(rect==null || parent==null) return;
            lastSize=parent.rect.size;
            float scale=Mathf.Min(lastSize.x/1920f,lastSize.y/1080f);
            if(scale>0) rect.localScale=Vector3.one*scale;
        }
    }
}
