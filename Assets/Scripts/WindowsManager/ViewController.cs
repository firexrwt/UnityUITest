using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using DentedPixel;

// ReSharper disable ForCanBeConvertedToForeach

[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(GraphicRaycaster))]
[RequireComponent(typeof(CanvasGroup))]
public class ViewController : MonoBehaviour
{
    public enum WindowState
    {
        None,
        Hidden,
        Showing,
        Visible,
        Hiding
    };

    protected class GOState
    {
        public bool isRoot;
        public GameObject obj;
        public Transform parent;
        public bool active;
        public GOState[] childrens;

        public GOState(GameObject root, bool isRoot)
        {
            this.isRoot = isRoot;
            Init(root);
        }

        public void Init(GameObject root)
        {
            obj = root;
            parent = obj.transform.parent;
            active = obj.activeSelf;
            childrens = new GOState[obj.transform.childCount];
            for (int i = 0; i < childrens.Length; i++)
                childrens[i] = new GOState(obj.transform.GetChild(i).gameObject, false);
        }

        public GOState GetChild(Transform trans)
        {
            for (int i = 0; i < childrens.Length; i++)
                if (childrens[i].obj == trans.gameObject)
                    return childrens[i];
            return null;
        }

        public void Restore()
        {
            if (obj == null)
                return;

            if (!isRoot)
                obj.SetActive(active);

            for (int i = 0; i < childrens.Length; i++)
                childrens[i].RestoreParents();

            for (int i = 0; i < obj.transform.childCount; i++)
            {
                Transform child = obj.transform.GetChild(i);
                if (GetChild(child) == null)
                    Destroy(child.gameObject);
            }

            for (int i = 0; i < childrens.Length; i++)
                childrens[i].Restore();
        }

        protected void RestoreParents()
        {
            if (obj == null || parent == null)
                return;

            obj.transform.SetParent(parent, false);
            for (int i = 0; i < childrens.Length; i++)
                childrens[i].RestoreParents();
        }
    };

    private static int lastMaxSortOrder = 0;

    protected static GameObject inputBlockerPrefab;
    protected static GameObject windowBackgroundPrefab;

    public event System.Action<ViewController> OnShowWindow;
    public event System.Action<ViewController> OnWindowWasShown;
    public event System.Action<ViewController> OnHideWindow;
    public event System.Action<ViewController> OnWindowClosed;
    public event System.Action<ViewController> OnActualSizeInit;
    public event System.Action<ViewController> OnDepthChanged;
    public event System.Action<ViewController> OnWindowDestroyed;

    public Vector3 Position
    {
        get => transform.localPosition;
        set => transform.localPosition = value;
    }

    [System.NonSerialized] public int ID;
    [System.NonSerialized] public ViewController Parent = null;
    [System.NonSerialized] public bool IsPreloaded = false;

    public bool AnimatedShow = true;
    public bool AnimatedClose = true;
    public bool InputBlockNeeded = true;
    public bool ShowBackground = true;
    public bool CloseByTapOnBackground = true;
    public bool DestroyOnClose = true;
    public bool CloseAllChainOnClose;
    public bool OnlyOneCanBeVisible = false;

    protected bool ignoreTimeScale = false;
    protected WindowState state = WindowState.None;
    protected bool immediateShowing;
    protected IWindowStarter typeInfo;
    public IWindowStarter TypeInfo => typeInfo;
    public WindowState State => state;
    public string UniqueID => WindowsManager.GetFullWindowTypeName(typeInfo) + "!" + ID.ToString();

    protected Canvas cachedPanel;
    public Canvas CachedPanel => cachedPanel;
    protected CanvasGroup canvasGroup;
    protected RectTransform rectTransform;
    protected RectTransform background;
    protected GOState rootState;
    public bool restoreHierarchyState = true;
    protected bool deinitialized = true;
    [System.NonSerialized] public float ShowTime = WindowsManager.WindowShowHideTime;

    public int Depth
    {
        get => rectTransform.GetSiblingIndex();
        set
        {
            if (rectTransform.GetSiblingIndex() != value)
            {
                rectTransform.SetSiblingIndex(value);
                OnDepthChanged?.Invoke(this);
            }
        }
    }

    protected static readonly string WndBackgroundPrefabPath = "UI/WindowBackground";
    protected static readonly string InputBlockerPrefabPath = "UI/WindowInputBlocker";

    protected static void PreloadResources()
    {
        if (windowBackgroundPrefab == null)
        {
            var async = Resources.Load<GameObject>(WndBackgroundPrefabPath);
            windowBackgroundPrefab = async;
        }

        if (inputBlockerPrefab == null)
        {
            var async = Resources.Load<GameObject>(InputBlockerPrefabPath);
            inputBlockerPrefab = async;
        }
    }

