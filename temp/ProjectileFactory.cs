using System.Collections.Generic;
using UnityEngine;

public class ProjectileFactory : Singleton<ProjectileFactory>
{

    #region <Fields>

    [SerializeField] private List<FormattedMonoBehaviour> _projectileObjectGroup;
    private Transform _transform;

    #endregion </Fields>

    #region <Enums>

    public enum Type
    {
        None = -1,
        HuntressArrowNormal,
        HuntressArrowTrailed,
        HuntressArrowCylinder,
        HuntressArrowBlack,
        HuntressArrowWire,
        PsyousMagicMissile,
        PsyousFireBall,
        PsyousMagmaBall,
        PsyousForceHammer,
        Count
    }

    #endregion </Enums>

    #region <Unity/Callbacks>

    protected override void Initialize()
    {
        base.Initialize();
        _transform = GetComponent<Transform>();
    }

    #endregion </Unity/Callbacks>
    
    public Projectile CreateProjectile(Type projectileType, Unit caster, float lifeTime, Vector3 birthLocation)
    {
        var projectile = ObjectManager.GetInstance.GetObject<Projectile>(ObjectManager.PoolTag.Projectile,
            _projectileObjectGroup[(int) projectileType], birthLocation, _transform);      

        return projectile.SetType(projectileType).SetCaster(caster).SetLifeTime(lifeTime);
    }
    
}