using UnityEngine;
using UnityEngine.InputSystem;

namespace RPG.Combat
{
    /// <summary>
    /// 戦闘用キャラクターコントローラー
    /// 移動とスキル発動を管理
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(CharacterController))]
    public class CombatCharacterController : MonoBehaviour
    {
        [Header("移動設定")]
        [SerializeField] private float walkSpeed = 2f;
        [SerializeField] private float runSpeed = 5f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float gravity = -9.81f;
        
        [Header("スキル設定")]
        [SerializeField] private SkillData[] attackSkills;     // 通常攻撃 (コンボ)
        [SerializeField] private SkillData skill1;             // スキル1
        [SerializeField] private SkillData skill2;             // スキル2
        [SerializeField] private SkillData ultimateSkill;      // 必殺技
        
        [Header("参照")]
        [SerializeField] private Transform cameraTransform;
        
        // コンポーネント
        private Animator animator;
        private CharacterController characterController;
        private SkillExecutor skillExecutor;
        
        // 状態
        private Vector2 moveInput;
        private bool isRunning;
        private float verticalVelocity;
        private int currentAttackIndex;
        
        // Animator パラメータ ID
        private static readonly int SpeedParam = Animator.StringToHash("Speed");
        private static readonly int IsGroundedParam = Animator.StringToHash("IsGrounded");
        private static readonly int AttackParam = Animator.StringToHash("Attack");
        private static readonly int AttackIndexParam = Animator.StringToHash("AttackIndex");
        private static readonly int Skill1Param = Animator.StringToHash("Skill1");
        private static readonly int Skill2Param = Animator.StringToHash("Skill2");
        private static readonly int UltimateParam = Animator.StringToHash("Ultimate");
        private static readonly int HitParam = Animator.StringToHash("Hit");
        private static readonly int DeathParam = Animator.StringToHash("Death");

        private void Awake()
        {
            animator = GetComponent<Animator>();
            characterController = GetComponent<CharacterController>();
            skillExecutor = GetComponent<SkillExecutor>();
            
            if (cameraTransform == null && UnityEngine.Camera.main != null)
            {
                cameraTransform = UnityEngine.Camera.main.transform;
            }
        }

        private void Update()
        {
            // Input System非対応時のフォールバック
            LegacyInput();

            HandleMovement();
            HandleGravity();
            UpdateAnimator();
        }

        private void HandleMovement()
        {
            // スキル実行中は移動不可
            if (skillExecutor != null && skillExecutor.IsExecutingSkill)
            {
                return;
            }
            
            if (moveInput.sqrMagnitude < 0.01f)
            {
                return;
            }
            
            // カメラ基準の移動方向
            Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();
            
            Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;
            
            // 移動速度
            float speed = isRunning ? runSpeed : walkSpeed;
            Vector3 velocity = moveDirection * speed;
            
            // 移動
            characterController.Move(velocity * Time.deltaTime);
            
            // 回転
            if (moveDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        private void HandleGravity()
        {
            if (characterController.isGrounded && verticalVelocity < 0)
            {
                verticalVelocity = -2f;
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
            }
            
            characterController.Move(new Vector3(0, verticalVelocity, 0) * Time.deltaTime);
        }

        private void UpdateAnimator()
        {
            float speed = moveInput.magnitude;
            if (isRunning) speed *= 2f;
            
            animator.SetFloat(SpeedParam, speed, 0.1f, Time.deltaTime);
            animator.SetBool(IsGroundedParam, characterController.isGrounded);
        }

        #region Input Handlers (Input System)
        
        public void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
        }

        public void OnRun(InputValue value)
        {
            isRunning = value.isPressed;
        }

        public void OnAttack(InputValue value)
        {
            if (!value.isPressed) return;
            PerformAttack();
        }

        /// <summary>
        /// 攻撃実行（レガシー入力とInput Systemの両方から呼び出し可能）
        /// </summary>
        private void PerformAttack()
        {
            if (skillExecutor != null && skillExecutor.IsExecutingSkill) return;

            // コンボ攻撃
            if (attackSkills != null && attackSkills.Length > 0)
            {
                SkillData skill = attackSkills[currentAttackIndex % attackSkills.Length];

                animator.SetInteger(AttackIndexParam, currentAttackIndex % 3);
                animator.SetTrigger(AttackParam);

                if (skillExecutor != null)
                {
                    skillExecutor.ExecuteSkill(skill);
                }

                currentAttackIndex++;

                // 3コンボでリセット
                if (currentAttackIndex >= 3)
                {
                    currentAttackIndex = 0;
                }
            }
            else
            {
                // attackSkillsが未設定でもアニメーションは再生
                animator.SetInteger(AttackIndexParam, currentAttackIndex % 3);
                animator.SetTrigger(AttackParam);
                currentAttackIndex = (currentAttackIndex + 1) % 3;
            }
        }

        public void OnSkill1(InputValue value)
        {
            if (!value.isPressed) return;
            ExecuteSkill(skill1, Skill1Param);
        }

        public void OnSkill2(InputValue value)
        {
            if (!value.isPressed) return;
            ExecuteSkill(skill2, Skill2Param);
        }

        public void OnUltimate(InputValue value)
        {
            if (!value.isPressed) return;
            ExecuteSkill(ultimateSkill, UltimateParam);
        }
        
        #endregion

        private void ExecuteSkill(SkillData skill, int triggerParam)
        {
            if (skill == null) return;
            if (skillExecutor != null && skillExecutor.IsExecutingSkill) return;
            
            animator.SetTrigger(triggerParam);
            
            if (skillExecutor != null)
            {
                skillExecutor.ExecuteSkill(skill);
            }
        }

        /// <summary>
        /// 被ダメージ時に呼ばれる
        /// </summary>
        public void OnHit()
        {
            animator.SetTrigger(HitParam);
            currentAttackIndex = 0; // コンボリセット
        }

        /// <summary>
        /// 死亡時に呼ばれる
        /// </summary>
        public void OnDeath()
        {
            animator.SetTrigger(DeathParam);
        }

        /// <summary>
        /// キーボード入力用（Input Systemがない場合のフォールバック）
        /// </summary>
        private void LegacyInput()
        {
            // 移動
            float h = UnityEngine.Input.GetAxis("Horizontal");
            float v = UnityEngine.Input.GetAxis("Vertical");
            moveInput = new Vector2(h, v);

            // 走る
            isRunning = UnityEngine.Input.GetKey(KeyCode.LeftShift);

            // 攻撃
            if (UnityEngine.Input.GetKeyDown(KeyCode.J) || UnityEngine.Input.GetMouseButtonDown(0))
            {
                PerformAttack();
            }

            // スキル
            if (UnityEngine.Input.GetKeyDown(KeyCode.K))
            {
                ExecuteSkill(skill1, Skill1Param);
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.L))
            {
                ExecuteSkill(skill2, Skill2Param);
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.U))
            {
                ExecuteSkill(ultimateSkill, UltimateParam);
            }
        }
    }
}