    protected virtual void Awake()
    {
        cachedPanel = GetComponent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
    }

    public virtual void Init(IWindowStarter starter)
    {
        typeInfo = starter;
        PreloadResources();

        if (InputBlockNeeded && background == null)
        {
            GameObject back = null;
            if (ShowBackground)
            {
                if (windowBackgroundPrefab != null)
                    back = Instantiate(windowBackgroundPrefab);
            }
            else
            {
                if (inputBlockerPrefab != null)
                    back = Instantiate(inputBlockerPrefab);
            }

            if (back != null)
            {
                back.name = "WindowBackground";
                back.transform.SetParent(transform, false);
                back.transform.localScale = Vector3.one;
                back.transform.localPosition = Vector3.zero;
                back.GetComponent<RectTransform>().SetAsFirstSibling();

                UnityEngine.EventSystems.EventTrigger eventTrigger =
                    back.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                if (eventTrigger == null)
                    eventTrigger = back.AddComponent<UnityEngine.EventSystems.EventTrigger>();

                var clickCallback = new UnityEngine.EventSystems.EventTrigger.Entry()
                { eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick };
                clickCallback.callback.AddListener(OnBackgroundClickSink);
                eventTrigger.triggers.Add(clickCallback);

                background = back.GetComponent<RectTransform>();
            }
        }

        if (!DestroyOnClose && restoreHierarchyState && rootState == null)
            rootState = new GOState(gameObject, true);

        deinitialized = false;
    }

    public virtual void Show()
    {
        Debug.Log($"===== ViewController: Show() called for '{this.name}' (ID: {this.ID}). Current state: {state} =====");
        if (state == WindowState.Showing || state == WindowState.Visible)
        {
            Debug.Log($"ViewController: Show() aborted, already showing/visible.");
            return;
        }

        state = WindowState.Showing;
        Debug.Log($"ViewController: State set to Showing. Activating GameObject...");
        gameObject.SetActive(true);
        Debug.Log($"ViewController: GameObject activeSelf: {gameObject.activeSelf}");

        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        Debug.Log($"ViewController: CanvasGroup is null? {canvasGroup == null}");

        if (cachedPanel != null)
        {
            lastMaxSortOrder++;
            cachedPanel.overrideSorting = true;
            cachedPanel.sortingOrder = lastMaxSortOrder;
            Debug.Log($"ViewController: Set Canvas Sort Order for '{this.name}' to {cachedPanel.sortingOrder}. OverrideSorting: {cachedPanel.overrideSorting}");
        }
        else
        {
            Debug.LogWarning($"ViewController: Cannot set Sort Order, cachedPanel (Canvas component) is null on '{this.name}'!");
        }

        BringToTop();

        OnShowWindow?.Invoke(this);

        Debug.Log($"ViewController: AnimatedShow flag is: {AnimatedShow}");
        if (AnimatedShow)
        {
            Debug.Log($"ViewController: Calling StartShowAnimation...");
            StartShowAnimation();
        }
        else
        {
            Debug.Log($"ViewController: Skipping animation, calling OnShownCoroutine via delayedCall.");
            LeanTween.delayedCall(gameObject, 0.01f, (System.Action)OnShownCoroutine);
        }
    }

    public virtual void ShowImmediate()
    {
        bool prevAS = AnimatedShow;
        AnimatedShow = false;
        immediateShowing = true;
        Show();
        AnimatedShow = prevAS;
    }

    public void BringToTop()
    {
        int depth = Depth;
        rectTransform.SetAsLastSibling();
        if (depth != Depth)
            OnDepthChanged?.Invoke(this);
    }

    protected virtual void FireOnActualSizeInit()
    {
        Debug.Log($"ViewController: FireOnActualSizeInit() called for '{this.name}'.");
        OnActualSizeInit?.Invoke(this);
        if (AnimatedShow && !immediateShowing)
        {
            Debug.Log($"ViewController: FireOnActualSizeInit - Disabling interactables for animation.");
            DisableInteractables();
        }
    }


    private void OnShownCoroutine()
    {
        Debug.Log($"===== ViewController: OnShownCoroutine() called for '{this.name}' =====");
        FireOnActualSizeInit();
        OnShown();
    }


    protected virtual void StartShowAnimation()
    {
        Debug.Log($"===== ViewController: StartShowAnimation() called for '{this.name}' =====");
        SetWindowInvisible();
        LeanTween.delayedCall(gameObject, 0.01f, (System.Action)StartShowAnimationCoroutine);
    }

    private float srcAlpha = 1.0f;

