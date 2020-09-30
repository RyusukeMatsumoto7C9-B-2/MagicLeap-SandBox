// ---------------------------------------------------------------------
//
// Copyright (c) 2018-present, Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Creator Agreement, located
// here: https://id.magicleap.com/terms/developer
//
// ---------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;
using MagicLeapTools;

[RequireComponent(typeof(LineRenderer))]
public class HandPointer : MonoBehaviour
{
#if PLATFORM_LUMIN
    //Public Variables:
    [Tooltip("Should the pointer stay rigid while not dragging anything?")]
    public bool rigidWhilePointing;
    
    [Tooltip("What layers should we interact with and ignore.")]
    public LayerMask layerMask;

    [Tooltip("How far from a collision on a surface should the pointer hover.")]
    public float surfaceOffset;
    
    [Tooltip("How far is the pointer when it is not targeting.")]
    public float idleDistance = 1;
    
    [Tooltip("How far can the pointer go.")]
    public float maxDistance = 2;
    
    [Tooltip("Closest distance things can be pulled in.")]
    public float minDistance = .4f;
    
    [Tooltip("How far does the motionSource need to move when selecting to be considered a drag.")]
    public float dragMovementThreshold = 0.01f;
    
    [Tooltip("How far does the motionSource need to rotate (in degrees) when selecting to be considered a drag.")]
    public float dragRotationThreshold = 1f;
    
    [Tooltip("How many points should the line be made out of.")]
    public int lineResolution = 20;
    
    [Tooltip("Simulates weight of the bendy pointer.")]
    public float bendyWeightMultiplier = 1;
    
    [Tooltip("At what percentage along the pointer should we bend.")]
    [Range(0, 1)]
    public float bendPointPercentage = .9f;

    [Tooltip("How much to exaggerate the bend.")]
    public float bendPredictionMultiplier = 14;
    
    [Tooltip("Allow for the ability to bring something in very close with little effort by simply dragging in close to the body.")]
    public bool allowYanking = true;
    
    [Tooltip("If yanking is allowed this will be the final distance from the pointer.")]
    public float yankedDistance = 0.0889f;
    
    [Tooltip("If yanking is allowed this distance is the minimum distance from the body before yanking triggers.")]
    public float minBodyDistance = 0.44f;
    
    [Tooltip("Process reachCurve to make grabbing far away things easier?")]
    public bool allowReachStretching = true;
    
    [Tooltip("Defines stretch evaluation for pointer when reaching while not dragging.")]
    public AnimationCurve reachStretchCurve;
    
    [Tooltip("Process effortMagnificationCurve to make moving things in and out easier?")]
    public bool allowEffortMagnification = true;
    
    [Tooltip("Defines effort magnification evaluation so an object can be more easily moved further distances.")]
    public AnimationCurve effortMagnificationCurve;

    // TODO : テストで追加.
    [SerializeField] Transform forwardObj;
    
    
    //Public Properties:
    /// <summary>
    /// What is the pointer currently doing?
    /// </summary>
    public PointerStatus Status
    {
        get
        {
            return _status;
        }

        private set
        {
            //gates:
            if (_dragging) return;
            if (value == PointerStatus.Dragging)
            {
                _dragging = true;
            }

            _status = value;
        }
    }

    /// <summary>
    /// What is currently targeted.
    /// </summary>
    public GameObject Target
    {
        get;
        private set;
    }

    /// <summary>
    /// The origin of the pointer.
    /// </summary>
    public Vector3 Origin
    {
        get
        {
            return forwardObj.position;
        }
    }

    /// <summary>
    /// The direction of the pointer.
    /// </summary>
    public Vector3 Direction
    {
        get
        {
            return Vector3.Normalize(Tip - forwardObj.position);
        }
    }

    /// <summary>
    /// USED INTERNALLY: The internally leveraged location that drag and raycast operations operate upon.
    /// </summary>
    public Vector3 InternalInteractionPoint
    {
        get;
        private set;
    }

    /// <summary>
    /// The visual end of the pointer.
    /// </summary>
    public Vector3 Tip
    {
        get;
        private set;
    }

