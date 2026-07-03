using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class AutoTiling : MonoBehaviour
{
    [SerializeField] float texelsPerUnit = 1f;

    MaterialPropertyBlock _block;
    Renderer _renderer;

    void OnEnable()
    {
        _renderer = GetComponent<Renderer>();
        _block = new MaterialPropertyBlock();
        Apply();
    }

    void Apply()
    {
        Vector3 s = transform.localScale;
        _renderer.GetPropertyBlock(_block);
        _block.SetVector("_BaseMap_ST", new Vector4(
            s.x * 1f * texelsPerUnit,  // X horizontal
            s.z * 1f * texelsPerUnit,  // Z profundidade
            0, 0
        ));
        _renderer.SetPropertyBlock(_block);
    }

#if UNITY_EDITOR
    void Update() => Apply();
#endif
}