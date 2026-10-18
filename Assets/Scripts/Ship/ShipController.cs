using UnityEngine;

public class ShipController : MonoBehaviour
{
    [SerializeField] private ShipMover shipMover;

    [Header("Turret Settings")]
    [SerializeField] private TurretCannon[] turrets;
    public AimTurret[] aimTurrets;
    private float turretAimValue;
    public bool bIsShooting;

    public IWeapon weapon;
    public IDamagable target;

    private void Awake()
    {
        if (shipMover == null)
        {
            shipMover = GetComponentInChildren<ShipMover>();
        }

        if (aimTurrets == null || aimTurrets.Length == 0)
        {
            aimTurrets = GetComponentsInChildren<AimTurret>();
        }

        if (turrets == null || turrets.Length == 0)
        {
            turrets = GetComponentsInChildren<TurretCannon>();
        }
    }

    public void HandleMoveShip(Vector2 movementVector)
    {
        shipMover.Move(movementVector);
    }

    public void HandleTurretMovement(Vector2 aimVector)
    {
        
            foreach (var aimTurret in aimTurrets)
            {
                aimTurret.Aim(aimVector);
            }
    }

    public void SetShootingState(bool bShooting)
    {
        bIsShooting = bShooting;
    }

    // Set by SubLightDrive while charging or cruising. The trigger state is kept, so a held trigger
    // opens fire again the moment the ship drops out of sub-light.
    public bool WeaponsLocked { get; set; }

    public void HandleShoot()
    {
        if (bIsShooting && !WeaponsLocked)
        {
            foreach (var turret in turrets)
            {
                turret.Shoot();
            }
        }
    }

    private void Update()
    {
        HandleShoot();
    }
}