    /// <summary>
    /// The direction of the surface the pointer is hitting.
    /// </summary>
    public Vector3 Normal
    {
        get;
        private set;
    }

    //Private Variables:
    private readonly float _predictionSpeed = 15;
    private readonly float _maxArmLength = .6f;
    private readonly float _maxArmTravelDistance = 0.2286f;
    private readonly float _bendyTipLerpSpeed = 7f;
    private readonly float _defaultLineWidth = 0.004f;
    private readonly float _nudgeAmount = 0.3048f;
    private float _currentNudge;
    private LineRenderer _lineRenderer;
    private float _currentDistance;
    private Camera _mainCamera;
    private List<Vector3> _bendPointHistory = new List<Vector3>();
    private readonly int _bendPointHistoryCount = 5;
    private Vector3 _selectedMotionSourceLocation;
    private Quaternion _selectedMotionSourceRotation;
    private float _selectedMotionSourceDistanceToHead;
    private Vector3 _draggedTipLocalLocation;
    private bool _dragging;
    private PointerStatus _status;
    private GameObject _lastSelection;
    private float _dragInitialDistance;
    private Vector3 _bendPredictionPoint;
    private InputReceiver _targetedInputReceiver;
    private bool _bendy;
    private Curve _curve;
    private float _minBodyDistanceBuffer = 0.0127f;
    private float _yankThreshold;
    private bool _yanked;

    Vector3 lastNormal = Vector3.zero;
    
    //Private Properties:
    private float Length
    {
        get
        {
            return Vector3.Distance(forwardObj.position, InternalInteractionPoint);
        }
    }

    private Vector3 DistanceLocation
    {
        get
        {
            /*
            //relative input driver location:
            Vector3 relative = _mainCamera.transform.InverseTransformPoint(forwardObj.position);

            //yanked?
            if (_dragging && allowYanking)
            {
                if (relative.z < _yankThreshold)
                {
                    _yanked = true;
                }
                else if (relative.z >= _yankThreshold + _minBodyDistanceBuffer)
                {
                    _yanked = false;
                }
            }
            */

            //apply offset:
            if (_yanked)
            {
                return forwardObj.position + forwardObj.forward * yankedDistance;
            }
            else
            {
                return forwardObj.position + forwardObj.forward * _currentDistance;
            }
        }
    }

    //Init:
    private void Reset()
    {
        //sets:
        layerMask = -1;

        //refs:
        LineRenderer lineRenderer = GetComponent<LineRenderer>();

        //setups:
        lineRenderer.positionCount = 0;
        lineRenderer.startWidth = _defaultLineWidth;

        //curve sets:
        reachStretchCurve = new AnimationCurve(new Keyframe(0, 0, 0, 0), new Keyframe(0.6f, 0, 0, 0), new Keyframe(1, 1, 6.519273f, 6.519273f));
        effortMagnificationCurve = new AnimationCurve(new Keyframe(0, 0, 0, 0), new Keyframe(1, 1, 2, 2));
    }

    private void Start()
    {
        //bendy mode:
        if (!rigidWhilePointing)
        {
            _bendy = true;
        }

        //refs:
        _lineRenderer = GetComponent<LineRenderer>();
        //_mainCamera = Camera.main;

        //sets:
        _curve = new Curve();
        Normal = forwardObj.forward * -1;

        //establish distance:
        _currentDistance = idleDistance;

        //input active?
        // TODO : 何をやってるものか調べる必要がある.
        if (!forwardObj.gameObject.activeSelf)
        {
            HandleDeactivate(null);
        }
    }

    //Flow:
    private void OnEnable()
    {
    }

    private void OnDisable()
    {
    }

    //Loops:
    private void Update()
    {
        //swap bendy status if needed:
        if (!_dragging && rigidWhilePointing)
        {
            _bendy = false;
        }

        if (!_dragging && !rigidWhilePointing)
        {
            _bendy = true;
        }

        //move or pin our tip:
        PlaceRaycastPoint();

        FieldDeterminations();

        //find any surfaces:
        Raycast();

        //did we move far enough to begin dragging?
        DragStartDetermination();

        //draw our line:
        UpdateLineVisuals();
    }

