using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FSO.Common.Rendering.Framework.Model;

namespace FSO.Client.UI.Framework
{
    /// <summary>
    /// Utility for performing animations on UIElements
    /// </summary>
    public class UITween : IUIProcess
    {
        private Action m_CompleteAction;
        private List<UITweenInstance> m_ActiveTweens = new List<UITweenInstance>();
        private List<List<UITweenInstanceMembers>> m_TweenQueue = new List<List<UITweenInstanceMembers>>();
        private List<Action> CompleteActionsQueue = new List<Action>();

        public UITweenInstance To(object obj, float duration, Dictionary<string, float> args)
        {
            return To(obj, duration, args, TweenLinear.EaseNone);
        }

        public UITweenInstance To(object obj, float duration, Dictionary<string, float> args, EaseFunction ease)
        {
            var inst = new UITweenInstance(this, obj, duration, args, ease);
            lock (m_ActiveTweens)
            {
                m_ActiveTweens.Add(inst);
            }
            inst.Start();
            return inst;
        }

        public Action CompleteAction
        {
            set
            {
                if (m_CompleteAction == null)
                    m_CompleteAction = value;
                else
                    CompleteActionsQueue.Add(value);
            }
            get
            {
                if (m_CompleteAction == null)
                {
                    if (CompleteActionsQueue.Count > 0)
                        return CompleteActionsQueue[0];
                }
                return m_CompleteAction;
            }
        }

        public void OverrideCompleteAction(Action newCompleteAction)
        {
            CompleteActionsQueue = new List<Action>();
            m_CompleteAction = newCompleteAction;
        }

        public void StopQueue(bool complete, bool completeAction)
        {
            StopAll(complete, completeAction);
        }

        public void StopAll(bool complete, bool completeAction)
        {
            if (m_ActiveTweens.Count > 0)
            {
                var instances = new List<UITweenInstance>(m_ActiveTweens);
                foreach (var inst in instances)
                    Stop(inst, complete);
            }
            if (!completeAction)
            {
                m_CompleteAction = null;
                CompleteActionsQueue = new List<Action>();
            }
            else
                CompleteActionHandler();
        }

        public void Stop(UITweenInstance inst, bool complete)
        {
            if (complete)
                inst.Complete();
            else if (inst.Active)
                inst.Stop();
            else
            {
                lock (m_ActiveTweens)
                {
                    m_ActiveTweens.Remove(inst);
                    if (m_ActiveTweens.Count == 0)
                    {
                        CompleteActionHandler();
                    }
                }
            }
        }

        private void CompleteActionHandler()
        {
            m_CompleteAction?.Invoke();
            if (CompleteActionsQueue.Count > 0)
            {
                m_CompleteAction = CompleteActionsQueue[0];
                CompleteActionsQueue.Remove(m_CompleteAction);
            }
            else
                m_CompleteAction = null;
        }

        public bool HasQueue
        {
            get { return m_TweenQueue.Count > 0 || m_ActiveTweens.Count > 0; }
        }

        public void PlayQueue()
        {
            if (m_TweenQueue.Count > 0)
            {
                AddQueueCompleteHandler();
                ProcessMembersIntoInstances(m_TweenQueue[0]);
                m_TweenQueue.Remove(m_TweenQueue[0]);
                PlayAll();
            }
            else if (m_ActiveTweens.Count == 0)
            {
                CompleteActionHandler();
            }
        }

        public void PlayAll()
        {
            lock (m_ActiveTweens)
            {
                if (m_ActiveTweens.Count > 0)
                {
                    foreach (var inst in m_ActiveTweens)
                        inst.Start();
                }
            }
        }

        public void ProcessMembersIntoInstances(List<UITweenInstanceMembers> memberQueue)
        {
            var collection = new List<UITweenInstanceMembers>(memberQueue);
            lock (m_ActiveTweens)
            {
                foreach (var tween in collection)
                {
                    var inst = new UITweenInstance(tween.Owner, tween.TargetObject, tween.Duration, tween.Arguments, tween.Ease).OnCompleteAction(tween.CompleteAction).OnUpdateAction(tween.UpdateAction);
                    m_ActiveTweens.Add(inst);
                }
            }
        }

        public UITween Queue(UITweenQueueTypes type, params UITweenInstanceMembers[] instances)
        {
            return Queue(type, null, instances);
        }