    protected virtual void SetWindowInvisible()
    {
        if (canvasGroup != null)
        {
            srcAlpha = canvasGroup.alpha;
            canvasGroup.alpha = 0.01f;
            Debug.Log($"ViewController: SetWindowInvisible executed. CanvasGroup alpha set to 0.01f (original was {srcAlpha}).");
        }
        else
        {
            Debug.LogWarning($"ViewController: SetWindowInvisible - canvasGroup is null on '{this.name}'!");
        }
    }

    protected virtual void SetWindowVisible()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = srcAlpha;
            Debug.Log($"ViewController: SetWindowVisible executed. CanvasGroup alpha restored to {srcAlpha}.");
        }
        else
        {
            Debug.LogWarning($"ViewController: SetWindowVisible - canvasGroup is null on '{this.name}'!");
        }
    }

    private void StartShowAnimationCoroutine()
    {
        Debug.Log($"===== ViewController: StartShowAnimationCoroutine() called for '{this.name}' =====");
        FireOnActualSizeInit();
        if (canvasGroup != null)
        {
            SetWindowVisible();
        }
        ShowAnimation();
    }

    protected virtual void ShowAnimation()
    {
        Debug.Log($"ViewController: ShowAnimation() called. Animating alpha from {canvasGroup?.alpha} to {srcAlpha}. Target: '{canvasGroup?.name}'");
        AnimateAlpha(canvasGroup != null ? canvasGroup.alpha : 0.01f, srcAlpha, (System.Action)OnShown);
    }

    protected virtual void HideAnimation()
    {
        AnimateAlpha(1.0f, 0.01f, (System.Action)OnHidden);
    }

    protected virtual void StartHideAnimation()
    {
        HideAnimation();
    }

    protected virtual void AnimateScaleShow(GameObject root, System.Action callback)
    {
        root.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        LeanTween.scale(root, Vector3.one, ShowTime).setEase(LeanTweenType.easeOutBack).setOnComplete(callback)
            .setUseEstimatedTime(ignoreTimeScale);
    }

    protected virtual void AnimateScaleHide(GameObject root, System.Action callback)
    {
        LeanTween.scale(root, new Vector3(0.5f, 0.5f, 0.5f), ShowTime).setEase(LeanTweenType.easeInBack)
            .setOnComplete(callback).setUseEstimatedTime(ignoreTimeScale);
    }

    protected void AnimateAlpha(float from, float to, System.Action callback = null)
    {
        Debug.Log($"ViewController: AnimateAlpha() called. From: {from}, To: {to}. Target: '{canvasGroup?.name}'");
        if (canvasGroup != null)
        {
            canvasGroup.alpha = from;
            System.Action wrappedCallback = () => {
                Debug.Log($"ViewController: AnimateAlpha LeanTween COMPLETE. Alpha should be {to}. Current: {canvasGroup?.alpha}. Calling callback (OnShown/OnHidden)...");
                callback?.Invoke();
            };
            LeanTween.alphaCanvas(canvasGroup, to, ShowTime).setUseEstimatedTime(ignoreTimeScale)
                     .setOnComplete(wrappedCallback);
        }
        else
        {
            Debug.LogError($"ViewController: AnimateAlpha failed, canvasGroup is null on {this.name}! Cannot animate alpha.");
            Debug.Log($"ViewController: Calling callback (OnShown/OnHidden) directly because canvasGroup is null.");
            callback?.Invoke();
        }
    }

    protected void AnimateAlpha(CanvasGroup widget, float from, float to, System.Action callback = null)
    {
        if (widget != null)
        {
            widget.alpha = from;
            LeanTween.alphaCanvas(widget, to, ShowTime).setUseEstimatedTime(ignoreTimeScale).setOnComplete(callback);
        }
    }

    protected virtual void OnShown()
    {
        Debug.Log($"===== ViewController: OnShown() called for '{this.name}'. Current state: {state} =====");
        state = WindowState.Visible;
        Debug.Log($"ViewController: State set to Visible.");

        if (AnimatedShow && !immediateShowing) EnableColliders();

        OnWindowWasShown?.Invoke(this);
        Debug.Log($"ViewController: OnShown() finished. Final CanvasGroup alpha: {canvasGroup?.alpha}");
        immediateShowing = false;
    }

    protected virtual void OnHidden()
    {
        Debug.Log($"===== ViewController: OnHidden() called for '{this.name}'. Current state: {state} =====");
        state = WindowState.Hidden;
        Debug.Log($"ViewController: State set to Hidden.");

        OnHiddenClose();
    }

    private void OnHiddenClose()
    {
        Debug.Log($"ViewController: OnHiddenClose() called for '{this.name}'. DestroyOnClose: {DestroyOnClose}");
        OnWindowClosed?.Invoke(this);

        if (DestroyOnClose)
        {
            DestroyWindow();
        }
        else
        {
            if (restoreHierarchyState)
            {
                Debug.Log($"ViewController: OnHiddenClose - Restoring hierarchy state for '{this.name}'.");
                rootState?.Restore();
            }
            Debug.Log($"ViewController: OnHiddenClose - Setting '{this.name}' GameObject inactive.");
            gameObject.SetActive(false);
        }

        if (!deinitialized)
            DeInit(false);
    }

    protected virtual void DeInit(bool fromDestroy)
    {
        Debug.Log($"ViewController: DeInit({fromDestroy}) called for '{this.name}'.");
        deinitialized = true;
    }

    public virtual void Close()
    {
        Debug.Log($"===== ViewController: Close() called for '{this.name}'. CloseAllChain: {CloseAllChainOnClose} =====");
        if (CloseAllChainOnClose)
            CloseAllChain();
        else
            CloseSingle();
    }

    protected virtual void CloseSingle()
    {
        Debug.Log($"===== ViewController: CloseSingle() called for '{this.name}'. Current state: {state} =====");
        if (state == WindowState.Hiding || state == WindowState.Hidden)
        {
            Debug.Log($"ViewController: CloseSingle() aborted, already hiding/hidden.");
            return;
        }

        state = WindowState.Hiding;
        Debug.Log($"ViewController: State set to Hiding. AnimatedClose: {AnimatedClose}");

        OnHideWindow?.Invoke(this);

        if (AnimatedClose)
        {
            Debug.Log($"ViewController: CloseSingle - Disabling interactables and starting hide animation for '{this.name}'.");
            DisableInteractables();
            StartHideAnimation();
        }
        else
        {
            Debug.Log($"ViewController: CloseSingle - Skipping animation, calling OnHidden directly for '{this.name}'.");
            OnHidden();
        }
    }

    protected void OnBackgroundClickSink(UnityEngine.EventSystems.BaseEventData baseEventData)
    {
        if (state == WindowState.Visible && CloseByTapOnBackground)
            OnBackgroundClick();
    }

    public virtual void OnBackgroundClick()
    {
        Debug.Log($"ViewController: OnBackgroundClick() called for '{this.name}'. Closing window.");
        Close();
    }

    public void CloseAllChain()
    {
        Debug.Log($"===== ViewController: CloseAllChain() called for '{this.name}' =====");
        CloseSingle();
        if (Parent != null)
        {
            Debug.Log($"ViewController: CloseAllChain - Calling Parent.CloseAllChain() for parent '{Parent.name}'.");
            Parent.CloseAllChain();
        }
    }

    protected System.Collections.Generic.List<CanvasGroup> disabledInteractables =
        new System.Collections.Generic.List<CanvasGroup>();

    protected virtual void DisableInteractables()
    {
        Debug.Log($"ViewController: DisableInteractables() called for '{this.name}'.");
        disabledInteractables.Clear();
        var canvasGroups = GetComponentsInChildren<CanvasGroup>(true);
        for (int i = 0; i < canvasGroups.Length; ++i)
        {
            if (InputBlockNeeded && background != null && canvasGroups[i].gameObject == background.gameObject)
            {
                Debug.Log($"ViewController: DisableInteractables - Skipping background CanvasGroup.");
                continue;
            }
            canvasGroups[i].interactable = false;
            disabledInteractables.Add(canvasGroups[i]);
        }
    }


    protected virtual void EnableColliders()
    {
        Debug.Log($"ViewController: EnableColliders() called for '{this.name}'.");
        for (int i = 0; i < disabledInteractables.Count; i++)
        {
            if (disabledInteractables[i] != null)
            {
                disabledInteractables[i].interactable = true;
            }
        }
        disabledInteractables.Clear();
    }

    public void DestroyWindow(bool immediate = false)
    {
        Debug.Log($"===== ViewController: DestroyWindow({immediate}) called for '{this.name}' =====");
        OnWindowDestroyed?.Invoke(this);

        if (immediate)
            DestroyImmediate(gameObject);
        else
            Destroy(gameObject);
    }

    protected virtual void OnDestroy()
    {
        Debug.Log($"===== ViewController: OnDestroy() called for '{this.name}' =====");
        if (!deinitialized)
            DeInit(true);

        OnShowWindow = null;
        OnWindowWasShown = null;
        OnHideWindow = null;
        OnDepthChanged = null;
        OnWindowClosed = null;
        OnActualSizeInit = null;
        OnWindowDestroyed = null;
    }
}