    //Private Methods:
    private void FieldDeterminations()
    {
        if (!_dragging)
        {
            if (!allowReachStretching)
            {
                return;
            }

            //pointer stretch:
            //stretch amount:
            /*
            float motionSourceDistance = Vector3.Distance(forwardObj.position, _mainCamera.transform.position);
            float motionSourceTravelPercentage = Mathf.Clamp01(motionSourceDistance / _maxArmLength);
            */

            //stretch the reach of the pointer:
            /*
            float stretchValue = Mathf.Clamp01(reachStretchCurve.Evaluate(motionSourceTravelPercentage));
            _currentDistance = Mathf.Lerp(idleDistance, maxDistance, stretchValue);
        */
        }
        else
        {
            if (!allowEffortMagnification)
            {
                return;
            }

            //effort modification:
            /*
            float currentMotionSourceDistance = Vector3.Distance(_mainCamera.transform.position, forwardObj.position);
            float motionSourceTraveledDistance = currentMotionSourceDistance - _selectedMotionSourceDistanceToHead;
            float motionSourceEffortPercentage = motionSourceTraveledDistance / _maxArmTravelDistance;
            float clampedPercentage = Mathf.Clamp(motionSourceEffortPercentage, -1, 1);
            float effortCurveEvaluation = Mathf.Clamp01(effortMagnificationCurve.Evaluate(Mathf.Abs(clampedPercentage)));
            */

            /*
            if (Mathf.Sign(clampedPercentage) == 1)
            {
                _currentDistance = Mathf.Lerp(_dragInitialDistance, maxDistance, effortCurveEvaluation);
            }
            else
            {
                _currentDistance = Mathf.Lerp(_dragInitialDistance, minDistance, effortCurveEvaluation);
            }
            */

            //apply nudge:
            _currentDistance += _currentNudge;
            _currentDistance = Mathf.Clamp(_currentDistance, minDistance, maxDistance);
        }
    }

    private void DragStartDetermination()
    {
        if (_dragging)
        {
            return;
        }
        
        //we can't drag if we aren't selecting:
        if (Status != PointerStatus.Selecting) return;

        //changes since selecting:
        float movedDistance = Vector3.Distance(_selectedMotionSourceLocation, forwardObj.position);
        float angleDistance = Quaternion.Angle(_selectedMotionSourceRotation, forwardObj.rotation);

        //did we start dragging?
        if (movedDistance > dragMovementThreshold || angleDistance > dragRotationThreshold)
        {
            //where is the distance at which we just yank the dragged thing all the way in to make things easier?
            _yankThreshold = minBodyDistance;

            //if we were already within the yank threshold add a tiny buffer so we can still have a yank threshold:
            /*
            Vector3 relative = _mainCamera.transform.InverseTransformPoint(forwardObj.position);
            if (relative.z <= minBodyDistance)
            {
                _yankThreshold -= _minBodyDistanceBuffer;
            }
            */

            StartDrag();
        }
    }

    private void PlaceRaycastPoint()
    {
        if (_bendy)
        {
            float weight = _bendyTipLerpSpeed * (1 / bendyWeightMultiplier);
            InternalInteractionPoint = Vector3.Lerp(InternalInteractionPoint, DistanceLocation, Time.deltaTime * weight);
        }
        else
        {
            InternalInteractionPoint = DistanceLocation;
        }
    }

    private void StartDrag()
    {
        //enable bend:
        _bendPredictionPoint = Vector3.Lerp(forwardObj.position, InternalInteractionPoint, bendPointPercentage);
        _bendPointHistory.Clear();
        _bendy = true;

        //pin the tip to where we started the drag on the target:
        _draggedTipLocalLocation = Target.transform.InverseTransformPoint(InternalInteractionPoint);

        //the length must now just reach the target:
        _currentDistance = Length;
        _dragInitialDistance = Length;
        _currentNudge = 0;

        //status:
        Status = PointerStatus.Dragging;

        //interactions:
        _targetedInputReceiver?.DragBegin(gameObject);
    }