        public UITween Queue(UITweenQueueTypes type, Action finalAction, params UITweenInstanceMembers[] instances)
        {
            UITween lastTween = this;
            bool appended = false;
            for (var count = 0; count < instances.Length; count++)
            {
                if (type.Equals(UITweenQueueTypes.AppendedSynchronous) && !appended)
                {
                    appended = true;
                    lastTween = EnQueue(instances[count], UITweenQueueTypes.Sequential, finalAction);
                }
                else
                    lastTween = EnQueue(instances[count], type, finalAction);
            }
            return lastTween;
        }

        public UITween Queue(UITweenQueueTypes type, Action finalAction, object obj, float duration, Dictionary<string, float> args)
        {
            var inst = new UITweenInstanceMembers(this, obj, duration, args, TweenLinear.EaseNone);
            return EnQueue(inst, type, finalAction);
        }

        public UITween Queue(UITweenQueueTypes type, Action finalAction, object obj, float duration, Dictionary<string, float> args, Action completeAction)
        {
            var inst = new UITweenInstanceMembers(this, obj, duration, args, TweenLinear.EaseNone).OnCompleteAction(completeAction);
            return EnQueue(inst, type, finalAction);
        }

        public UITween Queue(UITweenQueueTypes type, Action finalAction, object obj, float duration, Dictionary<string, float> args, Action completeAction, Action updateAction)
        {
            var inst = new UITweenInstanceMembers(this, obj, duration, args, TweenLinear.EaseNone).OnCompleteAction(completeAction).OnUpdateAction(updateAction);
            return EnQueue(inst, type, finalAction);
        }

        public UITween Queue(UITweenQueueTypes type, Action finalAction, object obj, float duration, Dictionary<string, float> args, EaseFunction ease)
        {
            var inst = new UITweenInstanceMembers(this, obj, duration, args, ease);
            return EnQueue(inst, type, finalAction);
        }

        public UITween Queue(UITweenQueueTypes type, Action finalAction, object obj, float duration, Dictionary<string, float> args, EaseFunction ease, Action completeAction)
        {
            var inst = new UITweenInstanceMembers(this, obj, duration, args, ease).OnCompleteAction(completeAction);
            return EnQueue(inst, type, finalAction);
        }

        public UITween Queue(UITweenQueueTypes type, Action finalAction, object obj, float duration, Dictionary<string, float> args, EaseFunction ease, Action completeAction, Action updateAction)
        {
            var inst = new UITweenInstanceMembers(this, obj, duration, args, ease).OnCompleteAction(completeAction).OnUpdateAction(updateAction);
            return EnQueue(inst, type, finalAction);
        }

        private UITween EnQueue(UITweenInstanceMembers inst, UITweenQueueTypes type, Action completeAction)
        {
            List<UITweenInstanceMembers> lastQueue = null;
            if (type.Equals(UITweenQueueTypes.Sequential))
            {
                lastQueue = AddQueueCompleteHandler();
                m_TweenQueue.Add(lastQueue);
            }
            else // synchronous
            {
                if (m_TweenQueue.Count > 0)
                    lastQueue = m_TweenQueue[m_TweenQueue.Count - 1];
                else
                {
                    lastQueue = new List<UITweenInstanceMembers>();
                    m_TweenQueue.Add(lastQueue);
                }
            }
            lock (lastQueue)
            {
                lastQueue.Add(inst);
            }
            if (completeAction != null)
                CompleteAction = completeAction;
            return this;
        }
        private List<UITweenInstanceMembers> AddQueueCompleteHandler()
        {
            // if there's a previous UITween in the queue, get an instance from it and make its complete action be PlayQueue
            var count = m_TweenQueue.Count;
            if (count > 0)
            {
                var queue = m_TweenQueue[count - 1];
                count = queue.Count;
                if (count > 0)
                {
                    var finalMember = queue[count - 1];
                    var action = finalMember.CompleteAction;
                    if (action != null)
                    {
                        finalMember.CompleteAction = () =>
                        {
                            action();
                            PlayQueue();
                        };
                    }
                    else
                        finalMember.CompleteAction = PlayQueue;
                }
            }
            return new List<UITweenInstanceMembers>();
        }

        #region IUIProcess Members

