using System.Collections.Generic;
using UnityEngine;

namespace Drawing
{
    public class LineRendererPool : MonoBehaviour
    {
        [SerializeField] private GameObject lineRendererPrefab;
        [SerializeField] private int initialPoolSize = 20;
        [SerializeField] private int maxPoolSize = 100;
        
        private Queue<LineRenderer> availableRenderers = new Queue<LineRenderer>();
        private HashSet<LineRenderer> activeRenderers = new HashSet<LineRenderer>();
        
        private static LineRendererPool instance;
        public static LineRendererPool Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject poolObject = new GameObject("LineRendererPool");
                    instance = poolObject.AddComponent<LineRendererPool>();
                    DontDestroyOnLoad(poolObject);
                }
                return instance;
            }
        }
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            InitializePool();
        }
        
        private void InitializePool()
        {
            if (lineRendererPrefab == null)
            {
                CreateDefaultPrefab();
            }
            
            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateNewRenderer();
            }
        }
        
        private void CreateDefaultPrefab()
        {
            lineRendererPrefab = new GameObject("LineRendererPrefab");
            var lr = lineRendererPrefab.AddComponent<LineRenderer>();
            
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.textureMode = LineTextureMode.Stretch;
            lr.numCapVertices = 10;
            lr.numCornerVertices = 10;
            lr.useWorldSpace = false;
            lr.sortingOrder = 1;
            
            // Performance optimizations
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            lr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            
            lineRendererPrefab.SetActive(false);
        }
        
        private LineRenderer CreateNewRenderer()
        {
            GameObject obj = Instantiate(lineRendererPrefab, transform);
            obj.SetActive(false);
            LineRenderer lr = obj.GetComponent<LineRenderer>();
            availableRenderers.Enqueue(lr);
            return lr;
        }
        
        public LineRenderer GetRenderer()
        {
            LineRenderer renderer;
            
            if (availableRenderers.Count > 0)
            {
                renderer = availableRenderers.Dequeue();
            }
            else if (activeRenderers.Count + availableRenderers.Count < maxPoolSize)
            {
                renderer = CreateNewRenderer();
            }
            else
            {
                Debug.LogWarning("LineRendererPool: Max pool size reached!");
                return null;
            }
            
            activeRenderers.Add(renderer);
            renderer.gameObject.SetActive(true);
            renderer.positionCount = 0;
            return renderer;
        }
        
        public void ReturnRenderer(LineRenderer renderer)
        {
            if (renderer == null) return;
            
            if (activeRenderers.Contains(renderer))
            {
                activeRenderers.Remove(renderer);
                
                // Reset renderer
                renderer.positionCount = 0;
                renderer.gameObject.SetActive(false);
                
                availableRenderers.Enqueue(renderer);
            }
        }
        
        public void ReturnAllRenderers()
        {
            foreach (var renderer in activeRenderers)
            {
                renderer.positionCount = 0;
                renderer.gameObject.SetActive(false);
                availableRenderers.Enqueue(renderer);
            }
            activeRenderers.Clear();
        }
        
        private void OnDestroy()
        {
            if (lineRendererPrefab != null)
            {
                Destroy(lineRendererPrefab);
            }
        }
    }
}