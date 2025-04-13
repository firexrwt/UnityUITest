using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WindowsManager : MonoBehaviour
{
    public class WindowPrefab
    {
        protected ViewController prefab;
        public ViewController Prefab => prefab;

        protected Dictionary<int, ViewController> windows;

        public WindowPrefab(ViewController prefab)
        {
            this.prefab = prefab;
            windows = new Dictionary<int, ViewController>();
        }

        public void AddWnd(ViewController wnd)
        {
            int id = -1;
            while (windows.ContainsKey(++id))
            {
            }

            wnd.ID = id;
            windows.Add(wnd.ID, wnd);
        }

        public ViewController GetWnd(int id)
        {
            if (!windows.ContainsKey(id))
                return null;

            return windows[id];
        }

        public List<ViewController> GetWnds(bool onlyActive)
        {
            var wnds = new List<ViewController>();
            foreach (var wnd in windows)
                if (wnd.Value != null)
                    if (onlyActive && wnd.Value.gameObject.activeInHierarchy || !onlyActive)
                        wnds.Add(wnd.Value);
            return wnds;
        }

        public bool HaveWindows(bool includeInactive = false)
        {
            if (includeInactive)
                return windows.Count != 0;

            int activeCount = 0;
            foreach (ViewController wnd in windows.Values)
                if (wnd.gameObject.activeSelf)
                    activeCount++;

            return activeCount > 0;
        }

        public void RemoveWnd(ViewController wnd)
        {
            foreach (var item in windows)
                if (item.Key == wnd.ID)
                {
                    windows.Remove(item.Key);
                    break;
                }
        }

        public void RemoveWnd(int id)
        {
            windows.Remove(id);
        }

        public ViewController GetFirstNonActive()
        {
            foreach (ViewController wnd in windows.Values)
                if (!wnd.gameObject.activeSelf && wnd.IsPreloaded)
                    return wnd;
            return null;
        }

        public void CleanUp()
        {
            var keys = new List<int>();
            foreach (var wnd in windows)
                if (wnd.Value == null)
                    keys.Add(wnd.Key);

            foreach (int t in keys)
                windows.Remove(t);
        }
    }

    public static float WindowShowHideTime = 0.2f;

    public static event Action<ViewController> OnWindowCreated;
    public static event Action<ViewController> OnWindowClosed;

    protected Dictionary<string, WindowPrefab> loadedPrefabs = new Dictionary<string, WindowPrefab>();

    public Dictionary<string, WindowPrefab> LoadedPrefabs => loadedPrefabs;

    protected List<ViewController> visibleViews = new List<ViewController>();

    public List<ViewController> VisibleViews => visibleViews;

    public Canvas UIRoot => GetComponent<Canvas>();

    public Camera UICamera => GetComponentInParent<Camera>();

    protected static WindowsManager instance;

    public static WindowsManager Instance
    {
        get
        {
            Debug.Log("WindowsManager: Instance getter called");
            if (instance == null)
            {
                Debug.Log("WindowsManager: Instance is null, attempting to load UI/UI prefab...");
                GameObject instObj = null;
                try
                {
                    instObj = Instantiate(Resources.Load<GameObject>("UI/UI"));
                    Debug.Log($"WindowsManager: Resources.Load<GameObject>(\"UI/UI\") attempted. Result is null? {instObj == null}");

                    if (instObj != null)
                    {
                        instObj.name = "UI";
                        instance = instObj.GetComponentInChildren<WindowsManager>();
                        Debug.Log($"WindowsManager: GetComponentInChildren<WindowsManager>() attempted. Result is null? {instance == null}");

                        if (instance != null)
                        {
                            bool canvasFound = instance.UIRoot != null;
                            Debug.Log($"WindowsManager: UIRoot (Canvas) found? {canvasFound}");
                            if (!canvasFound)
                            {
                                Debug.LogError("WindowsManager: CRITICAL - UIRoot (Canvas) is NULL on the loaded UI/UI object or its parents/children accessible by GetComponent!");
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError("WindowsManager: Failed to load prefab from Resources/UI/UI.prefab!");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"WindowsManager: Error during Instance creation: {e.Message}\n{e.StackTrace}");
                }
            }
            else
            {
                Debug.Log("WindowsManager: Instance already exists.");
            }
            return instance;
        }
    }

    private void OnDestroy()
    {
        instance = null;
    }

    public static string GetFullWindowTypeName(IWindowStarter info)
    {
        return info.GetGroup() + "_" + info.GetName();
    }

    public string GetWindowPrefabPath(IWindowStarter info)
    {
        return "UI/" + info.GetGroup() + "/" + info.GetName();
    }

    private ViewController InstantiateView(ViewController prefab)
    {
        ViewController wnd = Instantiate(prefab, UIRoot.transform, false);
        wnd.name = wnd.name.Replace("(Clone)", "");
        wnd.transform.localScale = Vector3.one;

        return wnd;
    }
    

    public ViewController LoadWindowPrefab(IWindowStarter starter)
    {
        var go = Resources.Load<GameObject>(GetWindowPrefabPath(starter));
        return go.GetComponent<ViewController>();
    }

    public ViewController CreateWindow(ViewController parent, IWindowStarter starter)
    {
        return CreateWindow<ViewController>(parent, starter);
    }

    public ViewController CreateWindow(IWindowStarter starter)
    {
        return CreateWindow<ViewController>(null, starter);
    }

    public T CreateWindow<T>(IWindowStarter starter) where T : ViewController
    {
        return CreateWindow<T>(null, starter);
    }

    public T CreateWindow<T>(ViewController parent, IWindowStarter starter) where T : ViewController
    {
        string windowTypeName = GetFullWindowTypeName(starter);
        Debug.Log($"===== WindowsManager: CreateWindow called for Type: {windowTypeName} ====="); // <-- ËÎÃ

        WindowPrefab wndPrefab = null;
        if (LoadedPrefabs.ContainsKey(windowTypeName))
        {
            wndPrefab = LoadedPrefabs[windowTypeName];
            Debug.Log($"WindowsManager: Prefab '{windowTypeName}' found in cache."); // <-- ËÎÃ
        }

        if (wndPrefab == null)
        {
            string prefabPath = GetWindowPrefabPath(starter);
            Debug.Log($"WindowsManager: Window prefab not loaded yet. Loading from Resources/{prefabPath}.prefab"); // <-- ËÎÃ
            GameObject go = null;
            ViewController wndPrefabObj = null;
            try
            {
                go = Resources.Load<GameObject>(prefabPath);
                Debug.Log($"WindowsManager: Resources.Load prefab attempted. Result is null? {go == null}");

                if (go != null)
                {
                    wndPrefabObj = go.GetComponent<ViewController>();
                    Debug.Log($"WindowsManager: GetComponent<ViewController> on prefab attempted ('{go.name}'). Result is null? {wndPrefabObj == null}");

                    if (wndPrefabObj != null)
                    {
                        wndPrefab = new WindowPrefab(wndPrefabObj);
                        loadedPrefabs.Add(windowTypeName, wndPrefab);
                        Debug.Log($"WindowsManager: Prefab '{windowTypeName}' loaded and added to cache.");
                    }
                    else
                    {
                        Debug.LogError($"WindowsManager: Prefab '{prefabPath}' loaded, but GetComponent<ViewController> returned null! Check prefab setup.");
                        return null;
                    }
                }
                else
                {
                    Debug.LogError($"WindowsManager: Failed to load prefab from Resources/{prefabPath}.prefab!");
                    return null;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"WindowsManager: Error during Resources.Load or GetComponent: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }
        if (wndPrefab == null || wndPrefab.Prefab == null)
        {
            Debug.LogError($"WindowsManager: Critical error - wndPrefab or wndPrefab.Prefab is null after loading/cache check for {windowTypeName}! Cannot proceed."); // <-- ËÎÃ
            return null;
        }
        if (wndPrefab.Prefab.OnlyOneCanBeVisible)
        {
            Debug.Log($"WindowsManager: Prefab '{windowTypeName}' has OnlyOneCanBeVisible=true. Checking for existing visible instances...");
            RefreshVisibleViews();
            var visibleView = VisibleViews.FirstOrDefault(e => e.TypeInfo.GetGroup() == starter.GetGroup() && e.TypeInfo.GetName() == starter.GetName());
            if (visibleView != null)
            {
                Debug.Log($"WindowsManager: Closing existing visible instance ID: {visibleView.ID}"); // <-- ËÎÃ
                visibleView.Close();
            }
        }
        Debug.Log($"WindowsManager: Attempting to get cached or instantiate view for {windowTypeName}...");
        ViewController controller;
        ViewController cached = wndPrefab.GetFirstNonActive();

        if (cached != null)
        {
            Debug.Log($"WindowsManager: Reusing cached view instance ID: {cached.ID}");
            controller = cached;
            controller.IsPreloaded = false;
            Debug.Log($"WindowsManager: Reusing controller '{controller.name}' (ID: {controller.ID}). State before Init: {controller.State}");
        }
        else
        {
            Debug.Log($"WindowsManager: Instantiating new view instance from prefab '{wndPrefab.Prefab.name}'...");
            controller = InstantiateView(wndPrefab.Prefab);

            if (controller != null)
            {
                wndPrefab.AddWnd(controller);
                Debug.Log($"WindowsManager: Instantiated new view. Instance is null? false. Assigned ID: {controller.ID}. Name: '{controller.name}'");
            }
            else
            {
                Debug.LogError($"WindowsManager: InstantiateView returned null for prefab '{wndPrefab.Prefab.name}'!");
                return null;
            }
        }

        if (controller == null)
        {
            Debug.LogError($"WindowsManager: Failed to obtain controller instance (cached or new) for {windowTypeName}!");
            return null;
        }

        if (controller != null)
        {
            Debug.Log($"WindowsManager: Setting up controller '{controller.name}' (ID: {controller.ID}) before Init...");
            starter.SetupModels(controller);
            controller.Parent = parent;

            try
            {
                Debug.Log($"WindowsManager: Calling controller.Init('{controller.name}', ID: {controller.ID})...");
                controller.Init(starter);
                Debug.Log($"WindowsManager: controller.Init() finished successfully for '{controller.name}'.");
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to init view controller for starter (" + GetFullWindowTypeName(starter) + "):" +
                               e.Message + "\n" + e.StackTrace);
#if UNITY_EDITOR
                throw;
#endif
            }
            controller.gameObject.SetActive(false); // Îðèãèíàëüíûé êîä
            Debug.Log($"WindowsManager: Controller '{controller.name}' GameObject set to inactive (waiting for Show()).");

            controller.OnDepthChanged -= OnWindowDepthChangedInternal;
            controller.OnDepthChanged += OnWindowDepthChangedInternal;
            controller.OnWindowClosed -= OnWindowClosedInternal;
            controller.OnWindowClosed += OnWindowClosedInternal;
            controller.OnWindowDestroyed -= OnWindowDestroyedInternal;
            controller.OnWindowDestroyed += OnWindowDestroyedInternal;

            Debug.Log($"WindowsManager: Firing OnWindowCreated event for '{controller.name}'.");
            OnWindowCreated?.Invoke(controller);

            Debug.Log($"WindowsManager: CreateWindow finished for {windowTypeName}. Returning controller '{controller?.name}'.");
            return controller as T;
        }

        
        Debug.LogError($"WindowsManager: Reached final return null unexpectedly for {windowTypeName}.");
        return null;
    }

    private void OnWindowDestroyedInternal(ViewController obj)
    {
        RemoveWindow(obj);
    }

    private void OnWindowClosedInternal(ViewController obj)
    {
        OnWindowClosedInternal(obj, true);
    }

    private void OnWindowClosedInternal(ViewController obj, bool fireCallback)
    {
        if (!obj.DestroyOnClose)
            obj.IsPreloaded = true;

        visibleViews.RemoveAll((v) => v == obj);
        SortWindows();

        if (fireCallback)
            OnWindowClosed?.Invoke(obj);
    }

    private void OnWindowDepthChangedInternal(ViewController obj)
    {
        if (!visibleViews.Contains(obj))
            visibleViews.Add(obj);

        SortWindows();
    }

    private static int CompareViews(ViewController view1, ViewController view2)
    {
        return view2.Depth.CompareTo(view1.Depth);
    }

    protected void SortWindows()
    {
        visibleViews.Sort(CompareViews);
    }

    protected void RefreshVisibleViews()
    {
        visibleViews.Clear();

        foreach (var wnd in LoadedPrefabs)
        {
            var wnds = wnd.Value.GetWnds(true);
            foreach (ViewController t in wnds)
                if (t.State == ViewController.WindowState.Showing || t.State == ViewController.WindowState.Visible)
                    visibleViews.Add(t);
        }

        SortWindows();
    }

    public ViewController GetWindow(string typeName, int id)
    {
        if (LoadedPrefabs.ContainsKey(typeName))
            return LoadedPrefabs[typeName].GetWnd(id);

        return null;
    }

    public ViewController GetWindow(string uniqueID)
    {
        string[] parts = uniqueID.Split('!');
        if (parts.Length != 2)
            return null;

        int id = int.Parse(parts[1]);

        return GetWindow(parts[0], id);
    }

    public List<ViewController> GetAllWindows(string typeName, bool onlyActive)
    {
        if (LoadedPrefabs.ContainsKey(typeName))
            return LoadedPrefabs[typeName].GetWnds(onlyActive);

        return new List<ViewController>();
    }

    public bool HaveWindows(string typeName)
    {
        if (LoadedPrefabs.ContainsKey(typeName))
            return LoadedPrefabs[typeName].HaveWindows();
        return false;
    }

    public List<ViewController> GetAllWindows()
    {
        var Windows = new List<ViewController>();
        foreach (var wnd in LoadedPrefabs)
        {
            var vw = wnd.Value.GetWnds(false);
            foreach (ViewController vc in vw)
                if (Windows.Contains(vc) == false)
                    Windows.Add(vc);
        }

        return Windows;
    }

    private void RemoveWindow(ViewController wnd)
    {
        string key = GetFullWindowTypeName(wnd.TypeInfo);

        OnWindowClosedInternal(wnd, false);

        if (LoadedPrefabs.ContainsKey(key))
        {
            WindowPrefab prefab = LoadedPrefabs[key];
            prefab.RemoveWnd(wnd);
            if (!prefab.HaveWindows())
                LoadedPrefabs.Remove(key);
        }
    }

    private void ClearDestroyedWindows()
    {
        foreach (var wnd in LoadedPrefabs)
            wnd.Value.CleanUp();
    }
}