        public void Update(UpdateState state)
        {

            var done = new List<UITweenInstance>();

            lock (m_ActiveTweens)
            {
                var now = state.Time.TotalGameTime.Ticks;

                var copy = m_ActiveTweens.ToList();
                foreach (var tween in copy)
                {
                    tween.Update(now, state);
                    if (!tween.Active)
                    {
                        /** Done **/
                        done.Add(tween);
                    }
                }
            }

            foreach (var tween in done)
            {
                tween.Complete();
            }
        }

        #endregion
    }

    public class UITweenInstanceMembers
    {
        public UITweenQueueTypes Type;
        public Action FinalAction;
        public UITween Owner;
        public object TargetObject;
        public float Duration;
        public Dictionary<string, float> Arguments;
        public EaseFunction Ease = TweenLinear.EaseNone;
        public Action UpdateAction;
        public Action CompleteAction;

        public UITweenInstanceMembers(UITween owner, object obj, float duration, Dictionary<string, float> args, EaseFunction ease)
        {
            Owner = owner;
            TargetObject = obj;
            Duration = duration;
            Arguments = args;
            Ease = ease;
        }
        public UITweenInstanceMembers OnUpdateAction(Action action)
        {
            UpdateAction = action;
            return this;
        }
        public UITweenInstanceMembers OnCompleteAction(Action action)
        {
            CompleteAction = action;
            return this;
        }
    }

    public delegate void TweenEvent(UITweenInstance tween, float progress);

    public class UITweenInstance
    {
        private List<UITweenInstanceField> m_Fields;
        private Action m_CompleteAction;
        private Action m_UpdateAction;
        private object m_Object;
        private float m_Duration;
        private float m_StartTime;
        private bool m_Active;
        private EaseFunction m_EaseFunction;
        private UITween m_Owner;
        private float m_LastProgress;


        public event TweenEvent OnComplete;
        public event TweenEvent OnUpdate;


        public UITweenInstance(UITween owner, object obj, float duration, Dictionary<string, float> args, EaseFunction ease)
        {
            this.m_Owner = owner;
            this.m_EaseFunction = ease;
            this.m_Object = obj;
            /** Convert secs into ticks **/
            this.m_Duration = new TimeSpan(0, 0, 0, 0, (int)(duration * 1000.0f)).Ticks;

            var clazz = obj.GetType();

            m_Fields = new List<UITweenInstanceField>();
            foreach (var key in args.Keys)
            {
                var prop = clazz.GetMember(key)[0];
                if(prop is FieldInfo){
                    m_Fields.Add(new UITweenInstanceField_ForField(prop as FieldInfo, obj)
                    {
                        End = (float)args[key]
                    });
                }
                else if (prop is PropertyInfo)
                {
                    m_Fields.Add(new UITweenInstanceField_ForProperty(prop as PropertyInfo, obj)
                    {
                        End = (float)args[key]
                    });
                }
            }
        }
        public UITweenInstance OnUpdateAction(Action action)
        {
            m_UpdateAction = action;
            return this;
        }
        public UITweenInstance OnCompleteAction(Action action)
        {
            m_CompleteAction = action;
            return this;
        }

        public void Start()
        {
            m_StartTime = GameFacade.GameRunTime.Ticks;
            m_Active = true;
        }

        public void Complete()
        {
            if (LastProgress != 1.0f)
            {
                RenderPercent(1.0f);
                Stop();
            }
            else
                m_Owner.Stop(this, false);
        }


        public void Update(long ticks, UpdateState state)
        {
            if (!m_Active) { return; }

            var time = ticks - m_StartTime;
            var progress = (time) / m_Duration;
            var visProgress = progress;
            if (progress >= 1)
            {
                progress = 1;
                visProgress = 1;
            }
            else if (progress < 0)
            {
                progress = 0;
                visProgress = 0;
            }
            else
            {
                visProgress = m_EaseFunction(time, 0, 1, m_Duration);
            }
            m_LastProgress = progress;
            RenderPercent(visProgress);

            if (progress >= 1.0f)
            {
                Stop();
            }
        }

        public void Stop()
        {
            m_Active = false;
            m_Owner.Stop(this, false);
        }

        public void RenderPercent(float progress)
        {
            for (int index = 0; index < m_Fields.Count; index++)
            {
                var field = m_Fields[index];
                field.SetValue(field.Start + ((field.End - field.Start) * progress), m_Object);
            }
            OnUpdate?.Invoke(this, progress);
            m_UpdateAction?.Invoke();
            if (progress == 1.0f)
            {
                OnComplete?.Invoke(this, 1.0f);
                m_CompleteAction?.Invoke();
            }
        }

