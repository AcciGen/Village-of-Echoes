using System.Collections;
using UHFPS.Runtime;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class EnemyAI : MonoBehaviour
{
    public enum State { Patrolling, Investigating, Chasing }

    [Header("References")]
    [SerializeField] private JumpscareTrigger _jumpscareScript;

    [Header("Attack Settings")]
    [SerializeField] private float _attackDistance = 1.2f;
    [SerializeField] private float _attackCooldown = 1.5f;
    [SerializeField] private float _delayBeforeHit = 0.3f;
    [SerializeField] private int _damageAmount = 30;

    [Header("Detection Settings")]
    [SerializeField] private float _visionAngle = 120f;
    [SerializeField] private float _visionDistance = 22f;
    [SerializeField] private float _hearDistance = 35f;
    [SerializeField] private LayerMask _obstacleMask;


    [Header("Movement Settings")]
    [SerializeField] private float _walkSpeed = 2f;
    [SerializeField] private float _runSpeed = 4.25f;
    [SerializeField] private float _waitTime = 5f;
    [SerializeField] private float _patrollingRadius = 20f;
    [SerializeField] private float _minDistanceToPlayer = 3f;
    [SerializeField] private float _animationSmoothness = 10f;

    [Header("Audio: 2D Music (Global)")]
    [SerializeField] private AudioSource _ambienceAudioSource;
    [SerializeField] private AudioClip _ambienceAudioClip;
    [SerializeField] private AudioClip _chasingAudioClip;
    [SerializeField] private float _musicFadeSpeed = 0.5f;
    [SerializeField] private float _musicVolume = 0.4f;

    [Header("Audio: 3D Monster Sounds")]
    [FormerlySerializedAs("monsterAudioSource")][SerializeField] private AudioSource _monsterAudioSource;
    [FormerlySerializedAs("patrollingBreathAudioClip")][SerializeField] private AudioClip _patrollingBreathAudioClip;
    [FormerlySerializedAs("chasingBreathAudioClip")][SerializeField] private AudioClip _chasingBreathAudioClip;
    [FormerlySerializedAs("screamAudioClip")][SerializeField] private AudioClip _screamAudioClip;

    [Header("Audio: Footsteps")]
    [FormerlySerializedAs("footstepsAudioSource")][SerializeField] private AudioSource _footstepsAudioSource;
    [FormerlySerializedAs("patrollingStepSpeed")][SerializeField] private float _patrollingStepSpeed = 0.8f;
    [FormerlySerializedAs("chasingStepSpeed")][SerializeField] private float _chasingStepSpeed = 1.4f;

    private NavMeshAgent _agent;
    private Animator _anim;
    private Transform _player;
    private PlayerHealth _playerHealth;
    private CharacterController _playerController;

    private State _currentState = State.Patrolling;
    private Vector3 _lastKnownPosition;
    private float _smoothedSpeed;
    private bool _isWaiting;
    private bool _isAttacking;
    private bool _hasScreamed;
    private bool _hasStarted = false;

    [Header("AI Thresholds")]
    [SerializeField] private float _activationDistance = 5f;
    [SerializeField] private float _patrolArrivalDistance = 0.5f;
    [SerializeField] private float _investigateArrivalDistance = 1f;
    [SerializeField] private float _hearingMovementThreshold = 2.8f;
    [SerializeField] private float _attackHitTolerance = 2f;
    [SerializeField] private float _attackRotateDuration = 0.2f;
    [SerializeField] private float _idleSpeedThreshold = 0.1f;
    [SerializeField] private float _footstepThreshold = 0.2f;
    [SerializeField] private float _musicTransitionThreshold = 0.02f;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _anim = GetComponent<Animator>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _player = playerObj.transform;
            _playerHealth = playerObj.GetComponent<PlayerHealth>();
            _playerController = playerObj.GetComponent<CharacterController>();
        }
    }

    void Start()
    {
        SetPatrollingDestination();

        if (_ambienceAudioSource != null && _ambienceAudioClip != null)
        {
            _ambienceAudioSource.clip = _ambienceAudioClip;
            _ambienceAudioSource.volume = _musicVolume;
            _ambienceAudioSource.Play();
        }
    }

    void Update()
    {
        if (_playerHealth != null && _playerHealth.IsDead) return;
        if (_player == null || _playerController == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, _player.position);
        float playerSpeed = _playerController.velocity.magnitude;

        if (!_hasStarted && distanceToPlayer > _activationDistance)
        {
            if (!_agent.isStopped)
            {
                _agent.isStopped = true;
                _agent.velocity = Vector3.zero;
            }

            HandleMonsterAudio();

            _smoothedSpeed = Mathf.Lerp(_smoothedSpeed, 0f, Time.deltaTime * _animationSmoothness);
            _anim.SetFloat("Speed", _smoothedSpeed);
            return;
        }
        else
        {
            _hasStarted = true;

            if (_agent.isStopped)
                _agent.isStopped = false;
        }

        if (CanSeePlayer() && _currentState != State.Chasing)
        {
            _currentState = State.Chasing;
            StopAllCoroutines();
            _isWaiting = false;
            _isAttacking = false;
        }
        else if (_currentState != State.Chasing && distanceToPlayer < _hearDistance && playerSpeed > _hearingMovementThreshold)
        {
            _lastKnownPosition = _player.position;

            if (_currentState != State.Investigating)
                _currentState = State.Investigating;
        }

        switch (_currentState)
        {
            case State.Patrolling: UpdatePatrol(); break;
            case State.Investigating: UpdateInvestigate(); break;
            case State.Chasing: UpdateChase(); break;
        }

        HandleMonsterAudio();
        HandleFootsteps();
        HandleMusicCrossfade();

        float realSpeed = _agent.velocity.magnitude;
        if (_isAttacking || realSpeed < _idleSpeedThreshold) realSpeed = 0f;
        _smoothedSpeed = Mathf.Lerp(_smoothedSpeed, realSpeed, Time.deltaTime * _animationSmoothness);
        _anim.SetFloat("Speed", _smoothedSpeed);
    }

    void UpdatePatrol()
    {
        _agent.speed = _walkSpeed;

        if (!_agent.pathPending && _agent.remainingDistance < _patrolArrivalDistance && !_isWaiting)
            StartCoroutine(WaitAndMove());
    }

    void UpdateInvestigate()
    {
        _agent.speed = _walkSpeed;
        _agent.SetDestination(_lastKnownPosition);

        if (!_agent.pathPending && _agent.remainingDistance < _investigateArrivalDistance && !_isWaiting)
            StartCoroutine(WaitAndMove());
    }

    void UpdateChase()
    {
        if (_isAttacking) return;

        _agent.speed = _runSpeed;
        _agent.SetDestination(_player.position);

        float distanceToPlayer = Vector3.Distance(transform.position, _player.position);

        if (distanceToPlayer <= _attackDistance && !_isAttacking)
        {
            StartCoroutine(AttackRoutine());
        }

        if (distanceToPlayer > _visionDistance && !CanSeePlayer())
        {
            _lastKnownPosition = _player.position;
            _currentState = State.Investigating;
        }
    }

    void SetPatrollingDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * _patrollingRadius;
        randomDirection += _player.position;

        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, _patrollingRadius, 1))
        {
            float distanceToPlayer = Vector3.Distance(hit.position, _player.position);

            if (distanceToPlayer > _minDistanceToPlayer)
                _agent.SetDestination(hit.position);
            else
                SetPatrollingDestination();
        }
    }

    IEnumerator WaitAndMove()
    {
        _isWaiting = true;
        _agent.isStopped = true;

        yield return new WaitForSeconds(_waitTime);

        _currentState = State.Patrolling;
        SetPatrollingDestination();

        _isWaiting = false;
        _agent.isStopped = false;
    }

    IEnumerator AttackRoutine()
    {
        _isAttacking = true;
        _agent.isStopped = true;
        _agent.velocity = Vector3.zero;

        float rotateTime = _attackRotateDuration;
        float timer = 0f;
        Quaternion startRot = transform.rotation;

        Vector3 dir = _player.position - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude > _idleSpeedThreshold)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            while (timer < rotateTime)
            {
                transform.rotation = Quaternion.Slerp(startRot, targetRot, timer / rotateTime);
                timer += Time.deltaTime;
                yield return null;
            }
            transform.rotation = targetRot;
        }

        _anim.SetTrigger("Attack");

        yield return new WaitForSeconds(_delayBeforeHit);

        HitPlayer();

        yield return new WaitForSeconds(_attackCooldown - _delayBeforeHit);

        _isAttacking = false;
        if (_playerHealth != null && !_playerHealth.IsDead) _agent.isStopped = false;
    }

    public void HitPlayer()
    {
        if (_playerHealth != null && !_playerHealth.IsDead)
        {
            float distance = Vector3.Distance(transform.position, _player.position);
            if (distance <= _attackDistance + _attackHitTolerance)
            {
                _playerHealth.OnApplyDamage(_damageAmount, transform);
            }
        }
    }

    bool CanSeePlayer()
    {
        Vector3 directionToPlayer = (_player.position - transform.position).normalized;
        if (Vector3.Angle(transform.forward, directionToPlayer) < _visionAngle / 2)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, _player.position);
            if (!Physics.Raycast(transform.position + Vector3.up, directionToPlayer, distanceToPlayer, _obstacleMask))
            {
                if (distanceToPlayer < _visionDistance) return true;
            }
        }
        return false;
    }

    void HandleMonsterAudio()
    {
        if (_monsterAudioSource == null) return;

        AudioClip targetBreath = (_currentState == State.Chasing) ? _chasingBreathAudioClip : _patrollingBreathAudioClip;

        if (_monsterAudioSource.clip != targetBreath)
        {
            _monsterAudioSource.clip = targetBreath;
            _monsterAudioSource.Play();
        }

        if (_currentState == State.Chasing && !_hasScreamed)
        {
            if (_screamAudioClip != null)
                _monsterAudioSource.PlayOneShot(_screamAudioClip);
            _hasScreamed = true;
        }
        else if (_currentState != State.Chasing)
        {
            _hasScreamed = false;
        }
    }

    void HandleFootsteps()
    {
        if (_footstepsAudioSource == null) return;

        float speed = _agent.velocity.magnitude;
        if (speed > _footstepThreshold && !_isWaiting)
        {
            if (!_footstepsAudioSource.isPlaying)
                _footstepsAudioSource.Play();

            _footstepsAudioSource.pitch = (_currentState == State.Chasing) ? _chasingStepSpeed : _patrollingStepSpeed;
        }
        else
        {
            if (_footstepsAudioSource.isPlaying)
                _footstepsAudioSource.Stop();
        }
    }

    void HandleMusicCrossfade()
    {
        if (_ambienceAudioSource == null) return;

        AudioClip targetClip = (_currentState == State.Chasing) ? _chasingAudioClip : _ambienceAudioClip;

        if (_ambienceAudioSource.clip != targetClip)
        {
            _ambienceAudioSource.volume = Mathf.MoveTowards(_ambienceAudioSource.volume, 0, Time.deltaTime * _musicFadeSpeed);

            if (_ambienceAudioSource.volume <= _musicTransitionThreshold)
            {
                _ambienceAudioSource.Stop();
                _ambienceAudioSource.clip = targetClip;
                _ambienceAudioSource.Play();
            }
        }
        else
        {
            _ambienceAudioSource.volume = Mathf.MoveTowards(_ambienceAudioSource.volume, _musicVolume, Time.deltaTime * _musicFadeSpeed);
        }
    }
}
