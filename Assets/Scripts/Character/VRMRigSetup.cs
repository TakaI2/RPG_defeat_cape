using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace RPGDefete.Character
{
    /// <summary>
    /// VRMモデルにAnimation Rigging用のRig構造を自動生成するヘルパー
    /// </summary>
    public class VRMRigSetup : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private Animator animator;

        [Header("自動生成されたコンポーネント")]
        [SerializeField] private RigBuilder rigBuilder;
        [SerializeField] private Rig lookAtRig;
        [SerializeField] private Rig leftHandIKRig;
        [SerializeField] private Rig rightHandIKRig;
        [SerializeField] private Rig leftFootIKRig;
        [SerializeField] private Rig rightFootIKRig;
        [SerializeField] private Rig hipIKRig;

        [Header("IK制約")]
        [SerializeField] private MultiAimConstraint headAimConstraint;
        [SerializeField] private TwoBoneIKConstraint leftHandIKConstraint;
        [SerializeField] private TwoBoneIKConstraint rightHandIKConstraint;
        [SerializeField] private TwoBoneIKConstraint leftFootIKConstraint;
        [SerializeField] private TwoBoneIKConstraint rightFootIKConstraint;
        [SerializeField] private MultiPositionConstraint hipConstraint;

        [Header("ターゲット")]
        [SerializeField] private Transform lookAtTarget;
        [SerializeField] private Transform leftHandTarget;
        [SerializeField] private Transform leftHandHint;
        [SerializeField] private Transform rightHandTarget;
        [SerializeField] private Transform rightHandHint;
        [SerializeField] private Transform leftFootTarget;
        [SerializeField] private Transform leftFootHint;
        [SerializeField] private Transform rightFootTarget;
        [SerializeField] private Transform rightFootHint;
        [SerializeField] private Transform hipTarget;

        // 公開プロパティ
        public RigBuilder RigBuilder => rigBuilder;
        public MultiAimConstraint HeadAimConstraint => headAimConstraint;
        public TwoBoneIKConstraint LeftHandIKConstraint => leftHandIKConstraint;
        public TwoBoneIKConstraint RightHandIKConstraint => rightHandIKConstraint;
        public TwoBoneIKConstraint LeftFootIKConstraint => leftFootIKConstraint;
        public TwoBoneIKConstraint RightFootIKConstraint => rightFootIKConstraint;
        public MultiPositionConstraint HipConstraint => hipConstraint;

        public Transform LookAtTarget => lookAtTarget;
        public Transform LeftHandTarget => leftHandTarget;
        public Transform LeftHandHint => leftHandHint;
        public Transform RightHandTarget => rightHandTarget;
        public Transform RightHandHint => rightHandHint;
        public Transform LeftFootTarget => leftFootTarget;
        public Transform LeftFootHint => leftFootHint;
        public Transform RightFootTarget => rightFootTarget;
        public Transform RightFootHint => rightFootHint;
        public Transform HipTarget => hipTarget;

        /// <summary>
        /// Rig構造が設定済みか
        /// </summary>
        public bool IsSetup => rigBuilder != null && headAimConstraint != null;

#if UNITY_EDITOR
        [ContextMenu("Setup Rig Structure")]
        public void SetupRigStructure()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (animator == null || !animator.isHuman)
            {
                Debug.LogError("[VRMRigSetup] Animator with Humanoid avatar is required");
                return;
            }

            // RigBuilder作成
            rigBuilder = GetComponent<RigBuilder>();
            if (rigBuilder == null)
            {
                rigBuilder = gameObject.AddComponent<RigBuilder>();
            }

            // Rig用親オブジェクト
            var rigRoot = CreateChildObject(transform, "Rig");

            // 各Rigを作成
            SetupLookAtRig(rigRoot.transform);
            SetupHandIKRig(rigRoot.transform, true);  // Left
            SetupHandIKRig(rigRoot.transform, false); // Right
            SetupFootIKRig(rigRoot.transform, true);  // Left
            SetupFootIKRig(rigRoot.transform, false); // Right
            SetupHipIKRig(rigRoot.transform);

            // RigBuilderにRigを登録
            rigBuilder.layers.Clear();
            rigBuilder.layers.Add(new RigLayer(lookAtRig, true));
            rigBuilder.layers.Add(new RigLayer(leftHandIKRig, true));
            rigBuilder.layers.Add(new RigLayer(rightHandIKRig, true));
            rigBuilder.layers.Add(new RigLayer(leftFootIKRig, true));
            rigBuilder.layers.Add(new RigLayer(rightFootIKRig, true));
            rigBuilder.layers.Add(new RigLayer(hipIKRig, true));

            // Rigのweightを1に設定（Constraintのweightで制御するため）
            lookAtRig.weight = 1f;
            leftHandIKRig.weight = 1f;
            rightHandIKRig.weight = 1f;
            leftFootIKRig.weight = 1f;
            rightFootIKRig.weight = 1f;
            hipIKRig.weight = 1f;

            // Constraintの初期weightを0に設定
            headAimConstraint.weight = 0f;
            leftHandIKConstraint.weight = 0f;
            rightHandIKConstraint.weight = 0f;
            leftFootIKConstraint.weight = 0f;
            rightFootIKConstraint.weight = 0f;
            hipConstraint.weight = 0f;

            Debug.Log("[VRMRigSetup] Rig structure setup complete");
        }

        private void SetupLookAtRig(Transform rigRoot)
        {
            var rigObj = CreateChildObject(rigRoot, "LookAtRig");
            lookAtRig = rigObj.AddComponent<Rig>();

            var constraintObj = CreateChildObject(rigObj.transform, "HeadAim");
            headAimConstraint = constraintObj.AddComponent<MultiAimConstraint>();

            // ターゲット作成
            lookAtTarget = CreateChildObject(rigObj.transform, "LookAtTarget").transform;
            lookAtTarget.localPosition = new Vector3(0, 1.5f, 2f);

            // 制約設定
            var headBone = animator.GetBoneTransform(HumanBodyBones.Head);
            headAimConstraint.data.constrainedObject = headBone;

            var sourceObjects = new WeightedTransformArray(1);
            sourceObjects.Add(new WeightedTransform(lookAtTarget, 1f));
            headAimConstraint.data.sourceObjects = sourceObjects;

            // VRMモデルの頭ボーンは通常Z軸が前方向だが、-Z（後方）を向いている場合がある
            headAimConstraint.data.aimAxis = MultiAimConstraintData.Axis.Z_NEG;
            headAimConstraint.data.upAxis = MultiAimConstraintData.Axis.Y;
        }

        private void SetupHandIKRig(Transform rigRoot, bool isLeft)
        {
            string side = isLeft ? "Left" : "Right";
            var upperArm = isLeft ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm;
            var lowerArm = isLeft ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm;
            var hand = isLeft ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand;

            var rigObj = CreateChildObject(rigRoot, $"{side}HandIKRig");
            var rig = rigObj.AddComponent<Rig>();

            var constraintObj = CreateChildObject(rigObj.transform, $"{side}HandIK");
            var constraint = constraintObj.AddComponent<TwoBoneIKConstraint>();

            // ターゲット作成
            var target = CreateChildObject(rigObj.transform, $"{side}HandTarget").transform;
            var hint = CreateChildObject(rigObj.transform, $"{side}HandHint").transform;

            // ボーン取得
            var upperArmBone = animator.GetBoneTransform(upperArm);
            var lowerArmBone = animator.GetBoneTransform(lowerArm);
            var handBone = animator.GetBoneTransform(hand);

            // 初期位置設定
            target.position = handBone.position;
            target.rotation = handBone.rotation;
            hint.position = lowerArmBone.position + (isLeft ? Vector3.left : Vector3.right) * 0.3f - Vector3.forward * 0.2f;

            // 制約設定
            constraint.data.root = upperArmBone;
            constraint.data.mid = lowerArmBone;
            constraint.data.tip = handBone;
            constraint.data.target = target;
            constraint.data.hint = hint;
            constraint.data.targetPositionWeight = 1f;
            constraint.data.targetRotationWeight = 1f;
            constraint.data.hintWeight = 1f;

            if (isLeft)
            {
                leftHandIKRig = rig;
                leftHandIKConstraint = constraint;
                leftHandTarget = target;
                leftHandHint = hint;
            }
            else
            {
                rightHandIKRig = rig;
                rightHandIKConstraint = constraint;
                rightHandTarget = target;
                rightHandHint = hint;
            }
        }

        private void SetupFootIKRig(Transform rigRoot, bool isLeft)
        {
            string side = isLeft ? "Left" : "Right";
            var upperLeg = isLeft ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg;
            var lowerLeg = isLeft ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg;
            var foot = isLeft ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot;

            var rigObj = CreateChildObject(rigRoot, $"{side}FootIKRig");
            var rig = rigObj.AddComponent<Rig>();

            var constraintObj = CreateChildObject(rigObj.transform, $"{side}FootIK");
            var constraint = constraintObj.AddComponent<TwoBoneIKConstraint>();

            // ターゲット作成
            var target = CreateChildObject(rigObj.transform, $"{side}FootTarget").transform;
            var hint = CreateChildObject(rigObj.transform, $"{side}FootHint").transform;

            // ボーン取得
            var upperLegBone = animator.GetBoneTransform(upperLeg);
            var lowerLegBone = animator.GetBoneTransform(lowerLeg);
            var footBone = animator.GetBoneTransform(foot);

            // 初期位置設定
            target.position = footBone.position;
            target.rotation = footBone.rotation;
            hint.position = lowerLegBone.position + Vector3.forward * 0.3f;

            // 制約設定
            constraint.data.root = upperLegBone;
            constraint.data.mid = lowerLegBone;
            constraint.data.tip = footBone;
            constraint.data.target = target;
            constraint.data.hint = hint;
            constraint.data.targetPositionWeight = 1f;
            constraint.data.targetRotationWeight = 1f;
            constraint.data.hintWeight = 1f;

            if (isLeft)
            {
                leftFootIKRig = rig;
                leftFootIKConstraint = constraint;
                leftFootTarget = target;
                leftFootHint = hint;
            }
            else
            {
                rightFootIKRig = rig;
                rightFootIKConstraint = constraint;
                rightFootTarget = target;
                rightFootHint = hint;
            }
        }

        private void SetupHipIKRig(Transform rigRoot)
        {
            var rigObj = CreateChildObject(rigRoot, "HipIKRig");
            hipIKRig = rigObj.AddComponent<Rig>();

            var constraintObj = CreateChildObject(rigObj.transform, "HipIK");
            hipConstraint = constraintObj.AddComponent<MultiPositionConstraint>();

            // ターゲット作成
            hipTarget = CreateChildObject(rigObj.transform, "HipTarget").transform;

            // ボーン取得
            var hipBone = animator.GetBoneTransform(HumanBodyBones.Hips);

            // 初期位置設定
            hipTarget.position = hipBone.position;

            // 制約設定
            hipConstraint.data.constrainedObject = hipBone;

            var sourceObjects = new WeightedTransformArray(1);
            sourceObjects.Add(new WeightedTransform(hipTarget, 1f));
            hipConstraint.data.sourceObjects = sourceObjects;

            // constrainedAxes で位置制約の軸を設定
            hipConstraint.data.constrainedXAxis = true;
            hipConstraint.data.constrainedYAxis = true;
            hipConstraint.data.constrainedZAxis = true;

            // オフセットを維持（初期位置からの相対移動）
            hipConstraint.data.maintainOffset = true;
        }

        private GameObject CreateChildObject(Transform parent, string name)
        {
            // 既存のオブジェクトを探す
            var existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var obj = new GameObject(name);
            obj.transform.SetParent(parent);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;
            return obj;
        }

        [ContextMenu("Remove Rig Structure")]
        public void RemoveRigStructure()
        {
            var rigRoot = transform.Find("Rig");
            if (rigRoot != null)
            {
                DestroyImmediate(rigRoot.gameObject);
            }

            if (rigBuilder != null)
            {
                DestroyImmediate(rigBuilder);
            }

            lookAtRig = null;
            leftHandIKRig = null;
            rightHandIKRig = null;
            leftFootIKRig = null;
            rightFootIKRig = null;
            hipIKRig = null;
            headAimConstraint = null;
            leftHandIKConstraint = null;
            rightHandIKConstraint = null;
            leftFootIKConstraint = null;
            rightFootIKConstraint = null;
            hipConstraint = null;

            Debug.Log("[VRMRigSetup] Rig structure removed");
        }
#endif
    }
}
