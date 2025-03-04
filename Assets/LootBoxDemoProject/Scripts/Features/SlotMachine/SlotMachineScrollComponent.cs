using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Features.SlotMachine
{
    [RequireComponent(typeof(ScrollRect))]
    public class SlotMachineScrollComponent : MonoBehaviour
    {
        [SerializeField] private float _snapSpeed = 3f;
        [SerializeField] private float _snapVelocityThreshold = 60f;
        
        private ScrollRect _scrollRect;
        private ContentSizeFitter _contentSizeFitter;
        private VerticalLayoutGroup _verticalLayoutGroup;
        private HorizontalLayoutGroup _horizontalLayoutGroup;
        private GridLayoutGroup _gridLayoutGroup;
        private bool _hasDisabledGridComponents = false;
        private Vector2 _newAnchoredPosition = Vector2.zero;
        private Vector2 _threshold = Vector2.zero;
        private int _itemCount = 0;
        private bool _isSnapping = false;
        private bool _dragging = false;
        private CancellationTokenSource _snapCTS;
        private float _currentScrollSpeed = 0f;
        private CancellationTokenSource _scrollCTS;
        private bool _isVertical = false;
        private bool _isHorizontal = false;
        private List<RectTransform> items = new List<RectTransform>();

        protected virtual void Awake()
        {
            Init();
        }
        
        #region Start Init
        
        private void SetItems()
        {
            foreach (RectTransform child in _scrollRect.content.transform)
            {
                child.pivot = Vector3.zero;
            }

            for (int i = 0; i < _scrollRect.content.childCount; i++)
            {
                items.Add(_scrollRect.content.GetChild(i).GetComponent<RectTransform>());
            }

            _itemCount = _scrollRect.content.childCount;
        }

        public void Init()
        {
            if (GetComponent<ScrollRect>() != null)
            {
                _scrollRect = GetComponent<ScrollRect>();
                _scrollRect.onValueChanged.AddListener(OnScroll);
                _scrollRect.movementType = ScrollRect.MovementType.Unrestricted;

                if (_scrollRect.content.GetComponent<VerticalLayoutGroup>() != null)
                {
                    _verticalLayoutGroup = _scrollRect.content.GetComponent<VerticalLayoutGroup>();
                }
                if (_scrollRect.content.GetComponent<HorizontalLayoutGroup>() != null)
                {
                    _horizontalLayoutGroup = _scrollRect.content.GetComponent<HorizontalLayoutGroup>();
                }
                if (_scrollRect.content.GetComponent<GridLayoutGroup>() != null)
                {
                    _gridLayoutGroup = _scrollRect.content.GetComponent<GridLayoutGroup>();
                }
                if (_scrollRect.content.GetComponent<ContentSizeFitter>() != null)
                {
                    _contentSizeFitter = _scrollRect.content.GetComponent<ContentSizeFitter>();
                }

                _isHorizontal = _scrollRect.horizontal;
                _isVertical = _scrollRect.vertical;
                _threshold = _scrollRect.GetComponent<RectTransform>().sizeDelta * 0.5f;

                if (_isHorizontal && _isVertical)
                {
                    Debug.LogError("Scroll doesn't support scrolling in both directions, please choose one direction (horizontal or vertical)");
                }

                SetItems();
            }
            else
            {
                Debug.LogError("No ScrollRect component found");
            }
        }

        private void DisableGridComponents()
        {
            if (_verticalLayoutGroup)
            {
                _verticalLayoutGroup.enabled = false;
            }
            if (_horizontalLayoutGroup)
            {
                _horizontalLayoutGroup.enabled = false;
            }
            if (_contentSizeFitter)
            {
                _contentSizeFitter.enabled = false;
            }
            if (_gridLayoutGroup)
            {
                _gridLayoutGroup.enabled = false;
            }
            _hasDisabledGridComponents = true;
        }
        
        #endregion
        
        #region Infinity Scroll
        
        private void OnScroll(Vector2 pos)
        {
            if (!_hasDisabledGridComponents)
                DisableGridComponents();

            var firstChild = _scrollRect.content.GetChild(0).GetComponent<RectTransform>();
            var lastChild = _scrollRect.content.GetChild(_itemCount - 1).GetComponent<RectTransform>();

            for (int i = 0; i < items.Count; i++)
            {
                if (_isHorizontal)
                {
                    if (_scrollRect.transform.InverseTransformPoint(items[i].gameObject.transform.position).x > items[i].sizeDelta.x + _threshold.x && items[i] == lastChild)
                    {
                        _newAnchoredPosition = items[i].anchoredPosition;
                        _newAnchoredPosition.x = firstChild.anchoredPosition.x - items[i].sizeDelta.x;
                        items[i].anchoredPosition = _newAnchoredPosition;
                        lastChild.transform.SetAsFirstSibling();
                    }
                    else if (_scrollRect.transform.InverseTransformPoint(items[i].gameObject.transform.position).x < -items[i].sizeDelta.x - _threshold.x - 100 && items[i] == firstChild)
                    {
                        _newAnchoredPosition = items[i].anchoredPosition;
                        _newAnchoredPosition.x = lastChild.anchoredPosition.x + lastChild.sizeDelta.x;
                        items[i].anchoredPosition = _newAnchoredPosition;
                        firstChild.transform.SetAsLastSibling();
                    }
                }

                if (_isVertical)
                {
                    if (_scrollRect.transform.InverseTransformPoint(items[i].gameObject.transform.position).y > items[i].sizeDelta.y + _threshold.y - 200 && items[i] == firstChild)
                    {
                        _newAnchoredPosition = items[i].anchoredPosition;
                        _newAnchoredPosition.y = lastChild.anchoredPosition.y - items[i].sizeDelta.y;
                        items[i].anchoredPosition = _newAnchoredPosition;
                        firstChild.transform.SetAsLastSibling();
                    }
                    else if (_scrollRect.transform.InverseTransformPoint(items[i].gameObject.transform.position).y < -items[i].sizeDelta.y - _threshold.y + 200 && items[i] == lastChild)
                    {
                        _newAnchoredPosition = items[i].anchoredPosition;
                        _newAnchoredPosition.y = firstChild.anchoredPosition.y + firstChild.sizeDelta.y;
                        items[i].anchoredPosition = _newAnchoredPosition;
                        lastChild.transform.SetAsFirstSibling();
                    }
                }
            }
        }
        
        #endregion

        #region Snap
        
        private async Task DelayedSnapAsync(CancellationToken token)
        {
            await Task.Delay(100, token);
            
            while (!token.IsCancellationRequested && _scrollRect.velocity.magnitude > _snapVelocityThreshold)
            {
                await Task.Yield();
            }
            
            await SnapToClosestItemAsync(token);
        }

        private async Task SnapToClosestItemAsync(CancellationToken token)
        {
            _isSnapping = true;
            
            RectTransform viewport = _scrollRect.viewport;
            if (viewport == null)
                viewport = _scrollRect.GetComponent<RectTransform>();
            
            Vector2 viewportCenter = viewport.rect.center;
            Vector3 worldViewportCenter = viewport.TransformPoint(viewportCenter);
            
            RectTransform closest = null;
            float minDistance = Mathf.Infinity;
            
            foreach (RectTransform item in items)
            {
                if (token.IsCancellationRequested)
                    return;
                
                Vector3 itemCenter = item.TransformPoint(item.rect.center);
                float dist = Vector3.Distance(itemCenter, worldViewportCenter);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = item;
                }
            }

            if (closest == null)
            {
                _isSnapping = false;
                return;
            }
            
            Vector3 closestCenterWorld = closest.TransformPoint(closest.rect.center);
            Vector2 itemLocalCenterPos = viewport.InverseTransformPoint(closestCenterWorld);
            Vector2 difference = itemLocalCenterPos - viewport.rect.center;
            
            Vector2 targetAnchoredPos = _scrollRect.content.anchoredPosition;
            
            if (_isHorizontal)
            {
                targetAnchoredPos.x -= difference.x;
            }
            else if (_isVertical)
            {
                targetAnchoredPos.y -= difference.y;
            }
            
            while (!token.IsCancellationRequested &&
                   Vector2.Distance(_scrollRect.content.anchoredPosition, targetAnchoredPos) > 0.1f)
            {
                _scrollRect.content.anchoredPosition = 
                    Vector2.Lerp(_scrollRect.content.anchoredPosition, targetAnchoredPos, Time.deltaTime * _snapSpeed);
                await Task.Yield();
            }
            
            if (!token.IsCancellationRequested)
            {
                _scrollRect.content.anchoredPosition = targetAnchoredPos;
                _scrollRect.velocity = Vector2.zero;
            }
            _isSnapping = false;
        }
        
        #endregion

        #region External scroll control
        
        public async UniTask StartScrolling(float accelerationTime, float speed)
        {
            if (_scrollCTS != null)
            {
                _scrollCTS.Cancel();
                _scrollCTS.Dispose();
                _scrollCTS = null;
            }
            
            if (_snapCTS != null)
            {
                _snapCTS.Cancel();
                _snapCTS.Dispose();
                _snapCTS = null;
            }

            _scrollCTS = new CancellationTokenSource();
            CancellationToken token = _scrollCTS.Token;

            _currentScrollSpeed = 0f;
            float elapsed = 0f;
            
            while (!token.IsCancellationRequested && elapsed < accelerationTime)
            {
                float dt = Time.deltaTime;
                elapsed += dt;
                _currentScrollSpeed = Mathf.Lerp(0, speed, elapsed / accelerationTime);
                if (_isHorizontal)
                {
                    _scrollRect.content.anchoredPosition = new Vector2(_scrollRect.content.anchoredPosition.x - _currentScrollSpeed * dt,
                                                                       _scrollRect.content.anchoredPosition.y);
                }
                else if (_isVertical)
                {
                    _scrollRect.content.anchoredPosition = new Vector2(_scrollRect.content.anchoredPosition.x,
                                                                       _scrollRect.content.anchoredPosition.y - _currentScrollSpeed * dt);
                }
                await Task.Yield();
            }

            if (token.IsCancellationRequested)
                return;
            
            _currentScrollSpeed = speed;
            
            while (!token.IsCancellationRequested)
            {
                float dt = Time.deltaTime;
                
                if (_isHorizontal)
                {
                    _scrollRect.content.anchoredPosition = new Vector2(_scrollRect.content.anchoredPosition.x - _currentScrollSpeed * dt,
                                                                       _scrollRect.content.anchoredPosition.y);
                }
                else if (_isVertical)
                {
                    _scrollRect.content.anchoredPosition = new Vector2(_scrollRect.content.anchoredPosition.x,
                                                                       _scrollRect.content.anchoredPosition.y - _currentScrollSpeed * dt);
                }
                
                await Task.Yield();
            }
        }
        
        public async UniTask StopScrolling(float stopTime)
        {
            if (_scrollCTS == null)
                return;
            
            _scrollCTS.Cancel();
            _scrollCTS.Dispose();
            _scrollCTS = null;

            float initialSpeed = _currentScrollSpeed;
            float elapsed = 0f;
           
            while (elapsed < stopTime)
            {
                float dt = Time.deltaTime;
                elapsed += dt;
                _currentScrollSpeed = Mathf.Lerp(initialSpeed, 0f, elapsed / stopTime);
                
                if (_isHorizontal)
                {
                    _scrollRect.content.anchoredPosition = new Vector2(_scrollRect.content.anchoredPosition.x - _currentScrollSpeed * dt,
                                                                       _scrollRect.content.anchoredPosition.y);
                }
                else if (_isVertical)
                {
                    _scrollRect.content.anchoredPosition = new Vector2(_scrollRect.content.anchoredPosition.x,
                                                                       _scrollRect.content.anchoredPosition.y - _currentScrollSpeed * dt);
                }
                await Task.Yield();
            }
            
            _scrollRect.velocity = Vector2.zero;
            
            if (_snapCTS != null)
            {
                _snapCTS.Cancel();
                _snapCTS.Dispose();
                _snapCTS = null;
            }
            
            _snapCTS = new CancellationTokenSource();
            
            try
            {
                await DelayedSnapAsync(_snapCTS.Token);
            }
            catch (OperationCanceledException)
            {
                throw new Exception();
            }
            finally
            {
                if (_snapCTS != null)
                {
                    _snapCTS.Dispose();
                    _snapCTS = null;
                }
            }
        }
        
        #endregion
    }
}