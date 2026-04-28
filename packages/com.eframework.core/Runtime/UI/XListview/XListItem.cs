// Created by lorin
// 2024/08/08 7:15 PM

using UnityEngine;

namespace EFrameWork.Runtime.UI.XListview
{
    public class XListItem : MonoBehaviour
    {
        public int Index { get; private set; }
        public int PrefabIndex { get; private set; }
        public RectTransform RectTransform { get; private set; }
        [HideInInspector]public bool IsInit;


        public void Init(int index)
        {
            Index = index;
            RectTransform = GetComponent<RectTransform>();
            SetActive(true);
        }
    
        public void SetPrefabIndex(int prefabIndex)
        {
            PrefabIndex = prefabIndex;
        }
    
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }
    }
}