using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

namespace RPG.Combat.Editor
{
    /// <summary>
    /// 戦闘用Animator Controllerを自動生成するエディタ拡張
    /// </summary>
    public class CombatAnimatorSetup : EditorWindow
    {
        private const string CONTROLLER_PATH = "Assets/Animation/Controllers/CombatAnimator.controller";
        
        [MenuItem("RPG/Combat/Create Combat Animator Controller")]
        public static void CreateCombatAnimatorController()
        {
            // フォルダ確認
            string dir = Path.GetDirectoryName(CONTROLLER_PATH);
            if (!AssetDatabase.IsValidFolder(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }
            
            // 既存のコントローラーがあれば確認
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER_PATH) != null)
            {
                if (!EditorUtility.DisplayDialog("確認", 
                    "既存のCombatAnimator.controllerを上書きしますか？", "上書き", "キャンセル"))
                {
                    return;
                }
            }
            
            // Animator Controller 作成
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);
            
            // パラメータ追加
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Skill1", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Skill2", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Ultimate", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("AttackIndex", AnimatorControllerParameterType.Int);
            
            // Base Layer (Locomotion)
            AnimatorControllerLayer baseLayer = controller.layers[0];
            AnimatorStateMachine baseSM = baseLayer.stateMachine;
            
            // Idle State
            AnimatorState idleState = baseSM.AddState("Idle", new Vector3(250, 0, 0));
            baseSM.defaultState = idleState;
            
            // Walk State
            AnimatorState walkState = baseSM.AddState("Walk", new Vector3(250, 60, 0));
            
            // Run State  
            AnimatorState runState = baseSM.AddState("Run", new Vector3(250, 120, 0));
            
            // Idle -> Walk
            AnimatorStateTransition idleToWalk = idleState.AddTransition(walkState);
            idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            idleToWalk.hasExitTime = false;
            idleToWalk.duration = 0.1f;
            
            // Walk -> Idle
            AnimatorStateTransition walkToIdle = walkState.AddTransition(idleState);
            walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            walkToIdle.hasExitTime = false;
            walkToIdle.duration = 0.1f;
            
            // Walk -> Run
            AnimatorStateTransition walkToRun = walkState.AddTransition(runState);
            walkToRun.AddCondition(AnimatorConditionMode.Greater, 0.5f, "Speed");
            walkToRun.hasExitTime = false;
            walkToRun.duration = 0.1f;
            
            // Run -> Walk
            AnimatorStateTransition runToWalk = runState.AddTransition(walkState);
            runToWalk.AddCondition(AnimatorConditionMode.Less, 0.5f, "Speed");
            runToWalk.hasExitTime = false;
            runToWalk.duration = 0.1f;
            
            // Combat Layer (Override)
            controller.AddLayer("Combat");
            AnimatorControllerLayer combatLayer = controller.layers[1];
            combatLayer.defaultWeight = 1f;
            // Layer設定を更新
            var layers = controller.layers;
            layers[1].defaultWeight = 1f;
            layers[1].blendingMode = AnimatorLayerBlendingMode.Override;
            controller.layers = layers;
            
            AnimatorStateMachine combatSM = combatLayer.stateMachine;
            
            // Empty State (待機)
            AnimatorState emptyState = combatSM.AddState("Empty", new Vector3(250, 0, 0));
            combatSM.defaultState = emptyState;
            
            // Attack States
            AnimatorState attack1 = combatSM.AddState("Attack1", new Vector3(500, -60, 0));
            AnimatorState attack2 = combatSM.AddState("Attack2", new Vector3(500, 0, 0));
            AnimatorState attack3 = combatSM.AddState("Attack3", new Vector3(500, 60, 0));
            
            // Skill States
            AnimatorState skill1 = combatSM.AddState("Skill1", new Vector3(500, 150, 0));
            AnimatorState skill2 = combatSM.AddState("Skill2", new Vector3(500, 210, 0));
            AnimatorState ultimate = combatSM.AddState("Ultimate", new Vector3(500, 270, 0));
            
            // Empty -> Attack1
            AnimatorStateTransition toAttack1 = emptyState.AddTransition(attack1);
            toAttack1.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            toAttack1.AddCondition(AnimatorConditionMode.Equals, 0, "AttackIndex");
            toAttack1.hasExitTime = false;
            toAttack1.duration = 0.05f;
            
            // Empty -> Attack2
            AnimatorStateTransition toAttack2 = emptyState.AddTransition(attack2);
            toAttack2.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            toAttack2.AddCondition(AnimatorConditionMode.Equals, 1, "AttackIndex");
            toAttack2.hasExitTime = false;
            toAttack2.duration = 0.05f;
            
            // Empty -> Attack3
            AnimatorStateTransition toAttack3 = emptyState.AddTransition(attack3);
            toAttack3.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            toAttack3.AddCondition(AnimatorConditionMode.Equals, 2, "AttackIndex");
            toAttack3.hasExitTime = false;
            toAttack3.duration = 0.05f;
            
            // Empty -> Skill1
            AnimatorStateTransition toSkill1 = emptyState.AddTransition(skill1);
            toSkill1.AddCondition(AnimatorConditionMode.If, 0, "Skill1");
            toSkill1.hasExitTime = false;
            toSkill1.duration = 0.1f;
            