    private void StopDrag()
    {
        if (_dragging)
        {
            //disable bend:
            if (rigidWhilePointing)
            {
                _bendy = false;
            }

            _dragging = false;

            //pointer status sync:
            if (Target != null)
            {
                Status = PointerStatus.Targeting;
            }
            else
            {
                Status = PointerStatus.Idle;
            }

            //interactions:
            _targetedInputReceiver.DragEnd(gameObject);

            //reset distance since it likely changed from the idle drag distance:
            _currentDistance = idleDistance;
        }
    }

    private void Raycast()
    {
        //closest params:
        float closestDistance = float.MaxValue;
        RaycastHit closestRaycastHit = new RaycastHit();
        RaycastHit secondClosestRaycastHit = new RaycastHit();

        RaycastHit[] hits = Physics.RaycastAll(forwardObj.position, Vector3.Normalize(InternalInteractionPoint - forwardObj.position), _currentDistance, layerMask);
        if (hits.Length > 0)
        {
            //find closest:
            foreach (var item in hits)
            {
                if (item.distance < closestDistance)
                {
                    closestDistance = item.distance;
                    secondClosestRaycastHit = closestRaycastHit;
                    closestRaycastHit = item;
                }
            }

            //look for targets:
            InputReceiver currentInputReceiver = closestRaycastHit.collider.GetComponent<InputReceiver>();

            //we are colliding if we don't have a target:
            if (Target == null)
            {
                //status:
                Status = PointerStatus.Colliding;
            }

            //only consider targetEnter and targetExit if we aren't dragging:
            if (!_dragging)
            {
                if (currentInputReceiver != null)
                {
                    if (_targetedInputReceiver != currentInputReceiver)
                    {
                        TargetExit();
                        TargetEnter(currentInputReceiver);
                        _bendPointHistory.Clear();
                    }
                }
                else
                {
                    TargetExit();
                }
            }

            //while dragging we need to make sure raycasts against the dragged object do not
            //get considered in moving the hit point - if they did this dragged object would jump
            //closer and closer to the origin of the raycast:
            bool ignoreBecauseDragTarget = false;
            if (currentInputReceiver != null)
            {
                if (Status == PointerStatus.Dragging && _lastSelection == currentInputReceiver.gameObject)
                {
                    ignoreBecauseDragTarget = true;
                }
            }

            //if this isn't our dragged object then go ahead and ride the cursor along this surface:
            if (!ignoreBecauseDragTarget)
            {
                //adjust hit information:
                Vector3 backup = Vector3.Normalize(closestRaycastHit.point - forwardObj.position) * surfaceOffset;
                closestRaycastHit.point -= backup;
                InternalInteractionPoint = closestRaycastHit.point;

                //only change normal if we are not dragging:
                if (Status != PointerStatus.Dragging)
                {
                    Normal = closestRaycastHit.normal;
                }
            }

            //find backing surface hit:
            if (Status == PointerStatus.Dragging)
            {
                //are we hitting something behind the target?
                if (secondClosestRaycastHit.collider != closestRaycastHit.collider && secondClosestRaycastHit.collider != null)
                {
                    Vector3 backup = Vector3.Normalize(secondClosestRaycastHit.point - forwardObj.position) * surfaceOffset;
                    InternalInteractionPoint = secondClosestRaycastHit.point;
                }
            }
        }
        else
        {
            Status = PointerStatus.Idle;
            TargetExit();
        }
    }

    private void TargetEnter(InputReceiver inputReceiver)
    {
        //status:
        Status = PointerStatus.Targeting;

        //sets:
        Target = inputReceiver.gameObject;

        //catalog:
        _targetedInputReceiver = inputReceiver;
        _targetedInputReceiver.TargetEnter(gameObject);
    }

    private void TargetExit()
    {
        if (_dragging)
        {
            return;
        }

        if (Target != null)
        {
            Status = PointerStatus.Idle;

            //interactions:
            _targetedInputReceiver?.TargetExit(gameObject);

            //sets:
            Target = null;
            _targetedInputReceiver = null;
        }

        //adjust hit information:
        Normal = forwardObj.forward * -1;
    }