        public bool Active
        {
            get
            {
                return m_Active;
            }
        }

        public float LastProgress
        {
            get
            {
                return m_LastProgress;
            }
        }
    }


    public abstract class UITweenInstanceField
    {
        public float Start;
        public float End;

        public abstract void SetValue(object value, object inst);
    }


    public class UITweenInstanceField_ForProperty : UITweenInstanceField
    {
        public PropertyInfo Property;

        public UITweenInstanceField_ForProperty(PropertyInfo property, object inst)
        {
            this.Property = property;
            Start = (float)GetValue(inst);
        }


        public object GetValue(object inst)
        {
            return Property.GetValue(inst, null);
        }

        public override void SetValue(object value, object inst)
        {
            Property.SetValue(inst, value, null);
        }
    }

    public class UITweenInstanceField_ForField : UITweenInstanceField
    {
        public FieldInfo Field;

        public UITweenInstanceField_ForField(FieldInfo field, object inst)
        {
            this.Field = field;
            Start = (float)GetValue(inst);
        }


        public object GetValue(object inst)
        {
            return Field.GetValue(inst);
        }

        public override void SetValue(object value, object inst)
        {
            Field.SetValue(inst, value);
        }
    }





    public delegate float EaseFunction(float time, float min, float max, float duration);


    #region Easing Functions

    public class TweenLinear
    {
        public static EaseFunction EaseNone = new EaseFunction(_EaseNone);

        private static float _EaseNone(float t, float b, float c, float d){
            return c*t/d + b;
        }
    }

    public class TweenQuad
    {
        public static EaseFunction EaseIn = new EaseFunction(_EaseIn);
        public static EaseFunction EaseOut = new EaseFunction(_EaseOut);
        public static EaseFunction EaseInOut = new EaseFunction(_EaseInOut);

        private static float _EaseIn(float t, float b, float c, float d)
        {
            return c * (t /= d) * t + b;
        }

        private static float _EaseOut(float t, float b, float c, float d)
        {
            return -c * (t /= d) * (t - 2) + b;
        }

        private static float _EaseInOut(float t, float b, float c, float d)
        {
            if ((t /= d * 0.5f) < 1) return c * 0.5f * t * t + b;
            return -c * 0.5f * ((--t) * (t - 2) - 1) + b;
        }
    }

    public class TweenElastic
    {
        public static EaseFunction EaseIn = new EaseFunction(_EaseIn);
        public static EaseFunction EaseOut = new EaseFunction(_EaseOut);
        //public static EaseFunction EaseInOut = new EaseFunction(_EaseInOut);

        private static float _2PI = (float)Math.PI * 2.0f;

        private static float _EaseIn(float t, float b, float c, float d)
        {
            if(t == 0){
                return b;
            }
            if ((t /= d) == 1)
            {
                return b + c;
            }
            var p = d * 0.3f;
            var a = c;
            var s = p / 4.0f;

            return -(a * (float)Math.Pow(2.0f, 10.0f * (t -= 1)) * (float)Math.Sin((t * d - s) * _2PI / p)) + b;
        }

        private static float _EaseOut(float t, float b, float c, float d)
        {
            t /= d;
            var ts = t * t;
            var tc = ts * t;
            return b + c * (56 * tc * ts + -175 * ts * ts + 200 * tc + -100 * ts + 20 * t);

            /*
            if (t == 0)
            {
                return b;
            }
            if ((t /= d) == 1)
            {
                return b + c;
            }
            var p = d * 0.3f;
            var a = c;
            var s = p / 4.0f;

            return (a * (float)Math.Pow(2.0f, -10.0f * t) * (float)Math.Sin((t * d - s) * _2PI / p) + c + b);
            */
        }

        //private static float _EaseInOut(float t, float b, float c, float d)
        //{
        //    if ((t /= d * 0.5f) < 1) return c * 0.5f * t * t + b;
        //    return -c * 0.5f * ((--t) * (t - 2) - 1) + b;
        //}
    }



    #endregion

    public enum UITweenQueueTypes
    {
        Synchronous = 0,
        Sequential = 1,
        AppendedSynchronous = 2
    }
}
