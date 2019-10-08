// #define VISUAL_DEBUG

using UnityEngine;

public class OutlineMaker : MonoBehaviour
{
	#region <Consts>

	// min distance offset
	private const float DistanceToCalculateOutline = 70f;
	// property
	private const string PropertyName = "_OutlineWidth";
	
	#endregion </Consts>

	#region <Fields>

	// body, weapon, shield
	private RaycastHit _raycastHit;
	private Transform _transform;
	private Unit _mUnit;
	private K514MaterialApplier _mMaterialApplier;

    private Champion _champion
    {
        get { return PlayerChampionHandler.GetInstance.Handle; }
    }

    #endregion </Fields>

    #region <Unity/Callbacks>

    private void Awake()
	{
        // @SEE: head is the best place, maybe
        _raycastHit = new RaycastHit();
        _transform = GetComponent<Transform>();
		_mUnit = GetComponent<Unit>();
		_mMaterialApplier = GetComponent<K514MaterialApplier>();
	}
	
	private void Update()
	{
		if (Vector3.SqrMagnitude(_champion.transform.position-_transform.position) < DistanceToCalculateOutline && _mUnit.State != Unit.UnitState.Dead)
		{
			CheckIsBehindObstacle();
		}
	}
	
	#endregion

	#region <Methods>

	private void CheckIsBehindObstacle()
	{
        var camera_position = CameraManager.GetInstance.MainCameraTransform.position;
        Ray ray = new Ray(_transform.position + new Vector3(0, 2f, 0), camera_position - _transform.position - new Vector3(0, 2f, 0));
        bool hit = Physics.Raycast(ray,out _raycastHit, Mathf.Infinity);
        if (hit)
            _mMaterialApplier.SetTrigger(K514MaterialStorage.MAT_STATE.KOutLine);
        else
            _mMaterialApplier.RevertTrigger();

    }
	#endregion
}