    private void UpdateLineVisuals()
    {
        //align line renderer resolution:
        if (_bendy)
        {
            if (_lineRenderer.positionCount != lineResolution + 1)
            {
                _lineRenderer.positionCount = lineResolution + 1;
            }
        }
        else
        {
            if (_lineRenderer.positionCount != 2)
            {
                _lineRenderer.positionCount = 2;
            }
        }

        //update visuals of line renderer:
        if (_bendy)
        {
            //add bend point to history:
            _bendPointHistory.Add(Vector3.Lerp(forwardObj.position, InternalInteractionPoint, bendPointPercentage));
            if (_bendPointHistory.Count > _bendPointHistoryCount)
            {
                _bendPointHistory.RemoveAt(0);
            }

            //props for averages:
            Vector3 averageVelocity = Vector3.zero;
            Vector3 averageBendPoint = Vector3.zero;

            //find averages:
            for (int i = 0; i < _bendPointHistory.Count; i++)
            {
                if (i > 0)
                {
                    //velocity average additions:
                    averageVelocity += _bendPointHistory[i] - _bendPointHistory[i - 1];
                }

                //bend point average additions:
                averageBendPoint += _bendPointHistory[i];
            }

            //averages:
            averageVelocity /= _bendPointHistory.Count;
            averageBendPoint /= _bendPointHistory.Count;

            //add prediction and multiplier to bend location:
            Vector3 calculatedBendPredictionPoint = averageBendPoint + (averageVelocity * bendPredictionMultiplier);

            //clamp bend point to bendPointPercentage if it ends up behind the motion source:
            Vector3 toEnd = Vector3.Normalize(InternalInteractionPoint - forwardObj.position);
            Vector3 toBend = Vector3.Normalize(calculatedBendPredictionPoint - forwardObj.position);

            if (Vector3.Dot(toEnd, toBend) < 0)
            {
                calculatedBendPredictionPoint = Vector3.Lerp(forwardObj.position, InternalInteractionPoint, bendPointPercentage);
            }

            //smooth prediction:
            _bendPredictionPoint = Vector3.Lerp(_bendPredictionPoint, calculatedBendPredictionPoint, Time.deltaTime * _predictionSpeed);

            //bendy:
            for (int i = 0; i < lineResolution + 1; i++)
            {
                float percentage = i / (float)lineResolution;

                //if dragging then bend the curve to the spring location otherwise bend to the spring:
                Vector3 curveEndLocation = Vector3.zero;
                if (_dragging)
                {
                    curveEndLocation = _lastSelection.transform.TransformPoint(_draggedTipLocalLocation);
                }
                else
                {
                    curveEndLocation = InternalInteractionPoint;
                }

                //find point on curve:
                _curve.Update(forwardObj.position, _bendPredictionPoint, curveEndLocation, lineResolution);
                Vector3 pointOnCurve = _curve.GetPosition(percentage);
                //Vector3 pointOnCurve = CurveUtilities.GetPointRaw(forwardObj.position, _bendPredictionPoint, curveEndLocation, percentage);

                //set line renderer position:
                _lineRenderer.SetPosition(i, pointOnCurve);
            }
        }
        else
        {
            //rigid:
            _lineRenderer.SetPosition(0, forwardObj.position);

            //if dragging then slant the line to the initial tip location otherwise connect to the spring:
            Vector3 lineEndLocation = Vector3.zero;
            if (_dragging)
            {
                Debug.Log("kokodayo");
                lineEndLocation = _lastSelection.transform.TransformPoint(_draggedTipLocalLocation);
            }
            else
            {
                Debug.Log("imakoko");
                lineEndLocation = InternalInteractionPoint;

            }

            _lineRenderer.SetPosition(1, lineEndLocation);
        }

        //synchronize tip location:
        Tip = _lineRenderer.GetPosition(_lineRenderer.positionCount - 1);
    }
    
    private void HandleDeactivate(InputDriver sender)
    {
        //hide renderers:
        foreach (var item in GetComponentsInChildren<Renderer>())
        {
            item.enabled = false;
        }
    }
    
#endif
}
