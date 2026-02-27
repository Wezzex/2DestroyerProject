using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Actions
{
    public class ActionStack : MonoBehaviour
    {
        public interface IAction
        {
            void OnBegin(bool bFirstTime);
            void OnUpdate();
            void OnEnd();
            bool IsDone();
        }

        public abstract class Action : IAction
        {
            public virtual bool IsDone() { return true; }
            public virtual void OnBegin(bool bFirstTime) { }
            public virtual void OnEnd() { }
            public virtual void OnUpdate() { }

            public override string ToString()
            {
                return GetType().Name;
            }
        }

        public abstract class ActionBehavior : MonoBehaviour, IAction
        {
            public virtual bool IsDone() { return true; }
            public virtual void OnBegin(bool bFirstTime) { }
            public virtual void OnEnd() { }
            public virtual void OnUpdate() { }

            public override string ToString()
            {
                return GetType().Name;
            }
        }

        public abstract class ActionObject : ScriptableObject, IAction
        {
            public virtual bool IsDone() { return true; }
            public virtual void OnBegin(bool bFirstTime) { }
            public virtual void OnEnd() { }
            public virtual void OnUpdate() { }

            public override string ToString()
            {
                return GetType().Name;
            }
        }


        private List<IAction> m_actionStack = new List<IAction>();
        private HashSet<IAction> m_firstTimeActions = new HashSet<IAction>();
        private IAction m_currentAction;

        private static ActionStack sm_main;

        #region Properties

        public List<IAction> Stack => m_actionStack;

        public IAction CurrentAction => m_currentAction;

        public bool IsEmpty => m_currentAction == null && m_actionStack.Count == 0;

        public static ActionStack Main
        {
            get
            {
                if (sm_main == null && Application.isPlaying)
                {
                    GameObject go = new GameObject("MainActionStack");
                    DontDestroyOnLoad(go);
                    sm_main = go.AddComponent<ActionStack>();
                }

                return sm_main;
            }
        }

        #endregion

        public void PushAction(IAction action)
        {
            if (action != null)
            {
                // is the action already on the stack?
                m_actionStack.RemoveAll(a => a == action);

                // add to top of stack
                m_actionStack.Insert(0, action);

                // reset current action
                if (m_currentAction != null &&
                    m_currentAction != action)
                {
                    m_currentAction = null;
                }
            }
        }

        protected virtual void Update()
        {
            UpdateActions();
        }

        protected virtual void UpdateActions()
        {
            // do we have actions?
            if (IsEmpty)
            {
                return;
            }

            // new action?
            while (m_currentAction == null &&
                   m_actionStack.Count > 0)
            {
                // set the current action
                m_currentAction = m_actionStack[0];

                // call OnBegin
                bool bFirstTime = !m_firstTimeActions.Contains(m_currentAction);
                m_firstTimeActions.Add(m_currentAction);
                m_currentAction.OnBegin(bFirstTime);

                // did OnBegin push or remove another action?
                if (m_currentAction != null)
                {
                    if (m_actionStack.Count > 0 &&
                        m_currentAction != m_actionStack[0])
                    {
                        m_currentAction = null;
                        UpdateActions();
                        return;
                    }
                }
            }

            // call OnUpdate
            if (m_currentAction != null)
            {
                // update it!
                m_currentAction.OnUpdate();

                // are we still the current action?
                if (m_actionStack.Count > 0 &&
                    m_currentAction == m_actionStack[0])
                {
                    // are we done?
                    if (m_currentAction.IsDone())
                    {
                        m_actionStack.RemoveAt(0);
                        m_currentAction.OnEnd();
                        m_firstTimeActions.Remove(m_currentAction);
                        m_currentAction = null;
                    }
                }
                else
                {
                    m_currentAction = null;
                }
            }
        }

        private void OnGUI()
        {
            if (this != sm_main)
            {
                return;
            }

#if UNITY_EDITOR
            const float LINE_HEIGHT = 32.0f;

            GUI.color = new Color(0.0f, 0.0f, 0.0f, 0.7f);
            Rect r = new Rect(0, 0, 250.0f, LINE_HEIGHT * m_actionStack.Count);
            GUI.DrawTexture(r, Texture2D.whiteTexture);

            Rect line = new Rect(10, 0, r.width - 20, LINE_HEIGHT);
            for (int i = 0; i < m_actionStack.Count; i++)
            {
                GUI.color = m_actionStack[i] == m_currentAction ? Color.green : Color.white;
                GUI.Label(line, "#" + i + ": " + m_actionStack[i].ToString(), i == 0 ? UnityEditor.EditorStyles.boldLabel : UnityEditor.EditorStyles.label);
                line.y += line.height;
            }
#endif
        }
    }
}