            // Empty -> Skill2
            AnimatorStateTransition toSkill2 = emptyState.AddTransition(skill2);
            toSkill2.AddCondition(AnimatorConditionMode.If, 0, "Skill2");
            toSkill2.hasExitTime = false;
            toSkill2.duration = 0.1f;
            
            // Empty -> Ultimate
            AnimatorStateTransition toUltimate = emptyState.AddTransition(ultimate);
            toUltimate.AddCondition(AnimatorConditionMode.If, 0, "Ultimate");
            toUltimate.hasExitTime = false;
            toUltimate.duration = 0.1f;
            
            // Attack/Skill -> Empty (Exit Time)
            AnimatorState[] combatStates = { attack1, attack2, attack3, skill1, skill2, ultimate };
            foreach (var state in combatStates)
            {
                AnimatorStateTransition toEmpty = state.AddTransition(emptyState);
                toEmpty.hasExitTime = true;
                toEmpty.exitTime = 0.9f;
                toEmpty.duration = 0.1f;
            }
            
            // Reaction Layer (Additive)
            controller.AddLayer("Reaction");
            var allLayers = controller.layers;
            allLayers[2].defaultWeight = 1f;
            allLayers[2].blendingMode = AnimatorLayerBlendingMode.Additive;
            controller.layers = allLayers;
            
            AnimatorStateMachine reactionSM = controller.layers[2].stateMachine;
            
            // Empty State
            AnimatorState reactionEmpty = reactionSM.AddState("Empty", new Vector3(250, 0, 0));
            reactionSM.defaultState = reactionEmpty;
            
            // Hit State
            AnimatorState hitState = reactionSM.AddState("Hit", new Vector3(500, 0, 0));
            
            // Death State
            AnimatorState deathState = reactionSM.AddState("Death", new Vector3(500, 60, 0));
            
            // Empty -> Hit
            AnimatorStateTransition toHit = reactionEmpty.AddTransition(hitState);
            toHit.AddCondition(AnimatorConditionMode.If, 0, "Hit");
            toHit.hasExitTime = false;
            toHit.duration = 0.05f;
            
            // Hit -> Empty
            AnimatorStateTransition hitToEmpty = hitState.AddTransition(reactionEmpty);
            hitToEmpty.hasExitTime = true;
            hitToEmpty.exitTime = 0.9f;
            hitToEmpty.duration = 0.1f;
            
            // Empty -> Death
            AnimatorStateTransition toDeath = reactionEmpty.AddTransition(deathState);
            toDeath.AddCondition(AnimatorConditionMode.If, 0, "Death");
            toDeath.hasExitTime = false;
            toDeath.duration = 0.1f;
            
            // 保存
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"Combat Animator Controller created at: {CONTROLLER_PATH}");
            
            // 作成したコントローラーを選択
            Selection.activeObject = controller;
            EditorGUIUtility.PingObject(controller);
        }
        
        [MenuItem("RPG/Combat/Setup Megu Character")]
        public static void SetupMeguCharacter()
        {
            // megu.vrm を検索
            string[] guids = AssetDatabase.FindAssets("megu t:GameObject", new[] { "Assets/Characters" });
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("エラー", "megu.vrm が見つかりません", "OK");
                return;
            }
            
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            GameObject meguPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            // Animator Controller を取得
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER_PATH);
            if (controller == null)
            {
                EditorUtility.DisplayDialog("エラー", 
                    "CombatAnimator.controller が見つかりません。\n先に 'RPG/Combat/Create Combat Animator Controller' を実行してください", "OK");
                return;
            }
            
            // シーンにインスタンスを作成
            GameObject meguInstance = (GameObject)PrefabUtility.InstantiatePrefab(meguPrefab);
            meguInstance.name = "Megu_Player";
            meguInstance.transform.position = Vector3.zero;
            
            // Animator 設定
            Animator animator = meguInstance.GetComponent<Animator>();
            if (animator == null)
            {
                animator = meguInstance.AddComponent<Animator>();
            }
            animator.runtimeAnimatorController = controller;
            
            // CharacterController 追加 (CombatCharacterControllerに必要)
            CharacterController charController = meguInstance.GetComponent<CharacterController>();
            if (charController == null)
            {
                charController = meguInstance.AddComponent<CharacterController>();
                // VRMキャラクター用のデフォルト設定
                charController.center = new Vector3(0, 0.9f, 0);
                charController.height = 1.8f;
                charController.radius = 0.3f;
            }

            // SkillExecutor 追加
            if (meguInstance.GetComponent<SkillExecutor>() == null)
            {
                meguInstance.AddComponent<SkillExecutor>();
            }

            // CombatCharacterController 追加 (入力処理)
            if (meguInstance.GetComponent<CombatCharacterController>() == null)
            {
                meguInstance.AddComponent<CombatCharacterController>();
            }

            Selection.activeGameObject = meguInstance;
            Debug.Log("Megu character setup complete! Components added: Animator, CharacterController, SkillExecutor, CombatCharacterController");
        }
    }
}
